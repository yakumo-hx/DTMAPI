using System;
using System.Collections.Generic;

namespace DTMAPI.MonsterContent
{
    /// <summary>One object per visible execution. Stop runs once, including after a failed Start.</summary>
    public interface IMonsterBehavior
    {
        void Start(IMonsterBehaviorContext context);
        void Tick(float gameSeconds);
        void Stop();
    }
    public interface IMonsterBehaviorContext
    {
        MonsterResult<MonsterInstanceSnapshot> GetSelf();
        MonsterResult<IReadOnlyList<MonsterAttackOption>> GetAttacks();
        /// <summary>Current-room active target chosen by native sight. Success with null means no target.</summary>
        MonsterResult<MonsterTargetSnapshot?> GetTarget();
        MonsterResult<MonsterActionHandle> Attack(string attackId, float maxGameSeconds);
        /// <summary>Run a definition-local skill. Completion waits for recovery and all volley projectiles.</summary>
        MonsterResult<MonsterActionHandle> UseSkill(string skillId, float maxGameSeconds);
        MonsterResult<MonsterActionHandle> MoveTo(float x, float y, float maxGameSeconds, float maxTravel);
        MonsterResult<MonsterActionSnapshot> GetAction(MonsterActionHandle handle);
        MonsterResult<bool> Cancel(MonsterActionHandle handle);
    }
    public enum MonsterActionState { Running, Completed, Cancelled, TimedOut, TravelLimit, Interrupted, Failed, PhaseChanged }
    /// <summary>Cause of an interrupted action. None or an unknown value grants no automatic next round.</summary>
    public enum MonsterActionInterruptionReason { None, TargetInvalidated, Hurt, AimUnavailable, ExternalDisplacement, NativeAttackStopped }
    public readonly struct MonsterActionHandle : IEquatable<MonsterActionHandle>
    {
        internal MonsterActionHandle(MonsterHandle instance, long sequence) { Instance = instance; Sequence = sequence; }
        public MonsterHandle Instance { get; }
        public long Sequence { get; }
        public bool Equals(MonsterActionHandle other) => Instance.Equals(other.Instance) && Sequence == other.Sequence;
        public override bool Equals(object? obj) => obj is MonsterActionHandle other && Equals(other);
        public override int GetHashCode() => Instance.GetHashCode() ^ Sequence.GetHashCode();
        public override string ToString() => Instance + "/" + Sequence;
    }
    public sealed class MonsterAttackOption
    {
        public MonsterAttackOption(string id, bool available, bool targetInRange, float cooldownSeconds)
        { Id = id; Available = available; TargetInRange = targetInRange; CooldownSeconds = cooldownSeconds; }
        public string Id { get; }
        public bool Available { get; }
        public bool TargetInRange { get; }
        public float CooldownSeconds { get; }
    }
    public sealed class MonsterActionSnapshot
    {
        public MonsterActionSnapshot(MonsterActionHandle handle, MonsterActionState state, MonsterActionInterruptionReason interruptionReason, float elapsed, float travel, float x, float y, float targetX, float targetY)
        { Handle = handle; State = state; InterruptionReason = interruptionReason; ElapsedGameSeconds = elapsed; Travel = travel; X = x; Y = y; ResolvedTargetX = targetX; ResolvedTargetY = targetY; }
        public MonsterActionHandle Handle { get; }
        public MonsterActionState State { get; }
        public MonsterActionInterruptionReason InterruptionReason { get; }
        public float ElapsedGameSeconds { get; }
        public float Travel { get; }
        public float X { get; }
        public float Y { get; }
        public float ResolvedTargetX { get; }
        public float ResolvedTargetY { get; }
    }
}
