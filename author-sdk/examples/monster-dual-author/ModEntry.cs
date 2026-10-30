using System;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterOverrideCode
{
    public sealed class ModEntry : DtmMod
    {
        // Change this value, increment package/info.json version, then rebuild with the kit script.
        private const int Defense = 12;

        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Install the matching Runtime 0.7.5 candidate.");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var lease = Require(api.RegisterOverride(new MonsterNativeOverride
            {
                LocalId = "armor", TargetId = "bee", Defense = Defense
            }));
            owned.Register(lease);
            helper.Monitor.Log("Armor author registered " + lease.Snapshot.Id
                + " loadIndex=" + lease.Snapshot.LoadIndex + " declarationOrder=" + lease.Snapshot.DeclarationOrder);
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("armor", command =>
            {
                foreach (var row in Require(api.GetOverrides()))
                    command.WriteLine(row.Id + " target=" + row.TargetId + " loadIndex=" + row.LoadIndex
                        + " declarationOrder=" + row.DeclarationOrder + " state=" + row.State + " "
                        + string.Join("; ", row.Fields.Select(field => field.Field + " requested=" + field.Requested
                            + " effective=" + field.Effective + " baseline=" + field.Baseline
                            + " winner=" + field.WinnerId + " state=" + field.State)));
            }, description: "Show native armor authors, fixed loading order and the effective winner.");
        }

        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value!
            : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
