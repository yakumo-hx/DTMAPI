using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mono.Cecil;
using Mono.Cecil.Cil;
using DTMAPI.Tooling.Metadata;

// Signature inputs only; the SDK produces admission receipts.
string game = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]);
string path = Path.Combine(game, "DolocTown_Data/Managed/Assembly-CSharp.dll");
using var resolver = new DefaultAssemblyResolver(); resolver.AddSearchDirectory(Path.GetDirectoryName(path));
using var host = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { AssemblyResolver = resolver });
string wwisePath = Path.Combine(Path.GetDirectoryName(path)!, "AK.Wwise.Unity.API.dll"), firstpassPath = Path.Combine(Path.GetDirectoryName(path)!, "Assembly-CSharp-firstpass.dll");
using var wwise = AssemblyDefinition.ReadAssembly(wwisePath, new ReaderParameters { AssemblyResolver = resolver });
using var firstpass = AssemblyDefinition.ReadAssembly(firstpassPath, new ReaderParameters { AssemblyResolver = resolver });
using var probe = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("AudioMetadataInputs", new Version(1, 0)), "AudioMetadataInputs", ModuleKind.Dll);
var type = new TypeDefinition("Evidence", "AudioBoundary", TypeAttributes.Class | TypeAttributes.Public, probe.MainModule.TypeSystem.Object); probe.MainModule.Types.Add(type);
var method = new MethodDefinition("SignaturesOnly", MethodAttributes.Static | MethodAttributes.Public, probe.MainModule.TypeSystem.Void); type.Methods.Add(method);
var il = method.Body.GetILProcessor();
il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(firstpass.MainModule.Types.Single(t => t.FullName == "SimpleJSON.JSONNode").Methods.Single(m => m.Name == "Clone" && m.Parameters.Count == 0))); il.Emit(OpCodes.Pop);
var audioTypes = wwise.MainModule.Types.Concat(firstpass.MainModule.Types).ToArray();
var soundEngine = audioTypes.Single(t => t.FullName == "AkSoundEngine");
foreach (var member in new[] {
    soundEngine.Methods.Single(m => m.Name == "AddBasePath" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String"),
    soundEngine.Methods.Single(m => m.Name == "LoadBank" && m.Parameters.Count == 4 && m.Parameters[0].ParameterType.FullName == "System.String" && m.Parameters[1].ParameterType.FullName == "AkCallbackManager/BankCallback" && m.Parameters[3].ParameterType.FullName == "System.UInt32&"),
    soundEngine.Methods.Single(m => m.Name == "UnloadBank" && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.FullName == "System.UInt32" && m.Parameters[1].ParameterType.FullName == "System.IntPtr"),
    soundEngine.Methods.Single(m => m.Name == "RenderAudio" && m.Parameters.Count == 0),
    audioTypes.Single(t => t.FullName == "AkCallbackManager").Methods.Single(m => m.Name == "PostCallbacks" && m.Parameters.Count == 0)
}) { il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(member)); il.Emit(OpCodes.Pop); }
var cdManager = host.MainModule.Types.Single(t => t.FullName == "DolocTown.CDManager");
il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(cdManager.Methods.Single(m => m.IsConstructor && m.Parameters.Count == 6))); il.Emit(OpCodes.Pop);
il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(cdManager.Fields.Single(f => f.Name == "unlockedIds"))); il.Emit(OpCodes.Pop);
var manager = host.MainModule.Types.Single(t => t.FullName == "DolocTown.WwiseSoundManager");
var modManager = host.MainModule.Types.Single(t => t.FullName == "DolocTown.Config.ModManager");
foreach (string name in new[] { "LoadWithMods", "MergeArray" })
{ il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(modManager.Methods.Single(m => m.Name == name))); il.Emit(OpCodes.Pop); }
var target = manager.Methods.Single(m => m.Name == "InternalPostSoundEvent" && m.Parameters.Count == 4 && m.ReturnType.FullName == "System.Boolean" && m.Parameters[0].ParameterType.FullName == "System.String" && m.Parameters[1].ParameterType.FullName == "UnityEngine.GameObject" && m.Parameters[2].ParameterType.FullName == "AkCallbackManager/EventCallback" && m.Parameters[3].ParameterType.FullName == "System.Boolean");
il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(target)); il.Emit(OpCodes.Pop);
il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(manager.Methods.Single(m =>
    m.Name == "TryOccupySoundGroup" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String" &&
    m.ReturnType.FullName == "System.Boolean"))); il.Emit(OpCodes.Pop);
