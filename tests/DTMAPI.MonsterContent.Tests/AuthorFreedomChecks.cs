using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void AuthorFreedomChecks()
    {
        var d = Definition(new string('a', 160));
        d.Health = int.MaxValue; d.Defense = 1000000; d.HurtDuration = 120;
        d.Behavior = "configured-sequence";
        d.Sequence = new MonsterSequence { RepeatCount = 40 };
        for (int i = 0; i < 40; i++)
        {
            d.Sequence.Steps.Add(new MonsterSequenceStep { Id = "step" + i, Action = "move-to", OffsetX = 256,
                OffsetY = -256, MaxTravel = 1024, MaxGameSeconds = 300 });
            d.Skills.Add(new MonsterSkill { Id = "skill" + i, Scatter = new MonsterScatter {
                ProjectileAttackId = "AtkNormal", BulletCount = 100, SpreadDegrees = 720,
                BulletSpeed = 256, BulletLifetimeGameSeconds = 120, Damage = 1000000, Range = 256,
                WindupGameSeconds = 120, RecoveryGameSeconds = 120, CooldownGameSeconds = 300 } });
        }
        d.HealthPhases = new MonsterHealthPhases();
        for (int i = 0; i < 10; i++) d.HealthPhases.Stages.Add(new MonsterHealthPhase {
            Id = "phase" + i, AtOrBelow = .95f - i * .09f, DamageMultiplier = 20, SpeedMultiplier = 20 });
        d.NativeAttacks.Add(new MonsterNativeAttackParameters { AttackId = "AtkNormal", Damage = 1000000,
            CooldownGameSeconds = 1000, SalvoShoot = new MonsterSalvoShootParameters {
                BulletCount = 1000, SectorAngleDegrees = 720, ProjectileSpeed = 256, ProjectileLifetimeGameSeconds = 120 } });
        for (int i = 0; i < 10; i++) d.NativeParts.Add(new MonsterNativePartParameters {
            TemplateId = "part" + i, NativeAttacks = { new MonsterNativeAttackParameters { AttackId = "AtkNormal", Damage = 1000000 } } });
        Check(MonsterValidation.Validate("Freedom", d) == null,
            "One combined author definition exceeds former numeric and collection budgets without new feature permissions.");
        var input = new ContentInput { OwnerId = "Freedom" };
        input.Definitions.Add(d);
        for (int i = 0; i < 70; i++) input.NativeOverrides.Add(new MonsterNativeOverride {
            LocalId = "override" + i, TargetId = "test-template", Defense = 1000000, HurtDuration = 120 });
        string payload = ContentCodec.WriteContent(input);
        var reopened = ContentCodec.ReadContent(payload, "Freedom", _ => System.Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && reopened.NativeOverrides.Count == 70
            && MonsterValidation.Validate("Freedom", reopened.Definitions.Single()) == null,
            "Compiled author payload preserves large values and collections through reopen, without executing a stress test in the game.");
    }
}
