using System;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterNativeOverrides
{
    // The independent sample builder substitutes these authored constants for A and B.
    public sealed class ModEntry : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId) ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var lease = Require(api.RegisterOverride(new MonsterNativeOverride {
                LocalId = "armor", TargetId = "bee", Defense = 6, HurtDuration = 0.6f
            }));
            owned.Register(lease);
            helper.Monitor.Log("OVERRIDE AUTHOR registered " + lease.Snapshot.Id + " loadIndex=" + lease.Snapshot.LoadIndex + " declarationOrder=" + lease.Snapshot.DeclarationOrder);
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("override", c => {
                if (c.Arguments.Count == 1 && c.Arguments[0] == "withdraw") { lease.Dispose(); c.WriteLine("withdrawn " + lease.Snapshot.State); }
                else foreach (var row in Require(api.GetOverrides()))
                    c.WriteLine(row.Id + " target=" + row.TargetId + " loadIndex=" + row.LoadIndex + " declarationOrder=" + row.DeclarationOrder + " state=" + row.State + " " + string.Join("; ", row.Fields.Select(f => f.Field + "=" + f.Effective + " baseline=" + f.Baseline + " winner=" + f.WinnerId + " state=" + f.State)));
            }, description: "Native override status | withdraw.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
