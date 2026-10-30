using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DolocTown;
using DolocTown.GameData;
using Google.Protobuf;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Yarn;
using Yarn.Unity;
using Runner = DolocTown.DialogueRunner;
using Op = Yarn.Instruction.Types.OpCode;

namespace DTMAPI.Internal.StoryS2
{
    // Explicit diagnostic harness. Its save guards/subscription are separate
    // from the adapter, and never exist in the cold-disabled route.
    internal sealed class Acceptance : IDisposable
    {
        // Advanced supervision requires the single SDK-declared owner, including
        // explicit QA patches. They are installed during the same Entry.
        private const string Owner = StoryModule.Owner;
        private static Yarn.Dialogue? watched;
        private static int replacements;
        private readonly StoryModule module;
        private readonly ExperimentConfig config;
        private readonly IDtmHelper helper;
        private readonly Harmony harmony = new Harmony(Owner);
        private bool started, disposed, paused, priorPause;
        private Coroutine? coroutine;
        internal Acceptance(StoryModule module, ExperimentConfig config, IDtmHelper helper) { this.module = module; this.config = config; this.helper = helper; }
        internal void Start()
        {
            Candidate.Require(config.SaveSlot == 2, "S2-QA-EXISTING-SLOT-2-REQUIRED");
            var block = new HarmonyMethod(typeof(Acceptance).GetMethod(nameof(BlockSave), BindingFlags.NonPublic | BindingFlags.Static));
            harmony.Patch(NativeBoundary.Method(typeof(DolocAPI), "SaveGame", typeof(bool), true, typeof(int)), prefix: block);
            harmony.Patch(NativeBoundary.Method(typeof(LocalSave), "SaveGame", typeof(bool), false, typeof(ArchiveDataHandle), typeof(int)), prefix: block);
            var observe = new HarmonyMethod(typeof(Acceptance).GetMethod(nameof(ObserveReplacement), BindingFlags.NonPublic | BindingFlags.Static));
            harmony.Patch(NativeBoundary.Method(typeof(Yarn.Dialogue), "SetProgram", typeof(void), false, typeof(Yarn.Program)), prefix: observe);
            harmony.Patch(NativeBoundary.Method(typeof(Yarn.Dialogue), "AddProgram", typeof(void), false, typeof(Yarn.Program)), prefix: observe);
            helper.Events.GameLoop.GameLaunched += Launched;
            TryStart();
        }
        private void Launched(object? sender, GameLaunchedEventArgs args) => TryStart();
        private void TryStart()
        {
            if (started || DolocAPI.gameManager == null || DolocAPI.userInput == null) return;
            started = true; coroutine = DolocAPI.StartCoroutine(Run());
        }
        private static bool BlockSave(ref bool __result) { __result = false; Debug.LogError("StoryS2 QA forbidden save attempt"); return false; }
        private static void ObserveReplacement(Yarn.Dialogue __instance) { if (ReferenceEquals(__instance, watched)) replacements++; }
        private void Pass(string message) => module.Log("QA PASS " + message);
        private IEnumerator Run()
        {
            var stack = new Stack<IEnumerator>(); stack.Push(Body()); bool pass = false;
            try
            {
                while (stack.Count != 0 && !disposed)
                {
                    var iterator = stack.Peek(); bool more = false; Exception? failure = null;
                    try { more = iterator.MoveNext(); } catch (Exception ex) { failure = ex; }
                    if (failure != null) { module.Log("QA FAIL " + failure); yield break; }
                    if (!more) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (iterator.Current is IEnumerator child) { stack.Push(child); continue; }
                    yield return iterator.Current;
                }
                pass = !disposed;
            }
            finally
            {
                while (stack.Count != 0) (stack.Pop() as IDisposable)?.Dispose();
                watched = null;
                if (paused && DolocAPI.gameLoop != null) DolocAPI.gameLoop.IsGlobalPaused = priorPause;
                module.Log("QA END pass=" + pass + "; bound=" + module.BoundCount + "; rejected=" + module.RejectedCount + "; rollback=" + module.RolledBackCount + "; replacements=" + replacements);
                coroutine = null;
                if (config.QuitAfterAcceptance) Application.Quit();
            }
        }
        private IEnumerator Body()
        {
            yield return Until(() => DolocAPI.userInput?.CurrentState is HomePageUiState, 90, "title");
            module.Log("QA START title; NoNativeSave; UI slot=2; adapter bound before any test read");
            RejectIdentityAndMember();
            // Real native constructors at title; no global archive substitution.
            var title = new DialogueManager();
            Validate(title, "title");
            var original = module.Prepared!.Original;
            string nativeProgramHash = Candidate.Hash(original.Program.ToByteArray());
            string nativeTextHash = NativeTextHash(original);
            ProbeColdFailureAndStorage();
            foreach (var stage in new[] { "project", "localization", "metadata", "publish-project", "publish-provider", "runner" })
            {
                module.WithdrawDefinitions();
                object priorProject = module.Boundary.ManagerProject.GetValue(null)!;
                object priorProvider = module.Boundary.ManagerProvider.GetValue(null)!;
                module.FaultNext = stage;
                DialogueManager? failed = null;
                try { failed = new DialogueManager(); }
                catch (Exception ex) { Candidate.Require(stage == "runner" && ex.GetBaseException().Message == "S2-INJECTED:runner", "S2-QA-WRONG-FAILURE:" + ex); }
                Candidate.Require(module.Prepared == null && PreparedProject.LiveObjects == 0 && PreparedProject.ObserveUnityObjects() == 0 && module.FaultNext == "", "S2-QA-PARTIAL-ROOTS:" + stage);
                Candidate.Require(ReferenceEquals(priorProject, module.Boundary.ManagerProject.GetValue(null)) && ReferenceEquals(priorProvider, module.Boundary.ManagerProvider.GetValue(null)), "S2-QA-STATIC-RESTORE:" + stage);
                if (failed != null) Candidate.Require(module.BindingId(module.Boundary.Runner(failed)) == 0, "S2-QA-FAILED-BOUND");
                var recovered = new DialogueManager(); Validate(recovered, "retry-" + stage);
                Candidate.Require(Candidate.Hash(original.Program.ToByteArray()) == nativeProgramHash && NativeTextHash(original) == nativeTextHash, "S2-QA-NATIVE-MUTATION:" + stage);
                Pass("partial-failure stage=" + stage + "; exact-static-restore; unity=0 before retry; native-content=unchanged; retry=bound");
            }
            ProbeActiveNativeVm();
            Runner? first = null; int firstId = 0;
            for (int load = 1; load <= 2; load++)
            {
                yield return Until(() => DolocAPI.userInput.CurrentState is HomePageUiState && !DolocAPI.IsDataLoaded, 30, "title-before-load");
                int before = module.BoundCount;
                var home = (HomePageUiState)DolocAPI.userInput.CurrentState;
                home.Kill(); Candidate.Require(DolocAPI.userInput.TryPopState(home), "S2-QA-TITLE-EXIT");
                Candidate.Require(DolocAPI.LoadGame(config.SaveSlot - 1), "S2-QA-LOAD-FAILED");
                yield return Until(() => DolocAPI.IsDataLoaded && DolocAPI.CurrentRoom != null && DolocAPI.userInput.CurrentState is NormalGameState, 90, "world-" + load);
                priorPause = DolocAPI.gameLoop.IsGlobalPaused; DolocAPI.gameLoop.IsGlobalPaused = true; paused = true;
                var manager = DolocAPI.archiveHandle.cityData.dialogueManager;
                var runner = module.Boundary.Runner(manager);
                Validate(manager, "load-" + load);
                Candidate.Require(module.BoundCount == before + 1, "S2-QA-LOAD-BIND-COUNT:" + (module.BoundCount - before));
                if (load == 1) { first = runner; firstId = module.BindingId(runner); }
                else Candidate.Require(!ReferenceEquals(first, runner) && module.BindingId(runner) != firstId, "S2-QA-LOAD-INSTANCE-REUSED");
                Pass("actual-load=" + load + "; nativeSlot=1; bind-delta=1; identity=" + module.BindingId(runner));
                DolocAPI.ReturnHome();
                yield return Until(() => !DolocAPI.IsDataLoaded && DolocAPI.userInput.CurrentState is HomePageUiState, 40, "returned-title-" + load);
                if (paused) { DolocAPI.gameLoop.IsGlobalPaused = priorPause; paused = false; }
            }
            Candidate.Require(Candidate.Hash(original.Program.ToByteArray()) == nativeProgramHash && NativeTextHash(original) == nativeTextHash, "S2-QA-NATIVE-CONTENT-CHANGED");
            module.Dispose();
            Candidate.Require(StoryModule.Active == null && PreparedProject.LiveObjects == 0 && PreparedProject.ObserveUnityObjects() == 0 && !Harmony.HasAnyPatches(StoryModule.Owner), "S2-QA-DISPOSE-ROOTS");
            Pass("withdraw; module=0; hooks=0; unity=0; native-program/text/metadata=unchanged");
        }
        private void RejectIdentityAndMember()
        {
            try { NativeBoundary.CheckIdentity(typeof(DialogueManager).Assembly, new string('0', 64)); throw new Exception("identity accepted"); }
            catch (InvalidOperationException e) { Candidate.Require(e.Message.StartsWith("S2-NATIVE-IDENTITY:"), "S2-QA-IDENTITY-DIAGNOSTIC"); }
            try { NativeBoundary.Field(typeof(DialogueManager), "project", typeof(string), true); throw new Exception("member accepted"); }
            catch (InvalidOperationException e) { Candidate.Require(e.Message.StartsWith("S2-NATIVE-FIELD:"), "S2-QA-MEMBER-DIAGNOSTIC"); }
            Pass("wrong native hash and required field type explicitly rejected");
        }
        private void ProbeColdFailureAndStorage()
        {
            module.WithdrawDefinitions();
            module.Boundary.ManagerProject.SetValue(null, null);
            module.Boundary.ManagerProvider.SetValue(null, null);
            module.FaultNext = "runner";
            bool failed = false;
            try { _ = new DialogueManager(); }
            catch (InvalidOperationException error) { failed = error.Message == "S2-INJECTED:runner"; }
            Candidate.Require(failed && module.Prepared == null && PreparedProject.LiveObjects == 0 &&
                module.Boundary.ManagerProject.GetValue(null) == null && module.Boundary.ManagerProvider.GetValue(null) == null, "S2-QA-COLD-ROLLBACK");
            var storage = new DialogueVariableStorage();
            storage.SetValue("$s2_native_control", 42f);
            string variable = module.Content.Program.InitialValues.Keys.First();
            storage.SetValue(variable, true);
            string before = storage.ToString();
            var recovered = new DialogueManager(variableStorage: storage);
            Validate(recovered, "cold-retry");
            Candidate.Require(ReferenceEquals(module.Boundary.Dialogue(module.Boundary.Runner(recovered)).VariableStorage, storage) && storage.ToString() == before,
                "S2-QA-STORAGE-REPLACED-OR-WRITTEN");
            Pass("cold null static pair restored after native runner failure; retry retains exact incoming storage and values");
            module.WithdrawDefinitions();
            var wrong = new DialogueVariableStorage(); wrong.SetValue(variable, "bad saved type");
            bool rejected = false;
            try { _ = new DialogueManager(variableStorage: wrong); }
            catch (InvalidOperationException error) { rejected = error.Message == "S2-STORAGE-TYPE:" + variable; }
            Candidate.Require(rejected && module.Prepared == null && PreparedProject.LiveObjects == 0, "S2-QA-STORAGE-CONFLICT-ACCEPTED");
            Validate(new DialogueManager(), "after-storage-rejection");
            Pass("incompatible saved variable type rejected without writes; next native construction succeeds");
        }
        private void Validate(DialogueManager manager, string label)
        {
            var runner = module.Boundary.Runner(manager); var vm = module.Boundary.Dialogue(runner);
            Candidate.Require(module.BindingId(runner) > 0 && module.Prepared != null, "S2-QA-UNBOUND:" + label);
            var prepared = module.Prepared!;
            foreach (var pair in prepared.Original.Program.Nodes)
                Candidate.Require(prepared.Project.Program.Nodes[pair.Key].Equals(pair.Value), "S2-QA-NATIVE-NODE-CHANGED");
            foreach (var node in module.Content.Program.Nodes.Values)
                Candidate.Require(vm.NodeExists(node.Name) && vm.GetTagsForNode(node.Name).SequenceEqual(node.Tags), "S2-QA-TAGS");
            foreach (var locale in prepared.Project.localizations)
            {
                prepared.Provider.textLanguageCode = locale.LocaleCode;
                foreach (var line in module.Content.Lines.Values)
                {
                    string id = (string)line["id"]!;
                    var actual = prepared.Provider.GetLocalizedLine(new Line { ID = id, Substitutions = Array.Empty<string>() });
                    Candidate.Require(actual.RawText == module.Content.Text(line, locale.LocaleCode) && manager.GetLocalizedText(id) == actual.RawText, "S2-QA-TEXT:" + locale.LocaleCode);
                    Candidate.Require((actual.Metadata ?? Array.Empty<string>()).SequenceEqual(line["metadata"]!.Values<string>()), "S2-QA-METADATA");
                }
            }
            manager.SwitchLanguage(DolocAPI.CurrentL10nId);
            Candidate.Require(!runner.StartDialogue(module.Content.Stories.Values.First(), ""), "S2-QA-PLAY-NOT-GUARDED");
            Pass("consumers=" + label + "; binding=" + module.BindingId(runner) + "; native nodes preserved; project/runner/static text/metadata/locales/Library=bound; playback=guarded");
        }
        private void ProbeActiveNativeVm()
        {
            var manager = new DialogueManager(); var runner = module.Boundary.Runner(manager);
            var vm = module.Boundary.Dialogue(runner);
            var native = module.Prepared!.Original;
            string node = native.Program.Nodes.Values.First(n => n.Instructions.Count > 1 && n.Instructions[0].Opcode == Op.RunLine).Name;
            var lines = new List<string>();
            var options = new List<string>();
            vm.LineHandler = line => lines.Add(line.ID);
            vm.CommandHandler = command => { }; // Synthetic manager only; never dispatch game commands.
            vm.OptionsHandler = set => options.AddRange(set.Options.Select(option => option.Line.ID + ":" + option.ID + ":" + option.IsAvailable));
            Candidate.Require(runner.StartDialogue(node, ""), "S2-QA-NATIVE-START");
            Candidate.Require(runner.isRunning && vm.IsActive && lines.Count == 1 && vm.CurrentNode == node, "S2-QA-NATIVE-VM-NOT-WAITING");
            watched = vm; replacements = 0;
            var next = new DialogueManager(); Validate(next, "alongside-active-native");
            module.WithdrawDefinitions();
            Candidate.Require(replacements == 0 && runner.isRunning && vm.IsActive && vm.CurrentNode == node && lines.Count == 1, "S2-QA-ACTIVE-VM-RESET");
            var control = new Yarn.Dialogue(new DialogueVariableStorage());
            var expected = new List<string>(); control.LineHandler = line => expected.Add(line.ID); control.CommandHandler = _ => { };
            var expectedOptions = new List<string>();
            control.OptionsHandler = set => expectedOptions.AddRange(set.Options.Select(option => option.Line.ID + ":" + option.ID + ":" + option.IsAvailable));
            control.SetProgram(native.Program); control.SetNode(node); control.Continue();
            vm.Continue(); control.Continue();
            Candidate.Require(lines.SequenceEqual(expected) && options.SequenceEqual(expectedOptions) && vm.CurrentNode == control.CurrentNode && vm.IsActive == control.IsActive, "S2-QA-ACTIVE-CURSOR-CHANGED");
            watched = null; vm.Stop(); control.Stop();
            Pass("active native runner VM; construct+withdraw SetProgram/AddProgram=0; next continuation matches untouched native control");
            Validate(new DialogueManager(), "after-active-withdraw");
        }
        private static string NativeTextHash(YarnProject project)
        {
            var text = new List<string>();
            foreach (var locale in project.localizations.Concat(new[] { project.baseLocalization }).Distinct())
                foreach (string id in locale.GetLineIDs().Distinct().OrderBy(id => id, StringComparer.Ordinal)) text.Add(locale.LocaleCode + "\n" + id + "\n" + locale.GetLocalizedString(id));
            foreach (string id in project.lineMetadata.GetLineIDs().OrderBy(id => id, StringComparer.Ordinal)) text.Add(id + "\n" + string.Join(" ", project.lineMetadata.GetMetadata(id)));
            return Candidate.Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("\0", text)));
        }
        private static IEnumerator Until(Func<bool> predicate, float seconds, string label)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!predicate()) { if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("S2-QA-TIMEOUT:" + label); yield return null; }
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (coroutine != null && DolocAPI.gameManager != null) DolocAPI.gameLoop.StopCoroutine(coroutine);
            coroutine = null; watched = null;
            harmony.UnpatchSelf();
            // Runtime owns the event token and removes it before IDisposable.
        }
    }
}
