using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DG.Tweening;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class InstanceScope
        {
            internal readonly Binding Binding;
            internal readonly IMonsterHost Host;
            internal readonly BattleSystem Battle;
            internal Monster? Logical;
            internal Monster? Summoner;
            internal MonsterHandle SummonerHandle;
            internal int NativeCallDepth;
            internal BastionExecution? Bastion;
            internal TrainingTargetLease? TrainingTarget;
            internal long TrainingMoveRevision;
            internal int NativeSkillDepth;
            internal NativeMimicryBody? MimicryBody;
            internal NativeShipState? Ship;
            internal InstanceScope? ShipParent;
            internal bool ShipDeathNotified;
            internal NianEncounter? Nian;
            internal NianTail? NianTail;
            internal bool NianFinalDeath, NianDeathStarted;
            internal readonly HealthPhaseState? HealthPhase;
            internal MonsterController? Controller;
            internal MonsterHandle Handle;
            // Persists across deferred/failed withdrawal, but only for this rental.
            internal MonsterHandle? SoundWithdrawalRental;
            internal bool Active, Initialized, Removed;
            internal bool DeadSoundHandled;
            internal string SpawnRuleId = "";
            internal long SpawnBatchGeneration;
            internal Vector2 RequestedPosition, SpawnOffset, ResolvedPosition;
            internal Vector2 PhysicalGroundPosition;
            internal MonsterPositionPolicy PositionPolicy;
            internal float PositionTolerance = 0.01f;
            internal MonsterPlacement? Placement;
            internal BehaviorRegistration? BehaviorRegistration;
            internal IMonsterBehavior? Behavior;
            internal BehaviorContext? BehaviorContext;
            internal ControlledAction? Action;
            internal long ActionSequence;
            internal double SkillClock;
            internal readonly Dictionary<string, double> SkillReadyAt = new Dictionary<string, double>(StringComparer.Ordinal);
            internal MonsterDiagnostic? ActionFailure;
            internal bool FaultPending, DrainingFault;
            internal int AuthorDepth;
            internal PresentationState? Presentation;
            internal readonly HashSet<Shot> Shots = new HashSet<Shot>();
            internal readonly HashSet<NativeSkillLease> NativeSkills = new HashSet<NativeSkillLease>();
            internal readonly HashSet<NativeBoomLease> NativeBooms = new HashSet<NativeBoomLease>();
            internal readonly HashSet<FollowingEffect> Effects = new HashSet<FollowingEffect>();
            internal readonly List<MonsterAttackBehaviour> AcquiredAttacks = new List<MonsterAttackBehaviour>();
            internal AttackDescriptor[]? AttackDescriptors;
            internal MonsterMover? AcquiredMover;
            internal MonsterMoverJump? JumpHelper;
            internal readonly HashSet<Tween> MovementTweens = new HashSet<Tween>();
            internal readonly List<GroupMembership> Groups = new List<GroupMembership>();
            internal bool PendingGroupRelease, PendingRemoveLogic;
            internal readonly List<Action> ExecutionReleases = new List<Action>();
            internal InstanceScope(Binding binding, IMonsterHost host) { Binding = binding; Host = host; Battle = DolocAPI.battleSystem; if (!binding.NativeChild && binding.Definition.Definition.HealthPhases != null) HealthPhase = new HealthPhaseState(binding.Definition.Definition.HealthPhases); }
        }
        public MonsterResult<MonsterInstanceSnapshot> Spawn(string owner, MonsterSpawnRequest request)
        {
            RequireThread();
            var definition = request == null ? null : definitions.Find(owner, request.DefinitionId);
            MonsterResult<MonsterInstanceSnapshot> Fail(string code, string message) => MonsterResult<MonsterInstanceSnapshot>.Fail(new MonsterDiagnostic(code, message, owner, request?.DefinitionId ?? "", definition?.Definition.TemplateId ?? "", "Spawn", recovery: "Use a creatable definition and a current ready room."));
            if (definition == null || definition.State != MonsterDefinitionState.Creatable || !bindings.TryGetValue(definition.NativeId, out var binding)) return Fail("DefinitionUnavailable", "Definition is missing, withdrawn or not creatable.");
            if (!context.Snapshot.IsWorldReady || DolocAPI.CurrentRoom == null || DolocAPI.battleSystem == null) return Fail("NativeNotReady", "No current WorldReady room.");
            if (float.IsNaN(request!.X) || float.IsNaN(request.Y) || float.IsInfinity(request.X) || float.IsInfinity(request.Y)) return Fail("InvalidPosition", "Coordinates must be finite.");
            IMonsterHost host = DolocAPI.CurrentRoom;
            var position = new Vector2(request.X, request.Y);
            if (!Enum.IsDefined(typeof(MonsterPositionPolicy), request.PositionPolicy) || !ContentValidation.Finite(request.PositionTolerance) || request.PositionTolerance < 0) return Fail("InvalidPositionPolicy", "Use a valid policy and a finite nonnegative tolerance.");
            var placementError = PlacementError(host, binding, position, false);
            if (placementError != null) return Fail("InvalidPosition", placementError);
            if (creating != null) return Fail("ReentrantSpawn", "Creation is already in progress.");
            try
            {
                var scope = CreateOwned(binding, host, position, Vector2.zero, true, request.PositionPolicy, request.PositionTolerance, "");
                return MonsterResult<MonsterInstanceSnapshot>.Ok(Snapshot(scope));
            }
            catch (Exception ex)
            {
                if (ex is SpawnFailure failure && definition.State != MonsterDefinitionState.RestartRequired) return MonsterResult<MonsterInstanceSnapshot>.Fail(failure.Diagnostic);
                return Fail(definition.State == MonsterDefinitionState.RestartRequired ? "RestartRequired" : ex.Message.StartsWith("StrictPositionMismatch", StringComparison.Ordinal) ? "StrictPositionMismatch" : "SpawnPostconditionFailed", ex.GetBaseException().Message);
            }
        }
        private void ValidateInstance(InstanceScope scope)
        {
            using var trace = OptimizationTrace.Begin(13);
            var monster = scope.Logical;
            var controller = scope.Controller;
            if (monster == null || controller == null || !scope.Host.DM_monster.AllMonsters.Contains(monster) || !ReferenceEquals(monster.Host, scope.Host) || !ReferenceEquals(monster.Controller, controller) || !ReferenceEquals(controller.Monster, monster)) throw new InvalidOperationException("Logical/host/controller postcondition failed.");
            if (controller.MonsterAI == null || controller.MonsterAI.GetType() != (UsesCode(scope) ? typeof(CodeAi) : scope.Binding.Template.Ai) || controller.mover == null || controller.Env != scope.Host.MonsterEnv || controller.Renderer == null || controller.Collider == null) throw new InvalidOperationException("AI/mover/environment/presentation postcondition failed.");
            foreach (var attack in monster.proto.AttackBehaviourProtos.Where(a => a.AttackId != MonsterAttackId.Unset))
                if (!controller.attackBehaviourManager.TryGetAttackBehaviour(attack.AttackId, out var instance) || instance == null || !instance.Validate()) throw new InvalidOperationException("Attack postcondition failed: " + attack.AttackId);
            foreach (var member in scope.Groups)
                if (!NativeAccess.Get<IList>(member.Group, "monsters").Contains(member.Ai)) throw new InvalidOperationException("Group membership has not completed.");
            var apc = NativeAccess.Get(scope.Battle, "_apcManager")!;
            var callbacks = NativeAccess.Get(apc, "OnFixedUpdateEvent") as Delegate;
            if (callbacks == null || !callbacks.GetInvocationList().Any(d => ReferenceEquals(d.Target, controller))) throw new InvalidOperationException("Battle membership postcondition failed.");
        }
        private MonsterInstanceSnapshot Snapshot(InstanceScope scope)
        {
            var monster = scope.Logical!;
            Vector2 position = scope.Controller != null ? scope.Controller.position2d : monster.position;
            return new MonsterInstanceSnapshot(scope.Handle, scope.Binding.Definition.Id, scope.Binding.Template.Proto.Name,
                ReadHealth(monster), monster.proto.Health, position.x, position.y) { Placement = scope.Placement,
                    Sequence = (scope.Behavior as IConfiguredBehavior)?.SequenceSnapshot, Combat = (scope.Behavior as IConfiguredBehavior)?.CombatSnapshot,
                    HealthPhase = scope.HealthPhase?.Snapshot() };
        }
        public MonsterResult<MonsterInstanceSnapshot> Instance(string owner, MonsterHandle handle)
        {
            RequireThread();
             { return handles.TryGetValue(handle, out var scope) && scope.Active && scope.Binding.Definition.Owner == owner
                ? MonsterResult<MonsterInstanceSnapshot>.Ok(Snapshot(scope))
                : MonsterResult<MonsterInstanceSnapshot>.Fail(new MonsterDiagnostic("StaleHandle", "Instance lease expired or belongs to another owner.", owner, stage: "Query")); }
        }
        public IReadOnlyList<MonsterInstanceSnapshot> Instances(string owner)
        { RequireThread(); return instances.Values.Where(s => !s.Binding.NativeChild && s.Binding.Definition.Owner == owner && s.Active && !s.Removed && s.Controller != null).Select(Snapshot).ToArray(); }
        public MonsterResult<bool> Despawn(string owner, MonsterHandle handle)
        {
            RequireThread();
            if (!handles.TryGetValue(handle, out var scope) || scope.Binding.Definition.Owner != owner || !scope.Active) return MonsterResult<bool>.Fail(new MonsterDiagnostic("StaleHandle", "Instance lease expired or belongs to another owner.", owner, stage: "Despawn"));
            try { Revoke(scope, true); return MonsterResult<bool>.Ok(true); }
            catch (Exception ex) { return MonsterResult<bool>.Fail(new MonsterDiagnostic("RestartRequired", ex.Message, owner, scope.Binding.Definition.Id, stage: "Despawn", recovery: "Restart the game.")); }
        }
        public MonsterResult<bool> CancelSequence(string owner, MonsterHandle handle)
        {
            RequireThread();
            if (!handles.TryGetValue(handle, out var scope) || scope.Binding.Definition.Owner != owner || !scope.Active)
                return MonsterResult<bool>.Fail(new MonsterDiagnostic("StaleHandle", "Instance lease expired or belongs to another owner.", owner, stage: "CancelSequence"));
            if (!(scope.Behavior is IConfiguredBehavior behavior))
                return MonsterResult<bool>.Fail(new MonsterDiagnostic("SequenceUnavailable", "Instance does not use a configured sequence.", owner, stage: "CancelSequence"));
            return NativeCall(scope, "CancelSequence", () => behavior.Cancel("Cancelled"));
        }
        private void RemoveInstances(Func<InstanceScope, bool> predicate)
        {
            var errors = new List<Exception>();
            foreach (var scope in instances.Values.Where(predicate).ToArray()) Attempt(errors, () => Revoke(scope, true));
            ThrowCleanup(errors);
        }
        private void Revoke(InstanceScope scope, bool removeLogic)
        {
            using var trace = OptimizationTrace.Begin(14);
            try { RevokeCore(scope, removeLogic); }
            catch (Exception ex)
            {
                scope.Binding.Definition.State = MonsterDefinitionState.RestartRequired;
                scope.Binding.Definition.Diagnostic = Error(scope.Binding.Definition, "RestartRequired", "revoke", ex.Message);
                throw;
            }
        }
        private void RevokeCore(InstanceScope scope, bool removeLogic)
        {
            scope.Active = false;
            scope.PendingRemoveLogic |= removeLogic;
            if (removeLogic && (scope.Ship != null || scope.Nian != null && ReferenceEquals(scope.Nian.Root, scope)))
            {
                // A failed root skill return must not leave its independent parts running.
                var errors = new List<Exception>();
                Attempt(errors, () => CancelActions(scope));
                Attempt(errors, () => RevokeShipParts(scope));
                if (scope.Nian != null) Attempt(errors, () => RevokeNianParts(scope.Nian));
                ThrowCleanup(errors);
            }
            else CancelActions(scope);
            // Native Call/skill creation still owns the stack. Keep exact logical/controller
            // records until its finalizer can unwind without resurrecting a pooled lease.
            if (scope.NativeCallDepth > 0 || scope.NativeSkillDepth > 0) return;
            using (OptimizationTrace.Begin(31)) if (!ReleaseGroups(scope)) return;
            Fault("Revoke");
            if (scope.Controller != null)
            {
                // A summoned native child uses its native pool, never its root definition alias.
                using (OptimizationTrace.Begin(32)) DolocAPI.EntitySystem.Recycle(scope.Controller, scope.Binding.Proto.Name);
                if (scope.Controller != null) throw new InvalidOperationException("Native recycle did not complete owned controller cleanup.");
            }
            if (scope.PendingRemoveLogic && scope.Logical != null)
            {
                using var removeTrace = OptimizationTrace.Begin(33);
                scope.Host.DM_monster.RemoveMonster(scope.Logical);
                instances.Remove(scope.Logical);
                // Native death owns a one-frame deferred removal. Keep its old logical host valid;
                // the controller link is already cleared, so that continuation cannot touch a reused lease.
                if (!scope.Logical.IsDead) scope.Logical.Host = null!;
                scope.Removed = true;
            }
        }
        // Cache type metadata only. Every cleanup pass reads the current tween
        // values again, including a retry after partial native cleanup failure.
        private static readonly ConditionalWeakTable<Type, FieldInfo[]> tweenFields = new ConditionalWeakTable<Type, FieldInfo[]>();
        private static FieldInfo[] FindTweenFields(Type type)
        {
            var fields = new List<FieldInfo>();
            for (Type? currentType = type; currentType != null && currentType != typeof(object); currentType = currentType.BaseType)
                foreach (var field in currentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                    if (typeof(Tween).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(MonsterAttackHelperTween)) fields.Add(field);
            return fields.ToArray();
        }
        private static void CancelTweens(object target)
        {
            foreach (var field in tweenFields.GetValue(target.GetType(), FindTweenFields))
            {
                if (typeof(Tween).IsAssignableFrom(field.FieldType))
                {
                    var tween = field.GetValue(target) as Tween;
                    if (tween != null) { CancelTween(tween); field.SetValue(target, null); }
                }
                else if (field.FieldType == typeof(MonsterAttackHelperTween) && field.GetValue(target) is object helper) CancelTweens(helper);
            }
        }
        private void CancelActions(InstanceScope scope)
        {
            using var trace = OptimizationTrace.Begin(30);
            scope.Active = false;
            StopBehavior(scope);
            var errors = new List<Exception>();
            Attempt(errors, () => EndAction(scope, MonsterActionState.Cancelled));
            if (IsSoundWithdrawal(scope))
                Attempt(errors, () => StopCommonSounds(r => ReferenceEquals(r.Scope, scope) && r.Rental.Equals(scope.Handle)));
            Attempt(errors, () => StopOwnedSound(scope));
            Attempt(errors, () => StopHummingSound(scope));
            foreach (var release in scope.ExecutionReleases.ToArray())
                Attempt(errors, () => { release(); scope.ExecutionReleases.Remove(release); });
            using (OptimizationTrace.Begin(38)) Attempt(errors, () => CancelShots(scope));
            Attempt(errors, () => CancelNativeSkills(scope));
            Attempt(errors, () => CancelNian(scope));
            Attempt(errors, () => CancelTrafficLight(scope));
            Attempt(errors, () => CancelBastion(scope));
            Attempt(errors, () => CancelTraining(scope));
            using (OptimizationTrace.Begin(39)) Attempt(errors, () => CancelFollowingEffects(scope));
            var controller = scope.Controller;
            if (controller == null) { ThrowCleanup(errors); return; }
            Attempt(errors, () => NativeAccess.Set(controller, "_shouldRunAI", false));
            Attempt(errors, () => NativeAccess.Set(controller, "animationEvtCallback", null));
            // OnHurtResumed includes decorator subscriptions installed once in __Init.
            // They belong to the pooled controller, not this execution lease.
            using (OptimizationTrace.Begin(34))
                foreach (var attack in scope.AcquiredAttacks.Concat(controller.attackBehaviourManager.AttackBehaviours).Distinct()) Attempt(errors, () => CancelTweens(attack));
            Attempt(errors, () => NativeAccess.Set(controller.attackBehaviourManager, "_callback", null));
            Attempt(errors, () => NativeAccess.Set(controller.attackBehaviourManager, "_currentBehaviour", null));
            foreach (var box in controller.GetComponentsInChildren<PhysicalDamageBox>(true)) Attempt(errors, () => { box.OnAttackedCallback = null; box.Enabled = false; box.ClearBuffer(); });
            using (OptimizationTrace.Begin(37)) Attempt(errors, () => CancelMovement(scope));
            foreach (var sight in controller.GetComponentsInChildren<MonsterSight>(true)) Attempt(errors, sight.ClearEnemy);
            var renderer = controller.Renderer;
            if (renderer != null)
            {
                using var rendererTrace = OptimizationTrace.Begin(35);
                Attempt(errors, () => {
                NativeAccess.Set(renderer, "onAnimationDone", null); NativeAccess.Set(renderer, "shouldCheckAnimationDone", false);
                var shiner = NativeAccess.Get(renderer, "shiner");
                if (shiner != null && NativeAccess.Get(shiner, "coroutine") is Coroutine coroutine)
                {
                    DolocAPI.StopCoroutine(coroutine); NativeAccess.Set(shiner, "coroutine", null);
                    renderer.SpriteRenderer.sharedMaterial = NativeAccess.Get<Material>(shiner, "originalMat");
                    renderer.SpriteRenderer.color = NativeAccess.Get<Color>(shiner, "originColor");
                }
                var shaker = NativeAccess.Get(renderer, "shaker");
                if (shaker != null && NativeAccess.Get(shaker, "tween") is Tween)
                { CancelTweens(shaker); NativeAccess.Get<Transform>(shaker, "transform").position = NativeAccess.Get<Vector3>(shaker, "originalPos"); }
                renderer.Animator.Rebind();
                });
            }
            ThrowCleanup(errors);
        }
        private void CompleteRecycle(InstanceScope scope, MonsterController controller)
        {
            using var trace = OptimizationTrace.Begin(36);
            using (OptimizationTrace.Begin(40)) UnbindPresentation(scope);
            using (OptimizationTrace.Begin(41)) NativeAccess.ClearCollection(controller.attackBehaviourManager, "attackBehaviours");
            using (OptimizationTrace.Begin(42))
            {
                var counter = NativeAccess.Get(controller.attackBehaviourManager, "counter")!;
                NativeAccess.Set(controller.attackBehaviourManager, "counter", Activator.CreateInstance(counter.GetType()));
            }
            scope.AcquiredAttacks.Clear();
            if (controller.Decorator is MonsterDecoratorSpaceShip ship)
            { NativeAccess.Set(ship, "_bastion01", null); NativeAccess.Set(ship, "_bastion02", null); }
            scope.MimicryBody = null;
            scope.Bastion = null;
            scope.TrainingTarget = null;
            scope.Action = null; scope.SkillReadyAt.Clear();
            scope.AttackDescriptors = null;
            scope.AcquiredMover = null; scope.JumpHelper = null; scope.MovementTweens.Clear();
            NativeAccess.SetProperty(controller, "Env", null); NativeAccess.SetProperty(controller, "mover", null); NativeAccess.SetProperty(controller, "MonsterAI", null);
            NativeAccess.Set(controller, "_battleSystem", null);
            controllers.Remove(controller); scope.Controller = null; scope.Initialized = false; scope.Active = false;
            handles.Remove(scope.Handle);
            if (scope.Removed || scope.Logical?.IsDead == true) { if (scope.Logical != null) instances.Remove(scope.Logical); }
        }
    }
}
