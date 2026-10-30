using System;
using DTMAPI.MonsterContent;

namespace DTMAPI.Modules.Monsters
{
    // Native fixed-time decisions around the existing finite runner. No timer callbacks,
    // target search, action advancement or independent cancellation owner.
    internal sealed class CombatSequence : IConfiguredBehavior
    {
        private readonly MonsterSequence? sequence;
        private readonly MonsterCombatPolicy policy;
        private IMonsterBehaviorContext? context;
        private ConfiguredSequence? round;
        private MonsterTargetHandle? target;
        private MonsterCombatState state = MonsterCombatState.WaitingTarget;
        private long roundNumber;
        private float remaining;
        private float checkRemaining;
        private string reason = "";
        private bool transitionedBeforeAction;

        internal CombatSequence(MonsterSequence? sequence, MonsterCombatPolicy policy)
        {
            this.sequence = sequence?.Copy(); this.policy = policy.Copy();
            state = policy.Mode == "passive" ? MonsterCombatState.Passive
                : policy.Mode == "retaliate" ? MonsterCombatState.WaitingHit : MonsterCombatState.WaitingTarget;
        }
        public MonsterSequenceSnapshot? SequenceSnapshot => round?.Snapshot;
        public MonsterCombatSnapshot CombatSnapshot => new MonsterCombatSnapshot(state, roundNumber, remaining, reason, target);
        public void Start(IMonsterBehaviorContext value) { context = value; }
        private bool Terminal => state == MonsterCombatState.Stopped || state == MonsterCombatState.Failed;
        // Called only after native damage has reduced health on this live execution.
        // No callbacks, actions or queue: further hits cannot reset a pending window.
        internal void OnEffectiveHit()
        {
            if (context == null || state != MonsterCombatState.WaitingHit) return;
            state = MonsterCombatState.WaitingTarget; reason = "EffectiveHit";
            remaining = policy.TargetWaitGameSeconds; checkRemaining = 0;
        }
        // Native hurt lock skips CodeAi.OnFixedUpdate. Consume waiting budgets on that
        // same controller fixed tick without starting a round inside the hurt callback.
        public void TickHurtWait(float gameSeconds)
        {
            if (context == null || gameSeconds <= 0) return;
            if (state == MonsterCombatState.Running)
            {
                round!.TickHurtWait(gameSeconds);
                ObserveRound();
                return;
            }
            if (policy.Mode != "retaliate") return;
            if (state != MonsterCombatState.WaitingTarget && state != MonsterCombatState.WaitingInterval) return;
            remaining = Math.Max(0, remaining - gameSeconds);
            checkRemaining = Math.Max(0, checkRemaining - gameSeconds);
            if (remaining > 0) return;
            if (state == MonsterCombatState.WaitingTarget) Wait("TargetWaitExpired");
            else { state = MonsterCombatState.WaitingHit; reason = "AwaitingHit"; }
        }

        public void BeforeAction(float gameSeconds)
        {
            if (state != MonsterCombatState.Running || context == null || gameSeconds <= 0) return;
            // An explicit action cancel or timeout wins over simultaneous target loss.
            var action = round!.Snapshot.Action;
            if (action.HasValue)
            {
                var current = context.GetAction(action.Value);
                if (!current.Success) { round.Fail(current.Diagnostic!.Code); ObserveRound(); return; }
                if (current.Value!.State != MonsterActionState.Running && current.Value.State != MonsterActionState.Completed && current.Value.State != MonsterActionState.PhaseChanged)
                {
                    round.Tick(gameSeconds);
                    ObserveRound();
                    transitionedBeforeAction = state == MonsterCombatState.WaitingInterval;
                    return;
                }
            }
            var currentTarget = context.GetTarget();
            if (!currentTarget.Success)
            {
                var cancelled = round.Cancel("TargetQueryFailed");
                Finish(MonsterCombatState.Failed, cancelled.Success ? currentTarget.Diagnostic!.Code : cancelled.Diagnostic!.Code);
                return;
            }
            if (currentTarget.Value != null && target.HasValue && currentTarget.Value.Handle.Equals(target.Value)) return;
            string changed = currentTarget.Value == null ? "TargetLost" : "TargetChanged";
            var stopped = round.Cancel(changed);
            if (!stopped.Success) { Finish(MonsterCombatState.Failed, stopped.Diagnostic!.Code); return; }
            Wait(changed);
            transitionedBeforeAction = true;
        }

        public void Tick(float gameSeconds)
        {
            if (context == null || Terminal || gameSeconds <= 0) return;
            if (state == MonsterCombatState.Passive || state == MonsterCombatState.WaitingHit) return;
            if (transitionedBeforeAction) { transitionedBeforeAction = false; return; }
            if (state == MonsterCombatState.Running)
            {
                round!.Tick(gameSeconds);
                ObserveRound();
                return;
            }
            remaining = Math.Max(0, remaining - gameSeconds);
            if (policy.Mode == "retaliate" && state == MonsterCombatState.WaitingTarget)
            {
                if (remaining <= 0) { Wait("TargetWaitExpired"); return; }
                checkRemaining = Math.Max(0, checkRemaining - gameSeconds);
                if (checkRemaining > 0) return;
            }
            else
            {
                if (remaining > 0) return;
                if (policy.Mode == "retaliate")
                { state = MonsterCombatState.WaitingHit; reason = "AwaitingHit"; return; }
            }
            // At most one check or round start per tick. Overshoot is discarded; no catch-up loop.
            var found = context.GetTarget();
            if (!found.Success) { Finish(MonsterCombatState.Failed, found.Diagnostic!.Code); return; }
            if (found.Value == null)
            {
                state = MonsterCombatState.WaitingTarget; reason = "TargetUnavailable";
                if (policy.Mode == "retaliate") checkRemaining = policy.CheckIntervalGameSeconds;
                else remaining = policy.CheckIntervalGameSeconds;
                return;
            }
            target = found.Value.Handle;
            remaining = 0;
            round = new ConfiguredSequence(sequence!);
            roundNumber++;
            state = MonsterCombatState.Running; reason = "";
            round.Start(context);
            ObserveRound(); // Start failure is terminal, not a new polling loop.
        }
        private void ObserveRound()
        {
            var result = round!.Snapshot;
            if (result.State == MonsterSequenceState.Completed) Wait("Completed");
            else if (result.State == MonsterSequenceState.Interrupted) Wait(result.Reason);
            else if (result.State == MonsterSequenceState.Stopped) Finish(MonsterCombatState.Stopped, result.Reason);
            else if (result.State == MonsterSequenceState.Failed) Finish(MonsterCombatState.Failed, result.Reason);
        }
        private void Wait(string detail)
        {
            state = MonsterCombatState.WaitingInterval; reason = detail;
            target = null; remaining = policy.RepeatIntervalGameSeconds;
        }
        private void Finish(MonsterCombatState value, string detail)
        { state = value; reason = detail; remaining = 0; target = null; }
        public MonsterResult<bool> Cancel(string detail)
        {
            if (Terminal) return MonsterResult<bool>.Ok(false);
            Finish(MonsterCombatState.Stopped, detail);
            var cancelled = round?.Cancel(detail);
            if (cancelled != null && !cancelled.Success) { Finish(MonsterCombatState.Failed, cancelled.Diagnostic!.Code); return cancelled; }
            return MonsterResult<bool>.Ok(true);
        }
        public void Stop()
        {
            context = null;
            round?.Stop();
            if (!Terminal) Finish(MonsterCombatState.Stopped, "ExecutionEnded");
        }
    }
}
