using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.ModConfigMenu
{
    internal sealed class ConfigMenuPage : IConfigMenuPage, IConfigMenuPendingPreview
    {
        private ConfigMenuRegistry? registry;
        private Action? reset;
        private Action? save;
        private readonly List<WeakReference<PendingPreviewScope>> previewScopes = new List<WeakReference<PendingPreviewScope>>();
        private readonly IManifest inactiveManifest;
        private IManifest manifest;
        private int nextItemNumber;
        private Func<string>? displayName;
        private bool isActive = true;

        public ConfigMenuPage(ConfigMenuRegistry registry, IManifest manifest, Action reset, Action save, bool titleScreenOnly, IManifest? inactiveManifest = null)
        {
            this.registry = registry;
            this.manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            this.inactiveManifest = inactiveManifest ?? ConfigMenuManifestSnapshot.Capture(manifest);
            this.reset = reset;
            this.save = save;
            TitleScreenOnly = titleScreenOnly;
            displayName = () => Manifest.Name;
        }

        public IManifest Manifest => manifest;
        public string DisplayName => isActive ? SafeInvoke(displayName) : string.Empty;
        public bool TitleScreenOnly { get; }
        public bool IsEditing { get; private set; }
        public bool HasPendingChanges => isActive && ItemsInternal.Any(i => i.HasPendingChange);
        public bool IsLocked { get; private set; }
        public string LockReason { get; private set; } = string.Empty;
        public List<ConfigMenuItemBase> ItemsInternal { get; } = new List<ConfigMenuItemBase>();
        public IReadOnlyList<IConfigMenuItem> Items => isActive ? ItemsInternal.Where(i => i.IsVisible).Cast<IConfigMenuItem>().ToArray() : Array.Empty<IConfigMenuItem>();
        internal bool IsActive => isActive;

        public string NextItemId(string kind)
        {
            ThrowIfInactive();
            nextItemNumber++;
            return Manifest.UniqueID + ":" + kind + ":" + nextItemNumber.ToString(CultureInfo.InvariantCulture);
        }

        public void SetDisplayName(Func<string> name)
        {
            ThrowIfInactive();
            displayName = name ?? (() => Manifest.Name);
        }

        public void AddItem(ConfigMenuItemBase item)
        {
            ThrowIfInactive();
            item.CaptureCommittedValue();
            if (!isActive)
            {
                item.Deactivate();
                ThrowIfInactive();
            }
            ItemsInternal.Add(item);
        }

        public void SetLocked(bool locked, string reason)
        {
            if (!isActive)
                return;
            IsLocked = locked;
            LockReason = locked ? reason ?? string.Empty : string.Empty;
        }

        public void BeginEditing()
        {
            ThrowIfInactive();
            foreach (ConfigMenuItemBase item in ItemsInternal)
                item.CaptureCommittedValue();
            IsEditing = true;
        }

        public IDisposable PreviewPendingValues()
        {
            if (!isActive)
                return PendingPreviewScope.CreateInactive();
            if (!TryReadCurrentValues("preview", out string[] previousValues))
                return PendingPreviewScope.CreateInactive();

            var changedIndices = new List<int>();
            for (int i = 0; i < ItemsInternal.Count; i++)
            {
                if (string.Equals(previousValues[i], ItemsInternal[i].PendingValue, StringComparison.Ordinal))
                    continue;
                changedIndices.Add(i);
            }

            if (changedIndices.Count == 0)
                return PendingPreviewScope.CreateInactive();

            int[] changed = changedIndices.ToArray();
            PendingPreviewScope scope = CreatePreviewScope(previousValues, changed);
            int appliedCount = 0;
            for (int changeIndex = 0; changeIndex < changed.Length; changeIndex++)
            {
                int i = changed[changeIndex];
                if (!isActive || i < 0 || i >= ItemsInternal.Count)
                    return PendingPreviewScope.CreateInactive();
                ConfigMenuItemBase item = ItemsInternal[i];
                try
                {
                    item.ApplyRawValue(item.PendingValue);
                    if (!isActive)
                        return PendingPreviewScope.CreateInactive();
                    appliedCount++;
                }
                catch (Exception ex)
                {
                    if (!isActive)
                        return PendingPreviewScope.CreateInactive();
                    item.SetValidationError("Preview failed: " + ConfigMenuCallbackRunner.Describe(ex));
                    RecordPreviewAudit("apply-pending", false, "changed=" + changed.Length.ToString(CultureInfo.InvariantCulture) + "; applied=" + appliedCount.ToString(CultureInfo.InvariantCulture) + "; failedItem=" + item.ItemId + "; " + ConfigMenuCallbackRunner.Describe(ex));
                    scope.Dispose();
                    return PendingPreviewScope.CreateInactive();
                }
            }
            RecordPreviewAudit("apply-pending", true, "changed=" + changed.Length.ToString(CultureInfo.InvariantCulture));
            return scope;
        }

        public void Reset()
        {
            ThrowIfInactive();
            ThrowIfLocked();
            if (!IsEditing)
                BeginEditing();
            string[] previousValues = ReadCurrentValues("reset");
            string[] previousPendingValues = CapturePendingValues();
            try
            {
                ConfigMenuCallbackRunner.Run(Manifest.UniqueID + ".reset", reset!);
                foreach (ConfigMenuItemBase item in ItemsInternal)
                {
                    try
                    {
                        item.CapturePendingFromGetter();
                    }
                    catch (Exception ex)
                    {
                        item.SetValidationError("Reset read failed: " + ConfigMenuCallbackRunner.Describe(ex));
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                TryRollbackOrThrow(previousValues, "reset", ex);
                RestorePendingValues(previousPendingValues);
                throw new InvalidOperationException("Reset failed and config values were rolled back: " + ConfigMenuCallbackRunner.Describe(ex), ex);
            }
        }

        public void Save()
        {
            ThrowIfInactive();
            ThrowIfLocked();
            if (registry!.HasKeybindConflict(this))
                throw new InvalidOperationException("存在按键冲突，无法保存配置。");

            string[] previousValues = ReadCurrentValues("save");
            try
            {
                foreach (ConfigMenuItemBase item in ItemsInternal)
                {
                    try
                    {
                        item.ApplyPendingValue();
                    }
                    catch (Exception ex)
                    {
                        item.SetValidationError("Apply failed: " + ConfigMenuCallbackRunner.Describe(ex));
                        throw;
                    }
                }
                ConfigMenuCallbackRunner.Run(Manifest.UniqueID + ".save", save!);
                if (!isActive)
                    return;
            }
            catch (Exception ex)
            {
                TryRollbackOrThrow(previousValues, "save", ex);
                throw new InvalidOperationException("Save failed and config values were rolled back: " + ConfigMenuCallbackRunner.Describe(ex), ex);
            }

            foreach (ConfigMenuItemBase item in ItemsInternal)
                item.CaptureCommittedValue();
            IsEditing = true;
        }

        public void Cancel()
        {
            ThrowIfInactive();
            var errors = new List<string>();
            foreach (ConfigMenuItemBase item in ItemsInternal)
            {
                try
                {
                    item.RestoreCommittedValue();
                }
                catch (Exception ex)
                {
                    string error = "Cancel restore failed: " + ConfigMenuCallbackRunner.Describe(ex);
                    item.SetValidationError(error);
                    errors.Add(item.Name + ": " + error);
                }
            }
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("; ", errors));
            IsEditing = false;
        }

        internal void Deactivate()
        {
            if (!isActive)
                return;

            // Mark inactive first so neither a stale preview scope nor a re-entrant
            // UI call can invoke owner callbacks while roots are being detached.
            isActive = false;
            IsEditing = false;
            IsLocked = false;
            LockReason = string.Empty;
            reset = null;
            save = null;
            displayName = null;
            manifest = inactiveManifest;

            foreach (WeakReference<PendingPreviewScope> reference in previewScopes)
            {
                if (reference.TryGetTarget(out PendingPreviewScope scope))
                    scope.Deactivate();
            }
            previewScopes.Clear();

            foreach (ConfigMenuItemBase item in ItemsInternal)
                item.Deactivate();
            ItemsInternal.Clear();

            // A stale page reference must not retain the registry and every other
            // owner's page through it.
            registry = null;
        }

        private PendingPreviewScope CreatePreviewScope(string[] previousValues, int[] changedIndices)
        {
            for (int i = previewScopes.Count - 1; i >= 0; i--)
            {
                if (!previewScopes[i].TryGetTarget(out PendingPreviewScope existing) || existing.IsDisposed)
                    previewScopes.RemoveAt(i);
            }

            var scope = new PendingPreviewScope(this, previousValues, changedIndices);
            previewScopes.Add(new WeakReference<PendingPreviewScope>(scope));
            return scope;
        }

        private void RecordPreviewAudit(string operation, bool success, string details)
        {
            if (isActive)
                registry?.RecordPreviewAudit(Manifest, "scope", "ChangedItems", operation, success, details);
        }

        private void ThrowIfInactive()
        {
            if (!isActive)
                throw new InvalidOperationException("This config menu page is inactive because its owner was deactivated.");
        }

        private void ThrowIfLocked()
        {
            if (IsLocked)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(LockReason) ? "此 Mod 当前由启用来源锁定，不能在 DTMAPI 中修改。" : LockReason);
        }

        private static string SafeInvoke(Func<string>? func)
        {
            if (func == null)
                return string.Empty;
            try
            {
                return func() ?? string.Empty;
            }
            catch (Exception ex)
            {
                return "<error: " + ex.GetType().Name + ">";
            }
        }

        private string[] ReadCurrentValues(string operation)
        {
            if (TryReadCurrentValues(operation, out string[] values))
                return values;
            throw new InvalidOperationException("Failed to read current config values for " + operation + ".");
        }

        private bool TryReadCurrentValues(string operation, out string[] values)
        {
            var result = new string[ItemsInternal.Count];
            for (int i = 0; i < ItemsInternal.Count; i++)
            {
                try
                {
                    result[i] = ItemsInternal[i].ReadCurrentValueForPreview();
                }
                catch (Exception ex)
                {
                    ItemsInternal[i].SetValidationError(operation + " read failed: " + ConfigMenuCallbackRunner.Describe(ex));
                    values = Array.Empty<string>();
                    return false;
                }
            }
            values = result;
            return true;
        }

        private void RestoreRawValues(string[] values, string operation)
        {
            var errors = new List<string>();
            int count = Math.Min(ItemsInternal.Count, values.Length);
            for (int i = 0; i < count; i++)
            {
                try
                {
                    ItemsInternal[i].ApplyRawValue(values[i]);
                }
                catch (Exception ex)
                {
                    string error = operation + " failed: " + ConfigMenuCallbackRunner.Describe(ex);
                    ItemsInternal[i].SetValidationError(error);
                    errors.Add(ItemsInternal[i].Name + ": " + error);
                }
            }

            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("; ", errors));
        }

        private string[] CapturePendingValues()
        {
            return ItemsInternal.Select(item => item.PendingValue).ToArray();
        }

        private void RestorePendingValues(string[] values)
        {
            int count = Math.Min(ItemsInternal.Count, values.Length);
            for (int i = 0; i < count; i++)
                ItemsInternal[i].RestorePendingValue(values[i]);
        }

        private void TryRollbackOrThrow(string[] values, string operation, Exception original)
        {
            try
            {
                RestoreRawValues(values, operation + " rollback");
            }
            catch (Exception rollback)
            {
                throw new InvalidOperationException(
                    operation + " failed and rollback also failed: " +
                    ConfigMenuCallbackRunner.Describe(original) + "; rollback: " +
                    ConfigMenuCallbackRunner.Describe(rollback),
                    new AggregateException(original, rollback));
            }
        }

        private sealed class PendingPreviewScope : IDisposable
        {
            private ConfigMenuPage? page;
            private string[] previousValues;
            private int[] changedIndices;
            private bool disposed;

            public PendingPreviewScope(ConfigMenuPage page, string[] previousValues, int[] changedIndices)
            {
                this.page = page;
                this.previousValues = previousValues;
                this.changedIndices = changedIndices;
            }

            private PendingPreviewScope()
            {
                previousValues = Array.Empty<string>();
                changedIndices = Array.Empty<int>();
                disposed = true;
            }

            public bool IsDisposed => disposed;

            public static PendingPreviewScope CreateInactive() => new PendingPreviewScope();

            public void Deactivate()
            {
                disposed = true;
                page = null;
                previousValues = Array.Empty<string>();
                changedIndices = Array.Empty<int>();
            }

            public void Dispose()
            {
                if (disposed)
                    return;
                disposed = true;

                ConfigMenuPage? currentPage = page;
                string[] values = previousValues;
                int[] indices = changedIndices;
                page = null;
                previousValues = Array.Empty<string>();
                changedIndices = Array.Empty<int>();

                if (currentPage == null || !currentPage.IsActive)
                    return;

                int restored = 0;
                string firstFailure = string.Empty;
                for (int changeIndex = 0; changeIndex < indices.Length; changeIndex++)
                {
                    int i = indices[changeIndex];
                    if (i < 0 || i >= currentPage.ItemsInternal.Count || i >= values.Length)
                        continue;
                    try
                    {
                        currentPage.ItemsInternal[i].ApplyRawValue(values[i]);
                        restored++;
                    }
                    catch (Exception ex)
                    {
                        currentPage.ItemsInternal[i].SetValidationError("Preview restore failed: " + ConfigMenuCallbackRunner.Describe(ex));
                        if (string.IsNullOrWhiteSpace(firstFailure))
                            firstFailure = "failedItem=" + currentPage.ItemsInternal[i].ItemId + "; " + ConfigMenuCallbackRunner.Describe(ex);
                    }
                }
                if (indices.Length > 0)
                    currentPage.RecordPreviewAudit("restore", string.IsNullOrWhiteSpace(firstFailure), "changed=" + indices.Length.ToString(CultureInfo.InvariantCulture) + "; restored=" + restored.ToString(CultureInfo.InvariantCulture) + (string.IsNullOrWhiteSpace(firstFailure) ? string.Empty : "; " + firstFailure));
            }
        }
    }

    /// <summary>
    /// Captures manifest data at registration so a stale UI page never keeps a
    /// Mod-provided manifest implementation or its dependency collections alive.
    /// </summary>
    internal sealed class ConfigMenuManifestSnapshot : IManifest
    {
        private ConfigMenuManifestSnapshot(IManifest source)
        {
            Name = source.Name ?? string.Empty;
            Author = source.Author ?? string.Empty;
            Version = source.Version ?? string.Empty;
            Description = source.Description ?? string.Empty;
            UniqueID = source.UniqueID ?? string.Empty;
            EntryDll = source.EntryDll ?? string.Empty;
            EntryType = source.EntryType ?? string.Empty;
            MinimumDTMApiVersion = source.MinimumDTMApiVersion ?? string.Empty;
            MinimumGameVersion = source.MinimumGameVersion ?? string.Empty;
            Type = source.Type ?? string.Empty;
            Dependencies = (source.Dependencies ?? Array.Empty<IManifestDependency>())
                .Where(dependency => dependency != null)
                .Select(dependency => (IManifestDependency)new ConfigMenuManifestDependencySnapshot(dependency))
                .ToArray();
            UpdateKeys = (source.UpdateKeys ?? Array.Empty<string>())
                .Select(key => key ?? string.Empty)
                .ToArray();
        }

        public string Name { get; }
        public string Author { get; }
        public string Version { get; }
        public string Description { get; }
        public string UniqueID { get; }
        public string EntryDll { get; }
        public string EntryType { get; }
        public string MinimumDTMApiVersion { get; }
        public string MinimumGameVersion { get; }
        public string Type { get; }
        public IReadOnlyList<IManifestDependency> Dependencies { get; }
        public IReadOnlyList<string> UpdateKeys { get; }

        public static IManifest Capture(IManifest source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return source is ConfigMenuManifestSnapshot ? source : new ConfigMenuManifestSnapshot(source);
        }
    }

    internal sealed class ConfigMenuManifestDependencySnapshot : IManifestDependency
    {
        public ConfigMenuManifestDependencySnapshot(IManifestDependency source)
        {
            UniqueID = source.UniqueID ?? string.Empty;
            MinimumVersion = source.MinimumVersion ?? string.Empty;
            Required = source.Required;
        }

        public string UniqueID { get; }
        public string MinimumVersion { get; }
        public bool Required { get; }
    }
}
