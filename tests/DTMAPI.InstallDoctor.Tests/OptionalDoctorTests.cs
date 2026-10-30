using DTMAPI.InstallDoctor;
using DTMAPI.Testing;
using System.Text.Json.Nodes;

using var session = DtmApiTestSession.Start("OptionalDoctor");
string root = session.RootPath;
try
{
    string repo = AppContext.BaseDirectory;
    while (!File.Exists(Path.Combine(repo, "DTMAPI.sln"))) repo = Directory.GetParent(repo)?.FullName ?? throw new DirectoryNotFoundException();
    string package = Path.Combine(root, "package"); Directory.CreateDirectory(package);
    string entry = Path.Combine(package, "DTMAPI.Abstractions.dll");
    File.Copy(Path.Combine(repo, "src/DTMAPI.Abstractions/bin/Release/netstandard2.0/DTMAPI.Abstractions.dll"), entry);
    File.WriteAllText(Path.Combine(package, "manifest.json"), "{\"ManifestVersion\":2,\"UniqueID\":\"Test.Doctor\",\"Type\":\"CodeMod\",\"EntryDll\":\"DTMAPI.Abstractions.dll\",\"MinimumDTMApiVersion\":\"0.7.5\"}");
    File.WriteAllText(Path.Combine(package, "info.json"), "{\"version\":\"2.4.6\"}");
    File.WriteAllText(Path.Combine(package, "Unused.dll"), "unused corrupt DLL");
    File.WriteAllText(Path.Combine(package, "dtmapi-native-build.json"), "obsolete corrupt proof");
    var normal = new DoctorEngine().Inspect(package);
    Assert(normal.ErrorCount == 0 && normal.Artifacts.Single().Version == "2.4.6", "Unused DLLs and generated proofs do not cause a package failure.");
    Assert(normal.InitialTree == null && normal.Artifacts.All(a => a.Sha256 == "") && normal.Findings.Any(f => f.Code == "inspection-coverage"), "Normal Doctor reports coverage without a directory hash certificate.");
    var deep = new DoctorEngine().Inspect(package, new DoctorOptions { VerifyReadOnlyTreeHash = true });
    Assert(deep.TreeUnchanged == true && deep.InitialTree!.FileCount >= 5, "An explicitly requested integrity pass is read-only and covers the requested directory.");
    var incompatible = new DoctorEngine().Inspect(package, new DoctorOptions { InstalledDtmApiVersion = "0.7.3" });
    Assert(incompatible.Artifacts.Single().MinimumVersionStatus == DoctorMinimumVersionStatus.RequiresNewerRuntime && incompatible.ErrorCount > 0, "Runtime version incompatibility remains actionable.");
    File.WriteAllText(entry, "damaged selected entry");
    Assert(new DoctorEngine().Inspect(package).ErrorCount > 0, "Invalid selected code still fails inspection.");
    string game = Path.Combine(root, "game"), runtime = Path.Combine(game, "BepInEx/plugins/DTMAPI"); Directory.CreateDirectory(runtime);
    File.Copy(Path.Combine(repo, "src/DTMAPI.Core/bin/Release/netstandard2.0/DTMAPI.Core.dll"), Path.Combine(runtime, "DTMAPI.Core.dll"));
    Directory.CreateDirectory(Path.Combine(game, "DTMAPI"));
    File.WriteAllText(Path.Combine(game, "DTMAPI/install-state.json"), "{\"DTMAPIVersion\":\"0.7.5\",\"OptionalComponents\":[{\"Path\":\"DTMAPI/components/audio/missing.dll\"}]}");
    var probe = InstalledRuntimeVersionProbe.Inspect(game);
    Assert(probe.InstalledDtmApiVersion == "0.7.5" && probe.Findings.Any(f => f.Code == "optional-component-missing") && probe.Status != InstalledRuntimeVersionProbeStatus.Consistent, "A readable Core version survives independently reported component/set errors.");
    Console.WriteLine("Optional Doctor: PASS (actual files, unused candidates, coverage, explicit deep check, component/version separation).");
    return 0;
}
catch (Exception error) { Console.Error.WriteLine(error); return 1; }
static void Assert(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
