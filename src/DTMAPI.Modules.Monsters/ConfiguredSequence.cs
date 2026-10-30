using System;
using DTMAPI.MonsterContent;

namespace DTMAPI.Modules.Monsters
{
    internal interface IConfiguredBehavior : IMonsterBehavior
    {
        MonsterSequenceSnapshot? SequenceSnapshot { get; }
        MonsterCombatSnapshot? CombatSnapshot { get; }
        void BeforeAction(float gameSeconds);
        void TickHurtWait(float gameSeconds);
        MonsterResult<bool> Cancel(string reason);
    }
    // Decisions only: advancement, timeouts, pause and cleanup belong to the same
    // controlled native actions used by ordinary code behaviors. No second clock.
    internal sealed class ConfiguredSequence : IConfiguredBehavior
    {
        private readonly MonsterSequence sequence;
        private IMonsterBehaviorContext? context;
        private float originX, originY, waiting;
        private int index, iteration = 1;
        private MonsterActionHandle? action;
        private MonsterSequenceState state;
        private string reason = "";
        internal ConfiguredSequence(MonsterSequence sequence) { this.sequence = sequence.Copy(); }
        internal MonsterSequenceSnapshot Snapshot => new MonsterSequenceSnapshot(state, sequence.Steps[index].Id, iteration, reason, action);
        public MonsterSequenceSnapshot SequenceSnapshot => Snapshot;
        public MonsterCombatSnapshot? CombatSnapshot => null;
        public void BeforeAction(float gameSeconds) { }
        public void TickHurtWait(float gameSeconds)
        {
            // The native hurt branch skips AI, including its recovery tick. Only
            // unaccepted steps wait here; accepted actions use ControlledFixedPrefix.
            if (state != MonsterSequenceState.Running || context == null || action.HasValue || gameSeconds <= 0) return;
            ConsumeAdmissionWait(gameSeconds, "HurtLocked");
        }
        public void Start(IMonsterBehaviorContext value)
        {
            context = value;
            var self = value.GetSelf();
            if (!self.Success) { Finish(MonsterSequenceState.Failed, self.Diagnostic!.Code); return; }
            originX = self.Value!.X; originY = self.Value.Y;
            state = MonsterSequenceState.Running;
        }
        public void Tick(float gameSeconds)
        {
            if (state != MonsterSequenceState.Running || context == null || gameSeconds <= 0) return;
            if (action.HasValue)
            {
                var result = context.GetAction(action.Value);
                if (!result.Success) { Finish(MonsterSequenceState.Failed, result.Diagnostic!.Code); return; }
                var current = result.Value!.State;
                if (current == MonsterActionState.Running) return;
                if (current == MonsterActionState.PhaseChanged) { action = null; waiting = 0; reason = "PhaseChanged"; return; }
                if (current == MonsterActionState.Interrupted)
                {
                    var cause = result.Value.InterruptionReason;
                    bool normal = cause == MonsterActionInterruptionReason.TargetInvalidated || cause == MonsterActionInterruptionReason.Hurt || cause == MonsterActionInterruptionReason.AimUnavailable;
                    Finish(normal ? MonsterSequenceState.Interrupted : MonsterSequenceState.Failed, "Interrupted:" + cause);
                    return; // Retire this finite round, without replaying or skipping the step.
                }
                if (current != MonsterActionState.Completed)
                { Finish(current == MonsterActionState.Cancelled ? MonsterSequenceState.Stopped : MonsterSequenceState.Failed, current.ToString()); return; }
                if (index + 1 == sequence.Steps.Count && iteration == sequence.RepeatCount)
                { Finish(MonsterSequenceState.Completed, ""); return; }
                action = null; waiting = 0;
                if (++index == sequence.Steps.Count) { index = 0; iteration++; }
                return; // At most one admission/transition per native fixed tick.
            }
            var step = sequence.Steps[index];
            float remaining = step.MaxGameSeconds - waiting;
            if (remaining <= 0) { Finish(MonsterSequenceState.Failed, "AdmissionTimedOut:" + reason); return; }
            var started = step.Action == "attack"
                ? context.Attack(step.AttackId, remaining)
                : step.Action == "skill" ? context.UseSkill(step.SkillId, remaining)
                : context.MoveTo(originX + step.OffsetX, originY + step.OffsetY, remaining, step.MaxTravel);
            if (started.Success) { action = started.Value; reason = ""; return; }
            reason = started.Diagnostic!.Code;
            switch (reason)
            {
                case "GamePaused": return;
                case "ActionBusy": case "HurtLocked": case "TargetUnavailable": case "TargetOutOfRange": case "AttackCoolingDown": case "SkillCoolingDown":
                    ConsumeAdmissionWait(gameSeconds, reason); return;
                default: Finish(MonsterSequenceState.Failed, reason); return;
            }
        }
        private void ConsumeAdmissionWait(float gameSeconds, string detail)
        {
            float limit = sequence.Steps[index].MaxGameSeconds;
            waiting = Math.Min(limit, waiting + gameSeconds);
            reason = detail;
            if (waiting >= limit) Finish(MonsterSequenceState.Failed, "AdmissionTimedOut:" + detail);
        }
        private void Finish(MonsterSequenceState value, string detail) { state = value; reason = detail; }
        internal void Fail(string detail) => Finish(MonsterSequenceState.Failed, detail);
        public MonsterResult<bool> Cancel(string detail)
        {
            if (state == MonsterSequenceState.Stopped || state == MonsterSequenceState.Failed || state == MonsterSequenceState.Completed || state == MonsterSequenceState.Interrupted)
                return MonsterResult<bool>.Ok(false);
            Finish(MonsterSequenceState.Stopped, detail); // Retire decisions before native cleanup can call back.
            if (context != null && action.HasValue)
            {
                var cancelled = context.Cancel(action.Value);
                if (!cancelled.Success) { Finish(MonsterSequenceState.Failed, cancelled.Diagnostic!.Code); return cancelled; }
            }
            return MonsterResult<bool>.Ok(true);
        }
        public void Stop()
        {
            // Stop is called after context revocation. Common instance cleanup owns
            // cancellation and can retain failed resources for RestartRequired.
            context = null;
            if (state == MonsterSequenceState.Running || state == MonsterSequenceState.Ready) Finish(MonsterSequenceState.Stopped, "ExecutionEnded");
        }
    }
}
