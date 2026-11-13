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
                new DebugConsoleTestRuntime(
                    runtime));

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
            DebugConsoleUiMessagesStayLocalized();
            string product = Path.Combine(
                FindRepositoryRoot(),
                "mods",
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
                    values.ContainsKey("config.customTime.name") &&
                    values.ContainsKey("config.customTime.description") &&
                    values.ContainsKey("debug.advanced.scaleCustom") &&
                    values.ContainsKey("debug.god.on") &&
                    !values.Keys.Any(key => key.StartsWith("debug.creative.", StringComparison.Ordinal)) &&
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
                    "Every DebugConsole UI locale must contain mode/config labels and non-empty keys, retain visible complete/partial spawn text, and omit retired creative/rollback/circuit/current-location/CSV copy.");
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
                "mods",
                "DebugConsole",
                "src",
                "Ui",
                "DebugConsoleRawInput.cs"));
            string uiSource = string.Join(
                "\n",
                Directory.GetFiles(
                        Path.Combine(
                            repo,
                            "mods",
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


        }

        private static void DebugConsoleEscapeCloseDrainProtectsNativeOwnerBoundary()
        {
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            var host = CreateDebugConsoleUi(runtime);
            SetPrivateField(host, "suppressEscapeCloseUntilReleased", true);

            try
            {
                Assert(host.AdvanceEscapeCloseDrain(escapePressed: true, escapeDown: true), "A held or repeated Escape edge must keep the post-close native-input drain active.");
                Assert(DebugConsoleInputGate.NativeInputDrainActive, "The optional Compatibility native-input boundary must stay active while Escape is held.");

                bool enterUiResult = false;
                Assert(!DebugConsoleInputHooks.EnterUiCheckPrefix(ref enterUiResult) && enterUiResult, "EnterUICheck must be skipped during the Escape close drain even after the visual modal has closed.");
                Assert(!DebugConsoleInputHooks.UseToolPrefix() && !DebugConsoleInputHooks.UseItemPrefix(), "Tool and item native input must remain isolated during the Escape close drain.");

                Assert(host.AdvanceEscapeCloseDrain(escapePressed: false, escapeDown: false), "One clean frame must not end the Escape close drain.");
                Assert(!host.AdvanceEscapeCloseDrain(escapePressed: false, escapeDown: false), "Two consecutive clean frames must end the bounded Escape close drain.");
                Assert(!DebugConsoleInputGate.NativeInputDrainActive, "The native-input drain must release after the clean-frame boundary.");

                enterUiResult = false;
                Assert(DebugConsoleInputHooks.EnterUiCheckPrefix(ref enterUiResult) && !enterUiResult, "Native EnterUICheck must resume after the Escape close drain ends.");
                Assert(DebugConsoleInputHooks.UseToolPrefix() && DebugConsoleInputHooks.UseItemPrefix(), "Native tool and item input must resume after the Escape close drain ends.");
            }
            finally
            {
                DebugConsoleInputGate.Reset();
            }
        }

        private static void DebugConsoleFinalSpeedMultiplierPreservesNativeBuffState()
        {
            var runtime = new DtmApiRuntime(
                new FakeHost(NewTempGameDir()),
                new ConfigMenuRegistry());
            var adapter = new DebugConsoleTestRuntime(
                runtime);
            var actions = new DebugConsoleNativeActions(adapter);
            DolocAPI.archiveHandle = new ModeTestArchive();
            actions.BeginSaveSession();
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

            var closeUi = new DebugConsoleUi(adapter);
            closeUi.Bind(owner, null, null, null, null, null);
            closeUi.Open(owner, "button");
            closeUi.Close(owner, "button");
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
            var adapter = new DebugConsoleTestRuntime(
                runtime);
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
                    AppContext.BaseDirectory,
                    "HarmonyOwnerFixture",
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
            var result = DTMAPI.Testing.BoundedTestProcess.Run(start);
            string output = result.Output;
            string error = result.Error;
            Assert(
                result.ExitCode == 0 &&
                output.Contains(
                    "DebugConsoleHarmonyOwnerFixture: OK",
                    StringComparison.Ordinal),
                "The DebugConsole Harmony owner fixture failed. exit=" +
                result.ExitCode + "; stdout=" + output +
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

        private static void DebugConsoleHostBindingFollowsOwnerLifetime()
        {
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            var ui = CreateDebugConsoleUi(runtime);
            var owner = DebugConsoleUnitManifest();
            ui.Bind(owner, null, null, null, null, null);
            ui.Open(owner, "owner cleanup");
            GetPrivateField<List<object>>(ui, "eventBinders").Add(new object());
            GetPrivateField<List<object>>(ui, "inputFields").Add(new object());
            SetPrivateField(ui, "root", new object());
            Assert(ui.CountOwnerResources(owner.UniqueID) == 1 && runtime.UI.IsOpen, "Product owns its UI session.");
            ui.RemoveOwner(owner.UniqueID, "owner deactivation");
            Assert(!ui.IsOpen && !runtime.UI.IsOpen && ui.CountOwnerResources(owner.UniqueID) == 0,
                "Product cleanup must release its modal and binding.");
            Assert(GetPrivateField<List<object>>(ui, "eventBinders").Count == 0 &&
                GetPrivateField<List<object>>(ui, "inputFields").Count == 0 &&
                typeof(DebugConsoleUi).GetField("root", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(ui) == null, "Cleanup must sever the reflected UI graph.");
            for (int frame = 0; frame < 4; frame++) ui.Update();
            Assert(!ui.IsOpen && ui.CountOwnerResources(owner.UniqueID) == 0, "An inactive product must not recreate UI.");
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
            private bool godMode;
            public bool GodModeEnabled => godMode;
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
            public RoomCropMaturityResult MatureAllCrops(IManifest owner, int radius) =>
                throw new NotSupportedException();
            public SelectedStackDeletePreparation PrepareSelectedStackDelete(IManifest owner, long window) => throw new NotSupportedException();
            public bool IsSelectedStackDeleteCurrent(IManifest owner, SelectedStackDeleteToken token, long window) => false;
            public void CancelSelectedStackDelete(SelectedStackDeleteToken token) { }
            public SelectedStackDeleteResult ConfirmSelectedStackDelete(IManifest owner, SelectedStackDeleteToken token, long window) => throw new NotSupportedException();
            public DebugCommandResult SetGodMode(
                IManifest owner,
                bool enabled)
            {
                godMode = enabled;
                return new DebugCommandResult { Success = true, Message = "Native English status must not leak into localized UI." };
            }
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
    }
}
