using System;
using System.Globalization;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterParameterTint
{
    public sealed class ModEntry : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            owned.Register(Require(api.RegisterDefinition(new MonsterDefinition { LocalId = "long-hurt", TemplateId = "bee", HurtDuration = 1.25f })));
            owned.Register(Require(api.RegisterDefinition(new MonsterDefinition { LocalId = "inherited", TemplateId = "bee" })));
            var tint = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "blue", TargetId = "bee", Tint = "#4080FF" }));
            owned.Register(tint);
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("mc29", command =>
            {
                if (command.Arguments.Count == 1 && command.Arguments[0] == "withdraw-tint")
                { tint.Dispose(); command.WriteLine("tint " + tint.Snapshot.State); return; }
                if (command.Arguments.Count == 3 && command.Arguments[0] == "spawn")
                {
                    float x = float.Parse(command.Arguments[1], CultureInfo.InvariantCulture), y = float.Parse(command.Arguments[2], CultureInfo.InvariantCulture);
                    foreach (string local in new[] { "long-hurt", "inherited" })
                    {
                        var result = api.Spawn(new MonsterSpawnRequest { DefinitionId = "Samples.MonsterParameterTint:" + local, X = x, Y = y });
                        command.WriteLine(result.Success ? result.Value!.DefinitionId + " " + result.Value.Handle : result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
                        x += 2;
                    }
                    return;
                }
                foreach (var row in Require(api.GetOverrides()))
                    command.WriteLine(row.Id + " " + string.Join("; ", row.Fields.Select(field => field.Field + "="
                        + (field.EffectiveTint ?? field.Effective?.ToString(CultureInfo.InvariantCulture)) + " winner=" + field.WinnerId + " state=" + field.State)));
            }, description: "status | spawn x y | withdraw-tint; new definitions keep the template's original color.");
        }
        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
