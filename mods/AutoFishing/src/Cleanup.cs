using System;
using System.Collections.Generic;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal static class Cleanup
    {
        internal static void Run(params Action[] releases)
        {
            List<Exception>? failures = null;
            foreach (var release in releases)
                try { release(); }
                catch (Exception ex) { (failures ??= new List<Exception>()).Add(ex); }
            if (failures != null) throw new AggregateException("AutoFishing cleanup failed; failed resources retained for retry.", failures);
        }
    }
}
