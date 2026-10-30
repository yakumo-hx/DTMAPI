using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static MonsterSound SharedAttackSound(float volume = .4f) => new MonsterSound { ResourcePath = ContentCodec.AudioPath(SoundWave()), VolumeDb = volume };
    static void CommonAttackChecks()
    {
        foreach (string field in new[] { "fireSound", "dashSound", "throwSound", "volleySound", "landSound", "countdownSound", "explosionSound" })
        {
            var property = typeof(MonsterNativeAttackParameters).GetProperty(char.ToUpperInvariant(field[0]) + field.Substring(1))!;
            MonsterNativeAttackParameters Row(MonsterSound sound, string slot = "AtkNormal")
            { var row = new MonsterNativeAttackParameters { AttackId = slot }; property.SetValue(row, sound); return row; }
            var value = Definition(); value.NativeAttacks.Add(Row(SharedAttackSound())); value.NativeAttacks.Add(Row(SharedAttackSound(.7f), "AtkSkill01"));
            Check(MonsterValidation.Validate("A", value) == null && SoundValidation.CommonEvents(value).Select(e => e.Key).Distinct().Count() == 2,
                "Root slots retain distinct common event identities: " + field);
            var copy = new DefinitionRecord("A", value, 1).Definition;
            NativeAttackValidation.Sound(value.NativeAttacks[0], field)!.VolumeDb = 0;
            Check(NativeAttackValidation.Sound(copy.NativeAttacks[0], field)!.VolumeDb == .4f, "Root attack input freezes: " + field);
            foreach (var mutate in new Action<MonsterSound>[] { s => s.ResourcePath = "", s => s.Loop = true, s => s.ResourcePath = "../escape.wav", s => s.VolumeDb = float.NaN })
            {
                var bad = Definition(); var sound = SharedAttackSound(); mutate(sound); bad.NativeAttacks.Add(Row(sound));
                Check(MonsterValidation.Validate("A", bad)?.Code == "InvalidSound", "Invalid common attack input rejects before allocation: " + field);
            }
            var legacyRoot = Definition(); legacyRoot.NativeAttacks.Add(Row(new MonsterSound()));
            Check(MonsterValidation.Validate("A", legacyRoot)?.Code == "InvalidSound", "Selected root field has one resource route: " + field);
            var original = new MonsterNativeOverride { LocalId = "original", TargetId = value.TemplateId, NativeAttacks = { Row(SharedAttackSound()) } };
            Check(MonsterOverrideAdmission.Validate("A", original) == null, "Native override uses the shared resource contract: " + field);
            var part = Definition(); part.NativeParts.Add(new MonsterNativePartParameters { TemplateId = "nian_tail", NativeAttacks = { Row(SharedAttackSound()) } });
            Check(MonsterValidation.Validate("A", part) == null && SoundValidation.CommonEvents(part).Single().Key == "part/nian_tail/" + SoundValidation.CommonAttackKey(field, "AtkNormal"), "Part common resources keep their exact template and slot: " + field);
            property.SetValue(part.NativeParts[0].NativeAttacks[0], new MonsterSound());
            Check(MonsterValidation.Validate("A", part)?.Code == "InvalidSound", "Parts require a public resource path: " + field);
        }
    }
}
