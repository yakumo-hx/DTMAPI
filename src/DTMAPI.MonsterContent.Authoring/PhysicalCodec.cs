using System.Collections.Generic;

namespace DTMAPI.MonsterContent.Authoring
{
    internal static class PhysicalCodec
    {
        private const string BodyKeys = "scale spawnVolume spawnOffsetX spawnOffsetY";
        private const string MovementKeys = "waitGameSeconds moveGameSeconds moveSpeed jumpSpeed targetRadius maxAcceleration slowRadius timeToTarget dangerRadius sensitivity rotationOffset directionCount aroundRange directional";
        private const string SightKeys = "rangeScale enemyThreshold minAlertAddition maxAlertAddition maxAlertDuration tooFarDistance disableEmotion";
        private static object? Get(IDictionary<string, object?> row, string key) => row.TryGetValue(key, out var value) ? value : null;
        private static float? N(IDictionary<string, object?> row, string key, string at) => Get(row, key) == null ? (float?)null : (float)Json.Number(row[key], at + "." + key);
        internal static MonsterBodyParameters? ReadBody(object? value)
        {
            if (value == null) return null;
            var row = Json.Object(value, "body", "", BodyKeys);
            return new MonsterBodyParameters { Scale = N(row, "scale", "body"), SpawnVolume = Json.NullableInt(Get(row, "spawnVolume"), "body.spawnVolume"),
                SpawnOffsetX = N(row, "spawnOffsetX", "body"), SpawnOffsetY = N(row, "spawnOffsetY", "body") };
        }
        internal static MonsterMovementParameters? ReadMovement(object? value)
        {
            if (value == null) return null;
            var row = Json.Object(value, "movement", "", MovementKeys);
            return new MonsterMovementParameters { WaitGameSeconds = N(row, "waitGameSeconds", "movement"), MoveGameSeconds = N(row, "moveGameSeconds", "movement"), MoveSpeed = N(row, "moveSpeed", "movement"), JumpSpeed = N(row, "jumpSpeed", "movement"),
                TargetRadius = N(row, "targetRadius", "movement"), MaxAcceleration = N(row, "maxAcceleration", "movement"), SlowRadius = N(row, "slowRadius", "movement"),
                TimeToTarget = N(row, "timeToTarget", "movement"), DangerRadius = N(row, "dangerRadius", "movement"), Sensitivity = N(row, "sensitivity", "movement"),
                RotationOffset = N(row, "rotationOffset", "movement"), DirectionCount = Json.NullableInt(Get(row, "directionCount"), "movement.directionCount"),
                AroundRange = Json.NullableInt(Get(row, "aroundRange"), "movement.aroundRange"),
                Directional = Get(row, "directional") == null ? (bool?)null : Json.Bool(row["directional"], "movement.directional") };
        }
        internal static MonsterSightParameters? ReadSight(object? value)
        {
            if (value == null) return null;
            var row = Json.Object(value, "sight", "", SightKeys);
            return new MonsterSightParameters { DisableEmotion = row.TryGetValue("disableEmotion", out var disable) && disable != null ? Json.Bool(disable, "sight.disableEmotion") : (bool?)null, RangeScale = N(row, "rangeScale", "sight"), EnemyThreshold = N(row, "enemyThreshold", "sight"),
                MinAlertAddition = N(row, "minAlertAddition", "sight"), MaxAlertAddition = N(row, "maxAlertAddition", "sight"),
                MaxAlertDuration = N(row, "maxAlertDuration", "sight"), TooFarDistance = N(row, "tooFarDistance", "sight") };
        }
        internal static object? Body(MonsterBodyParameters? b) => b == null ? null : Json.Map("scale", b.Scale, "spawnVolume", b.SpawnVolume, "spawnOffsetX", b.SpawnOffsetX, "spawnOffsetY", b.SpawnOffsetY);
        internal static object? Movement(MonsterMovementParameters? m) => m == null ? null : Json.Map("waitGameSeconds", m.WaitGameSeconds, "moveGameSeconds", m.MoveGameSeconds, "moveSpeed", m.MoveSpeed, "jumpSpeed", m.JumpSpeed,
            "targetRadius", m.TargetRadius, "maxAcceleration", m.MaxAcceleration, "slowRadius", m.SlowRadius, "timeToTarget", m.TimeToTarget,
            "dangerRadius", m.DangerRadius, "sensitivity", m.Sensitivity, "rotationOffset", m.RotationOffset, "directionCount", m.DirectionCount,
            "aroundRange", m.AroundRange, "directional", m.Directional);
        internal static object? Sight(MonsterSightParameters? s) => s == null ? null : Json.Map("rangeScale", s.RangeScale, "enemyThreshold", s.EnemyThreshold,
            "minAlertAddition", s.MinAlertAddition, "maxAlertAddition", s.MaxAlertAddition, "maxAlertDuration", s.MaxAlertDuration, "tooFarDistance", s.TooFarDistance, "disableEmotion", s.DisableEmotion);
        internal static object? Info(MonsterPhysicalInfo? p) => p == null ? null : Json.Map("bodyScale", p.BodyScale, "bodyReason", p.BodyReason,
            "spawnVolume", p.SpawnVolume, "spawnOffsetX", p.SpawnOffsetX, "spawnOffsetY", p.SpawnOffsetY,
            "movementKind", p.MovementKind, "movement", Movement(p.Movement), "sightKind", p.SightKind, "sight", Sight(p.Sight));
        internal static MonsterPhysicalInfo? ReadInfo(object? value)
        {
            if (value == null) return null;
            var row = Json.Object(value, "physical", "bodyScale bodyReason spawnVolume spawnOffsetX spawnOffsetY movementKind movement sightKind sight");
            return new MonsterPhysicalInfo(Json.Bool(row["bodyScale"], "bodyScale"), Json.Text(row["bodyReason"], "bodyReason"), Json.Int(row["spawnVolume"], "spawnVolume"),
                (float)Json.Number(row["spawnOffsetX"], "spawnOffsetX"), (float)Json.Number(row["spawnOffsetY"], "spawnOffsetY"),
                Json.Choice(row["movementKind"], "movementKind", "ground", "context-steering", "shuttle", "unsupported"), ReadMovement(row["movement"]),
                Json.Choice(row["sightKind"], "sightKind", "MonsterSight", "MonsterSightEx", "unsupported"), ReadSight(row["sight"]));
        }
    }
}
