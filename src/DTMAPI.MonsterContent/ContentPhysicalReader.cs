using System.Collections.Generic;

namespace DTMAPI.MonsterContent
{
    internal static class ContentPhysicalReader
    {
        private const string BodyKeys = "scale spawnVolume spawnOffsetX spawnOffsetY";
        private const string MovementKeys = "waitGameSeconds moveGameSeconds moveSpeed jumpSpeed targetRadius maxAcceleration slowRadius timeToTarget dangerRadius sensitivity rotationOffset directionCount aroundRange directional";
        private const string SightKeys = "rangeScale enemyThreshold minAlertAddition maxAlertAddition maxAlertDuration tooFarDistance disableEmotion";
        private static object? Get(IDictionary<string, object?> row, string key) => row.TryGetValue(key, out var value) ? value : null;
        private static float? N(IDictionary<string, object?> row, string key, string at, ContentJson json) => Get(row, key) == null ? (float?)null : (float)json.Number(row[key], at + "." + key);
        internal static MonsterBodyParameters? ReadBody(object? value, ContentJson json)
        {
            if (value == null) return null;
            var row = json.Object(value, "body", "", BodyKeys);
            return new MonsterBodyParameters { Scale = N(row, "scale", "body", json), SpawnVolume = json.NullableInt(Get(row, "spawnVolume"), "body.spawnVolume"),
                SpawnOffsetX = N(row, "spawnOffsetX", "body", json), SpawnOffsetY = N(row, "spawnOffsetY", "body", json) };
        }
        internal static MonsterMovementParameters? ReadMovement(object? value, ContentJson json)
        {
            if (value == null) return null;
            var row = json.Object(value, "movement", "", MovementKeys);
            return new MonsterMovementParameters { WaitGameSeconds = N(row, "waitGameSeconds", "movement", json), MoveGameSeconds = N(row, "moveGameSeconds", "movement", json), MoveSpeed = N(row, "moveSpeed", "movement", json), JumpSpeed = N(row, "jumpSpeed", "movement", json),
                TargetRadius = N(row, "targetRadius", "movement", json), MaxAcceleration = N(row, "maxAcceleration", "movement", json), SlowRadius = N(row, "slowRadius", "movement", json),
                TimeToTarget = N(row, "timeToTarget", "movement", json), DangerRadius = N(row, "dangerRadius", "movement", json), Sensitivity = N(row, "sensitivity", "movement", json),
                RotationOffset = N(row, "rotationOffset", "movement", json), DirectionCount = json.NullableInt(Get(row, "directionCount"), "movement.directionCount"),
                AroundRange = json.NullableInt(Get(row, "aroundRange"), "movement.aroundRange"),
                Directional = Get(row, "directional") == null ? (bool?)null : json.Bool(row["directional"], "movement.directional") };
        }
        internal static MonsterSightParameters? ReadSight(object? value, ContentJson json)
        {
            if (value == null) return null;
            var row = json.Object(value, "sight", "", SightKeys);
            return new MonsterSightParameters { DisableEmotion = row.TryGetValue("disableEmotion", out var disable) && disable != null ? json.Bool(disable, "sight.disableEmotion") : (bool?)null, RangeScale = N(row, "rangeScale", "sight", json), EnemyThreshold = N(row, "enemyThreshold", "sight", json),
                MinAlertAddition = N(row, "minAlertAddition", "sight", json), MaxAlertAddition = N(row, "maxAlertAddition", "sight", json),
                MaxAlertDuration = N(row, "maxAlertDuration", "sight", json), TooFarDistance = N(row, "tooFarDistance", "sight", json) };
        }
    }
}
