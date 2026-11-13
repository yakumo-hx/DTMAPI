using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent
{
    internal static class ContentNativePartReader
    {
        internal static List<MonsterNativePartParameters> Read(object? value, ContentReadContext context) => context.Json.Array(value, "nativeParts").Select((row, i) => {
            var json = context.Json;
            string at = "nativeParts[" + i + "]";
            var p = json.Object(row, at, "templateId", "nativeAttacks sounds health defense hurtDuration scale nativeParameters");
            return new MonsterNativePartParameters { TemplateId = json.Text(p["templateId"], at + ".templateId"),
                Health = p.TryGetValue("health", out var health) ? json.NullableInt(health, at + ".health") : null,
                Defense = p.TryGetValue("defense", out var defense) ? json.NullableInt(defense, at + ".defense") : null,
                HurtDuration = p.TryGetValue("hurtDuration", out var hurt) && hurt != null ? (float?)json.Number(hurt, at + ".hurtDuration") : null,
                Scale = p.TryGetValue("scale", out var scale) && scale != null ? (float?)json.Number(scale, at + ".scale") : null,
                NativeParameters = p.TryGetValue("nativeParameters", out var values) ? ContentNativeParameterReader.Read(values, json) : null,
                NativeAttacks = p.ContainsKey("nativeAttacks") ? ContentNativeAttackReader.Read(p["nativeAttacks"], context, at + ".nativeAttacks") : new List<MonsterNativeAttackParameters>(),
                Sounds = p.ContainsKey("sounds") ? ContentSoundReader.Read(p["sounds"], context, at + ".sounds") : null };
        }).ToList();
    }
}
