using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterMultiImpactParameters
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterMultiImpactParameters";
        static MonsterNativeAttackParameters Parameters(string slot, bool brief, bool native = false) => new MonsterNativeAttackParameters {
            AttackId = slot, CooldownGameSeconds = native ? 6 : brief ? .75f : 4,
            StartOnCooldown = !native && !brief, Damage = native ? 23 : brief ? 3 : 19, CriticalRate = brief ? 0 : 1,
            WindupGameSeconds = native ? .8f : .4f,
            MultiImpact = new DTMAPI.MonsterContent.MonsterMultiImpactParameters { WindupEnabled = !native && !brief,
                Distance = native ? 8 : brief ? 2 : 6, TravelSpeed = native ? 8 : brief ? 12 : 4,
                MinExtraDashes = native ? 3 : brief ? 1 : 2, MaxExtraDashes = native ? 3 : brief ? 1 : 2 } };
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0"); owned.Register(Require(api.RequestModule()));
            var definitions = new List<MonsterDefinitionLease>(); var native = new List<MonsterOverrideLease>();
            foreach (bool head in new[] { false, true }) foreach (bool brief in new[] { false, true }) {
                var definition = new MonsterDefinition { LocalId = (head ? "head-" : "") + (brief ? "brief" : "charge"),
                    TemplateId = head ? "nian" : "bird", Health = 500, Defense = 0 };
                if (head) definition.NativeParts.Add(new MonsterNativePartParameters { TemplateId = "nian_head", NativeAttacks = { Parameters("AtkNormal", brief) } });
                else definition.NativeAttacks.Add(Parameters("AtkSkill02", brief));
                var lease = Require(api.RegisterDefinition(definition)); definitions.Add(lease); owned.Register(lease);
            }
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("parameters", c => {
                var a = c.Arguments;
                if (a.Count == 4 && a[0] == "spawn") {
                    var value = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":" + a[1],
                        X = float.Parse(a[2], CultureInfo.InvariantCulture), Y = float.Parse(a[3], CultureInfo.InvariantCulture) })); c.WriteLine("spawned " + value.Handle);
                } else if (a.Count == 1 && a[0] == "enable-native") {
                    if (native.Count != 0) { c.WriteLine("already enabled"); return; }
                    foreach (bool head in new[] { false, true }) { var lease = Require(api.RegisterOverride(new MonsterNativeOverride {
                        LocalId = head ? "head" : "bird", TargetId = head ? "nian_head" : "bird",
                        NativeAttacks = { Parameters(head ? "AtkNormal" : "AtkSkill02", false, true) } })); native.Add(lease); owned.Register(lease); }
                    c.WriteLine("enabled original bird and head parameters");
                } else if (a.Count == 1 && (a[0] == "withdraw-native" || a[0] == "withdraw")) {
                    foreach (var lease in native) lease.Dispose(); native.Clear();
                    if (a[0] == "withdraw") foreach (var lease in definitions) lease.Dispose(); c.WriteLine("withdrawn");
                } else c.WriteLine("parameters spawn <charge|brief|head-charge|head-brief> <x> <y> | enable-native | withdraw-native | withdraw");
            }, description: "Configure repeated native dashes on bird and the complete nian's head; original overrides remain independent.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
