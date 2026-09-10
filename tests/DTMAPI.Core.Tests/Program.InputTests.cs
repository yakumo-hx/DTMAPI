#pragma warning disable CS0618 // Frozen compatibility contracts are intentionally exercised.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void OwnerBoundInputTracksAndCleansPerOwner()
        {
            var registrations = new List<string>();
            var cleanups = new List<string>();
            var input = new InputService(
                () => true,
                (owner, kind, key, details) => registrations.Add(owner + ":" + kind + ":" + key),
                (owner, kind, count, details) => cleanups.Add(owner + ":" + kind + ":" + count.ToString(CultureInfo.InvariantCulture)));

            IInputHelper ownerA = input.CreateOwnerBound("DTMAPI.Tests.OwnerA");
            IInputHelper ownerB = input.CreateOwnerBound("DTMAPI.Tests.OwnerB");
            ownerA.RegisterButton("F6");
            ownerB.RegisterButton("F6");
            ownerB.RegisterButton("Y");

            Assert(input.GetRegisteredButtons().OrderBy(v => v, StringComparer.OrdinalIgnoreCase).SequenceEqual(new[] { "F6", "Y" }), "Owner-bound input should expose the global listened-button union.");
            Assert(registrations.Count == 3, "Owner-bound input should record one registration per owner/button pair.");

            int removedA = input.RemoveOwner("DTMAPI.Tests.OwnerA");
            Assert(removedA == 1, "Removing owner A should remove only owner A's button registration.");
            Assert(input.GetRegisteredButtons().OrderBy(v => v, StringComparer.OrdinalIgnoreCase).SequenceEqual(new[] { "F6", "Y" }), "Owner B should keep shared F6 and Y registrations active.");

            int removedB = input.RemoveOwner("DTMAPI.Tests.OwnerB");
            Assert(removedB == 2, "Removing owner B should remove its remaining two button registrations.");
            Assert(input.GetRegisteredButtons().Count == 0, "All owner-bound buttons should be removed after all owners are cleaned.");
            Assert(cleanups.Any(line => line == "DTMAPI.Tests.OwnerA:InputButton:1") &&
                cleanups.Any(line => line == "DTMAPI.Tests.OwnerB:InputButton:2"), "Owner-bound input cleanup should be reported per owner.");
        }

        private static void InputKeybindParserCanonicalizesAliasesAndChords()
        {
            Assert(DtmButton.Normalize("Plus") == "Equals", "Plus should canonicalize to the physical Equals key.");
            Assert(DtmButton.Normalize("Equals") == "Equals", "Equals should stay canonical.");
            Assert(DtmButton.Normalize("KeypadPlus") == "KeypadPlus", "KeypadPlus should remain physically distinct.");
            Assert(DtmButton.Normalize("1") == "Alpha1", "Digit shorthand should canonicalize to Unity-style Alpha keys.");
            Assert(DtmKeybindList.Parse("Plus, Equals, KeypadPlus").ToString() == "Equals, KeypadPlus", "Duplicate physical aliases should collapse inside a keybind list.");
            Assert(DtmKeybindList.Parse("Ctrl+F6").ToString() == "Control+F6", "Generic modifier aliases should stay logical so either physical side can trigger.");
            Assert(DtmKeybindList.Parse("LeftCtrl+F6").ToString() == "LeftControl+F6", "Explicit left-side modifiers should stay side-specific.");
            Assert(DtmKeybindList.Parse("").ToString() == "None", "Blank keybinds should canonicalize to None.");
        }

        private static void InputScopeGatesSamplingAndDispatch()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.InputScope");
            owner.RegisterKeybind("title", "Y", DtmInputScope.Title);
            owner.RegisterKeybind("save", "Escape", DtmInputScope.SaveLoaded);
            owner.RegisterKeybind("gameplay", "F6", DtmInputScope.Gameplay);

            Assert(input.GetButtonsToSample(DtmInputScope.Title).SequenceEqual(new[] { "Y" }), "Title scope should sample only title keybinds.");
            Assert(input.GetButtonsToSample(DtmInputScope.SaveLoaded).SequenceEqual(new[] { "Escape" }), "SaveLoaded scope should not sample gameplay-only keybinds.");
            Assert(input.GetButtonsToSample(DtmInputScope.Gameplay).SequenceEqual(new[] { "Escape", "F6" }), "Gameplay scope should sample gameplay and save-loaded keybinds.");

            int pressed = 0;
            int keybindPressed = 0;
            input.RecordFrame(
                DtmInputScope.Title,
                new[] { new InputButtonSample("Y", true, true, false) },
                scope => scope == DtmInputScope.Title,
                _ => pressed++,
                _ => { },
                _ => keybindPressed++,
                _ => { });
            Assert(pressed == 1 && keybindPressed == 1, "Title-scoped keybinds should dispatch in the title scope when allowed.");
        }

        private static void InputSuppressionIsButtonScopedWithinFrame()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.InputSuppressionScope");
            owner.RegisterButton("Escape");
            owner.RegisterButton("Y");
            input.Suppress("Escape");

            var pressed = new List<string>();
            InputFrameResult result = input.RecordFrame(
                DtmInputScope.Gameplay,
                new[]
                {
                    new InputButtonSample("Escape", true, true, false),
                    new InputButtonSample("Y", true, true, false)
                },
                _ => true,
                button => pressed.Add(button),
                _ => { },
                _ => { },
                _ => { });

            Assert(result.PressedEvents == 1 && pressed.SequenceEqual(new[] { "Y" }), "Suppressing an Escape close edge must leave a simultaneous Y edge available to its ordinary input owner.");
            Assert(!owner.WasPressed("Escape") && owner.WasPressed("Y"), "One-frame suppression must remain button-scoped in helper state.");
        }

        private static void InputLegacyWrapperUsesKeybindRegistry()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.LegacyInput");
            owner.RegisterButton("Plus");
            owner.RegisterKeybind("zoom.increase", "Equals, KeypadPlus", DtmInputScope.Gameplay);

            int buttonPressed = 0;
            int keybindPressed = 0;
            InputFrameResult result = input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("Equals", true, true, false) },
                _ => true,
                button =>
                {
                    Assert(button == "Equals", "Legacy RegisterButton should dispatch the canonical physical button.");
                    buttonPressed++;
                },
                _ => { },
                keybind =>
                {
                    Assert(keybind.KeybindId == "zoom.increase" && keybind.TriggerButton == "Equals", "Typed keybind dispatch should report id and canonical trigger.");
                    keybindPressed++;
                },
                _ => { });

            Assert(result.SampledButtons == 1 && buttonPressed == 1 && keybindPressed == 1, "A single physical Equals press should trigger exactly one legacy button event and one keybind event.");
            Assert(owner.WasPressed("Plus") && owner.IsDown(DtmButton.Parse("Equals")), "Legacy helper state should use canonical button identity.");
            Assert(owner.WasKeybindPressed("zoom.increase"), "Owner helper should expose one-frame keybind pressed state.");
        }

        private static void InputLegacyTrackedButtonDoesNotDispatchKeybindEvent()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.SnapshotOnlyButton");
            owner.RegisterButton("W");

            int buttonPressed = 0;
            int keybindPressed = 0;
            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("W", true, true, false) },
                _ => true,
                _ => buttonPressed++,
                _ => { },
                _ => keybindPressed++,
                _ => { });

            Assert(buttonPressed == 1, "A legacy tracked button should still publish the compatibility ButtonPressed event.");
            Assert(keybindPressed == 0, "A legacy tracked button should not publish typed KeybindPressed events.");
            Assert(owner.IsDown("W") && owner.WasPressed("W"), "A legacy tracked button should still feed snapshot state for movement-cancel checks.");

            owner.UnregisterButton("W");
            Assert(owner.IsDown("W"), "Unregistering must not erase the physical held state; a normal-mode owner query may observe it until neutral.");
            input.ClearFrame();
            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("W", false, false, true) },
                _ => true,
                _ => { },
                _ => { },
                _ => { },
                _ => { });
            Assert(!owner.IsDown("W"), "The retained physical state should clear only after the sampled key becomes neutral.");
        }

        private static void InputPressedEdgeDispatchesWhenAlreadyReleased()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.ShortTap");
            owner.RegisterButton("Y");
            owner.RegisterKeybind("console", "Y", DtmInputScope.Gameplay);
            int buttonPressed = 0;
            int buttonReleased = 0;
            int keybindPressed = 0;

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("Y", false, true, true) },
                _ => true,
                _ => buttonPressed++,
                _ => buttonReleased++,
                _ => keybindPressed++,
                _ => { });

            Assert(buttonPressed == 1, "A native pressed edge should dispatch a legacy button press even when the key is no longer down.");
            Assert(buttonReleased == 0, "A same-frame short tap should not synthesize a release for a key DTMAPI never tracked as down.");
            Assert(keybindPressed == 1, "A single-key keybind should consume PressedEdge even when IsDownNow is false.");
            Assert(owner.WasPressed("Y") && !owner.IsDown("Y"), "Short-tap state should expose pressed-this-frame without sticking down-state.");
        }

        private static void InputRapidRetapDispatchesSecondPress()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.RapidRetap");
            owner.RegisterKeybind("console", "Y", DtmInputScope.Gameplay);
            int keybindPressed = 0;

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("Y", true, true, false) },
                _ => true,
                _ => { },
                _ => { },
                _ => keybindPressed++,
                _ => { });
            input.ClearFrame();

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("Y", true, true, false) },
                _ => true,
                _ => { },
                _ => { },
                _ => keybindPressed++,
                _ => { });

            Assert(keybindPressed == 2, "A second native pressed edge should dispatch even if Core still believed the key was held.");
            Assert(owner.IsKeybindDown("console"), "Rapid retap with final down-state should remain tracked as down.");
        }

        private static void InputKeybindChordUsesPressedEdge()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.Chord");
            owner.RegisterKeybind("toggle", "Ctrl+F6", DtmInputScope.Gameplay);
            int keybindPressed = 0;

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("Control", true, true, false) },
                _ => true,
                _ => { },
                _ => { },
                _ => keybindPressed++,
                _ => { });
            input.ClearFrame();

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[]
                {
                    new InputButtonSample("Control", true, false, false),
                    new InputButtonSample("F6", true, true, false)
                },
                _ => true,
                _ => { },
                _ => { },
                _ => keybindPressed++,
                _ => { });

            Assert(keybindPressed == 1, "A chord should dispatch only when all buttons are down and at least one member has PressedEdge.");
            Assert(owner.WasKeybindPressed("toggle"), "Chord pressed state should be exposed through owner-bound input.");
        }

        private static void InputKeybindChordShortTapDispatchesPressedAndReleased()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.ChordShortTap");
            owner.RegisterKeybind("toggle", "Ctrl+F6", DtmInputScope.Gameplay);
            var events = new List<string>();

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("Control", true, true, false) },
                _ => true,
                _ => { },
                _ => { },
                _ => events.Add("pressed"),
                _ => events.Add("released"));
            input.ClearFrame();

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[]
                {
                    new InputButtonSample("Control", true, false, false),
                    new InputButtonSample("F6", false, true, true)
                },
                _ => true,
                _ => { },
                _ => { },
                keybind => events.Add("pressed:" + keybind.TriggerButton),
                keybind => events.Add("released:" + keybind.TriggerButton));

            Assert(events.SequenceEqual(new[] { "pressed:F6", "released:F6" }), "A chord short tap should dispatch pressed then released even if the tapped key is already up.");
            Assert(owner.WasKeybindPressed("toggle"), "Chord short tap should expose pressed-this-frame.");
            Assert(!owner.IsKeybindDown("toggle"), "Chord short tap should not leave the aggregate keybind stuck down.");
        }

        private static void InputKeybindReleaseEdgeDispatchesAndClearsHeldState()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.ReleaseEdge");
            owner.RegisterKeybind("toggle", "F6", DtmInputScope.Gameplay);
            int keybindReleased = 0;

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("F6", true, true, false) },
                _ => true,
                _ => { },
                _ => { },
                _ => { },
                _ => keybindReleased++);
            Assert(owner.IsKeybindDown("toggle"), "Pressed keybind should be tracked as down before release.");
            input.ClearFrame();

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("F6", false, false, true) },
                _ => true,
                _ => { },
                _ => { },
                _ => { },
                _ => keybindReleased++);

            Assert(keybindReleased == 1, "A native released edge should dispatch KeybindReleased.");
            Assert(!owner.IsKeybindDown("toggle"), "Released edge should clear held keybind state.");
        }

        private static void InputGenericModifiersMatchEitherPhysicalSide()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.GenericModifier");
            owner.RegisterKeybind("toggle", "Ctrl+F6", DtmInputScope.Gameplay);
            int keybindPressed = 0;

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[]
                {
                    new InputButtonSample("RightControl", true, true, false),
                    new InputButtonSample("F6", true, true, false)
                },
                _ => true,
                _ => { },
                _ => { },
                _ => keybindPressed++,
                _ => { });

            Assert(keybindPressed == 1, "Generic Ctrl should match the right physical Control key.");
            Assert(DtmKeybindList.Parse("Ctrl+F6").Keybinds.Single().ContainsButton("LeftControl"), "Generic Ctrl should physically contain LeftControl.");
            Assert(DtmKeybindList.Parse("Ctrl+F6").Keybinds.Single().ContainsButton("RightControl"), "Generic Ctrl should physically contain RightControl.");
        }

        private static void InputOwnerBoundKeybindQueriesAreOwnerScoped()
        {
            var input = new InputService(() => true);
            IInputHelper ownerA = input.CreateOwnerBound("DTMAPI.Tests.OwnerScopedA");
            IInputHelper ownerB = input.CreateOwnerBound("DTMAPI.Tests.OwnerScopedB");
            ownerA.RegisterKeybind("toggle", "F6", DtmInputScope.Gameplay);
            ownerB.RegisterKeybind("toggle", "Y", DtmInputScope.Gameplay);

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("F6", true, true, false) },
                _ => true,
                _ => { },
                _ => { },
                _ => { },
                _ => { });

            Assert(ownerA.WasKeybindPressed("toggle"), "Owner A should see its own keybind state.");
            Assert(!ownerB.WasKeybindPressed("toggle"), "Owner B should not read Owner A's same-id keybind state.");
            Assert(ownerA.IsKeybindDown("toggle"), "Owner A should see its own held keybind.");
            Assert(!ownerB.IsKeybindDown("toggle"), "Owner B should not read Owner A's held keybind.");
            input.ClearFrame();

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[]
                {
                    new InputButtonSample("F6", false, false, true),
                    new InputButtonSample("Y", true, true, false)
                },
                _ => true,
                _ => { },
                _ => { },
                _ => { },
                _ => { });

            Assert(!ownerA.WasKeybindPressed("toggle"), "Owner A should not read Owner B's same-id keybind state.");
            Assert(ownerB.WasKeybindPressed("toggle"), "Owner B should see its own keybind state.");
            Assert(!ownerA.IsKeybindDown("toggle"), "Owner A held state should clear after its release edge.");
            Assert(ownerB.IsKeybindDown("toggle"), "Owner B should see its own held keybind.");
        }

        private static void InputRegistrationUpdatePreservesUnchangedHeldState()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.UpdateHeld");
            IInputRegistration registration = owner.RegisterKeybind("toggle", "F6", DtmInputScope.Gameplay);
            int keybindPressed = 0;

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("F6", true, true, false) },
                _ => true,
                _ => { },
                _ => { },
                _ => keybindPressed++,
                _ => { });
            Assert(owner.IsKeybindDown("toggle"), "Initial held keybind should be tracked as down.");
            input.ClearFrame();

            registration.Update("F6", DtmInputScope.Gameplay);
            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("F6", true, false, false) },
                _ => true,
                _ => { },
                _ => { },
                _ => keybindPressed++,
                _ => { });

            Assert(keybindPressed == 1, "Unchanged registration update while held should not synthesize a second press.");
            Assert(owner.IsKeybindDown("toggle"), "Unchanged registration update should preserve held keybind state.");
        }

        private static void InputOwnerCleanupInvalidatesCachedButtons()
        {
            var input = new InputService(() => true);
            IInputHelper ownerA = input.CreateOwnerBound("DTMAPI.Tests.CacheA");
            IInputHelper ownerB = input.CreateOwnerBound("DTMAPI.Tests.CacheB");
            ownerA.RegisterKeybind("a", "F6", DtmInputScope.Gameplay);
            ownerB.RegisterKeybind("b", "Y", DtmInputScope.Gameplay);
            Assert(input.GetButtonsToSample(DtmInputScope.Gameplay).SequenceEqual(new[] { "F6", "Y" }), "Initial cached gameplay buttons should include both owners.");

            input.RemoveOwner("DTMAPI.Tests.CacheA");
            Assert(input.GetButtonsToSample(DtmInputScope.Gameplay).SequenceEqual(new[] { "Y" }), "Owner cleanup should invalidate cached sampled buttons.");

            ownerB.RegisterKeybind("plus", "Plus", DtmInputScope.Gameplay);
            Assert(input.GetButtonsToSample(DtmInputScope.Gameplay).SequenceEqual(new[] { "Equals", "Y" }), "Alias registration after cleanup should rebuild the cache with canonical buttons.");
        }

        private static void InputBlockedReleaseCleanupDoesNotDispatchOrStick()
        {
            var input = new InputService(() => true);
            input.CreateOwnerBound("DTMAPI.Tests.BlockedRelease").RegisterKeybind("test", "F10", DtmInputScope.Gameplay);
            int pressed = 0;
            int released = 0;

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("F10", true, true, false) },
                _ => true,
                _ => pressed++,
                _ => released++,
                _ => { },
                _ => { });
            Assert(pressed == 1 && input.IsDown("F10"), "Gameplay press should dispatch and set down-state.");

            input.RecordFrame(
                DtmInputScope.SaveLoaded,
                new[] { new InputButtonSample("F10", false, false, true) },
                _ => false,
                _ => pressed++,
                _ => released++,
                _ => { },
                _ => { });
            Assert(released == 0 && !input.IsDown("F10"), "Blocked release should clear down-state without dispatching gameplay input.");
        }

        private static void InputHelperSnapshotMethodsSupportKeybindLists()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.HelperSnapshot");
            DtmKeybindList toggle = DtmKeybindList.Parse("Ctrl+F6");

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[]
                {
                    new InputButtonSample("LeftControl", true, true, false),
                    new InputButtonSample("F6", true, true, false)
                },
                _ => true,
                _ => { },
                _ => { },
                _ => { },
                _ => { });

            DtmButtonState f6 = owner.GetState(DtmButton.Parse("F6"));
            Assert(f6.Button.Equals(DtmButton.Parse("F6")) && f6.IsDown && f6.WasPressed && !f6.WasReleased, "IInputHelper.GetState should expose typed button down/pressed/released state.");
            Assert(toggle.IsDown(owner), "DtmKeybindList.IsDown(input) should match generic modifier aliases against physical side keys.");
            Assert(toggle.JustPressed(owner), "DtmKeybindList.JustPressed(input) should detect a pressed chord through the helper snapshot.");
            Assert(!toggle.JustReleased(owner), "Pressed chord should not report released in the same frame.");
            Assert(!DtmKeybindList.None.IsDown(owner) && !DtmKeybindList.None.JustPressed(owner) && !DtmKeybindList.None.JustReleased(owner), "None keybind lists should remain inert for helper-friendly methods.");

            input.ClearFrame();
            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[]
                {
                    new InputButtonSample("LeftControl", true, false, false),
                    new InputButtonSample("F6", false, false, true)
                },
                _ => true,
                _ => { },
                _ => { },
                _ => { },
                _ => { });

            Assert(!owner.GetState(DtmButton.Parse("F6")).IsDown && owner.WasReleased(DtmButton.Parse("F6")), "IInputHelper.WasReleased(DtmButton) should expose typed release edges.");
            Assert(toggle.JustReleased(owner), "DtmKeybindList.JustReleased(input) should detect a chord release while the modifier is still down.");

            input.ClearTransientState();
            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[]
                {
                    new InputButtonSample("RightShift", true, true, false),
                    new InputButtonSample("A", true, true, false)
                },
                _ => true,
                _ => { },
                _ => { },
                _ => { },
                _ => { });
            Assert(DtmKeybindList.Parse("Shift+A").IsDown(owner), "Generic Shift aliases should match either physical shift key through helper-friendly keybind queries.");
        }

        private static void InputLocalSnapshotQueriesSampleNextFrameWithoutRegisteredRoots()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.LocalSnapshot");
            DtmKeybindList toggle = DtmKeybindList.Parse("F6");

            Assert(input.GetRegisteredButtons().Count == 0, "Local snapshot helper queries should not create registered input roots.");
            Assert(input.GetButtonsToSample(DtmInputScope.Gameplay).Count == 0, "No local snapshot button should be sampled until a mod queries it.");

            Assert(!toggle.JustPressed(owner), "An unobserved local snapshot key should be false on the warm-up frame.");
            input.ClearFrame();
            Assert(input.GetRegisteredButtons().Count == 0, "Warm-up local snapshot queries should still not create registered roots.");
            Assert(input.GetButtonsToSample(DtmInputScope.Gameplay).SequenceEqual(new[] { "F6" }), "A helper query should request the key for the next input frame.");

            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("F6", true, true, false) },
                _ => true,
                _ => { },
                _ => { },
                _ => { },
                _ => { });
            Assert(toggle.JustPressed(owner), "Local snapshot keybind helpers should consume sampled pressed edges without KeybindPressed registration.");
            Assert(input.GetRegisteredButtons().Count == 0, "Local snapshot keybind helpers should remain absent from the registered button union.");

            int removed = input.RemoveOwner("DTMAPI.Tests.LocalSnapshot");
            Assert(removed == 1, "Owner cleanup should remove local snapshot button watches.");
            Assert(
                input.GetRegisteredButtons().Count == 0 &&
                input.GetButtonsToSample(DtmInputScope.Gameplay).SequenceEqual(new[] { "F6" }),
                "Owner cleanup should remove the watch/root immediately while retaining a physically held button only until its release is sampled.");
            input.RecordFrame(
                DtmInputScope.Gameplay,
                new[] { new InputButtonSample("F6", false, false, true) },
                _ => true,
                _ => { },
                _ => { },
                _ => { },
                _ => { });
            input.ClearFrame();
            Assert(
                input.GetButtonsToSample(DtmInputScope.Gameplay).Count == 0,
                "A released button with no owner watch, rearm requirement or settlement ledger must leave the physical sample set.");
        }

        private static void InactiveOwnerBoundInputQueriesCannotRecreateLocalWatch()
        {
            const string ownerId = "DTMAPI.Tests.LocalSnapshot.Inactive";
            bool ownerActive = true;
            var input = new InputService(() => true);
            IInputHelper helper = input.CreateOwnerBound(ownerId, () =>
            {
                if (!ownerActive)
                    throw new InvalidOperationException("owner inactive");
            });
            DtmButton button = DtmButton.Parse("F6");

            helper.GetState(button);
            InputOwnerSnapshot active = input.GetOwnerSnapshot();
            Assert(active.OwnerCount == 1 && active.OwnerRegistrations == 1 && active.ButtonsByOwner.TryGetValue(ownerId, out int activeRoots) && activeRoots == 1, "Owner root counting must include a demand-local typed input watch.");
            Assert(input.GetLocalSnapshotDiagnostics().OwnerCount == 1 && input.GetLocalSnapshotDiagnostics().WatchCount == 1, "Typed input warm-up should create exactly one owner-local watch.");

            Assert(input.RemoveOwner(ownerId) == 1, "Owner cleanup should remove the demand-local typed input watch.");
            ownerActive = false;
            AssertThrows(() => helper.GetState(button), "A stale owner-bound input helper must reject typed state queries after deactivation.");
            AssertThrows(() => DtmKeybindList.Parse("F6").JustPressed(helper), "A stale owner-bound input helper must reject typed keybind-list queries after deactivation.");

            InputOwnerSnapshot inactive = input.GetOwnerSnapshot();
            InputLocalSnapshotDiagnostics diagnostics = input.GetLocalSnapshotDiagnostics();
            Assert(inactive.OwnerCount == 0 && inactive.OwnerRegistrations == 0 && !inactive.ButtonsByOwner.ContainsKey(ownerId) && diagnostics.OwnerCount == 0 && diagnostics.WatchCount == 0, "Rejected stale typed input queries must not recreate an owner-local snapshot root.");
            Assert(input.GetButtonsToSample(DtmInputScope.Gameplay).Count == 0, "Rejected stale typed input queries must not re-add a button to the sampled union.");
        }

        private static void InputLocalSnapshotWarmPathDoesNotAllocate()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.LocalSnapshot.Allocation");
            DtmKeybindList toggle = DtmKeybindList.Parse("F6");
            var samples = new[] { new InputButtonSample("F6", false, false, false) };
            Func<DtmInputScope, bool> allow = _ => true;
            Action<string> ignoreButton = _ => { };
            Action<InputKeybindDispatch> ignoreKeybind = _ => { };

            toggle.JustPressed(owner);
            input.ClearFrame();
            for (int index = 0; index < 256; index++)
            {
                Assert(input.GetButtonsToSample(DtmInputScope.Gameplay).Count == 1, "Warm local snapshot should keep exactly one sampled button.");
                input.RecordFrame(DtmInputScope.Gameplay, samples, allow, ignoreButton, ignoreButton, ignoreKeybind, ignoreKeybind);
                toggle.JustPressed(owner);
                input.ClearFrame();
            }

            InputLocalSnapshotDiagnostics before = input.GetLocalSnapshotDiagnostics();
            IReadOnlyList<string> cachedBefore = input.GetButtonsToSample(DtmInputScope.Gameplay);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 10000; index++)
            {
                input.GetButtonsToSample(DtmInputScope.Gameplay);
                input.RecordFrame(DtmInputScope.Gameplay, samples, allow, ignoreButton, ignoreButton, ignoreKeybind, ignoreKeybind);
                toggle.JustPressed(owner);
                input.ClearFrame();
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            InputLocalSnapshotDiagnostics after = input.GetLocalSnapshotDiagnostics();
            IReadOnlyList<string> cachedAfter = input.GetButtonsToSample(DtmInputScope.Gameplay);
            Assert(allocated == 0, "Warm local snapshot polling should allocate zero bytes; allocated=" + allocated.ToString(CultureInfo.InvariantCulture) + ".");
            Assert(ReferenceEquals(cachedBefore, cachedAfter), "Stable local snapshot membership should preserve the cached sampled-button array instance.");
            Assert(before.CacheRebuildCount == after.CacheRebuildCount, "Stable local snapshot membership should not rebuild the active union cache.");
            Assert(after.OwnerCount == 1 && after.WatchCount == 1 && after.ActiveButtonCount == 1, "Warm local snapshot diagnostics should remain bounded to one owner/watch/button.");

            input.ClearFrame();
            Assert(input.GetButtonsToSample(DtmInputScope.Gameplay).Count == 0, "A local snapshot watch not queried in the previous update should leave the active sample union.");
            input.ClearFrame();
            input.ClearFrame();
            InputLocalSnapshotDiagnostics expired = input.GetLocalSnapshotDiagnostics();
            Assert(expired.OwnerCount == 0 && expired.WatchCount == 0 && expired.ExpiredWatchCount == 1, "Dormant local snapshot watches should be pruned after two inactive generations.");
        }

        private static void InputRegisteredKeybindWarmPathDoesNotAllocate()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.Registered.Allocation");
            owner.RegisterKeybind("toggle", "F6", DtmInputScope.Gameplay);
            var samples = new[] { new InputButtonSample("F6", false, false, false) };
            Func<DtmInputScope, bool> allow = _ => true;
            Action<string> ignoreButton = _ => { };
            Action<InputKeybindDispatch> ignoreKeybind = _ => { };

            Assert(input.GetButtonsToSample(DtmInputScope.Title).Count == 0, "A Gameplay-scoped registered hotkey must not be sampled on the title screen.");
            for (int index = 0; index < 256; index++)
            {
                input.GetButtonsToSample(DtmInputScope.Gameplay);
                input.RecordFrame(DtmInputScope.Gameplay, samples, allow, ignoreButton, ignoreButton, ignoreKeybind, ignoreKeybind);
                input.ClearFrame();
            }

            IReadOnlyList<string> cachedBefore = input.GetButtonsToSample(DtmInputScope.Gameplay);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 10000; index++)
            {
                input.GetButtonsToSample(DtmInputScope.Gameplay);
                input.RecordFrame(DtmInputScope.Gameplay, samples, allow, ignoreButton, ignoreButton, ignoreKeybind, ignoreKeybind);
                input.ClearFrame();
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            IReadOnlyList<string> cachedAfter = input.GetButtonsToSample(DtmInputScope.Gameplay);
            Assert(allocated == 0, "Warm registered keybind polling should allocate zero Core bytes after caching the registration key and direct state evaluation; allocated=" + allocated.ToString(CultureInfo.InvariantCulture) + ".");
            Assert(ReferenceEquals(cachedBefore, cachedAfter), "Stable registered keybind membership should preserve the cached sampled-button array instance.");
        }

        private static void InputAudienceSnapshotRemainsImmutableForTheFrame()
        {
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound("DTMAPI.Tests.Audience.Owner");
            owner.RegisterButton("F10");
            int pressed = 0;
            Func<InputButtonDispatch, EventDeliveryReceipt> press = dispatch =>
            {
                pressed++;
                return new EventDeliveryReceipt(new[] { "DTMAPI.Tests.Audience.Owner" });
            };
            Action<InputReleaseDispatch> release = _ => { };
            Func<InputKeybindDispatch, string, EventDeliveryReceipt> keyPress = (_, _) => EventDeliveryReceipt.Empty;
            Action<InputKeybindReleaseDispatch> keyRelease = _ => { };

            var normal = new InputAudienceSnapshot(InputAudienceMode.Normal, string.Empty, DtmInputScope.Gameplay, 1);
            var platform = new InputAudienceSnapshot(InputAudienceMode.PlatformModal, string.Empty, DtmInputScope.SaveLoaded, 2);
            Assert(input.GetButtonsToSample(normal).SequenceEqual(new[] { "F10" }), "Normal audience should sample its active registered button.");
            input.RecordFrame(
                platform,
                new[] { new InputButtonSample("F10", false, true, true) },
                press,
                release,
                keyPress,
                keyRelease);
            Assert(pressed == 1, "Opening a platform UI after sampling must not mutate the already-frozen normal audience.");

            input.ClearFrame();
            Assert(input.GetButtonsToSample(platform).SequenceEqual(new[] { "F10" }), "A platform-modal frame may keep a rearm button sampled without making it eligible for Mod input.");
            input.RecordFrame(
                normal,
                new[] { new InputButtonSample("F10", false, true, true) },
                press,
                release,
                keyPress,
                keyRelease);
            Assert(pressed == 1, "Closing the platform UI after sampling must take effect only on the next frame.");
        }

        private static void InputOwnerModalTargetsQueriesAndSettlesRelease()
        {
            const string ownerAId = "DTMAPI.Tests.Modal.OwnerA";
            const string ownerBId = "DTMAPI.Tests.Modal.OwnerB";
            var input = new InputService(() => true);
            IInputHelper ownerA = input.CreateOwnerBound(ownerAId);
            IInputHelper ownerB = input.CreateOwnerBound(ownerBId);
            ownerA.RegisterKeybind("close", "Y", DtmInputScope.SaveLoaded);
            ownerB.RegisterKeybind("observe", "Y", DtmInputScope.SaveLoaded);

            int ownerAPressed = 0;
            int ownerAReleased = 0;
            int ownerBPressed = 0;
            int ownerBReleased = 0;
            Func<InputButtonDispatch, EventDeliveryReceipt> press = dispatch =>
            {
                Assert(dispatch.TargetOwnerId == ownerAId, "Owner-modal button delivery must target only the modal owner.");
                ownerAPressed++;
                return new EventDeliveryReceipt(new[] { ownerAId });
            };
            Action<InputReleaseDispatch> release = dispatch =>
            {
                Assert(dispatch.IsSettlement && dispatch.RecipientOwnerIds.SequenceEqual(new[] { ownerAId }), "Release must settle only the owner that received Pressed.");
                ownerAReleased++;
            };
            Func<InputKeybindDispatch, string, EventDeliveryReceipt> keyPress = (dispatch, targetOwnerId) =>
            {
                Assert(dispatch.OwnerId == ownerAId && targetOwnerId == ownerAId, "Owner-modal keybind delivery must be registration- and event-owner targeted.");
                return new EventDeliveryReceipt(new[] { ownerAId });
            };
            Action<InputKeybindReleaseDispatch> keyRelease = dispatch =>
            {
                Assert(dispatch.IsSettlement && dispatch.RecipientOwnerIds.SequenceEqual(new[] { ownerAId }), "Keybind Release must retain the exact Pressed receipt.");
            };

            var ownerModal = new InputAudienceSnapshot(InputAudienceMode.OwnerModal, ownerAId, DtmInputScope.SaveLoaded, 10);
            input.GetButtonsToSample(ownerModal);
            input.RecordFrame(
                ownerModal,
                new[] { new InputButtonSample("Y", true, true, false) },
                press,
                release,
                keyPress,
                keyRelease);
            Assert(ownerAPressed == 1 && ownerA.IsDown("Y") && ownerA.WasPressed("Y"), "Modal owner should receive and query its registered non-gameplay input.");
            Assert(!ownerB.IsDown("Y") && !ownerB.WasPressed("Y"), "A non-owner must see neutral state even when it registered the same button.");
            ownerB.Suppress("Y");
            Assert(ownerA.IsDown("Y"), "A non-owner Suppress call must be inert during an owner modal.");
            ownerA.Suppress("Y");
            Assert(!ownerA.IsDown("Y") && !ownerA.WasPressed("Y"), "The modal owner may suppress only its current-frame visible state.");

            input.ClearFrame();
            var normal = new InputAudienceSnapshot(InputAudienceMode.Normal, string.Empty, DtmInputScope.Gameplay, 11);
            input.GetButtonsToSample(normal);
            input.RecordFrame(
                normal,
                new[] { new InputButtonSample("Y", false, false, true) },
                dispatch =>
                {
                    ownerBPressed++;
                    return new EventDeliveryReceipt(new[] { ownerAId, ownerBId });
                },
                release,
                keyPress,
                keyRelease);
            Assert(ownerAReleased == 1 && ownerBReleased == 0, "Closing the modal before physical release must still settle only the prior Pressed audience.");
            Assert(ownerA.WasReleased("Y") && !ownerB.WasReleased("Y"), "Settlement Release visibility must be exact-owner even after eligibility changes.");
            Assert(ownerBPressed == 0, "A release after modal close must not synthesize a Pressed for the newly eligible owner.");
        }

        private static void InputConfigurationUpdateRetainsPressedBindingForRelease()
        {
            const string ownerId = "DTMAPI.Tests.ConfigLedger";
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound(ownerId);
            IInputRegistration registration = owner.RegisterKeybind("toggle", "F6", DtmInputScope.Gameplay);
            string releasedBinding = string.Empty;
            int presses = 0;
            Func<InputButtonDispatch, EventDeliveryReceipt> buttonPress = _ => EventDeliveryReceipt.Empty;
            Action<InputReleaseDispatch> buttonRelease = _ => { };
            Func<InputKeybindDispatch, string, EventDeliveryReceipt> keyPress = (dispatch, _) =>
            {
                presses++;
                return new EventDeliveryReceipt(new[] { ownerId });
            };
            Action<InputKeybindReleaseDispatch> keyRelease = dispatch => releasedBinding = dispatch.Keybind.Keybinds.ToString();
            var normal = new InputAudienceSnapshot(InputAudienceMode.Normal, string.Empty, DtmInputScope.Gameplay, 20);

            input.GetButtonsToSample(normal);
            input.RecordFrame(
                normal,
                new[] { new InputButtonSample("F6", true, true, false) },
                buttonPress,
                buttonRelease,
                keyPress,
                keyRelease);
            Assert(presses == 1, "Initial configured keybind should deliver Pressed.");
            input.ClearFrame();

            registration.Update("Y", DtmInputScope.Gameplay);
            Assert(input.GetButtonsToSample(normal).OrderBy(value => value).SequenceEqual(new[] { "F6", "Y" }), "A changed binding must keep the old pressed chord sampled until settlement.");
            input.RecordFrame(
                normal,
                new[]
                {
                    new InputButtonSample("F6", false, false, true),
                    new InputButtonSample("Y", false, false, false)
                },
                buttonPress,
                buttonRelease,
                keyPress,
                keyRelease);
            Assert(releasedBinding == "F6", "Configuration mutation must release the exact old binding snapshot, not the replacement binding.");
            input.ClearFrame();

            input.GetButtonsToSample(normal);
            input.RecordFrame(
                normal,
                new[] { new InputButtonSample("Y", true, true, false) },
                buttonPress,
                buttonRelease,
                keyPress,
                keyRelease);
            Assert(presses == 2, "After a neutral sample, the replacement binding should begin a new input cycle normally.");
        }

        private static void InputSuppressionPreservesPhysicalCycleAndOwedRelease()
        {
            const string ownerId = "DTMAPI.Tests.SuppressLedger";
            var input = new InputService(() => true);
            IInputHelper owner = input.CreateOwnerBound(ownerId);
            owner.RegisterButton("F6");
            int presses = 0;
            int releases = 0;
            var normal = new InputAudienceSnapshot(InputAudienceMode.Normal, string.Empty, DtmInputScope.Gameplay, 30);
            Func<InputButtonDispatch, EventDeliveryReceipt> press = _ =>
            {
                presses++;
                return new EventDeliveryReceipt(new[] { ownerId });
            };
            Action<InputReleaseDispatch> release = _ => releases++;
            Func<InputKeybindDispatch, string, EventDeliveryReceipt> keyPress = (_, _) => EventDeliveryReceipt.Empty;
            Action<InputKeybindReleaseDispatch> keyRelease = _ => { };

            input.GetButtonsToSample(normal);
            input.RecordFrame(normal, new[] { new InputButtonSample("F6", true, true, false) }, press, release, keyPress, keyRelease);
            owner.Suppress("F6");
            Assert(!owner.IsDown("F6") && !owner.WasPressed("F6"), "Suppress should hide current owner-visible state after delivery.");
            input.ClearFrame();

            Assert(input.GetButtonsToSample(normal).Contains("F6", StringComparer.OrdinalIgnoreCase), "A physically held and release-owed button must remain sampled after suppression.");
            input.RecordFrame(normal, new[] { new InputButtonSample("F6", true, false, false) }, press, release, keyPress, keyRelease);
            Assert(presses == 1 && owner.IsDown("F6") && !owner.WasPressed("F6"), "Suppression must not erase physical down or manufacture a next-frame Pressed.");
            input.ClearFrame();

            input.GetButtonsToSample(normal);
            input.RecordFrame(normal, new[] { new InputButtonSample("F6", false, false, true) }, press, release, keyPress, keyRelease);
            Assert(releases == 1 && owner.WasReleased("F6"), "Suppress must not cancel the exact settlement Release owed by the prior Pressed.");
        }

        private static void InputAudienceSteadyStateDoesNotAllocate()
        {
            const string ownerId = "DTMAPI.Tests.Audience.Allocation";
            var input = new InputService(() => true);
            input.CreateOwnerBound(ownerId).RegisterKeybind("toggle", "F6", DtmInputScope.SaveLoaded);
            var samples = new[] { new InputButtonSample("F6", false, false, false) };
            Func<InputButtonDispatch, EventDeliveryReceipt> buttonPress = _ => EventDeliveryReceipt.Empty;
            Action<InputReleaseDispatch> buttonRelease = _ => { };
            Func<InputKeybindDispatch, string, EventDeliveryReceipt> keyPress = (_, _) => EventDeliveryReceipt.Empty;
            Action<InputKeybindReleaseDispatch> keyRelease = _ => { };
            var normal = new InputAudienceSnapshot(InputAudienceMode.Normal, string.Empty, DtmInputScope.Gameplay, 40);
            var modal = new InputAudienceSnapshot(InputAudienceMode.OwnerModal, ownerId, DtmInputScope.SaveLoaded, 41);

            for (int index = 0; index < 256; index++)
            {
                input.GetButtonsToSample(normal);
                input.RecordFrame(normal, samples, buttonPress, buttonRelease, keyPress, keyRelease);
                input.ClearFrame();
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long normalBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 10000; index++)
            {
                input.GetButtonsToSample(normal);
                input.RecordFrame(normal, samples, buttonPress, buttonRelease, keyPress, keyRelease);
                input.ClearFrame();
            }
            long normalAllocated = GC.GetAllocatedBytesForCurrentThread() - normalBefore;
            Assert(normalAllocated == 0, "10,000 stable normal-audience frames should allocate zero Core bytes; allocated=" + normalAllocated.ToString(CultureInfo.InvariantCulture) + ".");

            for (int index = 0; index < 256; index++)
            {
                input.GetButtonsToSample(modal);
                input.RecordFrame(modal, samples, buttonPress, buttonRelease, keyPress, keyRelease);
                input.ClearFrame();
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long modalBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 10000; index++)
            {
                input.GetButtonsToSample(modal);
                input.RecordFrame(modal, samples, buttonPress, buttonRelease, keyPress, keyRelease);
                input.ClearFrame();
            }
            long modalAllocated = GC.GetAllocatedBytesForCurrentThread() - modalBefore;
            Assert(modalAllocated == 0, "10,000 stable owner-modal frames should allocate zero Core bytes; allocated=" + modalAllocated.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private static void Suppress_OneFrame_ClearsAfterUpdate()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                IInputHelper input = GetInput(runtime);
                IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.Suppress");
                int pressed = 0;
                int released = 0;
                events.Input.ButtonPressed += (_, _) => pressed++;
                events.Input.ButtonReleased += (_, _) => released++;

                runtime.Start();
                runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "unit test");
                input.RegisterButton("F10");
                input.Suppress(" F10 ");
                runtime.RecordInputFrame(new[] { new InputButtonSample("F10", true, true, false) });
                runtime.RecordInputFrame(new[] { new InputButtonSample("F10", false, false, true) });
                Assert(pressed == 0 && released == 0, "Suppressed input should not dispatch DTMAPI input events during the current frame.");
                Assert(!input.WasPressed("F10"), "Suppressed input should not be reported as pressed.");
                Assert(!input.IsDown("F10"), "Suppressed input should not leave a down-state.");
                Assert(input.GetSuppressedButtons().Contains("F10", StringComparer.OrdinalIgnoreCase), "Suppressed input should be visible during the current frame.");

                runtime.Update();
                Assert(!input.WasPressed("F10"), "Runtime update should clear one-frame pressed input state.");
                Assert(!input.GetSuppressedButtons().Contains("F10", StringComparer.OrdinalIgnoreCase), "Runtime update should clear one-frame suppressed input state.");
                Assert(!input.IsDown("F10"), "Suppressed input should remain released after the frame clears.");

                runtime.RecordInputFrame(new[] { new InputButtonSample("F10", true, true, false) });
                Assert(pressed == 1 && input.WasPressed("F10") && input.IsDown("F10"), "Input should dispatch normally after one-frame suppression clears.");
                input.Suppress("F10");
                Assert(!input.WasPressed("F10") && !input.IsDown("F10"), "Suppressing after a press should clear helper pressed/down state even though the already-dispatched event cannot be undone.");
                runtime.RecordInputFrame(new[] { new InputButtonSample("F10", false, false, true) });
                Assert(released == 1, "One-frame suppression must not cancel the settlement Release owed to an owner that received Pressed.");
                runtime.Update();
                runtime.RecordInputFrame(new[] { new InputButtonSample("F10", true, true, false) });
                runtime.RecordInputFrame(new[] { new InputButtonSample("F10", false, false, true) });
                Assert(pressed == 2 && released == 2, "Input release should dispatch normally after suppression clears in addition to the prior settlement Release.");
                Assert(!input.IsDown("F10"), "Released input should clear the down-state.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }
    }
}