var animal = host.MainModule.Types.Single(t => t.FullName == "DolocTown.Animal");
il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(animal.Methods.Single(m => m.Name == "PlayAnimalSound" && m.Parameters.Count == 0))); il.Emit(OpCodes.Pop);
foreach (string name in new[] { "protoName", "proto", "data" })
{
    var property = animal.Properties.SingleOrDefault(p => p.Name == name);
    if (property?.GetMethod != null) il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(property.GetMethod));
    else il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(animal.Fields.Single(f => f.Name == name)));
    il.Emit(OpCodes.Pop);
}
foreach (var pair in new[] { (Holder: "data", Member: "isChild"), (Holder: "proto", Member: "SoundEventChild"), (Holder: "proto", Member: "SoundEvent") })
{
    var holder = animal.Properties.SingleOrDefault(p => p.Name == pair.Holder)?.PropertyType ?? animal.Fields.Single(f => f.Name == pair.Holder).FieldType;
    var definition = holder.Resolve();
    var property = definition.Properties.SingleOrDefault(p => p.Name == pair.Member);
    if (property?.GetMethod != null) il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(property.GetMethod));
    else il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(definition.Fields.Single(f => f.Name == pair.Member)));
    il.Emit(OpCodes.Pop);
}
il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(manager.Fields.Single(f => f.Name == "loadedSoundBanks"))); il.Emit(OpCodes.Pop);
il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(manager.Properties.Single(p => p.Name == "agentObj").GetMethod)); il.Emit(OpCodes.Pop);
foreach (string name in new[] { "PlayBGMInternal", "PauseBgm", "StopBgm", "BgmUpdate", "StopAll" })
{ il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(manager.Methods.Single(m => m.Name == name))); il.Emit(OpCodes.Pop); }
il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(manager.Methods.Single(m => m.Name == "PostSoundEvent" && m.Parameters.Count == 3 && m.Parameters[0].ParameterType.FullName == "System.String"))); il.Emit(OpCodes.Pop);
// Music cancellation releases only this accepted request's native playback gate.
foreach (string name in new[] { "isBgmPlaying", "isBgmStoped" })
{ il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(manager.Fields.Single(f => f.Name == name))); il.Emit(OpCodes.Pop); }
var currentMusic = manager.Properties.Single(p => p.Name == "currentBgmEventName");
foreach (var accessor in new[] { currentMusic.GetMethod, currentMusic.SetMethod })
{ il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(accessor)); il.Emit(OpCodes.Pop); }
il.Emit(OpCodes.Ret);
if (args.Length > 2 && (args[2] == "e3-music-probe" || args[2] == "e3-cassette-probe" || args[2] == "volume-save-probe"))
{
    il.Remove(method.Body.Instructions.Last());
    il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(manager.Methods.Single(m => m.Name == "HandleOnBgmEvent"))); il.Emit(OpCodes.Pop);
    il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(manager.Fields.Single(f => f.Name == "extraDelayTimer"))); il.Emit(OpCodes.Pop);
    foreach (string name in new[] { "currentBgmEventName", "isBgmPlaying", "isBgmStoped" })
    {
        var field = manager.Fields.SingleOrDefault(f => f.Name == name);
        if (field != null) il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(field));
        else il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(manager.Properties.Single(p => p.Name == name).GetMethod));
        il.Emit(OpCodes.Pop);
    }
    foreach (string name in new[] { "DolocAPI", "DolocTown.GameData.DataPersistenceManager" })
    foreach (var selected in host.MainModule.Types.Single(t => t.FullName == name).Methods.Where(m => m.Name == "SaveGame" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Int32"))
    { il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(selected)); il.Emit(OpCodes.Pop); }
    il.Emit(OpCodes.Ret);
}
if (args.Length > 2 && (args[2] == "e3-cassette-probe" || args[2] == "volume-save-probe"))
{
    il.Remove(method.Body.Instructions.Last());
    foreach (var selection in new[] {
        ("GameManager", new[] { "OnEverythingLoaded", "get_IsGameInitialized" }),
        ("DolocAPI", new[] { "LoadGame" }),
        ("DolocTown.CDManager", new[] { "PlayCD", "UnlockCD", "ToggleCDInPlaylist", "IsCDInPlaylist", "ToggleCDPlayerEnabled", "SetPlayMode" }) })
    foreach (var selected in host.MainModule.Types.Single(t => t.FullName == selection.Item1).Methods.Where(m => selection.Item2.Contains(m.Name)))
    { il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(selected)); il.Emit(OpCodes.Pop); }
    il.Emit(OpCodes.Ret);
}
if (args.Length > 2 && (args[2] == "e1-probe" || args[2] == "e21-probe" || args[2] == "e25-probe"))
{
    il.Remove(method.Body.Instructions.Last());
    foreach (var pair in new[] { ("DolocTown.DungeonResourceModelPaperBox", "OnInteract"), ("DolocAPI", "SaveGame"), ("DolocTown.GameData.DataPersistenceManager", "SaveGame") })
    {
        var selected = host.MainModule.Types.Single(t => t.FullName == pair.Item1).Methods.Where(m => m.Name == pair.Item2 && (pair.Item2 != "SaveGame" || m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Int32"));
        foreach (var member in selected) { il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(member)); il.Emit(OpCodes.Pop); }
    }
    if (args[2] == "e21-probe" || args[2] == "e25-probe")
    {
        var api = host.MainModule.Types.Single(t => t.FullName == "DolocAPI");
        foreach (var selected in new[] {
            api.Methods.Single(m => m.Name == "ReturnHome" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Boolean"),
            api.Methods.Single(m => m.Name == "LoadGame" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Int32") })
        { il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(selected)); il.Emit(OpCodes.Pop); }
        var hurt = host.MainModule.Types.Single(t => t.FullName == "DolocTown.MonsterController").Methods.Single(m => m.Name == "Hurt" && m.Parameters.Count == 4 && m.Parameters[0].ParameterType.FullName == "UnityEngine.Transform" && m.Parameters[1].ParameterType.FullName == "System.Single" && m.Parameters.Skip(2).All(p => p.ParameterType.FullName == "System.Boolean"));
        il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(hurt)); il.Emit(OpCodes.Pop);
    }
    if (args[2] == "e25-probe")
    {
        var hook = host.MainModule.Types.Single(t => t.FullName == "DolocTown.FishRodHook");
        il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(hook.Methods.Single(m => m.Name == "TryRaiseWaterSplash"))); il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(hook.Fields.Single(f => f.Name == "_hasRaiseWaterSplash"))); il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ldtoken, probe.MainModule.ImportReference(host.MainModule.Types.Single(t => t.FullName == "DolocTown.InteractiveWater").Methods.Single(m => m.Name == "Splash"))); il.Emit(OpCodes.Pop);
    }
    il.Emit(OpCodes.Ret);
}
using var stream = new MemoryStream(); probe.Write(stream);
var required = NativeGenericMetadata.Extract(stream.ToArray(), new[] { path, wwisePath, firstpassPath }, new[] { Path.GetDirectoryName(path)! });
File.WriteAllText(output, new JsonArray(required.Select(bytes => JsonNode.Parse(bytes)).ToArray()).ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("Generated audio required member inputs: " + required.Count);
