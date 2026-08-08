using System;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal sealed class FishingProductContext
    {
        internal FishingProductContext(IDtmHelper helper)
        {
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
        }

        internal IDtmHelper Helper { get; }
        internal IMonitor RuntimeMonitor => Helper.Monitor;
    }
}
