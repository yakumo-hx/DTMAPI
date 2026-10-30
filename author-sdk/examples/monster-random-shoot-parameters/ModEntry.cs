using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterRandomShootParameters
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterRandomShootParameters";
        static MonsterNativeAttackParameters Parameters(bool brief, bool native = false) => new MonsterNativeAttackParameters {
            AttackId = "AtkNormal", CooldownGameSeconds = native ? 6 : brief ? .75f : 4,
            StartOnCooldown = !native && !brief, Damage = native ? 23 : brief ? 3 : 19, CriticalRate = brief ? 0 : 1,
            RandomShoot = new DTMAPI.MonsterContent.MonsterRandomShootParameters { MinBulletCount = native ? 4 : brief ? 1 : 3,
                MaxBulletCount = native ? 4 : brief ? 1 : 3, ShotIntervalGameSeconds = native ? .2f : brief ? .1f : .25f,
                ProjectileSpeed = native ? 10 : brief ? 4 : 8, ProjectileLifetimeGameSeconds = native ? 4 : brief ? 1 : 3 } };
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0"); owned.Register(Require(api.RequestModule()));
            var definitions = new List<MonsterDefinitionLease>(); MonsterOverrideLease? native = null;
            foreach (string id in new[] { "burst", "brief" }) {
                var definition = new MonsterDefinition { LocalId = id, TemplateId = "nian", Health = 500, Defense = 0,
                    NativeParts = new List<MonsterNativePartParameters> { new MonsterNativePartParameters { TemplateId = "nian_tail",
                        NativeAttacks = new List<MonsterNativeAttackParameters> { Parameters(id == "brief") } } } };
                var lease = Require(api.RegisterDefinition(definition)); definitions.Add(lease); owned.Register(lease);
            }
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("parameters", c => {
                var a = c.Arguments;
                if (a.Count == 4 && a[0] == "spawn") {
                    var value = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":" + a[1],
                        X = float.Parse(a[2], CultureInfo.InvariantCulture), Y = float.Parse(a[3], CultureInfo.InvariantCulture) })); c.WriteLine("spawned " + value.Handle);
                } else if (a.Count == 1 && a[0] == "enable-native") {
                    if (native != null) { c.WriteLine("already enabled"); return; }
                    native = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "tail", TargetId = "nian_tail",
                        NativeAttacks = new List<MonsterNativeAttackParameters> { Parameters(false, true) } })); owned.Register(native); c.WriteLine("enabled original tail parameters");
                } else if (a.Count == 1 && (a[0] == "withdraw-native" || a[0] == "withdraw")) {
                    native?.Dispose(); native = null;
                    if (a[0] == "withdraw") foreach (var lease in definitions) lease.Dispose(); c.WriteLine("withdrawn");
                } else c.WriteLine("parameters spawn <burst|brief> <x> <y> | enable-native | withdraw-native | withdraw");
            }, description: "Configure native tail attacks through a complete nian definition; original overrides remain separate.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
