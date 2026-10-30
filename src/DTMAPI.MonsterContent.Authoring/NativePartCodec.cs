using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent.Authoring
{
    internal static class NativePartCodec
    {
        internal static List<MonsterNativePartParameters> Read(object? value, IDictionary<string, byte[]> assets, bool payload, Func<string, byte[]>? readBytes) => Json.Array(value, "nativeParts").Select((row, i) => {
            string at = "nativeParts[" + i + "]";
            var p = Json.Object(row, at, "templateId", "nativeAttacks sounds health defense hurtDuration scale nativeParameters");
            return new MonsterNativePartParameters { TemplateId = Json.Text(p["templateId"], at + ".templateId"),
                Health = p.TryGetValue("health", out var health) ? Json.NullableInt(health, at + ".health") : null,
                Defense = p.TryGetValue("defense", out var defense) ? Json.NullableInt(defense, at + ".defense") : null,
                HurtDuration = p.TryGetValue("hurtDuration", out var hurt) && hurt != null ? (float?)Json.Number(hurt, at + ".hurtDuration") : null,
                Scale = p.TryGetValue("scale", out var scale) && scale != null ? (float?)Json.Number(scale, at + ".scale") : null,
                NativeParameters = p.TryGetValue("nativeParameters", out var values) ? NativeParameterCodec.Read(values) : null,
                NativeAttacks = p.ContainsKey("nativeAttacks") ? NativeAttackCodec.Read(p["nativeAttacks"], assets, payload, readBytes, at + ".nativeAttacks") : new List<MonsterNativeAttackParameters>(),
                Sounds = p.ContainsKey("sounds") ? SoundCodec.Read(p["sounds"], assets, payload, readBytes, at + ".sounds") : null };
        }).ToList();
        internal static object Write(MonsterNativePartParameters p)
        {
            var row = Json.Map("templateId", p.TemplateId, "health", p.Health, "defense", p.Defense, "hurtDuration", p.HurtDuration, "scale", p.Scale, "nativeParameters", NativeParameterCodec.Write(p.NativeParameters));
            if (p.NativeAttacks.Count > 0) row["nativeAttacks"] = p.NativeAttacks.Select(NativeAttackCodec.Write).ToArray();
            if (p.Sounds != null) row["sounds"] = SoundCodec.Write(p.Sounds);
            return row;
        }
        internal static object Info(MonsterNativePartInfo p) => Json.Map("templateId", p.TemplateId,
            "nativeAttacks", p.NativeAttacks.Select(NativeAttackCodec.Info).ToArray(), "soundEvents", p.SoundEvents, "health", p.Health, "defense", p.Defense, "hurtDuration", p.HurtDuration, "nativeParameters", NativeParameterCodec.Write(p.NativeParameters));
        internal static IReadOnlyList<MonsterNativePartInfo> ReadInfo(object? value) => Array.AsReadOnly(Json.Array(value, "nativeParts").Select(row => {
            var p = Json.Object(row, "nativeParts[]", "templateId nativeAttacks", "soundEvents health defense hurtDuration nativeParameters");
            return new MonsterNativePartInfo(Json.Text(p["templateId"], "templateId"), NativeAttackCodec.ReadInfo(p["nativeAttacks"]),
                p.TryGetValue("soundEvents", out var events) ? Json.Array(events, "nativeParts.soundEvents").Select(v => Json.Choice(v, "nativeParts.soundEvents[]", "hurt", "dead", "humming", "recoveryWarning")) : null) { Health = p.TryGetValue("health", out var health) ? Json.NullableInt(health, "health") : null, Defense = p.TryGetValue("defense", out var defense) ? Json.NullableInt(defense, "defense") : null, HurtDuration = p.TryGetValue("hurtDuration", out var hurt) && hurt != null ? (float?)Json.Number(hurt, "hurtDuration") : null, NativeParameters = p.TryGetValue("nativeParameters", out var values) ? NativeParameterCodec.Read(values) : null };
        }).ToArray());
    }
}
