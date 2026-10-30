using System;
using System.Collections.Generic;
using DTMAPI.MonsterContent.Hosting;

namespace DTMAPI.MonsterContent
{
    /// <summary>Definition-local skill with exactly one matching parameter block.</summary>
    public sealed class MonsterSkill
    {
        public string Id { get; set; } = "";
        public string Kind { get; set; } = "scatter";
        public MonsterScatter? Scatter { get; set; }
        public MonsterDash? Dash { get; set; }
        public MonsterActionAnimation? Animation { get; set; }
        internal MonsterSkill Copy() => new MonsterSkill { Id = Id, Kind = Kind, Scatter = Scatter?.Copy(), Dash = Dash?.Copy(), Animation = Animation?.Copy() };
    }
    /// <summary>A bounded straight dash. Duration is Distance / Speed; walls truncate it.</summary>
    public sealed class MonsterDash
    {
        public float Distance { get; set; } = 6;
        public float Speed { get; set; } = 8;
        public float HitStartFraction { get; set; } = 0;
        public float HitEndFraction { get; set; } = 1;
        public float Damage { get; set; } = 10;
        public float Range { get; set; } = 10;
        public float WindupGameSeconds { get; set; } = .5f;
        public float RecoveryGameSeconds { get; set; } = .3f;
        public float CooldownGameSeconds { get; set; } = 2;
        internal MonsterDash Copy() => (MonsterDash)MemberwiseClone();
    }
    /// <summary>A single simultaneous fan using a template's existing linear projectile.</summary>
    public sealed class MonsterScatter
    {
        public string ProjectileAttackId { get; set; } = "";
        public int BulletCount { get; set; } = 5;
        /// <summary>Total included angle. One bullet always uses the center direction.</summary>
        public float SpreadDegrees { get; set; } = 60;
        public float BulletSpeed { get; set; } = 6;
        public float BulletLifetimeGameSeconds { get; set; } = 2;
        /// <summary>Raw native attack value, without critical hits. Zero makes no damage call.</summary>
        public float Damage { get; set; } = 10;
        public float Range { get; set; } = 10;
        public float WindupGameSeconds { get; set; } = .5f;
        public float RecoveryGameSeconds { get; set; } = .3f;
        /// <summary>Per instance and skill, counted from successful admission, including cancellation.</summary>
        public float CooldownGameSeconds { get; set; } = 2;
        internal MonsterScatter Copy() => (MonsterScatter)MemberwiseClone();
    }
    internal static class SkillValidation
    {
        internal static MonsterDiagnostic? Validate(string owner, MonsterDefinition definition)
        {
            MonsterDiagnostic Error(string field, string message) => new MonsterDiagnostic("InvalidSkill", message, owner, owner + ":" + definition.LocalId, definition.TemplateId, "Validate", "behavior.skills" + field);
            var skills = definition.Skills;
            if (skills == null) return Error("", "Skills must be a list.");
            if (skills.Count > 0 && definition.Behavior == "inherit-native") return Error("", "Skills require a controlled behavior mode.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < skills.Count; i++)
            {
                var skill = skills[i]; string at = "[" + i + "]";
                if (skill == null) return Error(at, "Skill is required.");
                if (!MonsterValidation.ValidId(skill.Id) || !ids.Add(skill.Id)) return Error(at + ".id", "Skill ID must be valid and unique within its definition.");
                var animationError = ActionAnimationValidation.Validate(owner, definition, skill, "behavior.skills" + at);
                if (animationError != null) return animationError;
                if (skill.Kind == "dash")
                {
                    if (skill.Dash == null || skill.Scatter != null) return Error(at + ".dash", "Dash requires only its matching parameter block.");
                    var d = skill.Dash; at += ".dash";
                    if (!Positive(d.Distance)) return Error(at + ".distance", "Expected a positive finite distance.");
                    if (!Positive(d.Speed)) return Error(at + ".speed", "Expected a positive finite speed.");
                    if (!In(d.HitStartFraction, 0, 1) || !In(d.HitEndFraction, 0, 1) || d.HitStartFraction >= d.HitEndFraction) return Error(at + ".hitEndFraction", "Expected 0 <= hitStartFraction < hitEndFraction <= 1.");
                    if (!Nonnegative(d.Damage)) return Error(at + ".damage", "Expected a nonnegative finite raw damage.");
                    if (!Positive(d.Range)) return Error(at + ".range", "Expected a positive finite targeting range.");
                    if (!Nonnegative(d.WindupGameSeconds)) return Error(at + ".windupGameSeconds", "Expected a nonnegative finite windup.");
                    if (!Nonnegative(d.RecoveryGameSeconds)) return Error(at + ".recoveryGameSeconds", "Expected a nonnegative finite recovery.");
                    if (!Positive(d.CooldownGameSeconds)) return Error(at + ".cooldownGameSeconds", "Expected a positive finite cooldown.");
                    continue;
                }
                if (skill.Kind != "scatter") return Error(at + ".kind", "Expected scatter or dash.");
                var s = skill.Scatter;
                if (s == null || skill.Dash != null) return Error(at + ".scatter", "Scatter requires only its matching parameter block.");
                at += ".scatter";
                if (!MonsterValidation.ValidId(s.ProjectileAttackId)) return Error(at + ".projectileAttackId", "Select a template linear projectile attack.");
                if (s.BulletCount < 1) return Error(at + ".bulletCount", "Expected a positive bullet count.");
                if (!Nonnegative(s.SpreadDegrees)) return Error(at + ".spreadDegrees", "Expected a nonnegative finite total angle.");
                if (!Positive(s.BulletSpeed)) return Error(at + ".bulletSpeed", "Expected a positive finite speed.");
                if (!Positive(s.BulletLifetimeGameSeconds)) return Error(at + ".bulletLifetimeGameSeconds", "Expected a positive finite lifetime.");
                if (!Nonnegative(s.Damage)) return Error(at + ".damage", "Expected a nonnegative finite raw damage.");
                if (!Positive(s.Range)) return Error(at + ".range", "Expected a positive finite targeting range.");
                if (!Nonnegative(s.WindupGameSeconds)) return Error(at + ".windupGameSeconds", "Expected a nonnegative finite windup.");
                if (!Nonnegative(s.RecoveryGameSeconds)) return Error(at + ".recoveryGameSeconds", "Expected a nonnegative finite recovery.");
                if (!Positive(s.CooldownGameSeconds)) return Error(at + ".cooldownGameSeconds", "Expected a positive finite cooldown.");
            }
            return null;
        }
        private static bool Positive(float value) => ContentValidation.Finite(value) && value > 0;
        private static bool Nonnegative(float value) => ContentValidation.Finite(value) && value >= 0;
        private static bool In(float value, float min, float max) => ContentValidation.Finite(value) && value >= min && value <= max;
    }
}
