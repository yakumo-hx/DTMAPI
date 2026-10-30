using System;
using DTMAPI.Abstractions;
namespace DTMAPI.Zoom
{
    internal sealed class ZoomInputBindings
    {
        // Most specific pressed chord wins. Ties are deterministic and prefer
        // direct actions; dispatch uses its registration id, so one action fires.
        internal static readonly string[] Ids = { "zoom.maximum", "zoom.reset", "zoom.increase", "zoom.decrease" };
        internal ZoomInputBindings(ZoomConfig config)
        {
            Values = new[] { DtmKeybindList.Parse(config.MaximumKey), DtmKeybindList.Parse(config.ResetKey),
                DtmKeybindList.Parse(config.IncreaseKey), DtmKeybindList.Parse(config.DecreaseKey) };
        }
        internal DtmKeybindList[] Values { get; }
        internal int Select(Func<string, bool> pressed, Func<string, bool> down, Func<string, bool> released)
        {
            int selected = -1, specificity = 0;
            for (int i = 0; i < Values.Length; ++i)
                foreach (var chord in Values[i].Keybinds)
                    if (chord.Buttons.Count > specificity && chord.IsPressed(pressed, down, released))
                    { selected = i; specificity = chord.Buttons.Count; }
            return selected;
        }
    }
}
