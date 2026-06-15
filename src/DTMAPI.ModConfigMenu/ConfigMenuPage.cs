using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.ModConfigMenu
{
    internal sealed class ConfigMenuPage : IConfigMenuPage, IConfigMenuPendingPreview
    {
        private readonly ConfigMenuRegistry registry;
        private readonly Action reset;
        private readonly Action save;
        private int nextItemNumber;
        private Func<string> displayName = null!;

        public ConfigMenuPage(ConfigMenuRegistry registry, IManifest manifest, Action reset, Action save, bool titleScreenOnly)
        {
            this.registry = registry;
            Manifest = manifest;
            this.reset = reset;
            this.save = save;
            TitleScreenOnly = titleScreenOnly;
            displayName = () => Manifest.Name;
        }

        public IManifest Manifest { get; }
        public string DisplayName => SafeInvoke(displayName);
        public bool TitleScreenOnly { get; }
        public bool IsEditing { get; private set; }
        public bool HasPendingChanges => ItemsInternal.Any(i => i.HasPendingChange);
        public bool IsLocked { get; private set; }
        public string LockReason { get; private set; } = string.Empty;
        public List<ConfigMenuItemBase> ItemsInternal { get; } = new List<ConfigMenuItemBase>();
        public IReadOnlyList<IConfigMenuItem> Items => ItemsInternal.Where(i => i.IsVisible).Cast<IConfigMenuItem>().ToArray();

        public string NextItemId(string kind)
        {
            nextItemNumber++;
            return Manifest.UniqueID + ":" + kind + ":" + nextItemNumber.ToString(CultureInfo.InvariantCulture);
        }

        public void SetDisplayName(Func<string> name)
        {
            displayName = name ?? (() => Manifest.Name);
        }

        public void AddItem(ConfigMenuItemBase item)
        {
            item.CaptureCommittedValue();
            ItemsInternal.Add(item);
        }

        public void SetLocked(bool locked, string reason)
        {
            IsLocked = locked;
            LockReason = locked ? reason ?? string.Empty : string.Empty;
        }

        public void BeginEditing()
        {
            foreach (ConfigMenuItemBase item in ItemsInternal)
                item.CaptureCommittedValue();
            IsEditing = true;
        }

        public IDisposable PreviewPendingValues()
        {
            if (!TryReadCurrentValues("preview", out string[] previousValues))
                return new PendingPreviewScope(this, Array.Empty<string>());

            var scope = new PendingPreviewScope(this, previousValues);
            for (int i = 0; i < ItemsInternal.Count; i++)
            {
                try
                {
                    ItemsInternal[i].ApplyRawValue(ItemsInternal[i].PendingValue);
                }
                catch (Exception ex)
                {
                    ItemsInternal[i].SetValidationError("Preview failed: " + ConfigMenuCallbackRunner.Describe(ex));
                    scope.Dispose();
                    return new PendingPreviewScope(this, Array.Empty<string>());
                }
            }
            return scope;
        }

        public void Reset()
        {
            ThrowIfLocked();
            if (!IsEditing)
                BeginEditing();
            string[] previousValues = ReadCurrentValues("reset");
            string[] previousPendingValues = CapturePendingValues();
            try
            {
                ConfigMenuCallbackRunner.Run(Manifest.UniqueID + ".reset", reset);
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
            ThrowIfLocked();
            if (registry.HasKeybindConflict(this))
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
                ConfigMenuCallbackRunner.Run(Manifest.UniqueID + ".save", save);
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

        private void ThrowIfLocked()
        {
            if (IsLocked)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(LockReason) ? "此 Mod 当前由启用来源锁定，不能在 DTMAPI 中修改。" : LockReason);
        }

        private static string SafeInvoke(Func<string> func)
        {
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
            private readonly ConfigMenuPage page;
            private readonly string[] previousValues;
            private bool disposed;

            public PendingPreviewScope(ConfigMenuPage page, string[] previousValues)
            {
                this.page = page;
                this.previousValues = previousValues;
            }

            public void Dispose()
            {
                if (disposed)
                    return;
                disposed = true;
                int count = Math.Min(page.ItemsInternal.Count, previousValues.Length);
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        page.ItemsInternal[i].ApplyRawValue(previousValues[i]);
                    }
                    catch (Exception ex)
                    {
                        page.ItemsInternal[i].SetValidationError("Preview restore failed: " + ConfigMenuCallbackRunner.Describe(ex));
                    }
                }
            }
        }
    }
}
