using System;

namespace DTMAPI.MonsterContent
{
    /// <summary>Static definition geometry. The native root still owns facing and animation.</summary>
    public sealed class MonsterBodyParameters
    {
        public float? Scale { get; set; }
        public int? SpawnVolume { get; set; }
        public float? SpawnOffsetX { get; set; }
        public float? SpawnOffsetY { get; set; }
        internal MonsterBodyParameters Copy() => (MonsterBodyParameters)MemberwiseClone();
    }

    /// <summary>Native mover values, in world units and game seconds. Null inherits the template.</summary>
    public sealed class MonsterMovementParameters
    {
        public float? WaitGameSeconds { get; set; }
        public float? MoveGameSeconds { get; set; }
        public float? MoveSpeed { get; set; }
        public float? JumpSpeed { get; set; }
        public float? TargetRadius { get; set; }
        public float? MaxAcceleration { get; set; }
        public float? SlowRadius { get; set; }
        public float? TimeToTarget { get; set; }
        public float? DangerRadius { get; set; }
        public float? Sensitivity { get; set; }
        public float? RotationOffset { get; set; }
        public int? DirectionCount { get; set; }
        public int? AroundRange { get; set; }
        public bool? Directional { get; set; }
        internal MonsterMovementParameters Copy() => (MonsterMovementParameters)MemberwiseClone();
    }

    /// <summary>Existing native sight only; does not add a target provider to another mechanism.</summary>
    public sealed class MonsterSightParameters
    {
        public float? RangeScale { get; set; }
        public float? EnemyThreshold { get; set; }
        public float? MinAlertAddition { get; set; }
        public float? MaxAlertAddition { get; set; }
        public float? MaxAlertDuration { get; set; }
        public float? TooFarDistance { get; set; }
        public bool? DisableEmotion { get; set; }
        internal MonsterSightParameters Copy() => (MonsterSightParameters)MemberwiseClone();
    }

    public sealed class MonsterPhysicalInfo
    {
        private readonly MonsterMovementParameters? movement;
        private readonly MonsterSightParameters? sight;
        public MonsterPhysicalInfo(bool bodyScale, string bodyReason, int spawnVolume, float spawnOffsetX, float spawnOffsetY,
            string movementKind, MonsterMovementParameters? movement, string sightKind, MonsterSightParameters? sight)
        {
            BodyScale = bodyScale; BodyReason = bodyReason; SpawnVolume = spawnVolume; SpawnOffsetX = spawnOffsetX; SpawnOffsetY = spawnOffsetY;
            MovementKind = movementKind; this.movement = movement?.Copy(); SightKind = sightKind; this.sight = sight?.Copy();
        }
        public bool BodyScale { get; }
        public string BodyReason { get; }
        public int SpawnVolume { get; }
        public float SpawnOffsetX { get; }
        public float SpawnOffsetY { get; }
        public string MovementKind { get; }
        public MonsterMovementParameters? Movement => movement?.Copy();
        public string SightKind { get; }
        public MonsterSightParameters? Sight => sight?.Copy();
    }

