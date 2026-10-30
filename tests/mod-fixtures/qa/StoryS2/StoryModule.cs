using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DolocTown;
using HarmonyLib;
using Yarn.Unity;
using TextLineProvider = DolocTown.TextLineProvider;
using Runner = DolocTown.DialogueRunner;

namespace DTMAPI.Internal.StoryS2
{
    internal sealed class StoryModule : IDisposable
    {
        internal const string Owner = "DTMAPI.Internal.StoryS2.Native";
        internal static StoryModule? Active;
        internal readonly Action<string> Log;
        internal NativeBoundary Boundary = null!;
        internal Candidate Content = null!;
        internal PreparedProject? Prepared;
        private readonly Harmony harmony = new Harmony(Owner);
        private readonly ConditionalWeakTable<Runner, Binding> bindings = new ConditionalWeakTable<Runner, Binding>();
        private readonly List<WeakReference> runners = new List<WeakReference>();
        private readonly Stack<Construction> constructors = new Stack<Construction>();
        internal int BoundCount, RejectedCount, RolledBackCount;
        internal string FaultNext = "";
        private bool closed;

        internal sealed class Binding { internal int Id; internal Binding(int id) { Id = id; } }
        private sealed class Construction
        {
            internal object? PriorProject, PriorProvider;
            internal PreparedProject? Created;
            internal Runner? Runner;
        }
        internal StoryModule(Action<string> log) { Log = log; }
        internal void Start()
        {
            Candidate.Require(Active == null, "S2-OWNER-ALREADY-ACTIVE");
            Boundary = new NativeBoundary(); // Exact identity/signatures before any hook or Unity allocation.
            Candidate.Require(!DolocAPI.IsDataLoaded, "S2-COLD-ENABLE-ONLY");
            Content = Candidate.ReadEmbedded();
            Active = this;
            try
            {
                Patch(Boundary.ManagerConstructor, nameof(BeforeManager), finalizer: nameof(FinishManager));
                Patch(Boundary.RunnerConstructor, postfix: nameof(AfterRunner));
                Patch(Boundary.StartDialogue, nameof(BlockStoryPlayback));
                Log("installed; candidate=" + CandidateIdentity.Sha256 + "; stories=" + Content.Stories.Count + "; hooks=3; play=unavailable; no subscriptions");
            }
            catch { harmony.UnpatchSelf(); Active = null; throw; }
        }
        private void Patch(MethodBase method, string? prefix = null, string? postfix = null, string? finalizer = null)
        {
            HarmonyMethod? Hook(string? name) => name == null ? null : new HarmonyMethod(typeof(StoryModule).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!);
            harmony.Patch(method, Hook(prefix), Hook(postfix), finalizer: Hook(finalizer));
        }
        internal void Fault(string stage)
        {
            if (FaultNext != stage) return;
            FaultNext = "";
            throw new InvalidOperationException("S2-INJECTED:" + stage);
        }
        private static void BeforeManager(out Construction? __state)
        {
            __state = null;
            var self = Active;
            if (self == null || self.closed) return;
            Candidate.Require(self.constructors.Count == 0, "S2-REENTRANT-CONSTRUCTION");
            var state = new Construction
            {
                PriorProject = self.Boundary.ManagerProject.GetValue(null),
                PriorProvider = self.Boundary.ManagerProvider.GetValue(null)
            };
            __state = state;
            self.constructors.Push(state);
            try
            {
                if (self.Prepared == null)
                {
                    var original = (YarnProject?)state.PriorProject ?? DolocAPI.gameManager.gameInitConfig.yarnProject;
                    var provider = (TextLineProvider?)state.PriorProvider ?? new TextLineProvider(original);
                    state.Created = PreparedProject.Build(original, provider, self.Content, self.Boundary, self.Fault);
                    self.Prepared = state.Created;
                    self.Boundary.ManagerProject.SetValue(null, state.Created.Project);
                    self.Fault("publish-project");
                    self.Boundary.ManagerProvider.SetValue(null, state.Created.Provider);
                    self.Fault("publish-provider");
                }
                else
                    Candidate.Require(ReferenceEquals(state.PriorProject, self.Prepared.Project) && ReferenceEquals(state.PriorProvider, self.Prepared.Provider), "S2-STATIC-OWNER-CHANGED");
            }
            catch (Exception error)
            {
                self.Rollback(state);
                // Optional content failure leaves this native construction valid.
                // A subsequent fresh construction may retry the same fixed input.
                self.RejectedCount++;
                self.Log("prepare-rejected; " + error.Message + "; restored=true; unity=" + PreparedProject.LiveObjects);
            }
        }
        private static void AfterRunner(Runner __instance, YarnProject project, TextLineProvider lineProvider, DialogueVariableStorage variableStorage)
        {
            var self = Active;
            if (self?.Prepared == null || !ReferenceEquals(project, self.Prepared.Project)) return;
            Candidate.Require(self.constructors.Count != 0, "S2-RUNNER-OUTSIDE-CONSTRUCTION");
            var state = self.constructors.Peek();
            state.Runner = __instance;
            Candidate.Require(!self.bindings.TryGetValue(__instance, out _), "S2-DUPLICATE-BINDING");
            var vm = self.Boundary.Dialogue(__instance);
            Candidate.Require(ReferenceEquals(self.Boundary.RunnerProject.GetValue(__instance), project) &&
                ReferenceEquals(self.Boundary.RunnerProvider.GetValue(__instance), lineProvider) &&
                ReferenceEquals(lineProvider, self.Prepared.Provider) && ReferenceEquals(vm.VariableStorage, variableStorage) && !vm.IsActive,
                "S2-RUNNER-BINDING");
            self.Content.ValidateLibrary(vm.Library);
            self.Content.ValidateStorage(variableStorage);
            foreach (var node in self.Content.Program.Nodes.Keys)
                Candidate.Require(vm.NodeExists(node), "S2-NODE-UNBOUND:" + node);
            self.Fault("runner");
            self.bindings.Add(__instance, new Binding(++self.BoundCount));
            self.runners.RemoveAll(weak => !weak.IsAlive);
            self.runners.Add(new WeakReference(__instance));
            self.Log("bound id=" + self.BoundCount + "; runner=" + RuntimeHelpers.GetHashCode(__instance) +
                "; storage=" + RuntimeHelpers.GetHashCode(variableStorage) + "; nodes=" + project.Program.Nodes.Count + "; once=true; active=false");
        }
        private static Exception? FinishManager(Exception? __exception, Construction? __state)
        {
            var self = Active;
            if (self == null || __state == null) return __exception;
            if (__exception != null)
            {
                self.Rollback(__state);
                self.Log("constructor-failed; rollback=true; " + __exception.GetBaseException().Message);
            }
            Candidate.Require(self.constructors.Count != 0 && ReferenceEquals(self.constructors.Pop(), __state), "S2-CONSTRUCTION-ORDER");
            return __exception;
        }
        private void Rollback(Construction state)
        {
            if (state.Runner != null) { bindings.Remove(state.Runner); state.Runner = null; }
            if (state.Created == null) return;
            // Restore only our two writes. Never overwrite a later foreign owner.
            if (ReferenceEquals(Boundary.ManagerProject.GetValue(null), state.Created.Project)) Boundary.ManagerProject.SetValue(null, state.PriorProject);
            if (ReferenceEquals(Boundary.ManagerProvider.GetValue(null), state.Created.Provider)) Boundary.ManagerProvider.SetValue(null, state.PriorProvider);
            state.Created.Dispose();
            if (ReferenceEquals(Prepared, state.Created)) Prepared = null;
            state.Created = null;
            RolledBackCount++;
        }
        private static bool BlockStoryPlayback(string nodeName, ref bool __result)
        {
            var self = Active;
            if (self == null || !self.Content.Program.Nodes.ContainsKey(nodeName)) return true;
            self.Log("play-rejected S2-BINDING-ONLY node=" + nodeName);
            __result = false; return false;
        }
        internal int BindingId(Runner runner) => bindings.TryGetValue(runner, out var value) ? value.Id : 0;
        public void Dispose()
        {
            if (closed) return;
            closed = true;
            harmony.UnpatchSelf();
            WithdrawDefinitions();
            if (ReferenceEquals(Active, this)) Active = null;
            Log("disposed; unity=" + PreparedProject.LiveObjects + "; hooks=" + harmony.GetPatchedMethods().Count() + "; activeVMs=untouched; restart-required-for-program-removal");
        }
        internal void WithdrawDefinitions()
        {
            Candidate.Require(constructors.Count == 0, "S2-WITHDRAW-DURING-CONSTRUCTION");
            var prepared = Prepared;
            if (prepared != null)
            {
                // S2 never starts its own story. Keep every existing native VM
                // and its Program untouched; restore only our Unity/text refs.
                foreach (var weak in runners)
                    if (weak.Target is Runner runner)
                    {
                        if (ReferenceEquals(Boundary.RunnerProject.GetValue(runner), prepared.Project)) Boundary.RunnerProject.SetValue(runner, prepared.Original);
                        if (ReferenceEquals(Boundary.RunnerProvider.GetValue(runner), prepared.Provider)) Boundary.RunnerProvider.SetValue(runner, prepared.OriginalProvider);
                        bindings.Remove(runner);
                    }
                prepared.OriginalProvider.textLanguageCode = prepared.Provider.textLanguageCode;
                if (ReferenceEquals(Boundary.ManagerProject.GetValue(null), prepared.Project)) Boundary.ManagerProject.SetValue(null, prepared.Original);
                if (ReferenceEquals(Boundary.ManagerProvider.GetValue(null), prepared.Provider)) Boundary.ManagerProvider.SetValue(null, prepared.OriginalProvider);
                prepared.Dispose(); Prepared = null;
            }
            runners.Clear(); constructors.Clear();
        }
    }
}
