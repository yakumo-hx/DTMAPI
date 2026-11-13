using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using Native = DTMAPI.DebugConsole.DebugConsoleNativeAccess;

namespace DTMAPI.DebugConsole
{
    // 只为 Y 补来源名称。物品及其数值仍取游戏当前表，不另建内容表。
    internal static class DebugConsoleContentSources
    {
        private static readonly InventoryDebugSourceGroup UnattributedMod = new InventoryDebugSourceGroup
        {
            Id = "__mods", SourceKind = "Mod", IsModSource = true, EnablementKnown = false
        };

        internal static InventoryDebugSourceGroup? Resolve(
            IReadOnlyDictionary<string, InventoryDebugSourceGroup>? sources,
            string id) => sources == null ? null : sources.TryGetValue(id, out InventoryDebugSourceGroup? source) ? source : UnattributedMod;

        internal static IReadOnlyDictionary<string, InventoryDebugSourceGroup>? Read(IDtmHelper helper, string table)
        {
            var sources = new Dictionary<string, InventoryDebugSourceGroup>(StringComparer.Ordinal);
            try
            {
                var vanilla = new InventoryDebugSourceGroup { Id = "__base", SourceKind = "Vanilla", Enabled = true, EnablementKnown = true };
                object json = Native.Read(Parse(ReadBase(table)), "AsArray")
                    ?? throw new InvalidDataException("Configs/" + table + ".json is not an array.");
                foreach (object row in Native.Enumerate(Native.Read(json, "Children")))
                    sources[Id(row)] = vanilla;
            }
            catch (Exception error)
            {
                helper.Monitor.LogOnce("debugconsole-content-sources-unavailable", "Y 无法读取本体来源 Configs/" + table + ".json，只显示全部：" + error.GetBaseException().Message, LogLevel.Warn);
                return null;
            }

            try
            {
                object manager = Native.Read(Native.Resolve("DolocAPI, Assembly-CSharp"), "modManager")!;
                var configs = (IDictionary)Native.Read(manager, "CachedConfigs")!;
                if (!configs.Contains(table)) return sources;
                MethodInfo validate = Native.Method(manager.GetType(), "TryValidateMergedConfig", 3, true)!;
                var known = helper.Workshop.GetDtmApiMods();
                // 顺序和文件列表都来自原生缓存，_ignore 不会进入这里。
                foreach (object mod in Native.Enumerate(configs[table]))
                {
                    string root = Native.Text(mod, "rootPath");
                    IWorkshopModInfo? manifest = known.FirstOrDefault(m =>
                        Path.GetFullPath(m.RootPath).Equals(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase));
                    var source = new InventoryDebugSourceGroup
                    {
                        Id = Native.First(manifest?.UniqueID ?? Native.Text(mod, "id"), "__mods"),
                        DisplayName = manifest?.Name ?? Native.Text(mod, "title"),
                        SourceKind = "Mod", IsModSource = true,
                        Enabled = true, EnablementKnown = true, WorkshopId = manifest?.WorkshopId
                    };
                    var files = (IDictionary)Native.Read(mod, "configs")!;
                    foreach (string path in Native.Enumerate(files[table]))
                    {
                        try
                        {
                            object array = Native.Read(Parse(File.ReadAllText(path)), "AsArray")!;
                            object?[] args = { table, array, null };
                            // 使用原生校验，不把坏文件或原生拒绝的行当作覆盖来源。
                            if (!(validate.Invoke(null, args) is bool accepted) || !accepted) continue;
                            foreach (object row in Native.Enumerate(Native.Read(array, "Children")))
                                AddModSource(sources, table, Id(row), source);
                        }
                        catch (Exception error)
                        {
                            helper.Monitor.Log("Y 来源文件读取失败：" + path + "；" + error.GetBaseException().Message, LogLevel.Warn);
                        }
                    }
                }
            }
            catch (Exception error)
            {
                helper.Monitor.Log("Y 无法读取模组来源名称：" + error.GetBaseException().Message, LogLevel.Warn);
            }
            return sources;
        }

        private static void AddModSource(Dictionary<string, InventoryDebugSourceGroup> sources,
            string table, string id, InventoryDebugSourceGroup source)
        {
            if (sources.TryGetValue(id, out InventoryDebugSourceGroup? existing))
            {
                // 本体中已有的 ID 始终算本体，即使某个包改过它的数值。
                if (!existing.IsModSource) return;
                if (table == "animal_tbanimal" && !existing.Id.Equals(source.Id, StringComparison.OrdinalIgnoreCase))
                    source = UnattributedMod;
            }
            sources[id] = source;
        }

        private static object? Parse(string text) => Native.Method(Native.Resolve("SimpleJSON.JSON, Assembly-CSharp"), "Parse", 1, true)!
            .Invoke(null, new object[] { text });

        private static string Id(object row)
        {
            object? value = row.GetType().GetProperty("Item", new[] { typeof(string) })!.GetValue(row, new object[] { "id" });
            return Native.Text(value, "Value");
        }

        private static string ReadBase(string table)
        {
            Type addressables = Native.Resolve("UnityEngine.AddressableAssets.Addressables, Unity.Addressables")!;
            Type textAsset = Native.Resolve("UnityEngine.TextAsset, UnityEngine.CoreModule")!;
            MethodInfo load = addressables.GetMethods().Single(m => m.Name == "LoadAssetAsync" && m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(object));
            object handle = load.MakeGenericMethod(textAsset).Invoke(null, new object[] { "Configs/" + table + ".json" })
                ?? throw new InvalidOperationException("Addressables did not return a handle for Configs/" + table + ".json.");
            try
            {
                object? asset = handle.GetType().GetMethod("WaitForCompletion", Type.EmptyTypes)!.Invoke(handle, Array.Empty<object>());
                return Native.Text(asset, "text");
            }
            finally
            {
                MethodInfo release = addressables.GetMethods().Single(m => m.Name == "Release" && m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsGenericType &&
                    m.GetParameters()[0].ParameterType.GetGenericTypeDefinition().Name == "AsyncOperationHandle`1");
                release.MakeGenericMethod(textAsset).Invoke(null, new[] { handle });
            }
        }
    }
}