    internal static class PhysicalValidation
    {
        internal static int Volume(MonsterBodyParameters? body, int original) => body == null ? original : body.SpawnVolume ?? Math.Max(1, (int)Math.Ceiling(original * (body.Scale ?? 1)));
        internal static MonsterDiagnostic? Validate(string owner, MonsterDefinition definition, MonsterPhysicalInfo? info = null, bool references = false)
        {
            MonsterDiagnostic Error(string field, string message, string code = "InvalidPhysicalParameters") => new MonsterDiagnostic(code, message, owner,
                owner + ":" + definition.LocalId, definition.TemplateId, references ? "ValidateReferences" : "Validate", field);
            bool Invalid(float? n, bool positive = false) => n.HasValue && (!ContentValidation.Finite(n.Value) || positive && n <= 0);
            var b = definition.Body;
            if (b != null)
            {
                if (Invalid(b.Scale, true)) return Error("body.scale", "Expected a positive finite value.");
                if (b.SpawnVolume < 1) return Error("body.spawnVolume", "Expected a positive native slot count.");
                if (Invalid(b.SpawnOffsetX)) return Error("body.spawnOffsetX", "Expected a finite value.");
                if (Invalid(b.SpawnOffsetY)) return Error("body.spawnOffsetY", "Expected a finite value.");
                if (references)
                {
                    if (info == null || !info.BodyScale) return Error("body", info?.BodyReason ?? "Refresh the Host catalog for physical capabilities.", "BodyUnsupported");
                    int minimum = Math.Max(1, (int)Math.Ceiling(info.SpawnVolume * (b.Scale ?? 1)));
                    if (b.SpawnVolume < minimum) return Error("body.spawnVolume", "Native slot width must cover the scaled template: at least " + minimum + ".");
                }
            }
            var m = definition.Movement;
            if (m != null)
            {
                if (Invalid(m.WaitGameSeconds) || m.WaitGameSeconds < 0) return Error("movement.waitGameSeconds", "Expected nonnegative finite game seconds.");
                if (Invalid(m.MoveGameSeconds, true)) return Error("movement.moveGameSeconds", "Expected positive finite game seconds.");
                if (Invalid(m.MoveSpeed, true)) return Error("movement.moveSpeed", "Expected a positive finite value.");
                if (Invalid(m.JumpSpeed, true)) return Error("movement.jumpSpeed", "Expected a positive finite value.");
                if (Invalid(m.TargetRadius, true)) return Error("movement.targetRadius", "Expected a positive finite value.");
                if (Invalid(m.MaxAcceleration, true)) return Error("movement.maxAcceleration", "Expected a positive finite value.");
                if (Invalid(m.SlowRadius, true)) return Error("movement.slowRadius", "Expected a positive finite value.");
                if (Invalid(m.TimeToTarget, true)) return Error("movement.timeToTarget", "Expected a positive finite value.");
                if (Invalid(m.DangerRadius, true)) return Error("movement.dangerRadius", "Expected a positive finite value.");
                if (Invalid(m.Sensitivity) || m.Sensitivity < 0) return Error("movement.sensitivity", "Expected a nonnegative finite value.");
                if (Invalid(m.RotationOffset)) return Error("movement.rotationOffset", "Expected a finite value.");
                if (m.DirectionCount < 1) return Error("movement.directionCount", "Expected a positive direction count.");
                if (m.AroundRange < 0) return Error("movement.aroundRange", "Expected a nonnegative native cell count.");
                if (references)
                {
                    if (info?.Movement == null || info.MovementKind != "ground" && info.MovementKind != "context-steering" && info.MovementKind != "shuttle")
                        return Error("movement", "This native mover has no editable parameter contract.", "MovementParametersUnsupported");
                    if (info.MovementKind != "shuttle" && (m.WaitGameSeconds.HasValue || m.MoveGameSeconds.HasValue)) return Error("movement", "Only Shuttle consumes wait/move duration.", "MovementFieldUnsupported");
                    if (info.MovementKind == "shuttle" && (m.MoveSpeed.HasValue || m.JumpSpeed.HasValue || m.TargetRadius.HasValue || m.MaxAcceleration.HasValue || m.SlowRadius.HasValue || m.TimeToTarget.HasValue || m.DangerRadius.HasValue || m.Sensitivity.HasValue || m.RotationOffset.HasValue || m.DirectionCount.HasValue)) return Error("movement", "Shuttle consumes only its durations and inherited direction/range settings.", "MovementFieldUnsupported");
                    if (info.MovementKind == "ground" && (m.MaxAcceleration.HasValue || m.SlowRadius.HasValue || m.TimeToTarget.HasValue || m.DangerRadius.HasValue || m.Sensitivity.HasValue || m.RotationOffset.HasValue || m.DirectionCount.HasValue))
                        return Error("movement", "Ground movement does not consume ContextSteering fields.", "MovementFieldUnsupported");
                    if (info.MovementKind == "context-steering")
                    {
                        if (m.JumpSpeed.HasValue) return Error("movement.jumpSpeed", "ContextSteering has no ground jump speed.", "MovementFieldUnsupported");
                    }
                }
            }
            var s = definition.Sight;
            if (s != null)
            {
                if (Invalid(s.RangeScale, true)) return Error("sight.rangeScale", "Expected a positive finite value.");
                if (Invalid(s.EnemyThreshold) || s.EnemyThreshold < 0) return Error("sight.enemyThreshold", "Expected a nonnegative finite value.");
                if (Invalid(s.MinAlertAddition) || s.MinAlertAddition < 0) return Error("sight.minAlertAddition", "Expected a nonnegative finite value.");
                if (Invalid(s.MaxAlertAddition) || s.MaxAlertAddition < 0) return Error("sight.maxAlertAddition", "Expected a nonnegative finite value.");
                if (Invalid(s.MaxAlertDuration, true)) return Error("sight.maxAlertDuration", "Expected a positive finite value.");
                if (Invalid(s.TooFarDistance, true)) return Error("sight.tooFarDistance", "Expected a positive finite value.");
                if (references)
                {
                    var baseline = info?.Sight;
                    if (baseline == null) return Error("sight", "This template has no editable native MonsterSight/MonsterSightEx.", "SightUnsupported");
                    if ((s.MinAlertAddition ?? baseline.MinAlertAddition) > (s.MaxAlertAddition ?? baseline.MaxAlertAddition))
                        return Error("sight.maxAlertAddition", "Maximum addition must not be below minimum addition after inheritance.");
                }
            }
            return null;
        }
    }
}
