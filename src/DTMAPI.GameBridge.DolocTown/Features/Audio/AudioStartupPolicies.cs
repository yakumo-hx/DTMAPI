using System;
using System.Collections.Generic;
using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    // Startup configuration and saved identity do not depend on media components.
    internal sealed class AudioStartupPolicies : IDisposable
    {
        private static JsonAccess? json;
        private static bool mergingBgm;
        private static bool retainCdIds;
        private static bool appendBgmActive;
        internal AudioStartupPolicies(DtmApiRuntime runtime, bool appendBgm)
        {
            var patcher = new HarmonyPatcher(runtime, "dtmapi.gamebridge.audio-startup");
            var bgm = new HarmonyPatcher(runtime, "dtmapi.gamebridge.bgm-append");
            MethodInfo Callback(string name) => typeof(AudioStartupPolicies).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;
            try
            {
                var cd = patcher.ResolveType("DolocTown.CDManager, Assembly-CSharp") ?? throw new MissingMemberException("CDManager");
                var mode = patcher.ResolveType("DolocTown.Config.Sound.BgmPlayMode, Assembly-CSharp") ?? throw new MissingMemberException("BgmPlayMode");
                if (cd.GetField("unlockedIds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.FieldType != typeof(HashSet<string>)) throw new MissingFieldException("CDManager.unlockedIds");
                if (!patcher.TryPatchConstructorPostfix(cd.AssemblyQualifiedName!, Callback(nameof(RetainCdIds)),
                    new HarmonyTargetSignature(parameterTypes: new[] { typeof(bool), typeof(HashSet<string>), typeof(List<string>), typeof(HashSet<string>), mode, typeof(string) })))
                    throw new InvalidOperationException("CD identity protection could not be installed.");
                retainCdIds = true;
                if (appendBgm)
                {
                    try
                    {
                    var node = patcher.ResolveType("SimpleJSON.JSONNode, Assembly-CSharp-firstpass") ?? throw new MissingMemberException("JSONNode");
                    var array = patcher.ResolveType("SimpleJSON.JSONArray, Assembly-CSharp-firstpass") ?? throw new MissingMemberException("JSONArray");
                    json = new JsonAccess(node, array);
                    const string manager = "DolocTown.Config.ModManager, Assembly-CSharp";
                    var load = new HarmonyTargetSignature(parameterTypes: new[] { typeof(string), node });
                    var merge = new HarmonyTargetSignature(parameterTypes: new[] { array, array, typeof(bool), typeof(string) });
                    if (!bgm.TryPatchPrefix(manager, "LoadWithMods", Callback(nameof(BeginBgm)), load) ||
                        !bgm.TryPatchFinalizer(manager, "LoadWithMods", Callback(nameof(EndBgm)), load) ||
                        !bgm.TryPatchPrefix(manager, "MergeArray", Callback(nameof(MergeBgm)), merge))
                        throw new InvalidOperationException("BGM startup append policy could not be installed.");
                    appendBgmActive = true;
                    }
                    catch (Exception ex)
                    {
                        appendBgmActive = false;
                        mergingBgm = false;
                        json = null;
                        runtime.RuntimeMonitor.Log("BGM append unavailable; CD identity protection retained: " + ex.GetBaseException().Message, DTMAPI.Abstractions.LogLevel.Warn);
                    }
                }
                runtime.RuntimeMonitor.Log("Audio startup policies active; CD identity protection=true; BGM append=" + appendBgmActive + ".");
            }
            catch { Dispose(); throw; }
        }
        private static void RetainCdIds(HashSet<string>? __1, HashSet<string> ___unlockedIds)
        { if (retainCdIds && __1 != null) ___unlockedIds.UnionWith(__1); }
        private static void BeginBgm(string __0, out bool __state) { __state = mergingBgm; mergingBgm = appendBgmActive && __0 == "sound_tbbgmgroup"; }
        private static Exception? EndBgm(Exception? __exception, bool __state) { mergingBgm = appendBgmActive && __state; return __exception; }
        private static void MergeBgm(object[] __args)
        {
            if (!appendBgmActive || !mergingBgm || !(__args[2] is bool ignore) || ignore || !Equals(__args[3], "id")) return;
            var access = json!;
            object baseArray = __args[0], modArray = access.Clone(__args[1]);
            var lists = new Dictionary<string, object>(StringComparer.Ordinal);
            for (int i = 0; i < access.Count(baseArray); i++)
            { var row = access.Get(baseArray, i); lists[access.Value(access.Get(row, "id"))] = access.Get(row, "event_list"); }
            for (int i = 0; i < access.Count(modArray); i++)
            {
                var row = access.Get(modArray, i); string id = access.Value(access.Get(row, "id"));
                object merged = access.Array(); var seen = new HashSet<string>(StringComparer.Ordinal);
                void Add(object input)
                {
                    for (int j = 0; j < access.Count(input); j++)
                    { var item = access.Get(input, j); if (seen.Add(access.Value(item))) access.Add(merged, access.Clone(item)); }
                }
                if (lists.TryGetValue(id, out var previous)) Add(previous);
                Add(access.Get(row, "event_list")); access.Set(row, "event_list", merged); lists[id] = merged;
            }
            __args[1] = modArray;
        }
        public void Dispose()
        {
            retainCdIds = false;
            appendBgmActive = false;
            mergingBgm = false;
            json = null;
        }
        private sealed class JsonAccess
        {
            private readonly Type array;
            private readonly PropertyInfo byIndex, byKey, count, value;
            private readonly MethodInfo clone, add;
            internal JsonAccess(Type node, Type array)
            {
                this.array = array;
                byIndex = node.GetProperty("Item", new[] { typeof(int) }) ?? throw new MissingMemberException("JSONNode[int]");
                byKey = node.GetProperty("Item", new[] { typeof(string) }) ?? throw new MissingMemberException("JSONNode[string]");
                count = node.GetProperty("Count") ?? throw new MissingMemberException("JSONNode.Count");
                value = node.GetProperty("Value") ?? throw new MissingMemberException("JSONNode.Value");
                clone = node.GetMethod("Clone", Type.EmptyTypes) ?? throw new MissingMethodException("JSONNode.Clone");
                add = node.GetMethod("Add", new[] { node }) ?? throw new MissingMethodException("JSONNode.Add");
            }
            internal object Array() => Activator.CreateInstance(array)!;
            internal object Clone(object input) => clone.Invoke(input, null)!;
            internal object Get(object input, int index) => byIndex.GetValue(input, new object[] { index })!;
            internal object Get(object input, string key) => byKey.GetValue(input, new object[] { key })!;
            internal void Set(object input, string key, object item) => byKey.SetValue(input, item, new object[] { key });
            internal int Count(object input) => (int)count.GetValue(input)!;
            internal string Value(object input) => (string)value.GetValue(input)!;
            internal void Add(object input, object item) => add.Invoke(input, new[] { item });
        }
    }
}
