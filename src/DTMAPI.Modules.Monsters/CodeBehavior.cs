using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DTMAPI.MonsterContent;
using DolocTown;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class BehaviorRegistration
        {
            internal string Owner = "", Id = "";
            internal Func<IMonsterBehavior> Factory = null!;
            internal Action Guard = null!;
            internal Action<string, string> Report = null!;
            internal bool Closed;
            internal MonsterBehaviorRequirements? Requirements;
        }
        private readonly Dictionary<string, BehaviorRegistration> behaviors = new Dictionary<string, BehaviorRegistration>(StringComparer.Ordinal);
        private readonly ConditionalWeakTable<IMonsterBehavior, object> usedBehaviors = new ConditionalWeakTable<IMonsterBehavior, object>();
        public MonsterResult<MonsterLease> RegisterBehavior(string owner, string localId, Func<IMonsterBehavior> factory, Action guard, Action<string, string> report)
            => RegisterBehaviorCore(owner, localId, null, factory, guard, report);
        public MonsterResult<MonsterLease> RegisterBehavior(string owner, string localId, MonsterBehaviorRequirements requirements, Func<IMonsterBehavior> factory, Action guard, Action<string, string> report)
            => RegisterBehaviorCore(owner, localId, requirements, factory, guard, report);
        private MonsterResult<MonsterLease> RegisterBehaviorCore(string owner, string localId, MonsterBehaviorRequirements? requirements, Func<IMonsterBehavior> factory, Action guard, Action<string, string> report)
        {
            RequireThread();
            string id = owner + ":" + localId;
            if (behaviors.ContainsKey(id)) return MonsterResult<MonsterLease>.Fail(new MonsterDiagnostic("DuplicateBehavior", "Behavior ID already registered in this process.", owner, stage: "Behavior"));
            var registration = new BehaviorRegistration { Owner = owner, Id = id, Factory = factory, Guard = guard, Report = report, Requirements = requirements?.Copy() };
            behaviors.Add(id, registration);
            return MonsterResult<MonsterLease>.Ok(new MonsterLease(() => { RequireThread(); ReleaseBehavior(registration); }));
        }
        private void ReleaseBehavior(BehaviorRegistration registration)
        {
            if (registration.Factory == null) return;
            registration.Closed = true;
            foreach (var definition in definitions.Records.Where(d => d.Definition.BehaviorId == registration.Id))
            {
                definition.State = MonsterDefinitionState.Rejected;
                definition.Diagnostic = Error(definition, "BehaviorUnavailable", "behaviorId", "The behavior registration ended.");
            }
            RemoveInstances(s => s.Binding.Definition.Definition.BehaviorId == registration.Id);
            definitions.RemoveBehavior(registration.Id);
            if (behaviors.TryGetValue(registration.Id, out var current) && ReferenceEquals(current, registration)) behaviors.Remove(registration.Id);
            registration.Factory = null!; registration.Guard = null!; registration.Report = null!;
        }
        private static bool UsesControlledBehavior(string mode) => mode == "code-provider" || mode == "configured-sequence";
        private static bool UsesCode(InstanceScope scope) => !scope.Binding.NativeChild && UsesControlledBehavior(scope.Binding.Definition.Definition.Behavior);
        private static void GuardBehavior(InstanceScope scope)
        {
            if (scope.BehaviorRegistration != null) scope.BehaviorRegistration.Guard();
            else if (scope.Binding.Definition.State != MonsterDefinitionState.Creatable) throw new InvalidOperationException("OwnerClosed");
        }
        private void StartBehavior(InstanceScope scope)
        {
            if (!UsesCode(scope)) return;
            bool configured = scope.Binding.Definition.Definition.Behavior == "configured-sequence";
            BehaviorRegistration? registration = null;
            if (!configured && (!behaviors.TryGetValue(scope.Binding.Definition.Definition.BehaviorId, out registration) || registration.Closed)) throw new InvalidOperationException("BehaviorUnavailable");
            scope.BehaviorRegistration = registration;
            scope.SkillClock = 0; scope.SkillReadyAt.Clear();
            scope.BehaviorContext = new BehaviorContext(this, scope);
            string stage = "Factory";
            scope.AuthorDepth++;
            try
            {
                GuardBehavior(scope);
                var definition = scope.Binding.Definition.Definition;
                IMonsterBehavior created = configured
                    ? definition.Combat == null ? (IMonsterBehavior)new ConfiguredSequence(definition.Sequence!) : new CombatSequence(definition.Sequence, definition.Combat)
                    : registration!.Factory() ?? throw new InvalidOperationException("Factory returned null.");
                if (usedBehaviors.TryGetValue(created, out _)) throw new InvalidOperationException("Factory reused a behavior object.");
                usedBehaviors.Add(created, new object());
                scope.Behavior = created;
                // Store before Start: failure must still Stop the acquired object exactly once.
                stage = "Start";
                scope.Behavior.Start(scope.BehaviorContext);
            }
            catch (Exception ex) { ReportBehavior(scope, stage, ex); throw; }
            finally { scope.AuthorDepth--; DrainActionFailure(scope); }
        }
        private void ReportBehavior(InstanceScope scope, string stage, Exception ex)
        {
            var detail = "contentOwner=" + scope.Binding.Definition.Owner + "; instance=" + scope.Handle + "; " + ex;
            Log("Behavior " + stage + " provider=" + scope.BehaviorRegistration?.Owner + "; " + detail);
            try { scope.BehaviorRegistration?.Report?.Invoke(stage, detail); }
            catch (Exception diagnosticError) { Log("Behavior diagnostic failed: " + diagnosticError.Message); }
        }
        private void StopBehavior(InstanceScope scope)
        {
            scope.BehaviorContext?.Close();
            var behavior = scope.Behavior;
            scope.Behavior = null;
            if (behavior == null) return;
            try { behavior.Stop(); }
            catch (Exception ex) { ReportBehavior(scope, "Stop", ex); }
        }
        private void TickBehavior(InstanceScope scope, float dt)
        {
            if (!scope.Active || scope.Behavior == null || dt <= 0) return;
             {
            scope.AuthorDepth++;
            try { GuardBehavior(scope); (scope.Behavior as IConfiguredBehavior)?.BeforeAction(dt); }
            catch (Exception ex) { ReportBehavior(scope, "Tick", ex); scope.Active = false; scope.FaultPending = true; }
            finally { scope.AuthorDepth--; DrainActionFailure(scope); }
            if (!scope.Active) return;
            try { AdvanceAction(scope, dt); }
            catch (Exception ex) { FailAction(scope, "Advance", ex); return; }
            if (!scope.Active) return;
            scope.AuthorDepth++;
            try
            {
                GuardBehavior(scope);
                if (scope.Active) scope.Behavior?.Tick(dt);
            }
            catch (Exception ex)
            {
                ReportBehavior(scope, "Tick", ex);
                scope.Active = false;
                scope.FaultPending = true;
            }
            finally { scope.AuthorDepth--; DrainActionFailure(scope); }
            }
        }
        private sealed class CodeAi : MonsterAI
        {
            private readonly NativeModule module;
            private readonly InstanceScope scope;
            internal CodeAi(NativeModule module, InstanceScope scope) { this.module = module; this.scope = scope; }
            public sealed class ControlledState : MonsterStateManager.MonsterState
            { public override MonsterStateManager.MonsterState NextState(float dt) => this; }
            public override void OnStart() { StateManager.SetDefaultState<ControlledState>(); base.OnStart(); }
            public override void OnUpdate(float dt) { }
            internal bool HasNativeTargetProvider => _sight != null;
            public override Transform LockedTarget => _sight?.LockedTarget!;
            public override void OnFixedUpdate(float dt)
            {
                if (!scope.Active) return;
                base.OnFixedUpdate(dt); // Native sight; the only state has no decisions.
                module.TickBehavior(scope, dt);
            }
            public override void OnStop() => module.StopBehavior(scope);
            public override void OnHurt(Transform attacker)
            {
                // Retire the animated action immediately, before another native manager
                // can advance its live volley in the same game step.
                if (scope.Action?.Animation == null) return;
                try { module.EndAction(scope, MonsterActionState.Interrupted, MonsterActionInterruptionReason.Hurt); }
                catch (Exception ex) { module.FailAction(scope, "Animation.HurtInterrupt", ex); }
            }
        }
        private sealed class BehaviorContext : IMonsterBehaviorContext
        {
            private NativeModule module;
            private InstanceScope scope;
            private readonly MonsterHandle instance;
            private readonly string ownerId, definitionId;
            private bool closed;
            private MonsterDiagnostic? terminalError;
            private Transform? observedTarget;
            private long targetRevision;
            internal BehaviorContext(NativeModule module, InstanceScope scope)
            { this.module = module; this.scope = scope; instance = scope.Handle; ownerId = scope.Binding.Definition.Owner; definitionId = scope.Binding.Definition.Id; }
            internal void Close()
            {
                if (closed) return;
                closed = true;
                terminalError = scope.ActionFailure;
                observedTarget = null;
                // Authors may retain stale contexts: keep only DTO identity, never a room/native scope.
                module = null!; scope = null!;
            }
            private MonsterDiagnostic? Check()
            {
                if (closed) return terminalError ?? Error("StaleHandle");
                if (!module.context.IsMainThread) return Error("WrongThread");
                if (scope.ActionFailure != null) return scope.ActionFailure;
                if (scope.Action?.Stopping == true) return Error("ActionStopping");
                if (!scope.Active || !scope.Handle.Equals(instance) || scope.Controller == null) return Error("StaleHandle");
                try { GuardBehavior(scope); } catch { return Error("OwnerClosed"); }
                return null;
            }
            private MonsterDiagnostic Error(string code) => new MonsterDiagnostic(code, code, ownerId, definitionId, stage: "Action");
            private MonsterResult<T> Call<T>(string stage, Func<MonsterResult<T>> work)
            {
                 { var error = Check(); return error == null ? module.NativeCall(scope, stage, work) : MonsterResult<T>.Fail(error); }
            }
            public MonsterResult<MonsterInstanceSnapshot> GetSelf()
            {

                {
                    var error = Check();
                    if (error != null) return MonsterResult<MonsterInstanceSnapshot>.Fail(error);
                    try { return MonsterResult<MonsterInstanceSnapshot>.Ok(module.Snapshot(scope)); }
                    catch (Exception ex) { return MonsterResult<MonsterInstanceSnapshot>.Fail(module.FailAction(scope, "QuerySelf", ex)); }
                }
            }
            public MonsterResult<IReadOnlyList<MonsterAttackOption>> GetAttacks()
            {
                var error = Check();
                if (error != null) return MonsterResult<IReadOnlyList<MonsterAttackOption>>.Fail(error);
                try { return MonsterResult<IReadOnlyList<MonsterAttackOption>>.Ok(module.AttackOptions(scope)); }
                catch (Exception ex) { return MonsterResult<IReadOnlyList<MonsterAttackOption>>.Fail(module.FailAction(scope, "QueryAttacks", ex)); }
            }
            public MonsterResult<MonsterTargetSnapshot?> GetTarget()
            {
                var error = Check();
                if (error != null) return MonsterResult<MonsterTargetSnapshot?>.Fail(error);
                if (scope.Controller!.MonsterAI is CodeAi ai && !ai.HasNativeTargetProvider)
                    return MonsterResult<MonsterTargetSnapshot?>.Fail(Error("TargetingUnsupported"));
                try
                {
                    var found = module.Target(scope);
                    if (!ReferenceEquals(found, observedTarget)) { observedTarget = found; targetRevision++; }
                    if (found == null) return MonsterResult<MonsterTargetSnapshot?>.Ok(null);
                    var position = found.position;
                    return MonsterResult<MonsterTargetSnapshot?>.Ok(new MonsterTargetSnapshot(new MonsterTargetHandle(instance, targetRevision), position.x, position.y));
                }
                catch (Exception ex) { return MonsterResult<MonsterTargetSnapshot?>.Fail(module.FailAction(scope, "QueryTarget", ex)); }
            }
            public MonsterResult<MonsterActionHandle> Attack(string attackId, float maxGameSeconds) => Call("StartAttack", () => module.BeginAttack(scope, attackId, maxGameSeconds));
            public MonsterResult<MonsterActionHandle> UseSkill(string skillId, float maxGameSeconds) => Call("StartSkill", () => module.BeginSkill(scope, skillId, maxGameSeconds));
            public MonsterResult<MonsterActionHandle> MoveTo(float x, float y, float maxGameSeconds, float maxTravel) => Call("StartMove", () => module.BeginMove(scope, x, y, maxGameSeconds, maxTravel));
            public MonsterResult<MonsterActionSnapshot> GetAction(MonsterActionHandle handle)
            {

                {
                    var error = Check();
                    if (error != null) return MonsterResult<MonsterActionSnapshot>.Fail(error);
                    // Same NativeCall fault boundary, without a per-query closure.
                    try { return scope.Action != null && scope.Action.Handle.Equals(handle)
                        ? MonsterResult<MonsterActionSnapshot>.Ok(scope.Action.Snapshot())
                        : MonsterResult<MonsterActionSnapshot>.Fail(Error("StaleAction")); }
                    catch (Exception ex) { return MonsterResult<MonsterActionSnapshot>.Fail(module.FailAction(scope, "QueryAction", ex)); }
                }
            }
            public MonsterResult<bool> Cancel(MonsterActionHandle handle) => Call("Cancel", () =>
            {
                if (scope.Action == null || !scope.Action.Handle.Equals(handle)) return MonsterResult<bool>.Fail(Error("StaleAction"));
                if (scope.Action.State != MonsterActionState.Running) return MonsterResult<bool>.Ok(false);
                module.EndAction(scope, MonsterActionState.Cancelled);
                return MonsterResult<bool>.Ok(true);
            });
        }
    }
}
