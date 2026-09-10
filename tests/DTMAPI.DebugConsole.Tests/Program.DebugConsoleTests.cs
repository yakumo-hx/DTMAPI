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
using DTMAPI.DebugConsole;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static DebugConsoleUi CreateDebugConsoleUi(
            DtmApiRuntime runtime) =>
            new DebugConsoleUi(
                new CompatibilityDebugConsoleRuntimeAdapter(
                    runtime,
                    installNativeHooks: false));

        private sealed class UnitOwnerBoundDebugConsoleHost :
            IDebugConsoleApi,
            IOwnerBoundApiHost
        {
            private readonly DebugConsoleUi ui;

            internal UnitOwnerBoundDebugConsoleHost(DebugConsoleUi ui)
            {
                this.ui = ui;
            }

            public bool IsOpen => ui.IsOpen;
            public void Bind(IManifest owner, IInventoryDebugApi? inventoryApi, IWeatherDebugApi? weatherApi, ITeleportDebugApi? teleportApi, ITimeDebugApi? timeApi, IMovementDebugApi? movementApi, IInstantSaveDebugApi? instantSaveApi = null) =>
                ui.Bind(owner, null, null, null, null, null, null);
            public void BindAdvanced(IManifest owner, IAdvancedDebugApi? advancedDebugApi) => ui.BindAdvanced(owner, null);
            // Frozen ABI entry point. Product language now follows the game locale.
            public void SetLanguage(IManifest owner, string language) { }
            public void Open(IManifest owner, string reason) => ui.Open(owner, reason);
            public void Close(IManifest owner, string reason) => ui.Close(owner, reason);
            public void Toggle(IManifest owner, string reason) => ui.Toggle(owner, reason);
            public BridgeFeatureStatus GetStatus(string uniqueId) => ui.GetStatus(uniqueId);
            public int CountOwnerResources(string ownerId) => ui.CountOwnerResources(ownerId);
            public int RemoveOwner(string ownerId, string reason) => ui.RemoveOwner(ownerId, reason);
            internal void Update() => ui.Update();
        }

        private static void RuntimeApiCanRegisterBeforeStart()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            IManifest bridgeManifest = new ManifestModel
            {
                Name = "Bridge",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.GameBridge.DolocTown",
                Type = "RuntimeApi"
            };
            runtime.RegisterRuntimeApi<IActionCompletionApi>(bridgeManifest, new FakeActionCompletionApi());
            var debugConsoleHost =
                new UnitOwnerBoundDebugConsoleHost(
                    CreateDebugConsoleUi(runtime));
            IManifest debugConsoleManifest = new ManifestModel
            {
                Name = "Debug Console Host",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.DebugConsoleHost",
                Type = "RuntimeApi"
            };
            runtime.RegisterRuntimeApi<IDebugConsoleApi>(debugConsoleManifest, debugConsoleHost, OwnerBoundGameBridgeApis.ForDebugConsole(debugConsoleHost));
            WriteAtomicProbePackage(dir, debugConsoleManifest.UniqueID, string.Empty);
            WriteAtomicProbePackage(dir, "DTMAPI.Smoke.OwnerLifetime.Control", string.Empty);
            runtime.Start();
            RuntimeSnapshot snapshot = runtime.CreateSnapshot();
            Assert(snapshot.Registry.Any(m => m.UniqueID == "DTMAPI.GameBridge.DolocTown"), "Runtime API manifest should stay registered after Start.");
            Assert(ReferenceEquals(runtime.ModRegistry.GetApi<IDebugConsoleApi>(debugConsoleManifest.UniqueID), debugConsoleHost), "An ordinary Mod collision must not replace the process-lifetime DebugConsole provider.");
            Assert(snapshot.Registry.Single(m => m.UniqueID == debugConsoleManifest.UniqueID).Type == "RuntimeApi" && !snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == debugConsoleManifest.UniqueID), "Reserved provider collision must preserve the runtime manifest and skip ordinary owner publication.");
            Assert(snapshot.Errors.Any(error => error.Owner == debugConsoleManifest.UniqueID && error.Message.Contains("保留", StringComparison.Ordinal)), "Reserved DebugConsole provider collisions should remain visible in diagnostics.");
            Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID.StartsWith("DTMAPI.Smoke.", StringComparison.OrdinalIgnoreCase)) && snapshot.Errors.Any(error => error.Owner == "DTMAPI.Smoke.OwnerLifetime.Control" && error.Message.Contains("保留", StringComparison.Ordinal)), "Internal smoke owner namespace collisions must be rejected before they can share or clean synthetic owner roots.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DebugConsoleResponsiveLayoutKeepsPhysicalTargetsAndStableBreakpoints()
        {
            DebugConsoleLayout lowResolution =
                DebugConsoleLayout.Create(1280, 720, 0, 0, 1280, 720);
            DebugConsoleLayout compactResolution =
                DebugConsoleLayout.Create(1024, 768, 0, 0, 1024, 768);
            Assert(
                lowResolution.Breakpoint == DebugConsoleBreakpoint.Tabs &&
                compactResolution.Breakpoint == DebugConsoleBreakpoint.Compact &&
                lowResolution.MinimumClickSize * lowResolution.ScaleFactor >= 43.999d &&
                compactResolution.MinimumClickSize * compactResolution.ScaleFactor >= 43.999d,
                "Low physical resolutions must use tabs/compact scrolling instead of being promoted back to the three-column layout by CanvasScaler coordinates.");

            DebugConsoleLayout[] proportionalWide =
            {
                DebugConsoleLayout.Create(1920, 1080, 0, 0, 1920, 1080),
                DebugConsoleLayout.Create(2560, 1440, 0, 0, 2560, 1440),
                DebugConsoleLayout.Create(3840, 2160, 0, 0, 3840, 2160),
                DebugConsoleLayout.Create(3440, 1440, 0, 0, 3440, 1440)
            };
            foreach (DebugConsoleLayout candidate in proportionalWide)
            {
                Assert(
                    candidate.Breakpoint == DebugConsoleBreakpoint.Wide &&
                    candidate.CatalogColumns >= 3 && candidate.CatalogColumns <= 8 &&
                    candidate.CatalogRows >= 3 && candidate.CatalogRows <= 6 &&
                    candidate.CatalogPageSize == candidate.CatalogColumns * candidate.CatalogRows &&
                    candidate.MinimumClickSize * candidate.ScaleFactor >= 43.999d &&
                    candidate.AnchorMinX >= 0f && candidate.AnchorMinY >= 0f &&
                    candidate.AnchorMaxX <= 1f && candidate.AnchorMaxY <= 1f,
                    "Responsive layout must preserve the wide logical surface, 3-8 by 3-6 paging, safe anchors, and 44 physical-pixel targets across standard, 4K and ultrawide resolutions.");
            }
            DebugConsoleLayout standardWide = proportionalWide[0];
            Assert(
                Math.Abs(standardWide.LogicalWidth - 1500d) < 0.001d &&
                Math.Abs(standardWide.LogicalHeight - 820d) < 0.001d &&
                standardWide.CatalogColumns == 6 &&
                standardWide.CatalogRows == 5 &&
                standardWide.CatalogPageSize == 30 &&
                standardWide.AnchorMaxX - standardWide.AnchorMinX >= 0.70f &&
                standardWide.AnchorMaxX - standardWide.AnchorMinX <= 0.80f &&
                standardWide.AnchorMaxY - standardWide.AnchorMinY >= 0.70f &&
                standardWide.AnchorMaxY - standardWide.AnchorMinY <= 0.80f,
                "The 1920x1080 wide panel must be centered at roughly 70-80 percent of the screen with a 6x5 square-card page instead of covering the safe area.");

            DebugConsoleLayout tabs = DebugConsoleLayout.Create(
                1920, 1080, 340, 0, 1200, 1080);
            DebugConsoleLayout compact = DebugConsoleLayout.Create(
                1920, 1080, 510, 0, 900, 1080);
            Assert(
                tabs.Breakpoint == DebugConsoleBreakpoint.Tabs &&
                compact.Breakpoint == DebugConsoleBreakpoint.Compact &&
                tabs.MinimumClickSize * tabs.ScaleFactor >= 43.999d &&
                compact.MinimumClickSize * compact.ScaleFactor >= 43.999d &&
                tabs.AnchorMinX > 0f && tabs.AnchorMaxX < 1f &&
                compact.AnchorMinX > tabs.AnchorMinX,
                "Safe-area logical widths must select tabs and compact layouts without shrinking physical click targets.");
        }

        private static void DebugConsoleMonsterPositionConvertsToExactNativeVector2()
        {
            var source = new DebugConsoleAgentPositionFixture
            {
                x = 12.5f,
                y = -3.25f,
                z = 99f
            };
            object? converted = DebugConsoleNativeAccess.Vector2(
                source,
                typeof(UnityEngine.Vector2));
            Assert(
                converted != null &&
                converted.GetType() == typeof(UnityEngine.Vector2) &&
                Math.Abs(DebugConsoleNativeAccess.Vector(converted, "x") - 12.5d) < 0.0001d &&
                Math.Abs(DebugConsoleNativeAccess.Vector(converted, "y") + 3.25d) < 0.0001d,
                "Reflected monster creation must pass an exact native Vector2 built from AgentPosition x/y instead of relying on MethodInfo.Invoke to apply Vector3 conversion operators.");
            Assert(
                DebugConsoleNativeAccess.Vector2(source, typeof(DebugConsoleAgentPositionFixture)) == null &&
                DebugConsoleNativeAccess.Vector2(new { x = 1f }, typeof(UnityEngine.Vector2)) == null,
                "Monster position conversion must fail closed for the wrong target type or an incomplete source vector.");
        }

        private static void DebugConsoleLocalizedContentHasOneNineLanguageKeySet()
        {
            string product = Path.Combine(
                FindRepositoryRoot(),
                "products",
                "first-party",
                "DebugConsole");
            string[] uiLocales =
            {
                "german", "english", "french", "japanese", "koreana",
                "brazilian", "russian", "schinese", "tchinese"
            };
            Dictionary<string, string>? expected = null;
            foreach (string locale in uiLocales)
            {
                Dictionary<string, string> values = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(Path.Combine(product, "i18n", locale + ".json"))) ??
                    throw new InvalidDataException("Missing DebugConsole translation dictionary for " + locale + ".");
                Assert(
                    values.Count == 171 &&
                    values.All(pair => !string.IsNullOrWhiteSpace(pair.Value)) &&
                    values.ContainsKey("debug.spawn.complete") &&
                    values.ContainsKey("debug.spawn.partial") &&
                    !values.ContainsKey("debug.spawn.rollbackFailed") &&
                    !values.ContainsKey("debug.spawn.disabledAfterRollback") &&
                    !values.ContainsKey("debug.teleport.currentOnly") &&
                    !values.ContainsKey("debug.teleport.current") &&
                    !values.ContainsKey("debug.teleport.exportCsv") &&
                    !values.ContainsKey("debug.teleport.exportedCsv") &&
                    !values.ContainsKey("debug.teleport.exportCsvFailed"),
                    "Every DebugConsole UI locale must contain the same 171 non-empty keys, retain visible complete/partial spawn text, and omit retired rollback/circuit/current-location/CSV copy.");
                if (expected == null)
                {
                    expected = values;
                    continue;
                }
                Assert(
                    values.Keys.OrderBy(value => value, StringComparer.Ordinal)
                        .SequenceEqual(expected.Keys.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal),
                    "All nine DebugConsole UI locales must have an identical key set.");
                foreach (string key in expected.Keys)
                {
                    string[] expectedPlaceholders = Regex.Matches(expected[key], @"\{\d+(?::[^}]*)?\}")
                        .Select(match => match.Value)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray();
                    string[] actualPlaceholders = Regex.Matches(values[key], @"\{\d+(?::[^}]*)?\}")
                        .Select(match => match.Value)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray();
                    Assert(
                        actualPlaceholders.SequenceEqual(expectedPlaceholders, StringComparer.Ordinal),
                        "Translation placeholders drifted for " + locale + ":" + key + ".");
                }
            }

            string[] contentLocales =
            {
                "de", "en", "fr", "ja", "ko", "pt_br", "ru", "zh_cn", "zh_tw"
            };
            foreach (string locale in contentLocales)
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(
                    Path.Combine(product, "Content", "localization_tbtextmapper" + locale + ".json")));
                JsonElement[] rows = document.RootElement.EnumerateArray().ToArray();
                Assert(
                    rows.Length == 2 &&
                    rows.Select(row => row.GetProperty("key").GetString()).SequenceEqual(new[]
                    {
                        "item_dtmapi_creative_generator",
                        "item_dtmapi_creative_generator_desc"
                    }) &&
                    rows.All(row => !string.IsNullOrWhiteSpace(row.GetProperty("text").GetString())),
                    "Every official-content locale must contain exactly the Infinite Fuel title and description keys.");
            }

            using JsonDocument itemDocument = JsonDocument.Parse(File.ReadAllText(
                Path.Combine(product, "Content", "item_tbitem.json")));
            JsonElement item = itemDocument.RootElement[0];
            Assert(
                item.GetProperty("id").GetString() == "dtmapi_creative_generator" &&
                item.GetProperty("electric_energy").GetInt64() == 1_000_000_000L &&
                item.GetProperty("overlay").GetInt32() == 999 &&
                item.GetProperty("ui_sprite_asset").GetProperty("url").GetString() == "icon_item_coal" &&
                item.GetProperty("function").GetProperty("$type").GetString() == "ItemFunction" &&
                !item.GetProperty("salable").GetBoolean() &&
                !item.GetProperty("consumable").GetBoolean() &&
                !item.GetProperty("cookable").GetBoolean(),
                "Infinite Fuel must preserve its save-compatible ID, ordinary 999 stack limit, finite one-billion fuel value, coal art and fuel-only semantics.");
        }

        private static void DebugConsoleRawYCloseDelegatesToOwnerKeybind()
        {
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            var host = CreateDebugConsoleUi(runtime);

            string repo = FindRepositoryRoot();
            string rawInputSource = File.ReadAllText(Path.Combine(
                repo,
                "products",
                "first-party",
                "DebugConsole",
                "src",
                "Ui",
                "DebugConsoleRawInput.cs"));
            string uiSource = string.Join(
                "\n",
                Directory.GetFiles(
                        Path.Combine(
                            repo,
                            "products",
                            "first-party",
                            "DebugConsole",
                            "src",
                            "Ui"),
                        "DebugConsoleUi*.cs",
                        SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
            Assert(rawInputSource.Contains(
                    "internal static DebugConsoleRawInputFrame Sample(",
                    StringComparison.Ordinal) &&
                rawInputSource.Contains(
                    "private static readonly MethodInfo? LegacyGetKeyDown",
                    StringComparison.Ordinal) &&
                rawInputSource.Contains(
                    "private static readonly object? BoxedEscapeKey",
                    StringComparison.Ordinal) &&
                rawInputSource.Contains(
                    "private struct InputSystemFrameContext",
                    StringComparison.Ordinal) &&
                rawInputSource.Contains(
                    "keyboard = keyboardCurrentProperty?.GetValue(null, null);",
                    StringComparison.Ordinal) &&
                uiSource.Contains(
                    "DebugConsoleRawInput.Sample(inputMask)",
                    StringComparison.Ordinal) &&
                uiSource.Contains(
                    "return GetOrCreateLayout(",
                    StringComparison.Ordinal) &&
                uiSource.Contains(
                    "object? currentEventSystem = GetCurrentEventSystem();",
                    StringComparison.Ordinal) &&
                uiSource.Contains(
                    "gameObjectActiveSelfProperty?.GetValue(go, null)",
                    StringComparison.Ordinal),
                "The ProductNative UI must sample one cached raw-input frame, keep Keyboard.current device reads dynamic, reuse unchanged layout geometry, prefer EventSystem.current, and skip redundant SetActive calls.");

            Assert(!host.TryConsumeRawYKeyDown(yIsDown: true, textInputFocused: false), "An ordinary Y close edge must remain available to DebugConsoleMod's owner-bound typed toggle keybind.");

            SetPrivateField(host, "suppressYCloseUntilReleased", true);
            Assert(host.TryConsumeRawYKeyDown(yIsDown: true, textInputFocused: false), "The UI host must consume a repeated callback from the opening Y physical cycle.");
            Assert(host.TryConsumeRawYKeyDown(yIsDown: false, textInputFocused: false) && !GetPrivateField<bool>(host, "suppressYCloseUntilReleased"), "A released opening cycle must clear the opener guard without leaking that edge to the close toggle.");

            Assert(host.TryConsumeRawYKeyDown(yIsDown: true, textInputFocused: true), "The UI host must keep Y with a focused text input instead of closing the console.");
            Assert(!host.TryConsumeRawYKeyDown(yIsDown: true, textInputFocused: false), "After opener/focus guards clear, a later Y press must reach the typed toggle exactly once.");

            List<object> trackedInputs = GetPrivateField<List<object>>(host, "inputFields");
            trackedInputs.Add(new FakeDebugConsoleInputField(true));
            Assert(host.TryConsumeFocusedTextInputY(), "The typed Y handler must consume the edge before Toggle when the console search field is focused.");
            trackedInputs.Clear();
            trackedInputs.Add(new FakeDebugConsoleInputField(false));
            Assert(!host.TryConsumeFocusedTextInputY(), "An unfocused search field must leave Y available to the typed toggle.");

            var owner = new ManifestModel
            {
                Name = "Legacy Debug Console",
                Author = "DTMAPI",
                Version = "0.5.2",
                UniqueID = "DTMAPI.DebugConsoleMod",
                Type = "CodeMod"
            };
            SetPrivateField(host, "ownerManifest", owner);
            IInputHelper ownerInput = runtime.Input.CreateOwnerBound(owner.UniqueID);
            ownerInput.RegisterButton("Y");
            Assert(host.ShouldUseLegacyYCloseFallback(), "A retained DebugConsole DLL with only RegisterButton(Y) must receive the narrow raw-close compatibility fallback.");

            const string otherOwnerId = "DTMAPI.Tests.OtherLegacyY";
            int ownerPressed = 0;
            int otherPressed = 0;
            IEventsHelper ownerEvents = runtime.Events.CreateOwnerBoundProxy(owner.UniqueID, () => { });
            IEventsHelper otherEvents = runtime.Events.CreateOwnerBoundProxy(otherOwnerId, () => { });
            runtime.Input.CreateOwnerBound(otherOwnerId).RegisterButton("Y");
            ownerEvents.Input.ButtonPressed += (_, e) =>
            {
                if (e.Button.Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    ownerPressed++;
                    runtime.UI.Close();
                }
            };
            otherEvents.Input.ButtonPressed += (_, e) =>
            {
                if (e.Button.Equals("Y", StringComparison.OrdinalIgnoreCase))
                    otherPressed++;
            };

            runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "legacy modal owner test");
            runtime.UI.OpenOwnerBoundCustomMenu("DTMAPI.DebugConsole", owner.UniqueID);
            Assert(runtime.TryDispatchLegacyModalButtonPressed("DTMAPI.DebugConsole", owner.UniqueID, "Y"), "The legacy modal lane must dispatch to the registered modal owner.");
            Assert(ownerPressed == 1 && otherPressed == 0, "The legacy modal lane must invoke only the active modal owner's ButtonPressed handler.");
            Assert(!runtime.UI.IsOpen && runtime.UI.ActiveMenuOwnerId.Length == 0, "The legacy owner handler must perform the close and clear the custom modal owner mapping.");

            runtime.UI.OpenOwnerBoundCustomMenu("DTMAPI.DebugConsole", owner.UniqueID);
            Assert(!runtime.TryDispatchLegacyModalButtonPressed("DTMAPI.DebugConsole", otherOwnerId, "Y"), "A different owner must not claim an owner-bound custom modal by menu ID alone.");
            runtime.UI.Close();
            Assert(!runtime.TryDispatchLegacyModalButtonPressed("DTMAPI.DebugConsole", owner.UniqueID, "Y"), "A closed modal must not receive legacy modal input.");

            runtime.UI.SetUiContext("HomePage", canDrawOverlay: true, gameplayHotkeysAllowed: false, reason: "title scope test");
            runtime.UI.OpenOwnerBoundCustomMenu("DTMAPI.DebugConsole", owner.UniqueID);
            Assert(!runtime.TryDispatchLegacyModalButtonPressed("DTMAPI.DebugConsole", owner.UniqueID, "Y"), "Title scope must not activate the in-save legacy modal input lane.");
            runtime.UI.Close();

            runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "suppression test");
            runtime.UI.OpenOwnerBoundCustomMenu("DTMAPI.DebugConsole", owner.UniqueID);
            runtime.Input.Suppress("Y");
            Assert(!runtime.TryDispatchLegacyModalButtonPressed("DTMAPI.DebugConsole", owner.UniqueID, "Y"), "An explicitly suppressed Y edge must not enter the legacy modal lane.");
            runtime.UI.Close();
            runtime.Input.ClearFrame();

            ownerInput.RegisterKeybind("debug-console.toggle", "Y", DtmInputScope.SaveLoaded);
            Assert(!host.ShouldUseLegacyYCloseFallback(), "Any typed Y keybind owned by the DebugConsole Mod must disable the legacy raw-close fallback and remain the sole toggle owner.");
            runtime.UI.OpenOwnerBoundCustomMenu("DTMAPI.DebugConsole", owner.UniqueID);
            Assert(!runtime.TryDispatchLegacyModalButtonPressed("DTMAPI.DebugConsole", owner.UniqueID, "Y"), "Typed and legacy registrations for one owner must not double-dispatch through the compatibility lane.");
            runtime.UI.Close();

            var broadcastRuntime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            int broadcastA = 0;
            int broadcastB = 0;
            IEventsHelper broadcastEventsA = broadcastRuntime.Events.CreateOwnerBoundProxy("DTMAPI.Tests.BroadcastA", () => { });
            IEventsHelper broadcastEventsB = broadcastRuntime.Events.CreateOwnerBoundProxy("DTMAPI.Tests.BroadcastB", () => { });
            broadcastRuntime.Input.CreateOwnerBound("DTMAPI.Tests.BroadcastA").RegisterButton("Y");
            broadcastRuntime.Input.CreateOwnerBound("DTMAPI.Tests.BroadcastB").RegisterButton("Y");
            broadcastEventsA.Input.ButtonPressed += (_, _) => broadcastA++;
            broadcastEventsB.Input.ButtonPressed += (_, _) => broadcastB++;
            broadcastRuntime.Input.ClearFrame();
            broadcastRuntime.RecordInputFrame(new[] { new InputButtonSample("Y", false, true, true) });
            Assert(broadcastA == 1 && broadcastB == 1, "Ordinary Gameplay legacy ButtonPressed dispatch must remain a broadcast outside the owner-bound modal lane.");
        }

        private static void DebugConsoleEscapeCloseDrainProtectsNativeOwnerBoundary()
        {
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            var host = CreateDebugConsoleUi(runtime);
            SetPrivateField(host, "suppressEscapeCloseUntilReleased", true);

            try
            {
                Assert(host.AdvanceEscapeCloseDrain(escapePressed: true, escapeDown: true), "A held or repeated Escape edge must keep the post-close native-input drain active.");
                Assert(CompatibilityDebugConsoleInputHooks.NativeInputDrainActive, "The optional Compatibility native-input boundary must stay active while Escape is held.");

                bool enterUiResult = false;
                Assert(!CompatibilityDebugConsoleInputHooks.EnterUiCheckPrefix(ref enterUiResult) && enterUiResult, "EnterUICheck must be skipped during the Escape close drain even after the visual modal has closed.");
                Assert(!CompatibilityDebugConsoleInputHooks.UseToolPrefix() && !CompatibilityDebugConsoleInputHooks.UseItemPrefix(), "Tool and item native input must remain isolated during the Escape close drain.");

                Assert(host.AdvanceEscapeCloseDrain(escapePressed: false, escapeDown: false), "One clean frame must not end the Escape close drain.");
                Assert(!host.AdvanceEscapeCloseDrain(escapePressed: false, escapeDown: false), "Two consecutive clean frames must end the bounded Escape close drain.");
                Assert(!CompatibilityDebugConsoleInputHooks.NativeInputDrainActive, "The native-input drain must release after the clean-frame boundary.");

                enterUiResult = false;
                Assert(CompatibilityDebugConsoleInputHooks.EnterUiCheckPrefix(ref enterUiResult) && !enterUiResult, "Native EnterUICheck must resume after the Escape close drain ends.");
                Assert(CompatibilityDebugConsoleInputHooks.UseToolPrefix() && CompatibilityDebugConsoleInputHooks.UseItemPrefix(), "Native tool and item input must resume after the Escape close drain ends.");
            }
            finally
            {
                CompatibilityDebugConsoleInputHooks.Reset();
            }
        }

        private static void DebugConsoleFinalSpeedMultiplierPreservesNativeBuffState()
        {
            var runtime = new DtmApiRuntime(
                new FakeHost(NewTempGameDir()),
                new ConfigMenuRegistry());
            var adapter = new CompatibilityDebugConsoleRuntimeAdapter(
                runtime,
                installNativeHooks: false);
            var actions = new DebugConsoleNativeActions(adapter);
            var player = new FakeDebugConsoleBodyController
            {
                NativeMoveSpeed = 10f,
                MoveScaler = 0.35f
            };
            DolocAPI.agent = player;
            IManifest owner = DebugConsoleUnitManifest();

            MovementSpeedResult applied = actions.SetSpeedMultiplier(owner, 2d);
            float observed = player.MoveSpeed;
            DebugConsoleMovementHooks.MoveSpeedPostfix(player, ref observed);
            Assert(
                applied.Success &&
                Math.Abs(observed - 20f) < 0.0001f &&
                Math.Abs(player.MoveScaler - 0.35f) < 0.0001f,
                "DebugConsole must multiply the final player MoveSpeed without changing the native/Buff MoveScaler.");

            player.NativeMoveSpeed = 12.5f;
            observed = player.MoveSpeed;
            DebugConsoleMovementHooks.MoveSpeedPostfix(player, ref observed);
            Assert(
                Math.Abs(observed - 25f) < 0.0001f,
                "A later native/Buff speed change must compose under the retained DebugConsole factor.");

            actions.RestoreModalScopedState("console closed");
            observed = player.MoveSpeed;
            DebugConsoleMovementHooks.MoveSpeedPostfix(player, ref observed);
            Assert(
                Math.Abs(observed - 25f) < 0.0001f,
                "Closing the Y console must not clear the selected movement multiplier.");

            var replacement = new FakeDebugConsoleBodyController
            {
                NativeMoveSpeed = 8f,
                MoveScaler = 0.6f
            };
            DolocAPI.agent = replacement;
            actions.Update();
            float oldObserved = player.MoveSpeed;
            float newObserved = replacement.MoveSpeed;
            DebugConsoleMovementHooks.MoveSpeedPostfix(player, ref oldObserved);
            DebugConsoleMovementHooks.MoveSpeedPostfix(replacement, ref newObserved);
            Assert(
                Math.Abs(oldObserved - 12.5f) < 0.0001f &&
                Math.Abs(newObserved - 16f) < 0.0001f,
                "A player BodyController replacement must transfer the factor without affecting the retired body.");

            MovementSpeedResult reset = actions.ResetSpeed(
                owner,
                "unit final-speed reset");
            newObserved = replacement.MoveSpeed;
            DebugConsoleMovementHooks.MoveSpeedPostfix(replacement, ref newObserved);
            Assert(
                reset.Success &&
                Math.Abs(newObserved - 8f) < 0.0001f &&
                Math.Abs(replacement.MoveScaler - 0.6f) < 0.0001f,
                "Reset must remove only the DebugConsole factor and preserve the latest native/Buff MoveScaler.");
            DebugConsoleMovementHooks.Reset();
            DolocAPI.agent = null;
        }

        private static void DebugConsoleOneHundredCompatibilitySemantics()
        {
            var runtime = new DtmApiRuntime(
                new FakeHost(NewTempGameDir()),
                new ConfigMenuRegistry());
            var adapter = new CompatibilityDebugConsoleRuntimeAdapter(
                runtime,
                installNativeHooks: false);
            var actions = new DebugConsoleNativeActions(adapter);
            IManifest owner = DebugConsoleUnitManifest();
            var archive = new FakeDebugConsoleWeatherArchive();
            DolocAPI.archiveHandle = archive;
            DolocAPI.ResetWeatherCommandFixture();
            DolocTown.Config.DolocConfig.Tables =
                new FakeDebugConsoleTables();

            try
            {
                WeatherDebugState state = actions.GetState();
                IReadOnlyList<WeatherDebugOption> options =
                    actions.GetAvailableWeathers();
                Assert(
                    state.CurrentWeatherId == "SUNNY" &&
                    state.SeasonName == "Spring" &&
                    state.CurrentDayForecastWeatherIds.SequenceEqual(
                        new[] { "RAIN" }) &&
                    options.Select(option => option.Id).SequenceEqual(
                        DebugConsoleNativeActions.StableWeatherIds) &&
                    options.Single(option => option.Id == "SUNNY").IsCurrent &&
                    options.Single(option => option.Id == "RAIN").IsCurrentDayForecast,
                    "DebugConsole 1.1 weather state must use LocalWeatherType plus the current room's exact forecast APIs while retaining seven fixed ordered slots.");

                WeatherSetResult changed = actions.SetWeather(
                    owner,
                    "RAIN",
                    patchCurrentPeriod: true);
                Assert(
                    changed.Success &&
                    changed.BeforeWeatherId == "SUNNY" &&
                    changed.AfterWeatherId == "RAIN" &&
                    DolocAPI.CommandSetWeatherCalls == 1 &&
                    DolocAPI.LastSetWeatherId == "RAIN" &&
                    DolocAPI.LastSetWeatherPatch,
                    "DebugConsole must invoke the exact official Command_SetWeather(string, bool) overload once and verify LocalWeatherType after the call.");

                DolocAPI.SuppressWeatherCommandMutation = true;
                WeatherSetResult mismatch = actions.SetWeather(
                    owner,
                    "SUNNY",
                    patchCurrentPeriod: false);
                Assert(
                    !mismatch.Success &&
                    mismatch.FailureReason == "result-mismatch" &&
                    mismatch.BeforeWeatherId == "RAIN" &&
                    mismatch.AfterWeatherId == "RAIN" &&
                    DolocAPI.CommandSetWeatherCalls == 2 &&
                    DolocAPI.LastSetWeatherId == "SUNNY" &&
                    !DolocAPI.LastSetWeatherPatch,
                    "A weather command that does not change LocalWeatherType must fail closed without duplicating the native group/key mutation logic.");

                actions.InvalidateCatalogs();
                DolocTown.Config.DolocConfig.Tables =
                    new FakeDebugConsoleTables(new[] { "SUNNY", "RAIN" });
                WeatherPanelSnapshot partialSnapshot =
                    actions.GetPanelSnapshot();
                WeatherSetResult missingWeather = actions.SetWeather(
                    owner,
                    "SCORCH_SUN",
                    patchCurrentPeriod: true);
                Assert(
                    partialSnapshot.Options.Select(option => option.Id)
                        .SequenceEqual(DebugConsoleNativeActions.StableWeatherIds) &&
                    partialSnapshot.AvailableWeatherIds.SequenceEqual(
                        new[] { "SUNNY", "RAIN" }) &&
                    actions.GetAvailableWeathers().Select(option => option.Id)
                        .SequenceEqual(new[] { "SUNNY", "RAIN" }) &&
                    !missingWeather.Success &&
                    missingWeather.FailureReason == "not-whitelisted" &&
                    DolocAPI.CommandSetWeatherCalls == 2,
                    "A partial native weather table must retain seven fixed UI slots while the compatibility projection and mutation allowlist expose only actual rows.");

                var advanced = new FakeDebugConsoleAdvancedActions
                {
                    FailPointTypeId = "OPERATE"
                };
                var ui = new DebugConsoleUi(adapter);
                ui.BindAdvanced(owner, advanced);
                MethodInfo addTechnologyPoints =
                    typeof(DebugConsoleUi).GetMethod(
                        "AddTechnologyPoints",
                        BindingFlags.NonPublic | BindingFlags.Instance) ??
                    throw new MissingMethodException(
                        typeof(DebugConsoleUi).FullName,
                        "AddTechnologyPoints");
                addTechnologyPoints.Invoke(ui, Array.Empty<object>());
                Assert(
                    advanced.Calls.Select(call => call.PointTypeId)
                        .SequenceEqual(
                            new[]
                            {
                                "NATURE",
                                "OPERATE",
                                "SCIENCE",
                                "ANIMAL"
                            }) &&
                    advanced.Calls.All(call => call.Amount == 100) &&
                    GetPrivateField<string>(ui, "statusMessage")
                        .Contains("OPERATE", StringComparison.Ordinal),
                    "The +tech button must add exactly 100 to NATURE, OPERATE, SCIENCE and ANIMAL in order, continuing after one bounded category failure.");
            }
            finally
            {
                DolocTown.Config.DolocConfig.Tables = null;
                DolocAPI.archiveHandle = null;
                DolocAPI.ResetWeatherCommandFixture();
            }
        }

        private static void
            DebugConsoleCompatibilityHookTopologyIsTransactionalAndStable()
        {
            var runtime = new DtmApiRuntime(
                new FakeHost(NewTempGameDir()),
                new ConfigMenuRegistry());
            var stableBackend =
                new FakeDebugConsolePatchBackend();
            var stable =
                new CompatibilityDebugConsoleRuntimeAdapter(
                    runtime,
                    stableBackend);

            stable.ModalOpen = true;
            Assert(
                stable.InstalledPatchCount == 3 &&
                stable.PatchOperationCount == 3 &&
                stable.TopologyTransitionCount == 1 &&
                stable.StatusPublicationCount == 1,
                "Compatibility input demand did not install exactly one three-Prefix topology edge.");
            int stablePatchOperations =
                stable.PatchOperationCount;
            int stableTransitions =
                stable.TopologyTransitionCount;
            int stableStatuses =
                stable.StatusPublicationCount;
            for (int frame = 0; frame < 240; frame++)
            {
                stable.ModalOpen = true;
                stable.NativeInputDrainActive = false;
            }
            Assert(
                stable.PatchOperationCount ==
                    stablePatchOperations &&
                stable.TopologyTransitionCount ==
                    stableTransitions &&
                stable.StatusPublicationCount ==
                    stableStatuses &&
                stable.InstalledPatchCount == 3,
                "Warmed Compatibility frames performed a Hook operation, topology transition or status publication.");
            stable.NativeInputDrainActive = true;
            stable.ModalOpen = false;
            Assert(
                stable.InstalledPatchCount == 3 &&
                stable.PatchOperationCount ==
                    stablePatchOperations,
                "Modal-to-input-drain handoff rebuilt an unchanged three-Prefix topology.");
            stable.NativeInputDrainActive = false;
            Assert(
                stable.InstalledPatchCount == 0 &&
                stable.PatchOperationCount ==
                    stablePatchOperations + 3 &&
                stable.TopologyTransitionCount == 2 &&
                stable.StatusPublicationCount == 2,
                "Compatibility input release did not remove exactly one topology edge.");

            var removalFailureBackend =
                new FakeDebugConsolePatchBackend
                {
                    FailUnpatchAttempt = 2
                };
            var removalFailure =
                new CompatibilityDebugConsoleRuntimeAdapter(
                    runtime,
                    removalFailureBackend);
            removalFailure.ModalOpen = true;
            AssertThrows<AggregateException>(
                () => removalFailure.ModalOpen = false,
                "A partial Compatibility input removal should fail the topology transition.");
            Assert(
                removalFailure.InstalledPatchCount == 3 &&
                removalFailureBackend.TotalOwnedPatches == 3 &&
                CompatibilityDebugConsoleInputHooks.ModalOpen,
                "Compatibility input removal failure did not restore the exact prior demand and three-Prefix topology.");
            removalFailureBackend.FailUnpatchAttempt = 0;
            removalFailure.ModalOpen = false;
            Assert(
                removalFailure.InstalledPatchCount == 0 &&
                removalFailureBackend.TotalOwnedPatches == 0 &&
                !CompatibilityDebugConsoleInputHooks.ModalOpen,
                "Compatibility input topology did not release after the injected removal failure cleared.");

            var inputFailureBackend =
                new FakeDebugConsolePatchBackend
                {
                    FailPatchAttempt = 2
                };
            var inputFailure =
                new CompatibilityDebugConsoleRuntimeAdapter(
                    runtime,
                    inputFailureBackend);
            var ui = new DebugConsoleUi(inputFailure);
            var owner = DebugConsoleUnitManifest();
            ui.Bind(owner, null, null, null, null, null);
            runtime.UI.SetUiContext(
                "Gameplay",
                canDrawOverlay: true,
                gameplayHotkeysAllowed: true,
                reason: "DebugConsole atomic-open Unit");
            AssertThrows<AggregateException>(
                () => ui.Open(owner, "fault injection"),
                "A partial Compatibility input install should fail the UI open transaction.");
            Assert(
                !ui.IsOpen &&
                !runtime.UI.IsOpen &&
                inputFailure.InstalledPatchCount == 0 &&
                inputFailureBackend.TotalOwnedPatches == 0 &&
                !CompatibilityDebugConsoleInputHooks.ModalOpen &&
                !CompatibilityDebugConsoleInputHooks
                    .NativeInputDrainActive,
                "Compatibility input install failure retained a modal token, UI-open state, desired gate or partial Hook.");

            var creativeFailureBackend =
                new FakeDebugConsolePatchBackend
                {
                    FailPatchAttempt = 5
                };
            var creativeFailure =
                new CompatibilityDebugConsoleRuntimeAdapter(
                    runtime,
                    creativeFailureBackend);
            AssertThrows<AggregateException>(
                () => creativeFailure.SetCreativeHookDemand(true),
                "A partial Compatibility creative install should fail atomically.");
            Assert(
                creativeFailure.InstalledPatchCount == 0 &&
                creativeFailureBackend.TotalOwnedPatches == 0 &&
                !DebugConsoleCreativeHooks.Installed,
                "Compatibility creative install failure retained desired state or a partial Hook.");

            var tombstoneBackend =
                new FakeDebugConsolePatchBackend
                {
                    FailPatchAttempt = 2,
                    FailUnpatchAttempt = 1
                };
            var tombstone =
                new CompatibilityDebugConsoleRuntimeAdapter(
                    runtime,
                    tombstoneBackend);
            AssertThrows<AggregateException>(
                () => tombstone.ModalOpen = true,
                "A combined patch/rollback-unpatch failure should retain an observable cleanup tombstone.");
            Assert(
                tombstone.CleanupPending &&
                tombstone.InstalledPatchCount == 1 &&
                tombstoneBackend.TotalOwnedPatches == 1 &&
                !CompatibilityDebugConsoleInputHooks.ModalOpen,
                "A combined patch/rollback-unpatch failure lost its cleanup-pending owner state or published a partial input demand.");
            tombstone.ShutdownHooks(
                "unit cleanup-pending retry");
            Assert(
                !tombstone.CleanupPending &&
                tombstone.InstalledPatchCount == 0 &&
                tombstoneBackend.TotalOwnedPatches == 0,
                "A later lifecycle cleanup did not consume the retained Compatibility Hook tombstone and prove owner zero.");

            var closeFailureBackend =
                new FakeDebugConsolePatchBackend();
            var closeFailure =
                new CompatibilityDebugConsoleRuntimeAdapter(
                    runtime,
                    closeFailureBackend);
            var closeFailureUi =
                new DebugConsoleUi(closeFailure);
            closeFailureUi.Bind(
                owner,
                null,
                null,
                null,
                null,
                null);
            closeFailureUi.Open(owner, "close failure unit");
            closeFailureBackend.FailUnpatchAttempt = 1;
            AssertThrows<AggregateException>(
                () =>
                    closeFailureUi.Close(
                        owner,
                        "hotkey Y"),
                "A Compatibility close Hook failure should remain retryable.");
            Assert(
                closeFailureUi.IsOpen &&
                runtime.UI.IsOpen &&
                runtime.UI.ActiveMenuId.Equals(
                    "DTMAPI.DebugConsole",
                    StringComparison.OrdinalIgnoreCase) &&
                closeFailure.InstalledPatchCount == 3 &&
                closeFailureBackend.TotalOwnedPatches == 3 &&
                CompatibilityDebugConsoleInputHooks.ModalOpen,
                "A failed Compatibility close hid the UI or lost its Core modal token instead of restoring the coherent open state.");
            closeFailureBackend.FailUnpatchAttempt = 0;
            closeFailureUi.Close(owner, "hotkey Y");
            Assert(
                !closeFailureUi.IsOpen &&
                !runtime.UI.IsOpen &&
                closeFailure.InstalledPatchCount == 0 &&
                closeFailureBackend.TotalOwnedPatches == 0,
                "The retry after a failed Compatibility close did not release UI, Core modal and exact-owner Hooks together.");

            var conflictBackend =
                new FakeDebugConsolePatchBackend
                {
                    ProductOwnerPresent = true
                };
            var conflict =
                new CompatibilityDebugConsoleRuntimeAdapter(
                    runtime,
                    conflictBackend);
            AssertThrows<InvalidOperationException>(
                () => conflict.ModalOpen = true,
                "Compatibility must reject ProductNative-first physical ownership.");
            Assert(
                conflict.InstalledPatchCount == 0 &&
                conflictBackend.TotalOwnedPatches == 0,
                "Compatibility changed topology after observing the ProductNative owner.");
            CompatibilityDebugConsoleInputHooks.Reset();
            DebugConsoleCreativeHooks.Reset();
        }

        private static void
            DebugConsoleCompatibilityConstructionOrdersOwnActionsExactlyOnce()
        {
            string? previousRoot =
                UseTempPersistentRoot();
            try
            {
                foreach (bool consoleFirst in
                         new[] { true, false })
                {
                    string dir = NewTempGameDir();
                    StageCompatibilityHostFixture(dir);
                    var runtime =
                        new DtmApiRuntime(
                            new FakeHost(dir),
                            new ConfigMenuRegistry());
                    CompatibilityHostBroker broker =
                        CompatibilityHostBroker.For(runtime);
                    if (consoleFirst)
                    {
                        _ = broker.GetService(
                            "DebugConsole",
                            Array.Empty<object>());
                        Assert(
                            broker.TryGetService(
                                "DebugActions",
                                out object? aliased) &&
                            aliased != null,
                            "DebugConsole-first construction did not publish the nested DebugActions lifecycle alias.");
                    }
                    else
                    {
                        object actions =
                            broker.GetService(
                                "DebugActions",
                                Array.Empty<object>());
                        _ = broker.GetService(
                            "DebugConsole",
                            Array.Empty<object>());
                        Assert(
                            broker.TryGetService(
                                "DebugActions",
                                out object? reused) &&
                            ReferenceEquals(
                                actions,
                                reused),
                            "DebugActions-first construction did not preserve the one action lifecycle owner when DebugConsole was added.");
                    }

                    var console =
                        new DebugConsoleCompatibilityProxy(
                            runtime);
                    var actionsProxy =
                        new DebugActionCompatibilityProxy(
                            runtime);
                    console.UpdateIfLoaded();
                    actionsProxy.UpdateIfLoaded();
                    console.ResetForSaveBoundaryIfLoaded(
                        3,
                        false);
                    actionsProxy.ResetForSaveBoundaryIfLoaded();
                    console.ResetForTitleBoundaryIfLoaded();
                    actionsProxy.ResetForTitleBoundaryIfLoaded();
                    console.ShutdownIfLoaded(
                        "construction-order unit");
                    actionsProxy.ShutdownIfLoaded(
                        "construction-order unit");

                    string summary =
                        actionsProxy.GetLifecycleSummary();
                    Assert(
                        summary.Contains(
                            "lifecycleUpdates=1",
                            StringComparison.Ordinal) &&
                        summary.Contains(
                            "saveBoundaryResets=1",
                            StringComparison.Ordinal) &&
                        summary.Contains(
                            "titleBoundaryResets=1",
                            StringComparison.Ordinal) &&
                        summary.Contains(
                            "shutdowns=1",
                            StringComparison.Ordinal),
                        (consoleFirst
                            ? "DebugConsole-first"
                            : "DebugActions-first") +
                        " construction did not pump the shared action owner exactly once per lifecycle boundary. summary=" +
                        summary);
                }
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void
            DebugConsoleSaveLoadedRestoresBeforePublishingSession()
        {
            var gate =
                new DebugConsoleSaveSessionGate();
            int retainedLedger = 1;
            int publishedSessions = 0;
            bool failRestore = true;

            AssertThrows<InvalidOperationException>(
                () =>
                    gate.Enter(
                        () =>
                        {
                            if (failRestore)
                            {
                                throw new InvalidOperationException(
                                    "injected exact restore failure");
                            }
                            retainedLedger = 0;
                        },
                        () => publishedSessions++),
                "SaveLoaded should propagate an exact restoration failure.");
            Assert(
                !gate.IsActive &&
                retainedLedger == 1 &&
                publishedSessions == 0,
                "SaveLoaded published a new in-save session before the retained native ledger restored.");

            failRestore = false;
            gate.Enter(
                () => retainedLedger = 0,
                () => publishedSessions++);
            Assert(
                gate.IsActive &&
                retainedLedger == 0 &&
                publishedSessions == 1,
                "A later SaveLoaded retry did not restore the exact old state, empty the ledger and publish one new session.");
            gate.Leave();
            Assert(
                !gate.IsActive,
                "ReturnedToTitle must close the ProductNative save-session gate.");
        }

        private static void
            DebugConsoleHarmonyOwnerFixturePasses()
        {
            string executable =
                Path.Combine(
                    FindRepositoryRoot(),
                    "tests",
                    "DTMAPI.UnitTests",
                    "Fixtures",
                    "DebugConsoleHarmonyOwnerFixture",
                    "bin",
                    "Release",
                    "net48",
                    "DebugConsoleHarmonyOwnerFixture.exe");
            if (!File.Exists(executable))
            {
                throw new FileNotFoundException(
                    "The focused Unit build did not produce the DebugConsole Harmony owner fixture.",
                    executable);
            }
            var start = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using Process process =
                Process.Start(start) ??
                throw new InvalidOperationException(
                    "Could not start the DebugConsole Harmony owner fixture.");
            string output =
                process.StandardOutput.ReadToEnd();
            string error =
                process.StandardError.ReadToEnd();
            if (!process.WaitForExit(30000))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                }
                throw new TimeoutException(
                    "The DebugConsole Harmony owner fixture timed out.");
            }
            Assert(
                process.ExitCode == 0 &&
                output.Contains(
                    "DebugConsoleHarmonyOwnerFixture: OK",
                    StringComparison.Ordinal),
                "The DebugConsole Harmony owner fixture failed. exit=" +
                process.ExitCode + "; stdout=" + output +
                "; stderr=" + error);
        }

        private static ManifestModel DebugConsoleUnitManifest() =>
            new ManifestModel
            {
                Name = "DebugConsole Unit",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.UnitTests.DebugConsole",
                Type = "CodeMod"
            };

        private static void
            DebugConsolePostWriteFailuresRetainLeaseLedger()
        {
            var runtime = new DtmApiRuntime(
                new FakeHost(NewTempGameDir()),
                new ConfigMenuRegistry());
            var adapter =
                new CompatibilityDebugConsoleRuntimeAdapter(
                    runtime,
                    installNativeHooks: false);
            var owner = DebugConsoleUnitManifest();

            var timeActions =
                new DebugConsoleNativeActions(adapter);
            var timeManager =
                new FakeDebugConsoleTimeScaleManager(1.75f)
                {
                    ArmPostWriteReadAndRollbackFailure =
                        true
                };
            DolocAPI.timeScaleManager = timeManager;
            TimeScaleDebugResult timeFailure =
                timeActions.SetTimeScale(owner, 4d);
            Assert(
                !timeFailure.Success &&
                GetPrivateField<bool>(
                    timeActions,
                    "timeScaleLeaseActive") &&
                GetPrivateField<bool>(
                    timeActions,
                    "timeScaleMutationUncertain") &&
                Math.Abs(timeManager.RawTimeScale - 4f) <
                    0.0001f,
                "Time-scale write-success/readback-failure/rollback-failure did not retain an uncertainty-aware exact-original ledger.");
            Assert(
                timeActions.ResetTimeScale(
                    owner,
                    "post-write fault retry").Success &&
                !GetPrivateField<bool>(
                    timeActions,
                    "timeScaleLeaseActive") &&
                Math.Abs(timeManager.RawTimeScale - 1.75f) <
                    0.0001f,
                "Time-scale retry did not restore the exact original after a post-write rollback failure.");
            DolocAPI.timeScaleManager = null;

            var creativeActions =
                new DebugConsoleNativeActions(adapter);
            var creativeConfig =
                new FakeDebugConsoleCreativeConfig
                {
                    ignoreMaterialCost = false,
                    skipMoneyVerifyInShop = false,
                    ignoreSpiritCost = true,
                    ArmPartialApplyAndRollbackFailure = true
                };
            DolocAPI.gameManager =
                new FakeDebugConsoleGameManager
                {
                    gameInitConfig = creativeConfig
                };
            CreativeModeResult creativeFailure =
                creativeActions.SetCreativeMode(owner, true);
            Assert(
                !creativeFailure.Success &&
                GetPrivateField<bool>(
                    creativeActions,
                    "creativeSnapshotValid") &&
                GetPrivateField<bool>(
                    creativeActions,
                    "creativeMutationUncertain") &&
                creativeConfig.ignoreMaterialCost,
                "Creative partial apply plus rollback failure discarded the exact-original ledger.");
            creativeActions.RestoreTransientState(
                "post-write creative fault retry");
            Assert(
                !GetPrivateField<bool>(
                    creativeActions,
                    "creativeSnapshotValid") &&
                !creativeConfig.ignoreMaterialCost &&
                !creativeConfig.skipMoneyVerifyInShop &&
                creativeConfig.ignoreSpiritCost,
                "Creative retry did not restore all exact original flags and clear the retained ledger.");
            DolocAPI.gameManager = null;
        }

        private static void DebugConsoleCreativeFlagTransactionRollsBackPartialFailure()
        {
            var config = new FakeDebugConsoleCreativeConfig
            {
                ignoreMaterialCost = false,
                skipMoneyVerifyInShop = false,
                ignoreSpiritCost = true,
                FailNextShopWrite = true
            };
            MethodInfo transaction =
                typeof(DebugConsoleNativeActions).GetMethod(
                    "TryWriteCreativeFlags",
                    BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException(
                    "DebugConsole creative transaction helper is unavailable.");
            object?[] arguments =
            {
                config,
                true,
                true,
                true,
                null,
                null
            };
            Assert(
                transaction.Invoke(null, arguments) is bool applied &&
                !applied &&
                !config.ignoreMaterialCost &&
                !config.skipMoneyVerifyInShop &&
                config.ignoreSpiritCost &&
                !string.IsNullOrWhiteSpace(arguments[4] as string) &&
                arguments[5] is bool rollbackVerified &&
                rollbackVerified,
                "A middle creative-flag write failure must roll back every earlier write to the exact three original values and surface a failure.");
        }

        private static void DebugConsoleTitleDestroysUiGraphAndSaveRequiresConfirmation()
        {
            var runtime = new DtmApiRuntime(
                new FakeHost(NewTempGameDir()),
                new ConfigMenuRegistry());
            var ui = CreateDebugConsoleUi(runtime);
            var owner = new ManifestModel
            {
                Name = "DebugConsole Unit",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.UnitTests.DebugConsole",
                Type = "CodeMod"
            };
            var save = new FakeDebugConsoleInstantSaveActions();
            ui.Bind(owner, null, null, null, null, null, save);
            GetPrivateField<List<object>>(ui, "eventBinders").Add(new object());
            GetPrivateField<List<object>>(ui, "inputFields").Add(new object());
            SetPrivateField(ui, "root", new object());

            MethodInfo saveHere = typeof(DebugConsoleUi).GetMethod(
                "SaveHere",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException(
                    "DebugConsole SaveHere callback is unavailable.");
            saveHere.Invoke(ui, null);
            Assert(
                save.SaveCalls == 0 &&
                GetPrivateField<bool>(ui, "saveConfirmationArmed"),
                "The first Save here click must arm confirmation without calling native SaveGame.");
            saveHere.Invoke(ui, null);
            Assert(
                save.SaveCalls == 1 &&
                !GetPrivateField<bool>(ui, "saveConfirmationArmed"),
                "Only the confirmed second click may call the native-save action.");

            ui.ResetForTitleBoundary();
            FieldInfo rootField = typeof(DebugConsoleUi).GetField(
                "root",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException(
                    "DebugConsole root field is unavailable.");
            Assert(
                rootField.GetValue(ui) == null &&
                GetPrivateField<List<object>>(ui, "eventBinders").Count == 0 &&
                GetPrivateField<List<object>>(ui, "inputFields").Count == 0 &&
                ui.GetOwnerObjectGraphSummary().Contains(
                    "Canvas=0",
                    StringComparison.Ordinal) &&
                ui.GetOwnerObjectGraphSummary().Contains(
                    "Button=0",
                    StringComparison.Ordinal) &&
                ui.GetOwnerObjectGraphSummary().Contains(
                    "InputField=0",
                    StringComparison.Ordinal),
                "ReturnedToTitle must release the complete Canvas/button/input/listener graph while retaining only the owner binding needed for a later save session.");
        }

        private static void DebugConsoleAdvancedAdmissionKeepsProductAndCompatibilityOwnersSeparate()
        {
            string repo = FindRepositoryRoot();
            string productRoot = Path.Combine(
                repo,
                "products",
                "first-party",
                "DebugConsole");
            string productSource = string.Join(
                "\n",
                Directory.GetFiles(
                        Path.Combine(productRoot, "src"),
                        "*.cs",
                        SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
            string manifest = File.ReadAllText(
                Path.Combine(productRoot, "manifest.json"));
            string entry = File.ReadAllText(
                Path.Combine(productRoot, "src", "ModEntry.cs"));
            string hooks = File.ReadAllText(
                Path.Combine(
                    productRoot,
                    "src",
                    "Native",
                    "DebugConsoleHookInstaller.cs"));
            string compatibility = string.Join(
                "\n",
                Directory.GetFiles(
                        Path.Combine(
                            repo,
                            "src",
                            "DTMAPI.GameBridge.DolocTown.Compatibility",
                            "DebugConsole"),
                        "*.cs",
                        SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
            string bootstrap = File.ReadAllText(
                Path.Combine(
                    repo,
                    "src",
                    "DTMAPI.BepInExBootstrap",
                    "BootstrapPlugin.cs"));
            string proxy = File.ReadAllText(
                Path.Combine(
                    repo,
                    "src",
                    "DTMAPI.GameBridge.DolocTown",
                    "CompatibilityHost",
                    "DebugConsoleCompatibilityProxy.cs"));
            string actionProxy = File.ReadAllText(
                Path.Combine(
                    repo,
                    "src",
                    "DTMAPI.GameBridge.DolocTown",
                    "CompatibilityHost",
                    "DebugActionCompatibilityProxy.cs"));
            string mandatoryDiagnostics = string.Join(
                "\n",
                Directory.GetFiles(
                        Path.Combine(
                            repo,
                            "src",
                            "DTMAPI.GameBridge.DolocTown",
                            "Diagnostics"),
                        "*.cs",
                        SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
            string nativeActions = File.ReadAllText(
                Path.Combine(
                    productRoot,
                    "src",
                    "Native",
                    "DebugConsoleNativeActions.Advanced.cs"));
            string weatherActions = File.ReadAllText(
                Path.Combine(
                    productRoot,
                    "src",
                    "Native",
                    "DebugConsoleNativeActions.Core.cs"));
            string spawnActions = File.ReadAllText(
                Path.Combine(
                    productRoot,
                    "src",
                    "Native",
                    "DebugConsoleNativeActions.Spawn.cs"));
            string nativeAccess = File.ReadAllText(
                Path.Combine(
                    productRoot,
                    "src",
                    "Native",
                    "DebugConsoleNativeAccess.cs"));
            string[] uiSourcePaths = Directory.GetFiles(
                    Path.Combine(productRoot, "src", "Ui"),
                    "DebugConsoleUi*.cs",
                    SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            Assert(
                uiSourcePaths.Select(Path.GetFileName)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .SequenceEqual(
                        new[]
                        {
                            "DebugConsoleUi.Actions.cs",
                            "DebugConsoleUi.Catalog.cs",
                            "DebugConsoleUi.cs",
                            "DebugConsoleUi.Layout.cs",
                            "DebugConsoleUi.Reflection.cs"
                        }.OrderBy(path => path, StringComparer.Ordinal),
                        StringComparer.Ordinal),
                "DebugConsole UI maintenance must retain the exact five-file behavior-neutral partial boundary.");
            string uiSource = string.Join("\n", uiSourcePaths.Select(File.ReadAllText));

            string[] frozenApiTokens =
            {
                "IDebugConsoleApi",
                "IInventoryDebugApi",
                "IWeatherDebugApi",
                "ITeleportDebugApi",
                "IInstantSaveDebugApi",
                "ITimeDebugApi",
                "IMovementDebugApi",
                "IAdvancedDebugApi"
            };
            foreach (string token in frozenApiTokens)
            {
                Assert(
                    !productSource.Contains(token, StringComparison.Ordinal),
                    "The Advanced DebugConsole product must not consume frozen Compatibility API token " +
                    token + ".");
            }
            Assert(
                manifest.Contains("\"CodeModKind\": \"Advanced\"", StringComparison.Ordinal) &&
                manifest.Contains("\"EntryDll\": \"DTMAPI.DebugConsole.dll\"", StringComparison.Ordinal) &&
                !manifest.Contains("DTMAPI.DebugConsoleHost", StringComparison.Ordinal) &&
                !manifest.Contains("DTMAPI.GameBridge.DolocTown", StringComparison.Ordinal),
                "The 1.1 manifest must admit one SDK-generated Advanced product without old DebugConsoleHost/GameBridge dependencies.");
            int focusedTextGuard = entry.IndexOf(
                "ui.TryConsumeFocusedTextInputY()",
                StringComparison.Ordinal);
            int typedToggle = entry.IndexOf(
                "ui.Toggle(helper.ModManifest, \"hotkey Y\")",
                StringComparison.Ordinal);
            Assert(
                focusedTextGuard >= 0 &&
                typedToggle > focusedTextGuard &&
                uiSource.Contains("TMPro.TMP_InputField, Unity.TextMeshPro", StringComparison.Ordinal) &&
                uiSource.Contains("textInputFocusMetadataResolved", StringComparison.Ordinal),
                "DebugConsole must check cached standard/TMP text-input focus in the typed handler before changing console visibility.");
            Assert(
                entry.Contains("RestoreAfterConsoleClose", StringComparison.Ordinal) &&
                entry.Contains("actions.RestoreModalScopedState(reason)", StringComparison.Ordinal) &&
                entry.Contains("actions.RestoreTransientState(\"ReturnedToTitle\")", StringComparison.Ordinal) &&
                entry.Contains("hooks?.Unpatch()", StringComparison.Ordinal),
                "Ordinary close must preserve movement while restoring modal-scoped state; title and deactivation must restore every transient factor and exact-owner Hook.");
            Assert(
                hooks.Contains("dtmapi.mod.dtmapi.debugconsolemod", StringComparison.Ordinal) &&
                hooks.Contains("dtmapi.compatibility.debugconsole.legacy", StringComparison.Ordinal) &&
                hooks.Contains("CompatibilityHarmonyOwner", StringComparison.Ordinal) &&
                hooks.Contains("Resolve(type, \"UseTool\", 1)", StringComparison.Ordinal) &&
                hooks.Contains("Resolve(type, \"UseItem\", 1)", StringComparison.Ordinal) &&
                hooks.Contains("Resolve(type, \"EnterUICheck\", 2)", StringComparison.Ordinal) &&
                hooks.Contains("Add(api, \"CostItemAt\", 4)", StringComparison.Ordinal) &&
                !hooks.Contains("Add(api, \"CostItemAt\", 2)", StringComparison.Ordinal) &&
                hooks.Contains("Add(bodyController, \"get_MoveSpeed\", 0)", StringComparison.Ordinal) &&
                productSource.Contains("DebugConsoleMovementHooks", StringComparison.Ordinal) &&
                !productSource.Contains("SetMoveScaler", StringComparison.Ordinal),
                "The product must own the exact Harmony owner, reject the Compatibility owner, retain the three native input Prefixes and apply movement only through the final player MoveSpeed Postfix.");
            Assert(
                weatherActions.Contains("\"Command_SetWeather\"", StringComparison.Ordinal) &&
                weatherActions.Contains("\"SUNNY\"", StringComparison.Ordinal) &&
                weatherActions.Contains("\"SCORCH_SUN\"", StringComparison.Ordinal) &&
                weatherActions.Contains("BindingFlags.NonPublic | BindingFlags.Static", StringComparison.Ordinal) &&
                weatherActions.Contains("new[] { typeof(string), typeof(bool) }", StringComparison.Ordinal) &&
                weatherActions.Contains("\"LocalWeatherType\"", StringComparison.Ordinal) &&
                !weatherActions.Contains("\"CurrentWeatherType\"", StringComparison.Ordinal) &&
                !weatherActions.Contains("\"PatchWeather\"", StringComparison.Ordinal) &&
                !weatherActions.Contains("WeatherRegulator", StringComparison.Ordinal),
                "DebugConsole weather mutation must delegate to the exact private official two-parameter command and verify LocalWeatherType without duplicating group/key or WeatherRegulator logic.");
            Assert(
                spawnActions.Contains("object.Equals(rootProto, expectedProto)", StringComparison.Ordinal) &&
                spawnActions.Contains("generateParameters[1].ParameterType", StringComparison.Ordinal) &&
                spawnActions.Contains("Native.Vector2(", StringComparison.Ordinal) &&
                spawnActions.Contains("ValidateMonsterAddition(", StringComparison.Ordinal) &&
                spawnActions.Contains("\"space_ship_bastion\"", StringComparison.Ordinal) &&
                spawnActions.Contains("AddedEntities(callBefore, callAfter)", StringComparison.Ordinal) &&
                spawnActions.Contains("requestedRoots=", StringComparison.Ordinal) &&
                spawnActions.Contains("succeededRoots=", StringComparison.Ordinal) &&
                spawnActions.Contains("addedEntities=", StringComparison.Ordinal) &&
                !spawnActions.Contains("RollbackMonsters", StringComparison.Ordinal) &&
                !spawnActions.Contains("RollbackAnimals", StringComparison.Ordinal) &&
                !spawnActions.Contains("OpenBatchSpawnCircuit", StringComparison.Ordinal) &&
                !spawnActions.Contains("RemoveMonster", StringComparison.Ordinal) &&
                !spawnActions.Contains("RemoveAnimal", StringComparison.Ordinal) &&
                !productSource.Contains("batchSpawnCircuitOpen", StringComparison.Ordinal) &&
                uiSource.Contains("\"debug.spawn.complete\"", StringComparison.Ordinal) &&
                uiSource.Contains("\"debug.spawn.partial\"", StringComparison.Ordinal) &&
                nativeAccess.Contains("targetType.FullName", StringComparison.Ordinal) &&
                nativeAccess.Contains("\"UnityEngine.Vector2\"", StringComparison.Ordinal) &&
                nativeAccess.Contains("Activator.CreateInstance(targetType, x, y)", StringComparison.Ordinal) &&
                spawnActions.Contains("FailSpawn(", StringComparison.Ordinal) &&
                spawnActions.Contains("LogMutation(", StringComparison.Ordinal),
                "Monster/animal creation must compare exact manager additions, validate the Old City Guardian composite, expose requested/succeeded/actual counts, and retain no rollback or cross-request circuit.");
            Assert(
                spawnActions.Contains("Native.TableList(\"TbAnimal\")", StringComparison.Ordinal) &&
                !spawnActions.Contains("StableAnimalIds", StringComparison.Ordinal) &&
                uiSource.Contains("BuildUnifiedCatalogSources", StringComparison.Ordinal) &&
                uiSource.Contains("CatalogSourceMatches", StringComparison.Ordinal) &&
                !uiSource.Contains("DTMAPI.DebugConsole.Teleport.ExportCsv", StringComparison.Ordinal) &&
                !uiSource.Contains("DTMAPI.DebugConsole.Teleport.State", StringComparison.Ordinal) &&
                !productSource.Contains("TeleportCsvExportResult", StringComparison.Ordinal) &&
                !productSource.Contains("ExportDestinationsCsv", StringComparison.Ordinal),
                "The product must enumerate runtime TbAnimal rows, keep source/category filtering unified, and contain no retired current-location or CSV-export UI/action code.");
            Assert(
                compatibility.Contains("IDebugConsoleApi", StringComparison.Ordinal) &&
                compatibility.Contains("CompatibilityDebugActionService", StringComparison.Ordinal) &&
                compatibility.Contains("DebugConsoleNativeActions", StringComparison.Ordinal) &&
                compatibility.Contains("Loaded frozen DebugConsole 0.3.1 compatibility UI", StringComparison.Ordinal) &&
                compatibility.Contains("ProductHarmonyOwner", StringComparison.Ordinal) &&
                compatibility.Contains("TopologyTransitionCount", StringComparison.Ordinal) &&
                compatibility.Contains("\"CostItemAt\", 4, boolTrue", StringComparison.Ordinal) &&
                !compatibility.Contains("\"CostItemAt\", 2, boolTrue", StringComparison.Ordinal),
                "The retained DebugConsole ABI, native action executor and warning must live in the optional Compatibility component with explicit product-owner exclusion; construction-order behavior tests own the exactly-once lifecycle contract.");
            Assert(
                proxy.Contains("broker.GetService(\"DebugConsole\"", StringComparison.Ordinal) &&
                actionProxy.Contains("broker.GetService(\"DebugActions\"", StringComparison.Ordinal) &&
                !mandatoryDiagnostics.Contains("GiveItem(", StringComparison.Ordinal) &&
                !mandatoryDiagnostics.Contains("SetWeather(", StringComparison.Ordinal) &&
                !mandatoryDiagnostics.Contains("Teleport(", StringComparison.Ordinal) &&
                !mandatoryDiagnostics.Contains("SetCreativeMode(", StringComparison.Ordinal) &&
                proxy.Contains("IfLoaded", StringComparison.Ordinal) &&
                !bootstrap.Contains("new DebugConsoleUi", StringComparison.Ordinal) &&
                !bootstrap.Contains("ReflectedDebugConsoleUi", StringComparison.Ordinal),
                "Mandatory Runtime must retain only lazy Compatibility proxies and no DebugConsole UI or native action body.");
            int disposeStart = entry.IndexOf(
                "public void Dispose()",
                StringComparison.Ordinal);
            int disposeFailureGate = entry.IndexOf(
                "if (failures.Count > 0)",
                disposeStart,
                StringComparison.Ordinal);
            int disposedCommit = entry.IndexOf(
                "disposed = true;",
                disposeStart,
                StringComparison.Ordinal);
            Assert(
                disposeStart >= 0 &&
                disposeFailureGate > disposeStart &&
                disposedCommit > disposeFailureGate,
                "DebugConsole ProductNative deactivation must commit disposed only after exact native restoration and cleanup succeed, preserving Core's retry opportunity.");
            Assert(
                nativeActions.Contains("currentTimeScale", StringComparison.Ordinal) &&
                productSource.Contains("timeScaleOriginalMultiplier", StringComparison.Ordinal) &&
                !productSource.Contains("RevertTimeScale", StringComparison.Ordinal) &&
                uiSource.Contains("ReleaseOwnerGraph(\"ReturnedToTitle\")", StringComparison.Ordinal) &&
                uiSource.Contains("DebugConsole native save confirmation armed; no SaveGame call was made.", StringComparison.Ordinal),
                "DebugConsole must preserve exact time-scale originals, fully release the title UI graph, and require a two-click native-save confirmation.");
            Assert(
                !uiSource.Contains("\"DTMAPI.DebugConsole.Advanced.Generator\"", StringComparison.Ordinal) &&
                !uiSource.Contains("\"DTMAPI.DebugConsole.Advanced.Monster\"", StringComparison.Ordinal) &&
                !uiSource.Contains("\"DTMAPI.DebugConsole.Advanced.Resource\"", StringComparison.Ordinal) &&
                !uiSource.Contains("\"DTMAPI.DebugConsole.Advanced.Note\"", StringComparison.Ordinal) &&
                uiSource.Contains("\"NATURE\"", StringComparison.Ordinal) &&
                uiSource.Contains("\"OPERATE\"", StringComparison.Ordinal) &&
                uiSource.Contains("\"SCIENCE\"", StringComparison.Ordinal) &&
                uiSource.Contains("\"ANIMAL\"", StringComparison.Ordinal) &&
                uiSource.Contains("AddTechnologyPoints", StringComparison.Ordinal) &&
                !uiSource.Contains("AddFirstTechPoint", StringComparison.Ordinal) &&
                !uiSource.Contains("void GiveCreativeGenerator", StringComparison.Ordinal) &&
                !uiSource.Contains("void SpawnFirstMonster", StringComparison.Ordinal) &&
                !uiSource.Contains("void SpawnFirstResource", StringComparison.Ordinal) &&
                nativeActions.Contains("GiveCreativeGenerator", StringComparison.Ordinal) &&
                nativeActions.Contains("SpawnMonster", StringComparison.Ordinal) &&
                nativeActions.Contains("SpawnResource", StringComparison.Ordinal) &&
                DebugConsoleNativeActions.StableItemCategoryIds.SequenceEqual(new[]
                {
                    "tool", "material", "farm", "husbandry", "product",
                    "food", "kit", "equipment", "construction", "special"
                }) &&
                uiSource.Contains("Concat(new[] { \"Monster\", \"Animal\" })", StringComparison.Ordinal) &&
                uiSource.Contains("EnsureCatalogCellPool(pageSize)", StringComparison.Ordinal) &&
                uiSource.Contains("PrepareRetainedUiForClose", StringComparison.Ordinal) &&
                uiSource.Contains("ClearRetainedCatalogCells", StringComparison.Ordinal) &&
                uiSource.Contains("itemSpriteCache.Clear()", StringComparison.Ordinal) &&
                !uiSource.Contains("private bool dirty", StringComparison.Ordinal) &&
                uiSource.Contains("ActivateTab(tab)", StringComparison.Ordinal) &&
                !uiSource.Contains("activeTab = tab;\n                    dirtyRegions |= UiDirtyRegion.Layout", StringComparison.Ordinal) &&
                uiSource.Contains("cell.BindItem(item, hover, tooltipX, y)", StringComparison.Ordinal) &&
                uiSource.Contains("CatalogPageAccumulator", StringComparison.Ordinal) &&
                !uiSource.Contains("cell.Click.Bind(() =>", StringComparison.Ordinal) &&
                uiSource.Contains("SetActive(hoverTooltipRoot, true)", StringComparison.Ordinal) &&
                weatherActions.Contains("InventorySourceGroups()", StringComparison.Ordinal) &&
                weatherActions.Contains("long requestedStart", StringComparison.Ordinal) &&
                spawnActions.Contains("return monsterCatalog;", StringComparison.Ordinal) &&
                nativeAccess.Contains("NativeMemberKey", StringComparison.Ordinal) &&
                nativeAccess.Contains("NativeMethodKey", StringComparison.Ordinal) &&
                !nativeAccess.Contains("AssemblyQualifiedName ?? type.FullName", StringComparison.Ordinal) &&
                spawnActions.Contains("ReferenceObjectComparer.Instance", StringComparison.Ordinal) &&
                spawnActions.Contains("new HashSet<long>()", StringComparison.Ordinal) &&
                spawnActions.Contains("walkableArguments", StringComparison.Ordinal) &&
                !spawnActions.Contains("Enumerable.Range", StringComparison.Ordinal) &&
                productSource.Contains("if (factor == 1f)", StringComparison.Ordinal) &&
                uiSource.Contains("UnityEngine.UI.ScrollRect", StringComparison.Ordinal) &&
                uiSource.Contains("UnityEngine.UI.RectMask2D", StringComparison.Ordinal) &&
                uiSource.Contains("CreateResponsiveRegion", StringComparison.Ordinal) &&
                weatherActions.Contains("StableWeatherIds", StringComparison.Ordinal) &&
                uiSource.Contains("DebugConsoleLayout.CatalogCellSize", StringComparison.Ordinal) &&
                uiSource.Contains("DebugConsoleLayout.CatalogPagerBottomInset", StringComparison.Ordinal) &&
                uiSource.Contains("Vector2(side, side)", StringComparison.Ordinal) &&
                uiSource.Contains("const int weatherColumns = 7", StringComparison.Ordinal) &&
                uiSource.Contains("? 4", StringComparison.Ordinal) &&
                uiSource.Contains("destinations.Length", StringComparison.Ordinal) &&
                uiSource.Contains("allowCompactScroll: false", StringComparison.Ordinal) &&
                uiSource.Contains("Vector2(76, source.Length > 0 ? 20 : 34)", StringComparison.Ordinal) &&
                !uiSource.Contains("teleportPage", StringComparison.Ordinal) &&
                !uiSource.Contains("DTMAPI.DebugConsole.Teleport.Page", StringComparison.Ordinal) &&
                !uiSource.Contains("forceScroll", StringComparison.Ordinal) &&
                !uiSource.Contains("100, 74", StringComparison.Ordinal) &&
                !uiSource.Contains("rows * 78", StringComparison.Ordinal) &&
                productSource.Contains("\"鹿神池塘\", \"林地深处-右端\"", StringComparison.Ordinal) &&
                !productSource.Contains("后山悬崖", StringComparison.Ordinal) &&
                !productSource.Contains("Expression.Compile", StringComparison.Ordinal) &&
                !productSource.Contains("AddPointerClickListener", StringComparison.Ordinal) &&
                !productSource.Contains("AddPointerEventListener", StringComparison.Ordinal) &&
                !productSource.Contains("CreateRow", StringComparison.Ordinal) &&
                !productSource.Contains("TryGetScreenSize", StringComparison.Ordinal) &&
                !productSource.Contains("modItemsOnly", StringComparison.Ordinal) &&
                !entry.Contains("Command_Generate", StringComparison.Ordinal) &&
                !entry.Contains("WeatherRegulator", StringComparison.Ordinal),
                "The UI must expose only the stable ten item categories plus Monster/Animal, retain pooled fixed slots and exact semantic data, and keep Generator/Resource compatibility executors outside the product UI.");
        }

        private static void DebugConsoleTitleCleanupRunnerUsesStableProductOwnershipEvidence()
        {
            string runner = File.ReadAllText(
                Path.Combine(
                    FindRepositoryRoot(),
                    "tools",
                    "scripts",
                    "game-smoke",
                    "phases",
                    "assess-evidence.ps1"));

            Assert(
                runner.Contains("$productLoadSourceObserved", StringComparison.Ordinal) &&
                runner.Contains("Code mod load-source owner=DTMAPI\\.DebugConsoleMod; ", StringComparison.Ordinal) &&
                runner.Contains("identity=Advanced CodeMod; provenance=sdk-reference-receipt-verified;.*", StringComparison.Ordinal) &&
                runner.Contains("expectedHarmonyOwner=dtmapi\\.mod\\.dtmapi\\.debugconsolemod;", StringComparison.Ordinal) &&
                runner.Contains("$productHostObserved", StringComparison.Ordinal) &&
                runner.Contains("DebugConsole status UI\\.DebugConsoleHost=product-native", StringComparison.Ordinal) &&
                runner.Contains("$productOwnerObserved = $productLoadSourceObserved -and $productHostObserved", StringComparison.Ordinal) &&
                !runner.Contains("-Pattern 'nativeOwner=ProductNative'", StringComparison.Ordinal),
                "The title-cleanup gate must bind the exact zero UI graph to stable Advanced receipt, Harmony-owner and product-host evidence instead of an incidental action-result token.");
        }

        private static void DebugConsoleHostBindingFollowsOwnerLifetime()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                var ui = CreateDebugConsoleUi(runtime);
                var host = new UnitOwnerBoundDebugConsoleHost(ui);
                var hostManifest = new ManifestModel
                {
                    Name = "Debug Console Host",
                    Author = "DTMAPI",
                    Version = DtmApiRuntime.ApiVersion,
                    UniqueID = "DTMAPI.DebugConsoleHost",
                    Type = "RuntimeApi"
                };
                runtime.RegisterRuntimeApi<IDebugConsoleApi>(hostManifest, host, OwnerBoundGameBridgeApis.ForDebugConsole(host));
                var bridge = new DolocTownGameBridge(runtime, debugConsoleApi: host);
                var owner = new ManifestModel
                {
                    Name = "Debug Console Consumer",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.DebugConsoleConsumer",
                    Type = "CodeMod"
                };
                var otherOwner = new ManifestModel
                {
                    Name = "Other Debug Console Consumer",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.OtherDebugConsoleConsumer",
                    Type = "CodeMod"
                };
                bool ownerActive = true;
                IModRegistry registry = runtime.ModRegistry.CreateOwnerBoundRegistry(owner, () =>
                {
                    if (!ownerActive)
                        throw new InvalidOperationException("owner inactive");
                });
                IDebugConsoleApi api = registry.GetApi<IDebugConsoleApi>(hostManifest.UniqueID)
                    ?? throw new InvalidOperationException("Debug console host should return an owner-bound facade.");
                IOwnerBoundApiHost ownerHost = host;

                api.Bind(owner, null, null, null, null, null);
                GetPrivateField<List<object>>(ui, "eventBinders").Add(new object());
                GetPrivateField<List<object>>(ui, "inputFields").Add(new object());
                SetPrivateField(ui, "root", new object());
                Assert(ownerHost.CountOwnerResources(owner.UniqueID) == 1 && CountGameBridgeOwnerResourcesForTest(bridge, owner.UniqueID) == 1, "A bound DebugConsole consumer should contribute one authoritative GameBridge owner root.");
                AssertThrows(() => ui.BindAdvanced(otherOwner, null), "The process-lifetime DebugConsole host must reject implicit replacement by another owner.");

                ModOwnerParticipantCleanupSummary participantSummary = runtime.CleanupModOwnerParticipants(owner.UniqueID, ModOwnerCleanupReason.EntryFailed);
                Assert(participantSummary.RemovedResources == 1 && participantSummary.RemainingResources == 0, "The GameBridge participant should remove the DebugConsole host binding and report zero remaining roots.");
                Assert(ownerHost.CountOwnerResources(owner.UniqueID) == 0 && CountGameBridgeOwnerResourcesForTest(bridge, owner.UniqueID) == 0, "DebugConsole participant cleanup should clear its canonical owner root.");
                FieldInfo ownerManifestField = ui.GetType().GetField("ownerManifest", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("DebugConsole host owner field should exist.");
                FieldInfo rootField = ui.GetType().GetField("root", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("DebugConsole host root field should exist.");
                Assert(ownerManifestField.GetValue(ui) == null && GetPrivateField<List<object>>(ui, "eventBinders").Count == 0 && GetPrivateField<List<object>>(ui, "inputFields").Count == 0 && rootField.GetValue(ui) == null, "DebugConsole owner cleanup should sever the manifest and reflected UI callback graph.");
                for (int frame = 0; frame < 4; frame++)
                    host.Update();
                Assert(rootField.GetValue(ui) == null && ownerHost.CountOwnerResources(owner.UniqueID) == 0, "Compatibility Update frames must not recreate a hidden DebugConsole UI root after owner cleanup.");

                api.Bind(owner, null, null, null, null, null);
                Assert(ownerHost.CountOwnerResources(owner.UniqueID) == 1, "An active owner may bind again after a participant-only cleanup retry.");
                ownerActive = false;
                string summary = InvokeFailedOwnerCleanup(runtime, owner.UniqueID);
                Assert(ownerHost.CountOwnerResources(owner.UniqueID) == 0 && CountGameBridgeOwnerResourcesForTest(bridge, owner.UniqueID) == 0, "Unified owner deactivation should leave no DebugConsole host root.");
                Assert(summary.Contains("remaining=0", StringComparison.Ordinal), "DebugConsole owner deactivation should report zero remaining roots.");
                AssertThrows(() => api.SetLanguage(owner, "english"), "A stale DebugConsole facade must reject calls after consumer deactivation.");
                for (int frame = 0; frame < 4; frame++)
                    host.Update();
                Assert(rootField.GetValue(ui) == null && ownerHost.CountOwnerResources(owner.UniqueID) == 0, "Inactive-owner Update frames must keep the DebugConsole host graph at zero.");
                Assert(runtime.ModRegistry.GetApi<IDebugConsoleApi>(hostManifest.UniqueID) != null, "Consumer deactivation must preserve the process-lifetime DebugConsole provider.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DebugConsoleEventSystemDoesNotChooseInputSystemModuleWhenNotReady()
        {
            Type consoleType = typeof(DebugConsoleUi);
            MethodInfo select = consoleType.GetMethod("TrySelectEventSystemInputModule", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("TrySelectEventSystemInputModule should exist.");

            object ui = RuntimeHelpers.GetUninitializedObject(consoleType);
            SetPrivateField(ui, "inputSystemUiInputModuleType", typeof(FakeInputSystemUiInputModule));
            SetPrivateField(ui, "standaloneInputModuleType", typeof(FakeStandaloneInputModule));
            object?[] args = { null, null };
            bool selected = (bool)(select.Invoke(ui, args) ?? false);
            Assert(selected, "Debug console should keep a fallback path when Unity Input System is not initialized.");
            Assert(ReferenceEquals(args[0], typeof(FakeStandaloneInputModule)), "Debug console should not select InputSystemUIInputModule before Unity Input System is initialized.");
            Assert(((string?)args[1] ?? string.Empty).Contains("StandaloneInputModule", StringComparison.Ordinal), "Debug console fallback source should be visible in diagnostics.");

            SetPrivateField(ui, "standaloneInputModuleType", null);
            args = new object?[] { null, null };
            selected = (bool)(select.Invoke(ui, args) ?? true);
            Assert(!selected, "Debug console should defer EventSystem creation instead of creating InputSystemUIInputModule when no safe module is available.");
            Assert(((string?)args[1] ?? string.Empty).Contains("not initialized", StringComparison.OrdinalIgnoreCase), "Deferred debug console EventSystem creation should explain the Input System state.");
        }

        private static void DebugConsoleUsesOnlyCellPointerDownRightClickGivePath()
        {
            Type consoleType = typeof(DebugConsoleUi);

            Assert(consoleType.GetMethod("TryGivePointerHitItem", BindingFlags.Instance | BindingFlags.NonPublic) == null, "Debug console should not keep the global Mouse1 hit-test give path.");
            Assert(consoleType.GetNestedType("ItemCellHitTarget", BindingFlags.NonPublic) == null, "Debug console should not keep screen-rectangle item hit targets after consolidating right-click give.");
            Assert(consoleType.GetField("hoveredItem", BindingFlags.Instance | BindingFlags.NonPublic) == null, "Debug console should not keep a hover-based Mouse1 fallback give path.");
            Assert(consoleType.GetMethod("TryGiveRightClickItem", BindingFlags.Instance | BindingFlags.NonPublic) != null, "Debug console should keep the item-cell PointerDown right-click give path.");
            Assert(consoleType.GetMethod("IsAnyTextInputFocused", BindingFlags.Instance | BindingFlags.NonPublic) != null, "Debug console should guard Y-close while a search input field has focus.");
            Assert(consoleType.GetField("inputFields", BindingFlags.Instance | BindingFlags.NonPublic) != null, "Debug console should track reflected input fields for focus-aware Y handling.");

            MethodInfo isYHotkeyReason = consoleType.GetMethod("IsYHotkeyReason", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("IsYHotkeyReason should exist.");
            Assert((bool)(isYHotkeyReason.Invoke(null, new object?[] { "Y" }) ?? false), "The physical Y hotkey reason should suppress same-frame close.");
            Assert((bool)(isYHotkeyReason.Invoke(null, new object?[] { "hotkey Y" }) ?? false), "The named Y hotkey reason should suppress same-frame close.");
            Assert(!(bool)(isYHotkeyReason.Invoke(null, new object?[] { "Only opened by button" }) ?? true), "Debug console should not treat arbitrary reason text containing the letter Y as the Y hotkey.");

            MethodInfo isAnyTextInputFocused = consoleType.GetMethod("IsAnyTextInputFocused", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("IsAnyTextInputFocused should exist.");
            object ui = RuntimeHelpers.GetUninitializedObject(consoleType);
            SetPrivateField(ui, "inputFields", new List<object> { new FakeDebugConsoleInputField(true) });
            Assert((bool)(isAnyTextInputFocused.Invoke(ui, Array.Empty<object>()) ?? false), "Focused search input should block the Y close hotkey.");
            SetPrivateField(ui, "inputFields", new List<object> { new FakeDebugConsoleInputField(false) });
            Assert(!(bool)(isAnyTextInputFocused.Invoke(ui, Array.Empty<object>()) ?? true), "Unfocused search input should not block the Y close hotkey.");

            MethodInfo classifySelectedInput = consoleType.GetMethod("IsFocusedTextInputComponent", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("The cached selected-input classifier should exist.");
            MethodInfo getComponent = typeof(FakeDebugConsoleGameObject).GetMethod(nameof(FakeDebugConsoleGameObject.GetComponent))
                ?? throw new InvalidOperationException("Fake GameObject GetComponent should exist.");
            PropertyInfo isFocused = typeof(FakeDebugConsoleInputField).GetProperty(nameof(FakeDebugConsoleInputField.isFocused))
                ?? throw new InvalidOperationException("Fake InputField focus property should exist.");
            Assert((bool)(classifySelectedInput.Invoke(null, new object?[]
            {
                new FakeDebugConsoleGameObject(new FakeDebugConsoleInputField(true)),
                typeof(FakeDebugConsoleInputField),
                getComponent,
                isFocused
            }) ?? false), "A selected and focused native-style InputField must reserve Y for text input.");
            Assert(!(bool)(classifySelectedInput.Invoke(null, new object?[]
            {
                new FakeDebugConsoleGameObject(new FakeDebugConsoleInputField(false)),
                typeof(FakeDebugConsoleInputField),
                getComponent,
                isFocused
            }) ?? true), "A selected but unfocused InputField must not reserve Y.");
        }

        private static void MovementDebugLeaseClearsAtSaveBoundariesAndMissingMotionReset()
        {
            DebugConsoleFinalSpeedMultiplierPreservesNativeBuffState();
        }

        private sealed class FakeDebugConsoleBodyController
        {
            public float NativeMoveSpeed { get; set; }
            public float MoveScaler { get; set; }
            public float MoveSpeed => NativeMoveSpeed;
        }

        private sealed class FakeDebugConsoleCreativeConfig
        {
            private bool material;
            private bool shop;
            public bool ignoreMaterialCost
            {
                get => material;
                set
                {
                    if (FailNextMaterialWrite)
                    {
                        FailNextMaterialWrite = false;
                        throw new InvalidOperationException(
                            "Injected creative material-flag write failure.");
                    }
                    material = value;
                    if (ArmPartialApplyAndRollbackFailure &&
                        value)
                    {
                        ArmPartialApplyAndRollbackFailure =
                            false;
                        FailNextShopWrite = true;
                        FailNextMaterialWrite = true;
                    }
                }
            }
            public bool ignoreSpiritCost { get; set; }
            public bool FailNextShopWrite { get; set; }
            public bool FailNextMaterialWrite { get; set; }
            public bool
                ArmPartialApplyAndRollbackFailure { get; set; }
            public bool skipMoneyVerifyInShop
            {
                get => shop;
                set
                {
                    if (FailNextShopWrite)
                    {
                        FailNextShopWrite = false;
                        throw new InvalidOperationException(
                            "Injected creative shop-flag write failure.");
                    }
                    shop = value;
                }
            }
        }

        private sealed class FakeDebugConsoleGameManager
        {
            public object? gameInitConfig;
        }

        private sealed class FakeDebugConsoleTimeScaleManager
        {
            private float timeScale;

            internal FakeDebugConsoleTimeScaleManager(
                float initial) =>
                timeScale = initial;

            public float currentTimeScale
            {
                get
                {
                    if (FailNextRead)
                    {
                        FailNextRead = false;
                        throw new InvalidOperationException(
                            "Injected time-scale readback failure.");
                    }
                    return timeScale;
                }
                set
                {
                    if (FailNextWrite)
                    {
                        FailNextWrite = false;
                        throw new InvalidOperationException(
                            "Injected time-scale rollback failure.");
                    }
                    timeScale = value;
                    if (ArmPostWriteReadAndRollbackFailure)
                    {
                        ArmPostWriteReadAndRollbackFailure =
                            false;
                        FailNextRead = true;
                        FailNextWrite = true;
                    }
                }
            }

            internal float RawTimeScale => timeScale;
            internal bool FailNextRead { get; set; }
            internal bool FailNextWrite { get; set; }
            internal bool
                ArmPostWriteReadAndRollbackFailure { get; set; }
        }

        private sealed class FakeDebugConsoleWeatherArchive
        {
            public string LocalWeatherType
            {
                get;
                set;
            } = "SUNNY";

            public FakeDebugConsoleDate DateNow { get; } =
                new FakeDebugConsoleDate();

            public FakeDebugConsoleTimeData timeData { get; } =
                new FakeDebugConsoleTimeData();

            public FakeDebugConsoleRoom currentRoom { get; } =
                new FakeDebugConsoleRoom();
        }

        private sealed class FakeDebugConsoleDate
        {
            public int Year { get; } = 1;
            public int Month { get; } = 2;
            public int Day { get; } = 3;
            public int Hour { get; } = 4;
        }

        private sealed class FakeDebugConsoleRoom
        {
            public FakeDebugConsoleRoomInfo RoomInfo { get; } =
                new FakeDebugConsoleRoomInfo();
        }

        private sealed class FakeDebugConsoleRoomInfo
        {
            public string SeasonGroupId { get; } = "farm";
        }

        private sealed class FakeDebugConsoleTimeData
        {
            public FakeDebugConsoleSeason GetSeasonInfo(
                string seasonGroupId) =>
                new FakeDebugConsoleSeason();

            public FakeDebugConsoleWeatherProto[] GetWeatherInfoOfDay(
                string seasonGroupId,
                int dayOffset) =>
                new[]
                {
                    new FakeDebugConsoleWeatherProto("RAIN")
                };
        }

        private sealed class FakeDebugConsoleSeason
        {
            public string Id { get; } = "SPRING";
            public string Title { get; } = "Spring";
        }

        private sealed class FakeDebugConsoleTables
        {
            internal FakeDebugConsoleTables(
                IEnumerable<string>? weatherIds = null)
            {
                TbWeather = new FakeDebugConsoleWeatherTable(weatherIds);
            }

            public FakeDebugConsoleWeatherTable TbWeather { get; }
        }

        private sealed class FakeDebugConsoleWeatherTable
        {
            internal FakeDebugConsoleWeatherTable(
                IEnumerable<string>? weatherIds)
            {
                DataList = (weatherIds ??
                        DebugConsoleNativeActions.StableWeatherIds)
                    .Select(value => new FakeDebugConsoleWeatherProto(value))
                    .ToArray();
            }

            public FakeDebugConsoleWeatherProto[] DataList { get; }
        }

        private sealed class FakeDebugConsoleWeatherProto
        {
            internal FakeDebugConsoleWeatherProto(
                string id)
            {
                Id = id;
                Title = id;
            }

            public string Id { get; }
            public string Title { get; }
            public string Description => string.Empty;
            public bool IsMalignantWeather => false;
            public bool IsRainy =>
                Id == "RAIN" ||
                Id == "THUNDERSTORM" ||
                Id == "ACID_RAIN";
            public bool IsWindy => false;
            public double Sun => 1d;
            public double Water => IsRainy ? 1d : 0d;
            public double Wind => 0d;
        }

        private sealed class FakeDebugConsoleAdvancedActions :
            IAdvancedActions
        {
            internal List<(string PointTypeId, int Amount)> Calls
            {
                get;
            } = new List<(string PointTypeId, int Amount)>();

            internal string FailPointTypeId { get; set; } = string.Empty;

            public IReadOnlyList<TechPointDebugOption>
                GetTechPointOptions() =>
                throw new InvalidOperationException(
                    "The 1.1 +tech button must not enumerate a category selector.");

            public IReadOnlyList<SpawnCatalogOption> GetMonsterCatalog() =>
                throw new NotSupportedException();
            public IReadOnlyList<AnimalCatalogOption> GetAnimalCatalog() =>
                throw new NotSupportedException();

            public DebugValueResult AddTechPoint(
                IManifest owner,
                string pointTypeId,
                int amount)
            {
                Calls.Add((pointTypeId, amount));
                bool success = !pointTypeId.Equals(
                    FailPointTypeId,
                    StringComparison.Ordinal);
                return new DebugValueResult
                {
                    Success = success,
                    ValueId = pointTypeId,
                    RequestedDelta = amount,
                    BeforeValue = 0,
                    AfterValue = success ? amount : 0,
                    FailureReason = success ? string.Empty : "injected-failure"
                };
            }

            public IReadOnlyList<SpawnDebugOption> GetMonsterOptions() =>
                throw new NotSupportedException();
            public IReadOnlyList<SpawnDebugOption> GetResourceOptions() =>
                throw new NotSupportedException();
            public CreativeModeState GetCreativeModeState() =>
                throw new NotSupportedException();
            public TimeSkipResult AdvanceTime(
                IManifest owner,
                AdvancedTimeAdvanceKind kind,
                int amount) =>
                throw new NotSupportedException();
            public TimeScaleDebugResult SetTimeScale(
                IManifest owner,
                double multiplier) =>
                throw new NotSupportedException();
            public TimeScaleDebugResult ResetTimeScale(
                IManifest owner,
                string reason) =>
                throw new NotSupportedException();
            public DebugValueResult AddMoney(
                IManifest owner,
                int amount) =>
                throw new NotSupportedException();
            public DebugCommandResult UnlockAllTechTrees(IManifest owner) =>
                throw new NotSupportedException();
            public CropMaturityResult MatureAllCrops(IManifest owner) =>
                throw new NotSupportedException();
            public CreativeModeResult SetCreativeMode(
                IManifest owner,
                bool enabled) =>
                throw new NotSupportedException();
            public InventoryGiveResult GiveCreativeGenerator(
                IManifest owner) =>
                throw new NotSupportedException();
            public SpawnActionResult SpawnMonster(
                IManifest owner,
                string monsterId,
                int count) =>
                throw new NotSupportedException();
            public SpawnActionResult SpawnAnimal(
                IManifest owner,
                string cardId,
                int count) =>
                throw new NotSupportedException();
            public SpawnDebugResult SpawnResource(
                IManifest owner,
                string resourceId,
                int count) =>
                throw new NotSupportedException();
            public BridgeFeatureStatus GetStatus() =>
                new BridgeFeatureStatus(
                    "diagnostic/unit",
                    "Unit fake");
        }

        private sealed class FakeDebugConsolePatchBackend :
            ICompatibilityDebugConsolePatchBackend
        {
            private readonly Dictionary<string, int> owned =
                new Dictionary<string, int>(
                    StringComparer.Ordinal);
            private int patchAttempt;
            private int unpatchAttempt;

            internal int FailPatchAttempt { get; set; }
            internal int FailUnpatchAttempt { get; set; }
            internal bool ProductOwnerPresent { get; set; }
            internal int TotalOwnedPatches =>
                owned.Values.Sum();

            public bool TryPatch(
                CompatibilityDebugConsolePatchSpec spec)
            {
                patchAttempt++;
                if (FailPatchAttempt > 0 &&
                    patchAttempt == FailPatchAttempt)
                    return false;
                owned[spec.Key] = 1;
                return true;
            }

            public bool TryUnpatch(
                CompatibilityDebugConsolePatchSpec spec)
            {
                unpatchAttempt++;
                if (FailUnpatchAttempt > 0 &&
                    unpatchAttempt == FailUnpatchAttempt)
                    return false;
                owned.Remove(spec.Key);
                return true;
            }

            public int CountOwnerPatches(
                CompatibilityDebugConsolePatchSpec spec,
                string ownerId)
            {
                if (ownerId.Equals(
                        CompatibilityDebugConsoleHookOwner
                            .ProductHarmonyOwner,
                        StringComparison.Ordinal))
                {
                    return ProductOwnerPresent ? 1 : 0;
                }
                return owned.TryGetValue(
                    spec.Key,
                    out int count)
                    ? count
                    : 0;
            }
        }

        private sealed class FakeDebugConsoleInstantSaveActions :
            IInstantSaveActions
        {
            internal int SaveCalls { get; private set; }

            public InstantSaveDebugState GetState() =>
                new InstantSaveDebugState
                {
                    CanSave = true,
                    SaveSlot = 3
                };

            public InstantSaveDebugResult Save(
                IManifest owner,
                bool reloadAfterSave)
            {
                SaveCalls++;
                return new InstantSaveDebugResult
                {
                    Success = true,
                    SaveSlot = 3
                };
            }

            public BridgeFeatureStatus GetStatus() =>
                new BridgeFeatureStatus(
                    "diagnostic/unit",
                    "Unit fake");
        }

        private sealed class FakeDebugConsoleInputField
        {
            public FakeDebugConsoleInputField(bool focused)
            {
                isFocused = focused;
            }

            public bool isFocused { get; }
        }

        private sealed class FakeDebugConsoleGameObject
        {
            private readonly object component;

            public FakeDebugConsoleGameObject(object component)
            {
                this.component = component;
            }

            public object? GetComponent(Type componentType) =>
                componentType.IsInstanceOfType(component)
                    ? component
                    : null;
        }

        private sealed class FakeActionCompletionApi : IActionCompletionApi
        {
            public void Configure(IManifest owner, ActionCompletionOptions options) { }
            public BridgeFeatureStatus GetStatus(string uniqueId) => new BridgeFeatureStatus("test", uniqueId);
        }
    }
}
