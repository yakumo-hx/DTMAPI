using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Services
{
    internal sealed partial class InputService
    {
        internal IDtmInputDiagnostics CreateDiagnostics(string owner, Action ensureActive) => new OwnerInputDiagnostics(this, NormalizeOwner(owner), ensureActive);

        private sealed class OwnerInputDiagnostics : IDtmInputDiagnostics
        {
            private readonly InputService input;
            private readonly string owner;
            private readonly Action ensureActive;
            internal OwnerInputDiagnostics(InputService input, string owner, Action ensureActive)
            { this.input = input; this.owner = owner; this.ensureActive = ensureActive; }
            public DtmInputContextSnapshot Snapshot
            {
                get
                {
                    ensureActive();
                    var audience = input.GetQueryAudience();
                    var mode = audience.Mode == InputAudienceMode.Normal ? DtmInputAudience.Normal :
                        audience.Mode == InputAudienceMode.OwnerModal ? DtmInputAudience.OwnerModal : DtmInputAudience.PlatformModal;
                    return new DtmInputContextSnapshot(input.inputFrameGeneration, audience.EffectiveScope, mode, audience.OwnerId);
                }
            }
            public IReadOnlyList<DtmInputBindingInfo> GetRegistrations()
            {
                ensureActive();
                return Array.AsReadOnly(input.registrationsByKey.Values.Where(r => r.OwnerId.Equals(owner, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(r => r.Id, StringComparer.Ordinal).Select(Describe).ToArray());
            }
            public IReadOnlyList<DtmInputBindingInfo> FindConflicts(DtmKeybindList keybinds, DtmInputScope scope)
            {
                ensureActive();
                if (keybinds == null) throw new ArgumentNullException(nameof(keybinds));
                if (!Enum.IsDefined(typeof(DtmInputScope), scope)) throw new ArgumentOutOfRangeException(nameof(scope));
                return Array.AsReadOnly(input.registrationsByKey.Values.Where(r => !r.Disposed &&
                    ScopesOverlap(scope, r.Scope) && ChordsOverlap(keybinds, r.Keybinds))
                    .OrderBy(r => r.OwnerId, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Id, StringComparer.Ordinal).Select(Describe).ToArray());
            }
            private DtmInputBindingInfo Describe(InputRegistrationState r)
            {
                var audience = input.GetQueryAudience();
                return new DtmInputBindingInfo(r.OwnerId, r.Id, r.Keybinds.ToString(), r.Scope,
                    input.IsRegistrationEligible(r, audience, null), input.IsSuppressedForOwner(r.OwnerId, r.Keybinds, audience),
                    input.keybindRequiresNeutral.Contains(r.Key));
            }
            private static bool ScopesOverlap(DtmInputScope left, DtmInputScope right) =>
                Enum.GetValues(typeof(DtmInputScope)).Cast<DtmInputScope>().Any(current => ScopeMatches(left, current) && ScopeMatches(right, current));
            private static bool ChordsOverlap(DtmKeybindList left, DtmKeybindList right) =>
                left.Keybinds.Any(a => right.Keybinds.Any(b => IsSubset(a, b) || IsSubset(b, a)));
            private static bool IsSubset(DtmKeybind a, DtmKeybind b) => a.IsBound && b.IsBound &&
                a.Buttons.All(x => b.Buttons.Any(y => SamePhysicalButton(x.Id, y.Id)));
            private static bool SamePhysicalButton(string left, string right) =>
                DtmButton.MatchesPhysicalState(left, id => id.Equals(right, StringComparison.OrdinalIgnoreCase)) ||
                DtmButton.MatchesPhysicalState(right, id => id.Equals(left, StringComparison.OrdinalIgnoreCase));
        }
    }
}
