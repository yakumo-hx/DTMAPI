using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent.Hosting;

namespace DTMAPI.MonsterContent
{
    public enum MonsterPhaseTransition { CancelCurrent, FinishCurrent }
    public sealed class MonsterHealthPhases
    {
        public MonsterPhaseTransition Transition { get; set; } = MonsterPhaseTransition.CancelCurrent;
        public List<MonsterHealthPhase> Stages { get; set; } = new List<MonsterHealthPhase>();
        internal MonsterHealthPhases Copy() => new MonsterHealthPhases { Transition = Transition, Stages = Stages.Select(s => s.Copy()).ToList() };
    }
    public sealed class MonsterHealthPhase
    {
        public string Id { get; set; } = "";
        public float AtOrBelow { get; set; } = .5f;
        public float SpeedMultiplier { get; set; } = 1;
        public float DamageMultiplier { get; set; } = 1;
        internal MonsterHealthPhase Copy() => (MonsterHealthPhase)MemberwiseClone();
    }
    public sealed class MonsterHealthPhaseSnapshot
    {
        internal MonsterHealthPhaseSnapshot(int index, int reached, string id, string pendingId, int revision, float speed, float damage)
        { Index = index; ReachedIndex = reached; Id = id; PendingId = pendingId; Revision = revision; SpeedMultiplier = speed; DamageMultiplier = damage; }
        /// <summary>Zero is the implicit base stage; stages are one-based.</summary>
        public int Index { get; }
        /// <summary>Deepest consumed threshold, including a pending transition.</summary>
        public int ReachedIndex { get; }
        public string Id { get; }
        public string PendingId { get; }
        public int Revision { get; }
        public float SpeedMultiplier { get; }
        public float DamageMultiplier { get; }
    }
    // One state per logical monster. Controller/action recreation must never recreate it.

    internal static class HealthPhaseValidation
    {
        internal static MonsterDiagnostic? Validate(string owner, MonsterDefinition definition)
        {
            var plan = definition.HealthPhases;
            if (plan == null) return null;
            MonsterDiagnostic Error(string field, string message) => new MonsterDiagnostic("InvalidHealthPhases", message, owner,
                owner + ":" + definition.LocalId, definition.TemplateId, "Validate", "behavior.healthPhases" + field);
            if (definition.Behavior == "inherit-native") return Error("", "Health phases require configured-sequence or code-provider behavior.");
            if (!Enum.IsDefined(typeof(MonsterPhaseTransition), plan.Transition)) return Error(".transition", "Select CancelCurrent or FinishCurrent.");
            if (plan.Stages == null || plan.Stages.Count < 1) return Error(".stages", "Expected one or more thresholds after the implicit base stage.");
            var ids = new HashSet<string>(StringComparer.Ordinal); float previous = 1;
            for (int i = 0; i < plan.Stages.Count; i++)
            {
                var stage = plan.Stages[i]; string at = ".stages[" + i + "]";
                if (stage == null || !MonsterValidation.ValidId(stage.Id) || !ids.Add(stage.Id)) return Error(at + ".id", "Expected a unique valid stage ID.");
                if (!ContentValidation.Finite(stage.AtOrBelow) || stage.AtOrBelow <= 0 || stage.AtOrBelow >= previous) return Error(at + ".atOrBelow", "Thresholds must be strictly decreasing in (0,1).");
                previous = stage.AtOrBelow;
                if (!In(stage.SpeedMultiplier)) return Error(at + ".speedMultiplier", "Expected a multiplier in (0, infinity).");
                if (!In(stage.DamageMultiplier)) return Error(at + ".damageMultiplier", "Expected a multiplier in (0, infinity).");

            }
            return null;
        }
        private static bool In(float value) => ContentValidation.Finite(value) && value > 0;
    }
}
