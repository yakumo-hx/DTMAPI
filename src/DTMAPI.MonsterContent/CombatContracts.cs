using System;

namespace DTMAPI.MonsterContent
{
    /// <summary>Opt-in trigger for a configured finite sequence; omission means run once.</summary>
    public sealed class MonsterCombatPolicy
    {
        public string Mode { get; set; } = "active";
        public float CheckIntervalGameSeconds { get; set; } = 0.5f;
        public float RepeatIntervalGameSeconds { get; set; } = 2;
        /// <summary>Retaliation only: maximum game time to acquire a native target after a hit.</summary>
        public float TargetWaitGameSeconds { get; set; } = 5;
        internal MonsterCombatPolicy Copy() => (MonsterCombatPolicy)MemberwiseClone();
    }

    /// <summary>Opaque identity within one visible execution, not a persistent target or native object.</summary>
    public readonly struct MonsterTargetHandle : IEquatable<MonsterTargetHandle>
    {
        public MonsterTargetHandle(MonsterHandle instance, long revision) { Instance = instance; Revision = revision; }
        public MonsterHandle Instance { get; }
        public long Revision { get; }
        public bool Equals(MonsterTargetHandle other) => Instance.Equals(other.Instance) && Revision == other.Revision;
        public override bool Equals(object? obj) => obj is MonsterTargetHandle other && Equals(other);
        public override int GetHashCode() => Instance.GetHashCode() ^ Revision.GetHashCode();
    }

    public sealed class MonsterTargetSnapshot
    {
        public MonsterTargetSnapshot(MonsterTargetHandle handle, float x, float y) { Handle = handle; X = x; Y = y; }
        public MonsterTargetHandle Handle { get; }
        public float X { get; }
        public float Y { get; }
    }

    public enum MonsterCombatState { WaitingTarget, Running, WaitingInterval, Stopped, Failed, WaitingHit, Passive }
    public sealed class MonsterCombatSnapshot
    {
        public MonsterCombatSnapshot(MonsterCombatState state, long round, float remaining, string reason, MonsterTargetHandle? target)
        { State = state; Round = round; RemainingGameSeconds = remaining; Reason = reason; Target = target; }
        public MonsterCombatState State { get; }
        public long Round { get; }
        public float RemainingGameSeconds { get; }
        public string Reason { get; }
        public MonsterTargetHandle? Target { get; }
    }
}
