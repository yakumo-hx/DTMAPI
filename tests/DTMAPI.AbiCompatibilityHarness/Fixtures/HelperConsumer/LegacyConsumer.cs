using System;
using DTMAPI.Abstractions;

namespace Frozen
{
    public sealed class LegacyConsumer : DtmMod
    {
        public int Result { get; private set; }
        public override void Entry(IDtmHelper helper)
        {
            if (helper.ModManifest != null || helper.Monitor != null || helper.Events != null ||
                helper.Config != null || helper.ModRegistry != null || helper.Workshop != null ||
                helper.UI != null || helper.Diagnostics != null || helper.Content != null ||
                helper.Input != null || helper.Translation != null)
                throw new InvalidOperationException("Unexpected fixture helper.");
            var config = helper.ReadConfig<LegacyConfig>();
            helper.WriteConfig(config);
            Result = config.Value;
        }
    }
    public sealed class LegacyConfig { public int Value { get; set; } = 73; }
}
