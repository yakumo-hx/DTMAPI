using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Services;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Core.Runtime
{
    public sealed partial class DtmApiRuntime
    {
        // Startup and official-page commits use the same package initialization.
        private sealed class ModPackageLoader
        {
            private readonly DtmApiRuntime runtime;
            internal ModPackageLoader(DtmApiRuntime runtime) { this.runtime = runtime; }

            internal bool Load(DiscoveredMod mod, DependencyAssemblyLoader dependencies, bool late)
            {
                string id = mod.Manifest.UniqueID;
                if (!mod.OfficialEnabled || runtime.IsLoadedMod(id) || runtime.modOwnerLifecycle.TryGetState(id, out _)) return false;
                if (runtime.IsReservedRuntimeOwnerId(id))
                {
                    runtime.Diagnostics.RecordError(id, "UniqueID 被 DTMAPI 运行时保留。", id);
                    return false;
                }
                if (!CanLoadApiVersion(mod) || !CanLoadGameVersion(mod)) return false;
                if (dependencies.Plan.Blocked.TryGetValue(id, out string? blockedReason))
                {
                    if (late && GetRestartReason(mod).Length != 0) return false;
                    runtime.Diagnostics.RecordError(id, blockedReason.StartsWith("required-dependency-cycle:", StringComparison.Ordinal)
                        ? "依赖循环阻止加载。" : blockedReason.Contains("version outside range") ? "必需依赖版本过低。"
                        : blockedReason.Contains("(missing)") ? "缺少必需依赖。" : "依赖或包文件阻止加载。", blockedReason);
                    if (!blockedReason.StartsWith("required-dependency", StringComparison.Ordinal))
                        runtime.modOwnerLifecycle.RequireColdStart(id);
                    return false;
                }
                if (late && GetRestartReason(mod).Length != 0) return false;
                if (!CanLoadDependencies(mod)) return false;
                runtime.Diagnostics.ClearErrors(id);
                bool loaded = mod.Classification.IsContentPack ? LoadContentPack(mod) : LoadCodeMod(mod, dependencies);
                if (!loaded) return false;
                runtime.Content.SetOwners(runtime.loadedModsSnapshot);
                runtime.CoordinateRuntimeModuleContent(mod);
                if (late) runtime.Events.DispatchGameLaunched(id);
                return true;
            }

            private bool LoadContentPack(DiscoveredMod mod)
            {
                string id = mod.Manifest.UniqueID;
                try
                {
                    runtime.modOwnerLifecycle.BeginEntry(id);
                    if (mod.Manifest.DependencyContract != null)
                        runtime.ownerDependencies.SetRequiredProviders(id, mod.Manifest.DependencyContract);
                    if (mod.Manifest.ContentPackBinding != null)
                        runtime.contentHosts.Activate(mod.Manifest, mod.Manifest.ContentPackBinding, mod.RootPath,
                            () => runtime.EnsureModOwnerRegistrationAllowed(id), runtime.ModRegistry,
                            new TranslationService(mod.RootPath, runtime.languageSource,
                                message => runtime.RuntimeMonitor.Log("ContentPack " + id + ": " + message, LogLevel.Warn)));
                    runtime.ModRegistry.AddLoaded(mod.Manifest);
                    runtime.loadedMods.Add(mod);
                    runtime.modOwnerLifecycle.Activate(id);
                    runtime.PublishLoadedModsSnapshot();
                }
                catch (Exception ex)
                {
                    runtime.DeactivateOwner(id, ModOwnerCleanupReason.EntryFailed, shutdown: false);
                    runtime.modOwnerLifecycle.RequireColdStart(id);
                    runtime.Diagnostics.RecordError(id, "内容包加载失败：" + ex.GetBaseException().Message, ex.ToString());
                    return false;
                }
                runtime.RuntimeMonitor.Log("已加载内容包 " + id + "。");
                return true;
            }

            internal string GetRestartReason(DiscoveredMod mod) => GetRestartReason(mod, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            private string GetRestartReason(DiscoveredMod mod, HashSet<string> visiting)
            {
                string id = mod.Manifest.UniqueID;
                if (runtime.IsLoadedMod(id) || runtime.modOwnerLifecycle.TryGetState(id, out _) || !visiting.Add(id)) return "";
                if (!IsVersionRequirementSatisfied(mod.Manifest.MinimumDTMApiVersion, ApiVersion, out _)) return "";
                if (runtime.activeDependencyLoader?.Plan.Blocked.TryGetValue(id, out string? blocked) == true &&
                    (!blocked.StartsWith("required-dependency", StringComparison.Ordinal) || blocked.Contains("(missing)") ||
                        blocked.Contains("required-dependency-cycle:"))) return "";
                if (mod.Manifest.RequiresRestart) return "重启后生效：作者要求在游戏启动时加载。";
                foreach (var dependency in Dependencies(mod).Where(dependency => dependency.Required))
                {
                    if (runtime.ModRegistry.TryGetVersion(dependency.Id, out string actual) && PackageRangeSatisfied(dependency, actual)) continue;
                    if (!runtime.discoveredById.TryGetValue(dependency.Id, out var provider) || !provider.OfficialEnabled) continue;
                    if (runtime.IsLoadedMod(dependency.Id) && PackageRangeSatisfied(dependency, provider.Manifest.Version))
                        return "重启后生效：必需依赖 " + provider.Manifest.Name + " 的新来源或版本尚未加载。";
                    string reason = GetRestartReason(provider, visiting);
                    if (reason.Length != 0) return "重启后生效：必需依赖 " + provider.Manifest.Name + " 需要重启。";
                }
                return "";
            }

            private static IEnumerable<PackageModDependency> Dependencies(DiscoveredMod mod) =>
                mod.Manifest.DependencyContract?.Dependencies ?? mod.Manifest.DependencyModels.Select(dependency =>
                    new PackageModDependency(dependency.UniqueID, dependency.Required,
                        new PackageVersionRange(string.IsNullOrEmpty(dependency.MinimumVersion) ? "0.0.0" : dependency.MinimumVersion, null, true))
                    { LegacyOrderingOnly = string.IsNullOrEmpty(dependency.MinimumVersion) }).ToArray();

            private bool LoadCodeMod(DiscoveredMod mod, DependencyAssemblyLoader dependencyLoader)
            {
                IMonitor? monitor = null;
                try
                {
                    if (!TryResolveEntryDllPath(mod, out string dllPath))
                    {
                        runtime.modOwnerLifecycle.RequireColdStart(mod.Manifest.UniqueID);
                        return false;
                    }
                    runtime.modOwnerLifecycle.BeginEntry(mod.Manifest.UniqueID);
                    runtime.RuntimeMonitor.Log(
                        "Code mod load-source owner=" + mod.Manifest.UniqueID +
                        "; identity=" + mod.Classification.IdentityName +
                        "; expectedHarmonyOwner=" + (mod.Classification.ExpectedHarmonyOwner.Length == 0 ? "none" : mod.Classification.ExpectedHarmonyOwner) +
                        "; source=" + mod.Source +
                        "; workshopId=" + (mod.WorkshopId.HasValue ? mod.WorkshopId.Value.ToString(CultureInfo.InvariantCulture) : "none") +
                        "; root=" + Path.GetFullPath(mod.RootPath) +
                        "; dll=" + dllPath + ".");
                    if (mod.Manifest.DependencyContract != null)
                    {
                        runtime.ownerDependencies.SetRequiredProviders(mod.Manifest.UniqueID, mod.Manifest.DependencyContract);
                    }
                    Assembly assembly = dependencyLoader.LoadEntry(mod.Manifest.UniqueID, dllPath);
                    runtime.MarkManagedAssemblyLoadAttempt(mod.Manifest.UniqueID);
                    if (!TryResolveEntryType(mod, assembly, out Type? entryType))
                        throw new InvalidOperationException("No valid DtmMod entry type could be resolved for " + mod.Manifest.UniqueID + ".");

                    var modInstance = (DtmMod)Activator.CreateInstance(entryType);
                    runtime.ownerInstances.Capture(mod.Manifest.UniqueID, modInstance);
                    monitor = new FileMonitor(runtime.host, mod.Manifest.UniqueID, runtime.Diagnostics.LatestLogPath, runtime.Diagnostics.RecordError);
                    modInstance.AttachContext(mod.Manifest, monitor);
                    Action ensureRuntimeThread = () => runtime.EnsureModHelperRuntimeThread(mod.Manifest.UniqueID);
                    Action ensureOwnerActive = () => runtime.EnsureModOwnerRegistrationAllowed(mod.Manifest.UniqueID);
                    var translation = new TranslationService(mod.RootPath, runtime.languageSource, message => monitor.Log(message, LogLevel.Warn));
                    var helper = new DtmHelper(
                        mod.Manifest,
                        monitor,
                        runtime.Events.CreateOwnerBoundProxy(mod.Manifest.UniqueID, ensureOwnerActive),
                        runtime.Config.CreateOwnerBound(mod.Manifest, ensureOwnerActive),
                        runtime.ModRegistry.CreateOwnerBoundRegistry(mod.Manifest, ensureOwnerActive),
                        runtime.Workshop,
                        runtime.UI.CreateOwnerBound(mod.Manifest.UniqueID, ensureRuntimeThread, ensureOwnerActive),
                        runtime.Diagnostics.CreateOwnerBound(mod.Manifest.UniqueID, ensureRuntimeThread, ensureOwnerActive),
                        runtime.Content,
                        runtime.Input.CreateOwnerBound(mod.Manifest.UniqueID, ensureOwnerActive),
                        translation,
                        runtime.ownerServices.Create(mod.Manifest.UniqueID, ensureOwnerActive, ensureRuntimeThread,
                            mod.RootPath, runtime.Paths.GlobalDataPath, runtime.Config, runtime.Input, translation, () =>
                            {
                                try { runtime.modOwnerLifecycle.EnsureRegistrationAllowed(mod.Manifest.UniqueID); }
                                catch (InvalidOperationException ex) { throw new ObjectDisposedException(mod.Manifest.UniqueID, ex.Message); }
                            }));
                    modInstance.Entry(helper);

                    runtime.ModRegistry.AddLoaded(mod.Manifest);
                    runtime.ownerInstances.Publish(mod.Manifest.UniqueID);
                    runtime.loadedMods.Add(mod);
                    runtime.PublishLoadedModsSnapshot();
                    runtime.modOwnerLifecycle.Activate(mod.Manifest.UniqueID);
                }
                catch (Exception ex)
                {
                    string cleanupSummary = runtime.DeactivateOwner(mod.Manifest.UniqueID, ModOwnerCleanupReason.EntryFailed, shutdown: false);
                    runtime.modOwnerLifecycle.RequireColdStart(mod.Manifest.UniqueID);
                    bool assemblyLoaded = runtime.ownerInstances.HasAssemblyLoadAttempt(mod.Manifest.UniqueID);
                    bool restartRequired = runtime.modOwnerLifecycle.RequiresRestart(mod.Manifest.UniqueID);
                    runtime.Diagnostics.RecordError(
                        mod.Manifest.UniqueID,
                        "代码 Mod 加载失败：" + ex.GetBaseException().Message,
                        ex + Environment.NewLine +
                        cleanupSummary + Environment.NewLine +
                        "AssemblyLoaded=" + assemblyLoaded.ToString(CultureInfo.InvariantCulture) +
                        "; RestartRequired=" + restartRequired.ToString(CultureInfo.InvariantCulture) +
                        "; unknown Harmony/static/native/Unity side effects are possible only after managed code was loaded or entered.");
                    runtime.RuntimeMonitor.LogException(ex, $"Failed to load {mod.Manifest.UniqueID}.");
                    runtime.RuntimeMonitor.Log("Failed code mod cleanup owner=" + mod.Manifest.UniqueID + " " + cleanupSummary + ".", LogLevel.Warn);
                    return false;
                }

                monitor!.Log("Mod Entry completed.");
                return true;
            }

            private bool CanLoadDependencies(DiscoveredMod mod)
            {
                if (mod.Manifest.DependencyContract != null)
                {
                    foreach (PackageModDependency dependency in mod.Manifest.DependencyContract.Dependencies)
                    {
                        bool present = runtime.ModRegistry.TryGetVersion(dependency.Id, out string providerVersion);
                        bool available = present && PackageRangeSatisfied(dependency, providerVersion);
                        if (available) continue;
                        if (!dependency.Required)
                        {
                            runtime.Diagnostics.RecordWarning(mod.Manifest.UniqueID,
                                present ? "可选依赖版本过低，Mod 保持活动。" : "可选依赖不可用，Mod 保持活动。",
                                "owner=" + mod.Manifest.UniqueID + "; dependency=" + dependency.Id + "; actual=" + providerVersion);
                            continue;
                        }
                        runtime.Diagnostics.RecordError(mod.Manifest.UniqueID, "必需依赖未成功启动或版本不满足。", mod.Manifest.UniqueID + " -> " + dependency.Id);
                        return false;
                    }
                    return true;
                }
                foreach (IManifestDependency dependency in ((IManifest)mod.Manifest).Dependencies)
                {
                    if (string.IsNullOrWhiteSpace(dependency.UniqueID))
                    {
                        runtime.Diagnostics.RecordError(mod.Manifest.UniqueID, "依赖声明缺少 UniqueID。", "Dependency UniqueID is empty.");
                        runtime.RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：依赖声明缺少 UniqueID。", LogLevel.Warn);
                        return false;
                    }

                    if (!runtime.ModRegistry.TryGetVersion(dependency.UniqueID, out string dependencyVersion))
                    {
                        if (!dependency.Required)
                            continue;
                        runtime.Diagnostics.RecordError(mod.Manifest.UniqueID, "缺少必需依赖。", dependency.UniqueID);
                        runtime.RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：缺少依赖 " + dependency.UniqueID + "。", LogLevel.Warn);
                        return false;
                    }

                    if (IsVersionRequirementSatisfied(dependency.MinimumVersion, dependencyVersion, out string dependencyVersionReason))
                        continue;

                    string message = dependency.Required ? "必需依赖版本过低。" : "可选依赖版本过低。";
                    string details = dependency.UniqueID + " requires >= " + dependency.MinimumVersion + ", loaded " + dependencyVersion + ". " + dependencyVersionReason;
                    if (dependency.Required)
                        runtime.Diagnostics.RecordError(mod.Manifest.UniqueID, message, details);
                    else
                        runtime.Diagnostics.RecordWarning(mod.Manifest.UniqueID, message, details);
                    runtime.RuntimeMonitor.Log(
                        (dependency.Required ? "跳过 " + mod.Manifest.UniqueID + "：" : "诊断 " + mod.Manifest.UniqueID + "：") +
                        dependency.UniqueID + " 版本不满足要求 " + dependency.MinimumVersion + "，当前 " + dependencyVersion + "。",
                        LogLevel.Warn);
                    if (dependency.Required)
                        return false;
                }
                return true;
            }

            private bool CanLoadApiVersion(DiscoveredMod mod)
            {
                string minimum = mod.Manifest.MinimumDTMApiVersion;
                if (string.IsNullOrWhiteSpace(minimum))
                    return true;

                if (IsVersionRequirementSatisfied(minimum, ApiVersion, out string reason))
                    return true;

                runtime.Diagnostics.RecordError(
                    mod.Manifest.UniqueID,
                    "DTMAPI 前置版本过旧。",
                    "This mod requires a newer DTMAPI runtime. MinimumDTMApiVersion=" + minimum + ", installed runtime=" + ApiVersion + ". Run 1_install_dtmapi.bat to update DTMAPI. " + reason);
                runtime.RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：DTMAPI 前置版本过旧；该 Mod 需要 DTMAPI >= " + minimum + "，当前 " + ApiVersion + "。请运行 1_install_dtmapi.bat 更新 DTMAPI。", LogLevel.Warn);
                return false;
            }

            private bool CanLoadGameVersion(DiscoveredMod mod)
            {
                string minimum = mod.Manifest.MinimumGameVersion;
                if (string.IsNullOrWhiteSpace(minimum))
                    return true;

                runtime.Diagnostics.RecordWarning(
                    mod.Manifest.UniqueID,
                    "MinimumGameVersion cannot be verified.",
                    "MinimumGameVersion=" + minimum + "; the current runtime host cannot detect the Doloc Town game version, so DTMAPI continues loading and records this as a structured warning.");
                runtime.RuntimeMonitor.Log(
                    "MinimumGameVersion warning for " + mod.Manifest.UniqueID + ": manifest requires Doloc Town >= " + minimum + ", but the current runtime host cannot detect the game version; DTMAPI will continue loading and treat this as an explicit warning instead of silently ignoring it.",
                    LogLevel.Warn);
                return true;
            }

            private bool TryResolveEntryDllPath(DiscoveredMod mod, out string dllPath)
            {
                dllPath = string.Empty;
                string entryDll = mod.Manifest.EntryDll.Trim();
                if (string.IsNullOrWhiteSpace(entryDll))
                {
                    runtime.Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 缺失。", mod.RootPath);
                    runtime.RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 缺失。", LogLevel.Warn);
                    return false;
                }

                if (Path.IsPathRooted(entryDll))
                {
                    runtime.Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 必须是相对路径。", entryDll);
                    runtime.RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 必须是相对路径，不能使用 " + entryDll + "。", LogLevel.Warn);
                    return false;
                }

                string root = Path.GetFullPath(mod.RootPath);
                string resolved = Path.GetFullPath(Path.Combine(root, entryDll));
                string rootWithSeparator = EnsureTrailingDirectorySeparator(root);
                if (!resolved.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
                {
                    runtime.Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 不能逃出 Mod 根目录。", "EntryDll=" + entryDll + ", resolved=" + resolved + ", root=" + root);
                    runtime.RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 不能逃出 Mod 根目录。", LogLevel.Warn);
                    return false;
                }

                if (!File.Exists(resolved))
                {
                    runtime.Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 缺失。", resolved);
                    runtime.RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 缺失，路径 " + resolved + "。", LogLevel.Warn);
                    return false;
                }

                dllPath = resolved;
                return true;
            }

            private bool TryResolveEntryType(DiscoveredMod mod, Assembly assembly, out Type? entryType)
            {
                entryType = null;
                string requestedEntryType = mod.Manifest.EntryType.Trim();
                if (requestedEntryType.Length > 0)
                {
                    Type? requested = assembly.GetType(requestedEntryType, throwOnError: false);
                    if (requested != null && typeof(DtmMod).IsAssignableFrom(requested) && !requested.IsAbstract && requested.GetConstructor(Type.EmptyTypes) != null)
                    { entryType = requested; return true; }
                }
                Type[] availableTypes;
                try { availableTypes = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex)
                {
                    // An unused integration must not hide a legacy inferred entry.
                    availableTypes = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
                }
                Type[] entryTypes = availableTypes
                    .Where(t => typeof(DtmMod).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                    .OrderBy(t => t.FullName, StringComparer.Ordinal)
                    .ToArray();

                if (!string.IsNullOrWhiteSpace(requestedEntryType))
                {
                    Type[] matches = entryTypes
                        .Where(t =>
                            string.Equals(t.FullName, requestedEntryType, StringComparison.Ordinal) ||
                            string.Equals(t.Name, requestedEntryType, StringComparison.Ordinal))
                        .ToArray();
                    if (matches.Length == 1)
                    {
                        entryType = matches[0];
                        return true;
                    }

                    string details = "EntryType=" + requestedEntryType + "; candidates=" + string.Join(", ", entryTypes.Select(t => t.FullName ?? t.Name).ToArray());
                    runtime.Diagnostics.RecordError(
                        mod.Manifest.UniqueID,
                        matches.Length == 0 ? "EntryType 未找到。" : "EntryType 匹配多个 DtmMod 类型。",
                        details);
                    runtime.RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryType " + requestedEntryType + (matches.Length == 0 ? " 未在 DLL 中找到。" : " 匹配多个 DtmMod 类型。"), LogLevel.Warn);
                    return false;
                }

                if (entryTypes.Length == 0)
                {
                    runtime.Diagnostics.RecordError(mod.Manifest.UniqueID, "未找到 DtmMod 入口类型。", "No concrete DtmMod entry type with a parameterless constructor was found.");
                    runtime.RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：DLL 内没有可构造的 DtmMod 入口类型。", LogLevel.Warn);
                    return false;
                }

                if (entryTypes.Length > 1)
                {
                    string details = "EntryType is required when a DLL contains multiple DtmMod subclasses: " + string.Join(", ", entryTypes.Select(t => t.FullName ?? t.Name).ToArray());
                    runtime.Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryType 缺失且 DLL 内包含多个 DtmMod 子类。", details);
                    runtime.RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryType 缺失且 DLL 内包含多个 DtmMod 子类，请在 manifest 中填写 EntryType。", LogLevel.Warn);
                    return false;
                }

                entryType = entryTypes[0];
                return true;
            }
        }
    }
}

