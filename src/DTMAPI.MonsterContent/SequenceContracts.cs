using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent.Hosting;

namespace DTMAPI.MonsterContent
{
    /// <summary>Finite ordered actions. Move offsets are relative to this visible execution's starting position.</summary>
    public sealed class MonsterSequence
    {
        public int RepeatCount { get; set; } = 1;
        public List<MonsterSequenceStep> Steps { get; set; } = new List<MonsterSequenceStep>();
        internal MonsterSequence Copy() => new MonsterSequence { RepeatCount = RepeatCount, Steps = Steps.Select(s => s.Copy()).ToList() };
    }
    public sealed class MonsterSequenceStep
    {
        public string Id { get; set; } = "";
        public string Action { get; set; } = "attack";
        public string AttackId { get; set; } = "";
        public string SkillId { get; set; } = "";
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        /// <summary>Budget for admission waiting and execution together, in positive game seconds.</summary>
        public float MaxGameSeconds { get; set; } = 10;
        public float MaxTravel { get; set; }
        internal MonsterSequenceStep Copy() => (MonsterSequenceStep)MemberwiseClone();
    }
    public enum MonsterSequenceState { Ready, Running, Completed, Stopped, Failed, Interrupted }
    public sealed class MonsterSequenceSnapshot
    {
        public MonsterSequenceSnapshot(MonsterSequenceState state, string stepId, int iteration, string reason, MonsterActionHandle? action)
        { State = state; StepId = stepId; Iteration = iteration; Reason = reason; Action = action; }
        public MonsterSequenceState State { get; }
        public string StepId { get; }
        public int Iteration { get; }
        public string Reason { get; }
        public MonsterActionHandle? Action { get; }
    }
    internal static class SequenceValidation
    {
        internal static MonsterDiagnostic? Validate(string owner, MonsterDefinition d)
        {
            MonsterDiagnostic Error(string field, string message) => new MonsterDiagnostic("InvalidSequence", message, owner, owner + ":" + d.LocalId, d.TemplateId, "Validate", "behavior.sequence" + field);
            if (d.Combat != null)
            {
                MonsterDiagnostic CombatError(string field, string message) => new MonsterDiagnostic("InvalidCombatPolicy", message, owner, owner + ":" + d.LocalId, d.TemplateId, "Validate", "behavior.combat" + field);
                if (d.Behavior != "configured-sequence") return CombatError("", "Combat policy requires configured-sequence mode.");
                if (d.Combat.Mode != "active" && d.Combat.Mode != "retaliate" && d.Combat.Mode != "passive") return CombatError(".mode", "Expected active, retaliate or passive.");
                if (!Interval(d.Combat.CheckIntervalGameSeconds)) return CombatError(".checkIntervalGameSeconds", "Expected game seconds in (0, infinity).");
                if (!Interval(d.Combat.RepeatIntervalGameSeconds)) return CombatError(".repeatIntervalGameSeconds", "Expected game seconds in (0, infinity).");
                if (!Interval(d.Combat.TargetWaitGameSeconds)) return CombatError(".targetWaitGameSeconds", "Expected game seconds in (0, infinity).");
                if (d.Combat.Mode != "retaliate" && d.Combat.TargetWaitGameSeconds != 5) return CombatError(".targetWaitGameSeconds", "A custom target wait is only used by retaliate mode.");
            }
            if (d.Behavior != "configured-sequence") return d.Sequence == null ? null : Error("", "Sequence requires configured-sequence mode.");
            var sequence = d.Sequence;
            if (sequence == null && d.Combat?.Mode == "passive") return null;
            if (sequence == null) return Error("", "Configure an explicit sequence.");
            if (sequence.RepeatCount < 1) return Error(".repeatCount", "Expected at least one repetition.");
            if (sequence.Steps == null || sequence.Steps.Count < 1) return Error(".steps", "Expected at least one ordered step.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < sequence.Steps.Count; i++)
            {
                var s = sequence.Steps[i]; string at = ".steps[" + i + "]";
                if (s == null) return Error(at, "Step is required.");
                if (!MonsterValidation.ValidId(s.Id) || !ids.Add(s.Id)) return Error(at + ".id", "Step ID must be valid and unique within the sequence.");
                if (!ContentValidation.Finite(s.MaxGameSeconds) || s.MaxGameSeconds <= 0) return Error(at + ".maxGameSeconds", "Expected a positive game-time budget.");
                if (s.Action == "attack")
                {
                    if (!string.IsNullOrEmpty(s.SkillId)) return Error(at + ".skillId", "An attack step cannot carry a skill ID.");
                    if (!MonsterValidation.ValidId(s.AttackId)) return Error(at + ".attackId", "Select an existing native attack ID.");
                    if (s.OffsetX != 0 || s.OffsetY != 0 || s.MaxTravel != 0) return Error(at, "An attack step cannot carry movement parameters.");
                }
                else if (s.Action == "move-to")
                {
                    if (!string.IsNullOrEmpty(s.SkillId)) return Error(at + ".skillId", "A move step cannot carry a skill ID.");
                    if (!string.IsNullOrEmpty(s.AttackId)) return Error(at + ".attackId", "A move step cannot carry an attack ID.");
                    if (!ContentValidation.Finite(s.OffsetX) || !ContentValidation.Finite(s.OffsetY)) return Error(at, "Move offsets must be finite.");
                    if (!ContentValidation.Finite(s.MaxTravel) || s.MaxTravel <= 0) return Error(at + ".maxTravel", "Expected travel limit in (0, infinity).");
                }
                else if (s.Action == "skill")
                {
                    if (!d.Skills.Any(skill => skill.Id == s.SkillId)) return Error(at + ".skillId", "Select a skill declared in this definition.");
                    if (!string.IsNullOrEmpty(s.AttackId) || s.OffsetX != 0 || s.OffsetY != 0 || s.MaxTravel != 0) return Error(at, "A skill step cannot carry attack or movement parameters.");
                }
                else return Error(at + ".action", "Only attack, move-to and skill are supported.");
            }
            return null;
        }
        private static bool Interval(float value) => ContentValidation.Finite(value) && value > 0;
    }
}
