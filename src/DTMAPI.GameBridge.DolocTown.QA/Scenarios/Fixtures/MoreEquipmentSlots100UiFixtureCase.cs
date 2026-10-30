using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private const int MoreEquipmentSlotsMaximumBackpackCapacity = 40;
        private const int MoreEquipmentSlotsMaximumOfficialPassiveCount = 5;
        private bool moreEquipmentSlots100UiAcceptanceEnabled;
        private int moreEquipmentSlots100UiStage;
        private DateTimeOffset moreEquipmentSlots100ResolutionRequestedAt;
        private int moreEquipmentSlots100ExpectedWidth;
        private int moreEquipmentSlots100ExpectedHeight;
        private int moreEquipmentSlots100OriginalWidth;
        private int moreEquipmentSlots100OriginalHeight;
        private bool moreEquipmentSlots100OriginalFullscreen;
        private int moreEquipmentSlots100OriginalPassiveCount = -1;
        private int moreEquipmentSlots100OriginalBackpackCount = -1;
        private object? moreEquipmentSlots100NativeAccessoriesBar;
        private object? moreEquipmentSlots100NativeBackpackPanel;
        private object? moreEquipmentSlots100NativeEquipmentPanel;
        private object? moreEquipmentSlots100ReuseSession;
        private object? moreEquipmentSlots100ReuseRow;
        private object[] moreEquipmentSlots100ReuseSlots =
            Array.Empty<object>();
        private readonly List<string> moreEquipmentSlots100Reports =
            new List<string>();
        private string moreEquipmentSlots100ConfigReceipt = string.Empty;
        private string moreEquipmentSlots100EvidenceDirectory = string.Empty;
        private string moreEquipmentSlots100SummaryPath = string.Empty;
        private string moreEquipmentSlots100PendingScreenshotPath =
            string.Empty;
        private string moreEquipmentSlots100PendingCanonicalCopyPath =
            string.Empty;
        private string moreEquipmentSlots100PendingScreenshotReport =
            string.Empty;
        private DateTimeOffset moreEquipmentSlots100PendingScreenshotAt;
        private int moreEquipmentSlots100ReopenStableFrames;
        private MoreEquipmentUiBounds? moreEquipmentSlots100LastReopenRowBounds;
        private MoreEquipmentUiBounds? moreEquipmentSlots100LastReopenDroneBounds;

        internal void ConfigureMoreEquipmentSlots100UiAcceptance(
            bool enabled) =>
            moreEquipmentSlots100UiAcceptanceEnabled = enabled;

        private static object? ReadMoreEquipmentSlotsProductRuntime()
        {
            Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => string.Equals(
                    candidate.GetName().Name,
                    "DTMAPI.MoreEquipmentSlots",
                    StringComparison.Ordinal));
            Type? callbacks = assembly?.GetType(
                "DTMAPI.MoreEquipmentSlots.MoreEquipmentSlotsCallbacks",
                throwOnError: false,
                ignoreCase: false);
            return callbacks?.GetField(
                "runtime",
                BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
        }

        private G4FixtureStepResult
            ObserveMoreEquipmentSlots100UiForFixture(
                object productRuntime,
                Type stateType,
                object? currentState,
                string canonicalScreenshotPath,
                string summaryPath)
        {
            moreEquipmentSlots100EvidenceDirectory =
                Path.GetDirectoryName(canonicalScreenshotPath) ?? ".";
            moreEquipmentSlots100SummaryPath = summaryPath;
            if (!TryCompleteMoreEquipmentSlots100Screenshot(
                    out string screenshotPendingReason))
            {
                return G4FixtureStepResult.Pending(
                    screenshotPendingReason);
            }
            bool equipmentOpen =
                currentState != null &&
                stateType.IsInstanceOfType(currentState);

            switch (moreEquipmentSlots100UiStage)
            {
                case 0:
                    if (!equipmentOpen)
                    {
                        return G4FixtureStepResult.Pending(
                            "Waiting for runner real B input to open EquipmentBarUiState for the MoreEquipmentSlots 1.0 UI acceptance.");
                    }
                    CaptureMoreEquipmentSlotsOriginalScreen();
                    StageMoreEquipmentSlotsMaximumNativeUi(
                        productRuntime,
                        currentState!);
                    RequestMoreEquipmentSlotsResolution(
                        1920,
                        1080);
                    moreEquipmentSlots100UiStage = 1;
                    return G4FixtureStepResult.Pending(
                        "MoreEquipmentSlots 1.0 requested the 1920x1080 acceptance surface.");

                case 1:
                    RequireMoreEquipmentSlotsOpen(equipmentOpen);
                    if (!IsMoreEquipmentSlotsResolutionReady(
                            1920,
                            1080))
                    {
                        return G4FixtureStepResult.Pending(
                            "Waiting for the exact 1920x1080 Unity surface before measuring Product UI.");
                    }
                    StageMoreEquipmentSlotsMaximumNativeUi(
                        productRuntime,
                        currentState!);
                    CaptureMoreEquipmentSlots100State(
                        productRuntime,
                        currentState!,
                        1920,
                        1080,
                        "equipment-slots-1920x1080-dynamic-row.png",
                        canonicalScreenshotPath);
                    moreEquipmentSlots100UiStage = 2;
                    return G4FixtureStepResult.Pending(
                        "Waiting for Unity to flush equipment-slots-1920x1080-dynamic-row.png.");

                case 2:
                    RequireMoreEquipmentSlotsOpen(equipmentOpen);
                    moreEquipmentSlots100ConfigReceipt =
                        ExerciseMoreEquipmentSlotsConfigurationCycle(
                            productRuntime,
                            currentState!);
                    RequestMoreEquipmentSlotsResolution(
                        1024,
                        768);
                    moreEquipmentSlots100UiStage = 3;
                    return G4FixtureStepResult.Pending(
                        "MoreEquipmentSlots 1.0 requested the 1024x768 acceptance surface after an exact configuration disable/re-enable cycle.");

                case 3:
                    RequireMoreEquipmentSlotsOpen(equipmentOpen);
                    if (!IsMoreEquipmentSlotsResolutionReady(
                            1024,
                            768))
                    {
                        return G4FixtureStepResult.Pending(
                            "Waiting for the exact 1024x768 Unity surface before measuring Product UI.");
                    }
                    StageMoreEquipmentSlotsMaximumNativeUi(
                        productRuntime,
                        currentState!);
                    CaptureMoreEquipmentSlots100State(
                        productRuntime,
                        currentState!,
                        1024,
                        768,
                        "equipment-slots-1024x768-dynamic-row.png",
                        canonicalCopyPath: null);
                    moreEquipmentSlots100UiStage = 4;
                    return G4FixtureStepResult.Pending(
                        "Waiting for Unity to flush equipment-slots-1024x768-dynamic-row.png.");

                case 4:
                    RequireMoreEquipmentSlotsOpen(equipmentOpen);
                    RestoreMoreEquipmentSlotsNativeUiFixture(
                        productRuntime,
                        currentState!);
                    CaptureMoreEquipmentSlotsReuseIdentity(
                        productRuntime);
                    WriteMoreEquipmentSlots100Summary(
                        "awaiting-first-close");
                    moreEquipmentSlots100UiStage = 5;
                    return G4FixtureStepResult.Pending(
                        "EVIDENCE_CAPTURED_WAITING_ESCAPE; uiOwner=product-1.0-dynamic-row; inputOwner=runner; evidenceOwner=qa; resolutions=1920x1080|1024x768.");

                case 5:
                    if (equipmentOpen)
                    {
                        return G4FixtureStepResult.Pending(
                            "EVIDENCE_CAPTURED_WAITING_ESCAPE; uiOwner=product-1.0; inputOwner=runner; evidenceOwner=qa.");
                    }
                    RequireMoreEquipmentSlotsClosedButRetained(
                        productRuntime,
                        "first-close");
                    moreEquipmentSlots100ReopenStableFrames = 0;
                    moreEquipmentSlots100LastReopenRowBounds = null;
                    moreEquipmentSlots100LastReopenDroneBounds = null;
                    moreEquipmentSlots100UiStage = 6;
                    return G4FixtureStepResult.Pending(
                        "MORE_EQUIPMENT_100_WAITING_REOPEN_B; firstClose=row-hidden; roots=retained; inputOwner=runner-real-B.");

                case 6:
                    if (!equipmentOpen)
                    {
                        return G4FixtureStepResult.Pending(
                            "MORE_EQUIPMENT_100_WAITING_REOPEN_B; firstClose=row-hidden; roots=retained; inputOwner=runner-real-B.");
                    }
                    RequireMoreEquipmentSlotsReuseIdentity(
                        productRuntime);
                    ForceMoreEquipmentSlotsCanvasLayout();
                    if (!TryObserveMoreEquipmentSlotsStableReopenSurface(
                            productRuntime,
                            1024,
                            768,
                            out string reopenPendingReason))
                    {
                        return G4FixtureStepResult.Pending(
                            reopenPendingReason);
                    }
                    string reopenScreenshot = Path.Combine(
                        moreEquipmentSlots100EvidenceDirectory,
                        "equipment-slots-1024x768-reopen.png");
                    BeginMoreEquipmentSlots100Screenshot(
                        reopenScreenshot,
                        canonicalCopyPath: null,
                        BuildMoreEquipmentSlots100Snapshot(
                            productRuntime,
                            currentState!,
                            1024,
                            768,
                            moreEquipmentSlots100OriginalBackpackCount,
                            moreEquipmentSlots100OriginalPassiveCount,
                            "1024x768-reopen-reuse"));
                    moreEquipmentSlots100UiStage = 7;
                    return G4FixtureStepResult.Pending(
                        "Waiting for Unity to flush equipment-slots-1024x768-reopen.png before final Escape is admitted.");

                case 7:
                    if (equipmentOpen)
                    {
                        return G4FixtureStepResult.Pending(
                            "MORE_EQUIPMENT_100_REOPEN_CAPTURED_WAITING_ESCAPE; roots=reused; dynamicRow=visible; inputOwner=runner-real-Escape.");
                    }
                    RequireMoreEquipmentSlotsClosedButRetained(
                        productRuntime,
                        "final-close");
                    RequestMoreEquipmentSlotsResolution(
                        moreEquipmentSlots100OriginalWidth,
                        moreEquipmentSlots100OriginalHeight,
                        moreEquipmentSlots100OriginalFullscreen);
                    moreEquipmentSlots100UiStage = 8;
                    return G4FixtureStepResult.Pending(
                        "MoreEquipmentSlots 1.0 final close passed; restoring the player's original screen mode before terminal status.");

                case 8:
                    if (!IsMoreEquipmentSlotsResolutionReady(
                            moreEquipmentSlots100OriginalWidth,
                            moreEquipmentSlots100OriginalHeight) ||
                        ReadStaticBool(
                            RequireMoreEquipmentSlotsRuntimeType(
                                "UnityEngine.Screen"),
                            "fullScreen") !=
                            moreEquipmentSlots100OriginalFullscreen)
                    {
                        return G4FixtureStepResult.Pending(
                            "Waiting for the original screen resolution to be restored after MoreEquipmentSlots UI acceptance.");
                    }
                    WriteMoreEquipmentSlots100Summary("verified");
                    g4EquipmentSlotsEvidenceCaptured = true;
                    g4EquipmentSlotsEvidenceSummary =
                        "uiOwner=product-1.0-dynamic-row; resolutions=1920x1080|1024x768; officialPassiveMax=5; productSlots=3; backpackMax=40; configCycle=passed; closeReopen=reused; perFramePolling=none; summary=" +
                        summaryPath;
                    moreEquipmentSlots100UiStage = 9;
                    return G4FixtureStepResult.Verified(
                        g4EquipmentSlotsEvidenceSummary);

                default:
                    return G4FixtureStepResult.Verified(
                        g4EquipmentSlotsEvidenceSummary);
            }
        }

        private static void RequireMoreEquipmentSlotsOpen(bool open)
        {
            if (!open)
            {
                throw new InvalidOperationException(
                    "EquipmentBarUiState closed before the MoreEquipmentSlots 1.0 UI acceptance reached its requested boundary.");
            }
        }

        private void CaptureMoreEquipmentSlotsOriginalScreen()
        {
            Type screen = RequireMoreEquipmentSlotsRuntimeType(
                "UnityEngine.Screen");
            moreEquipmentSlots100OriginalWidth =
                ReadStaticInt(screen, "width");
            moreEquipmentSlots100OriginalHeight =
                ReadStaticInt(screen, "height");
            moreEquipmentSlots100OriginalFullscreen =
                ReadStaticBool(screen, "fullScreen");
            if (moreEquipmentSlots100OriginalWidth <= 0 ||
                moreEquipmentSlots100OriginalHeight <= 0)
            {
                throw new InvalidOperationException(
                    "Unity Screen did not expose a valid original resolution.");
            }
        }

        private void RequestMoreEquipmentSlotsResolution(
            int width,
            int height,
            bool fullscreen = false)
        {
            Type screen = RequireMoreEquipmentSlotsRuntimeType(
                "UnityEngine.Screen");
            MethodInfo? setResolution = screen.GetMethod(
                "SetResolution",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[]
                {
                    typeof(int),
                    typeof(int),
                    typeof(bool)
                },
                modifiers: null);
            if (setResolution == null)
            {
                throw new MissingMethodException(
                    screen.FullName,
                    "SetResolution(int,int,bool)");
            }
            setResolution.Invoke(
                null,
                new object[]
                {
                    width,
                    height,
                    fullscreen
                });
            moreEquipmentSlots100ExpectedWidth = width;
            moreEquipmentSlots100ExpectedHeight = height;
            moreEquipmentSlots100ResolutionRequestedAt =
                DateTimeOffset.UtcNow;
        }

        private bool IsMoreEquipmentSlotsResolutionReady(
            int width,
            int height)
        {
            Type screen = RequireMoreEquipmentSlotsRuntimeType(
                "UnityEngine.Screen");
            return ReadStaticInt(screen, "width") == width &&
                ReadStaticInt(screen, "height") == height &&
                moreEquipmentSlots100ExpectedWidth == width &&
                moreEquipmentSlots100ExpectedHeight == height &&
                DateTimeOffset.UtcNow -
                    moreEquipmentSlots100ResolutionRequestedAt >=
                    TimeSpan.FromMilliseconds(750);
        }

        private void StageMoreEquipmentSlotsMaximumNativeUi(
            object productRuntime,
            object currentState)
        {
            object session = RequireMoreEquipmentSlotsSession(
                productRuntime);
            object accessoriesBar = RequireMemberForFixture(
                session,
                "AccessoriesBar");
            object panel = RequireMemberForFixture(
                currentState,
                "panel");
            object backpackPanel = RequireMemberForFixture(
                panel,
                "backpackPanel");
            moreEquipmentSlots100NativeAccessoriesBar =
                accessoriesBar;
            moreEquipmentSlots100NativeBackpackPanel =
                backpackPanel;
            moreEquipmentSlots100NativeEquipmentPanel = panel;
            int currentPassiveCount = ReadIntForFixture(
                session,
                "OfficialPassiveCount");
            int currentBackpackCount = CountObjects(
                ReadMember(backpackPanel, "allSelectablesArray"));
            if (moreEquipmentSlots100OriginalPassiveCount < 0)
            {
                if (currentPassiveCount < 1 ||
                    currentPassiveCount >
                        MoreEquipmentSlotsMaximumOfficialPassiveCount)
                {
                    throw new InvalidOperationException(
                        "The live native passive-slot count is outside the reviewed 1..5 range: " +
                        currentPassiveCount.ToString(
                            CultureInfo.InvariantCulture) +
                        ".");
                }
                moreEquipmentSlots100OriginalPassiveCount =
                    currentPassiveCount;
                moreEquipmentSlots100OriginalBackpackCount =
                    currentBackpackCount;
            }

            ExerciseMoreEquipmentSlotsSupportedPassiveRange(
                productRuntime,
                accessoriesBar);
            SetMoreEquipmentSlotsBackpackUiCapacity(
                backpackPanel,
                MoreEquipmentSlotsMaximumBackpackCapacity);
            InvokeNoArgForFixture(panel, "RebuildNavigation");
            ForceMoreEquipmentSlotsCanvasLayout();
        }

        private string
            RestoreMoreEquipmentSlots100EnvironmentAfterFailure(
                object productRuntime)
        {
            var receipts = new List<string>();
            try
            {
                if (moreEquipmentSlots100NativeAccessoriesBar != null)
                {
                    RequireMethodForFixture(
                        productRuntime.GetType(),
                        "OnAccessoriesBarClear",
                        parameterCount: 1).Invoke(
                            productRuntime,
                            new[]
                            {
                                moreEquipmentSlots100NativeAccessoriesBar
                            });
                    receipts.Add("product-row=hidden");
                }
            }
            catch (Exception ex)
            {
                receipts.Add(
                    "product-row=hide-failed:" +
                    ex.GetBaseException().Message);
            }
            try
            {
                if (moreEquipmentSlots100NativeAccessoriesBar != null &&
                    moreEquipmentSlots100OriginalPassiveCount >= 0)
                {
                    RenderMoreEquipmentSlotsPassiveCount(
                        moreEquipmentSlots100NativeAccessoriesBar,
                        moreEquipmentSlots100OriginalPassiveCount);
                    receipts.Add("passive-ui=restored");
                }
            }
            catch (Exception ex)
            {
                receipts.Add(
                    "passive-ui=restore-failed:" +
                    ex.GetBaseException().Message);
            }
            try
            {
                if (moreEquipmentSlots100NativeBackpackPanel != null &&
                    moreEquipmentSlots100OriginalBackpackCount >= 0)
                {
                    SetMoreEquipmentSlotsBackpackUiCapacity(
                        moreEquipmentSlots100NativeBackpackPanel,
                        moreEquipmentSlots100OriginalBackpackCount);
                    receipts.Add("backpack-ui=restored");
                }
                if (moreEquipmentSlots100NativeEquipmentPanel != null)
                {
                    InvokeNoArgForFixture(
                        moreEquipmentSlots100NativeEquipmentPanel,
                        "RebuildNavigation");
                }
                ForceMoreEquipmentSlotsCanvasLayout();
            }
            catch (Exception ex)
            {
                receipts.Add(
                    "backpack-ui=restore-failed:" +
                    ex.GetBaseException().Message);
            }
            try
            {
                if (moreEquipmentSlots100OriginalWidth > 0 &&
                    moreEquipmentSlots100OriginalHeight > 0)
                {
                    RequestMoreEquipmentSlotsResolution(
                        moreEquipmentSlots100OriginalWidth,
                        moreEquipmentSlots100OriginalHeight,
                        moreEquipmentSlots100OriginalFullscreen);
                    receipts.Add("screen=restore-requested");
                }
            }
            catch (Exception ex)
            {
                receipts.Add(
                    "screen=restore-failed:" +
                    ex.GetBaseException().Message);
            }
            return string.Join(";", receipts);
        }

        private void RestoreMoreEquipmentSlotsNativeUiFixture(
            object productRuntime,
            object currentState)
        {
            object session = RequireMoreEquipmentSlotsSession(
                productRuntime);
            object accessoriesBar = RequireMemberForFixture(
                session,
                "AccessoriesBar");
            object panel = RequireMemberForFixture(
                currentState,
                "panel");
            object backpackPanel = RequireMemberForFixture(
                panel,
                "backpackPanel");
            RenderMoreEquipmentSlotsPassiveCount(
                accessoriesBar,
                moreEquipmentSlots100OriginalPassiveCount);
            SetMoreEquipmentSlotsBackpackUiCapacity(
                backpackPanel,
                moreEquipmentSlots100OriginalBackpackCount);
            InvokeNoArgForFixture(panel, "RebuildNavigation");
            ForceMoreEquipmentSlotsCanvasLayout();
        }

        private static void RenderMoreEquipmentSlotsPassiveCount(
            object accessoriesBar,
            int count)
        {
            MethodInfo render = RequireMethodForFixture(
                accessoriesBar.GetType(),
                "RenderPassiveItems",
                parameterCount: 1);
            Type arrayType = render.GetParameters()[0].ParameterType;
            Type elementType = arrayType.GetElementType() ??
                throw new InvalidOperationException(
                    "AccessoriesBar.RenderPassiveItems did not expose an array element type.");
            render.Invoke(
                accessoriesBar,
                new object[]
                {
                    Array.CreateInstance(
                        elementType,
                        Math.Max(0, count))
                });
        }

        private static void
            ExerciseMoreEquipmentSlotsSupportedPassiveRange(
                object productRuntime,
                object accessoriesBar)
        {
            object retainedSession = RequireMoreEquipmentSlotsSession(
                productRuntime);
            object retainedRow = RequireMemberForFixture(
                retainedSession,
                "ProductRowRoot");
            object[] retainedSlots =
                ReadMoreEquipmentSlotsProductSlots(productRuntime);
            if (retainedSlots.Length != 3)
            {
                throw new InvalidOperationException(
                    "The supported passive-range fixture started without exactly three Product slots.");
            }

            for (int passiveCount = 1;
                 passiveCount <=
                    MoreEquipmentSlotsMaximumOfficialPassiveCount;
                 passiveCount++)
            {
                RenderMoreEquipmentSlotsPassiveCount(
                    accessoriesBar,
                    passiveCount);
                ForceMoreEquipmentSlotsCanvasLayout();
                object currentSession =
                    RequireMoreEquipmentSlotsSession(productRuntime);
                object diagnostics = InvokeNoArgResultForFixture(
                    productRuntime,
                    "GetDiagnosticsSnapshot");
                object[] currentSlots =
                    ReadMoreEquipmentSlotsProductSlots(productRuntime);
                int navigationCount = CountObjects(
                    ReadMember(
                        accessoriesBar,
                        "allSelectablesArray"));
                bool retained =
                    ReferenceEquals(
                        retainedSession,
                        currentSession) &&
                    ReferenceEquals(
                        retainedRow,
                        RequireMemberForFixture(
                            currentSession,
                            "ProductRowRoot")) &&
                    currentSlots.Length == retainedSlots.Length;
                for (int index = 0;
                     retained && index < currentSlots.Length;
                     index++)
                {
                    retained = ReferenceEquals(
                        retainedSlots[index],
                        currentSlots[index]);
                }
                if (!retained ||
                    ReadIntForFixture(
                        currentSession,
                        "OfficialPassiveCount") != passiveCount ||
                    ReadIntForFixture(
                        diagnostics,
                        "RootCount") != 1 ||
                    ReadIntForFixture(
                        diagnostics,
                        "CloneCount") != 3 ||
                    !ReadBoolForFixture(
                        diagnostics,
                        "UiVisible") ||
                    ReadBoolForFixture(
                        diagnostics,
                        "UiLayoutBlocked") ||
                    navigationCount != 2 + passiveCount + 3)
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots did not retain one visible native-plus-three Product row at reviewed official passive count " +
                        passiveCount.ToString(
                            CultureInfo.InvariantCulture) +
                        ".");
                }
            }
        }

        private static void SetMoreEquipmentSlotsBackpackUiCapacity(
            object backpackPanel,
            int count)
        {
            RequireMethodForFixture(
                backpackPanel.GetType(),
                "SetCapacity",
                parameterCount: 1).Invoke(
                    backpackPanel,
                    new object[]
                    {
                        Math.Max(0, count)
                    });
        }

        private string ExerciseMoreEquipmentSlotsConfigurationCycle(
            object productRuntime,
            object currentState)
        {
            object nativeAccessoriesBar = RequireMemberForFixture(
                RequireMoreEquipmentSlotsSession(productRuntime),
                "AccessoriesBar");
            for (int index = 0; index < 3; index++)
            {
                string itemId =
                    InvokeStringForFixture(
                        productRuntime,
                        "GetSlotItemId",
                        index);
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    throw new InvalidOperationException(
                        "The third-save UI acceptance cannot disable configuration while Product slot " +
                        index.ToString(CultureInfo.InvariantCulture) +
                        " contains committed item " +
                        itemId +
                        "; persistent owner recovery would no longer be a read-only UI check.");
                }
            }

            object currentConfig = RequireMemberForFixture(
                productRuntime,
                "config");
            object disabled = CloneMoreEquipmentSlotsConfig(
                currentConfig,
                enabled: false);
            InvokeMoreEquipmentSlotsConfigure(
                productRuntime,
                disabled,
                "QA 1.0 UI configuration disable");
            object disabledDiagnostics =
                InvokeNoArgResultForFixture(
                    productRuntime,
                    "GetDiagnosticsSnapshot");
            int disabledPatches = ReadIntForFixture(
                disabledDiagnostics,
                "PatchCount");
            int disabledRoots = ReadIntForFixture(
                disabledDiagnostics,
                "RootCount");
            int disabledClones = ReadIntForFixture(
                disabledDiagnostics,
                "CloneCount");
            if (disabledPatches != 0 ||
                disabledRoots != 0 ||
                disabledClones != 0)
            {
                throw new InvalidOperationException(
                    "Configuration disable did not remove all Product hooks/UI roots. patches=" +
                    disabledPatches.ToString(CultureInfo.InvariantCulture) +
                    "; roots=" +
                    disabledRoots.ToString(CultureInfo.InvariantCulture) +
                    "; clones=" +
                    disabledClones.ToString(CultureInfo.InvariantCulture) +
                    ".");
            }

            object enabled = CloneMoreEquipmentSlotsConfig(
                currentConfig,
                enabled: true);
            InvokeMoreEquipmentSlotsConfigure(
                productRuntime,
                enabled,
                "QA 1.0 UI configuration re-enable");
            RenderMoreEquipmentSlotsPassiveCount(
                nativeAccessoriesBar,
                1);
            StageMoreEquipmentSlotsMaximumNativeUi(
                productRuntime,
                currentState);
            object enabledDiagnostics =
                InvokeNoArgResultForFixture(
                    productRuntime,
                    "GetDiagnosticsSnapshot");
            int enabledPatches = ReadIntForFixture(
                enabledDiagnostics,
                "PatchCount");
            int enabledRoots = ReadIntForFixture(
                enabledDiagnostics,
                "RootCount");
            int enabledClones = ReadIntForFixture(
                enabledDiagnostics,
                "CloneCount");
            if (enabledPatches != 5 ||
                enabledRoots != 1 ||
                enabledClones != 3)
            {
                throw new InvalidOperationException(
                    "Configuration re-enable did not rebuild the exact Product generation. patches=" +
                    enabledPatches.ToString(CultureInfo.InvariantCulture) +
                    "; roots=" +
                    enabledRoots.ToString(CultureInfo.InvariantCulture) +
                    "; clones=" +
                    enabledClones.ToString(CultureInfo.InvariantCulture) +
                    ".");
            }
            return "disabled(patches=0,roots=0,clones=0); reenabled(patches=5,roots=1,clones=3); occupiedBefore=0";
        }

        private static object CloneMoreEquipmentSlotsConfig(
            object source,
            bool enabled)
        {
            object clone = Activator.CreateInstance(source.GetType()) ??
                throw new InvalidOperationException(
                    "MoreEquipmentSlots configuration could not be cloned for the QA-only in-memory cycle.");
            if (!WriteObjectMember(clone, "Enabled", enabled))
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots configuration exposes no writable Enabled member.");
            }
            object? verbose = ReadMember(source, "VerboseLogging");
            if (verbose != null)
                WriteObjectMember(clone, "VerboseLogging", verbose);
            return clone;
        }

        private static void InvokeMoreEquipmentSlotsConfigure(
            object productRuntime,
            object config,
            string reason)
        {
            RequireMethodForFixture(
                productRuntime.GetType(),
                "Configure",
                parameterCount: 2).Invoke(
                    productRuntime,
                    new[]
                    {
                        config,
                        reason
                    });
        }

        private void CaptureMoreEquipmentSlots100State(
            object productRuntime,
            object currentState,
            int width,
            int height,
            string screenshotName,
            string? canonicalCopyPath)
        {
            ForceMoreEquipmentSlotsCanvasLayout();
            string report = BuildMoreEquipmentSlots100Snapshot(
                productRuntime,
                currentState,
                width,
                height,
                MoreEquipmentSlotsMaximumBackpackCapacity,
                expectedPassiveCount:
                    MoreEquipmentSlotsMaximumOfficialPassiveCount,
                width.ToString(CultureInfo.InvariantCulture) +
                    "x" +
                    height.ToString(CultureInfo.InvariantCulture) +
                    "-dynamic-row");
            string screenshotPath = Path.Combine(
                moreEquipmentSlots100EvidenceDirectory,
                screenshotName);
            BeginMoreEquipmentSlots100Screenshot(
                screenshotPath,
                canonicalCopyPath,
                report);
        }

        private void BeginMoreEquipmentSlots100Screenshot(
            string screenshotPath,
            string? canonicalCopyPath,
            string report)
        {
            if (!string.IsNullOrWhiteSpace(
                    moreEquipmentSlots100PendingScreenshotPath))
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots 1.0 attempted to overlap two screenshot requests.");
            }
            if (File.Exists(screenshotPath))
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots 1.0 refused to overwrite pre-existing screenshot evidence: " +
                    screenshotPath +
                    ".");
            }
            if (!TryCaptureScreenshot(screenshotPath))
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots 1.0 could not request screenshot " +
                    Path.GetFileName(screenshotPath) +
                    ".");
            }

            moreEquipmentSlots100PendingScreenshotPath = screenshotPath;
            moreEquipmentSlots100PendingCanonicalCopyPath =
                canonicalCopyPath ?? string.Empty;
            moreEquipmentSlots100PendingScreenshotReport = report;
            moreEquipmentSlots100PendingScreenshotAt =
                DateTimeOffset.Now;
        }

        private bool TryCompleteMoreEquipmentSlots100Screenshot(
            out string pendingReason)
        {
            pendingReason = string.Empty;
            if (string.IsNullOrWhiteSpace(
                    moreEquipmentSlots100PendingScreenshotPath))
            {
                return true;
            }

            if (!IsNonEmptyFile(
                    moreEquipmentSlots100PendingScreenshotPath))
            {
                TimeSpan elapsed =
                    DateTimeOffset.Now -
                    moreEquipmentSlots100PendingScreenshotAt;
                if (elapsed > TimeSpan.FromSeconds(10))
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots 1.0 screenshot request did not produce a non-empty file within 10 seconds: " +
                        Path.GetFileName(
                            moreEquipmentSlots100PendingScreenshotPath) +
                        ".");
                }
                pendingReason =
                    "Waiting for Unity async screenshot flush: " +
                    Path.GetFileName(
                        moreEquipmentSlots100PendingScreenshotPath) +
                    ".";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(
                    moreEquipmentSlots100PendingCanonicalCopyPath))
            {
                File.Copy(
                    moreEquipmentSlots100PendingScreenshotPath,
                    moreEquipmentSlots100PendingCanonicalCopyPath,
                    overwrite: true);
            }
            moreEquipmentSlots100Reports.Add(
                moreEquipmentSlots100PendingScreenshotReport +
                Environment.NewLine +
                "Screenshot=" +
                moreEquipmentSlots100PendingScreenshotPath);
            moreEquipmentSlots100PendingScreenshotPath = string.Empty;
            moreEquipmentSlots100PendingCanonicalCopyPath =
                string.Empty;
            moreEquipmentSlots100PendingScreenshotReport = string.Empty;
            moreEquipmentSlots100PendingScreenshotAt = default;
            WriteMoreEquipmentSlots100Summary("capturing");
            return true;
        }

        private string BuildMoreEquipmentSlots100Snapshot(
            object productRuntime,
            object currentState,
            int expectedWidth,
            int expectedHeight,
            int expectedBackpackCount,
            int expectedPassiveCount,
            string label)
        {
            Type screen = RequireMoreEquipmentSlotsRuntimeType(
                "UnityEngine.Screen");
            int screenWidth = ReadStaticInt(screen, "width");
            int screenHeight = ReadStaticInt(screen, "height");
            if (screenWidth != expectedWidth ||
                screenHeight != expectedHeight)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots snapshot resolution drifted. expected=" +
                    expectedWidth.ToString(CultureInfo.InvariantCulture) +
                    "x" +
                    expectedHeight.ToString(CultureInfo.InvariantCulture) +
                    "; actual=" +
                    screenWidth.ToString(CultureInfo.InvariantCulture) +
                    "x" +
                    screenHeight.ToString(CultureInfo.InvariantCulture) +
                    ".");
            }

            object diagnostics = InvokeNoArgResultForFixture(
                productRuntime,
                "GetDiagnosticsSnapshot");
            int patchCount = ReadIntForFixture(
                diagnostics,
                "PatchCount");
            int cloneCount = ReadIntForFixture(
                diagnostics,
                "CloneCount");
            int rootCount = ReadIntForFixture(
                diagnostics,
                "RootCount");
            bool uiVisible = ReadBoolForFixture(
                diagnostics,
                "UiVisible");
            bool layoutBlocked = ReadBoolForFixture(
                diagnostics,
                "UiLayoutBlocked");
            if (patchCount != 5 ||
                cloneCount != 3 ||
                rootCount != 1 ||
                !uiVisible ||
                layoutBlocked)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots diagnostics did not expose one visible dynamic Product row. patches=" +
                    patchCount.ToString(CultureInfo.InvariantCulture) +
                    "; clones=" +
                    cloneCount.ToString(CultureInfo.InvariantCulture) +
                    "; roots=" +
                    rootCount.ToString(CultureInfo.InvariantCulture) +
                    "; visible=" +
                    uiVisible +
                    "; blocked=" +
                    layoutBlocked +
                    ".");
            }

            object session = RequireMoreEquipmentSlotsSession(
                productRuntime);
            int passiveCount = ReadIntForFixture(
                session,
                "OfficialPassiveCount");
            if (passiveCount != expectedPassiveCount)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots native passive-slot fixture drifted. expected=" +
                    expectedPassiveCount.ToString(CultureInfo.InvariantCulture) +
                    "; actual=" +
                    passiveCount.ToString(CultureInfo.InvariantCulture) +
                    ".");
            }
            object accessoriesBar = RequireMemberForFixture(
                session,
                "AccessoriesBar");
            object productRow = RequireMemberForFixture(
                session,
                "ProductRowRoot");
            object[] productSlots = ReadMoreEquipmentSlotsProductSlots(
                productRuntime);
            if (productSlots.Length != 3)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots UI generation did not retain exactly three Product slots.");
            }

            object panel = RequireMemberForFixture(
                currentState,
                "panel");
            object backpackPanel = RequireMemberForFixture(
                panel,
                "backpackPanel");
            object[] backpackSelectables = EnumerateObjects(
                ReadMember(backpackPanel, "allSelectablesArray"))
                .ToArray();
            if (backpackSelectables.Length != expectedBackpackCount)
            {
                throw new InvalidOperationException(
                    "The equipment UI did not expose the requested backpack layout. expected=" +
                    expectedBackpackCount.ToString(CultureInfo.InvariantCulture) +
                    "; actual=" +
                    backpackSelectables.Length.ToString(CultureInfo.InvariantCulture) +
                    ".");
            }
            object? closeButton = ReadMember(backpackPanel, "closeButton");
            object dronePanel = RequireMemberForFixture(
                session,
                "NativeDronePanel");
            object[] droneSelectables = GetComponentsInChildrenForFixture(
                dronePanel,
                RequireMoreEquipmentSlotsRuntimeType(
                    "UnityEngine.UI.Selectable"));
            object[] droneGraphics = GetComponentsInChildrenForFixture(
                dronePanel,
                RequireMoreEquipmentSlotsRuntimeType(
                    "UnityEngine.UI.Graphic"));
            if (droneSelectables.Length == 0 ||
                droneGraphics.Length == 0)
            {
                throw new InvalidOperationException(
                    "The native drone panel exposed no complete interaction surface for non-interference acceptance.");
            }

            var officialSlots = new List<object>();
            AddIfPresent(
                officialSlots,
                ReadMember(accessoriesBar, "hatItem"));
            AddIfPresent(
                officialSlots,
                ReadMember(accessoriesBar, "positiveItem"));
            MethodInfo getPassive = RequireMethodForFixture(
                accessoriesBar.GetType(),
                "GetPassiveSlotByIndex",
                parameterCount: 1);
            for (int index = 0; index < passiveCount; index++)
            {
                AddIfPresent(
                    officialSlots,
                    getPassive.Invoke(
                        accessoriesBar,
                        new object[]
                        {
                            index
                        }));
            }
            if (officialSlots.Count != 2 + passiveCount)
            {
                throw new InvalidOperationException(
                    "The native hat/active/passive surface was incomplete.");
            }

            int selectableCount = CountObjects(
                ReadMember(accessoriesBar, "allSelectablesArray"));
            int expectedSelectableCount =
                2 + passiveCount + 3;
            if (selectableCount != expectedSelectableCount)
            {
                throw new InvalidOperationException(
                    "AccessoriesBar navigation projection drifted. expected=" +
                    expectedSelectableCount.ToString(CultureInfo.InvariantCulture) +
                    "; actual=" +
                    selectableCount.ToString(CultureInfo.InvariantCulture) +
                    ".");
            }

            MoreEquipmentUiBounds rowBounds =
                ReadMoreEquipmentSlotsWorldBounds(productRow);
            MoreEquipmentUiBounds droneBounds =
                ReadMoreEquipmentSlotsWorldBounds(dronePanel);
            RequireMoreEquipmentSlotsLocalSize(
                productRow,
                360f,
                112f,
                "dynamic Product row");
            foreach (object productSlot in productSlots)
            {
                RequireMoreEquipmentSlotsLocalSize(
                    productSlot,
                    112f,
                    112f,
                    "Product slot");
            }
            if (!ReadMoreEquipmentSlotsIgnoreLayout(productRow))
            {
                throw new InvalidOperationException(
                    "The dynamic Product row must retain LayoutElement.ignoreLayout=true.");
            }
            if (!ReadActiveForFixture(productRow))
            {
                throw new InvalidOperationException(
                    "The dynamic Product row was not visible during equipment-panel acceptance.");
            }

            var forbidden = new List<object>();
            forbidden.AddRange(officialSlots);
            forbidden.AddRange(backpackSelectables);
            forbidden.AddRange(droneSelectables);
            AddIfPresent(forbidden, closeButton);
            RequireMoreEquipmentSlotsViewportContainment(
                officialSlots
                    .Concat(backpackSelectables)
                    .Concat(droneSelectables)
                    .Concat(productSlots)
                    .Concat(new[] { productRow, dronePanel })
                    .Concat(closeButton == null
                        ? Array.Empty<object>()
                        : new[] { closeButton }),
                screenWidth,
                screenHeight);
            foreach (object productSlot in productSlots)
            {
                RequireNoMoreEquipmentSlotsIntersection(
                    productSlot,
                    forbidden,
                    "dynamic Product slot");
            }

            MoreEquipmentUiBounds previous =
                ReadMoreEquipmentSlotsWorldBounds(
                    officialSlots[officialSlots.Count - 1]);
            foreach (object productSlot in productSlots)
            {
                MoreEquipmentUiBounds current =
                    ReadMoreEquipmentSlotsWorldBounds(productSlot);
                if (current.MinX <= previous.MaxX)
                {
                    throw new InvalidOperationException(
                        "The Product row did not follow the last active official slot in strict horizontal order.");
                }
                previous = current;
            }

            var report = new StringBuilder();
            report.AppendLine("State=" + label);
            report.AppendLine(
                "Screen=" +
                screenWidth.ToString(CultureInfo.InvariantCulture) +
                "x" +
                    screenHeight.ToString(CultureInfo.InvariantCulture));
            report.AppendLine(
                "DynamicRowVisible=true; Patches=" +
                patchCount.ToString(CultureInfo.InvariantCulture) +
                "; ProductSlots=" +
                cloneCount.ToString(CultureInfo.InvariantCulture) +
                "; OfficialPassives=" +
                passiveCount.ToString(CultureInfo.InvariantCulture) +
                "; BackpackSlots=" +
                backpackSelectables.Length.ToString(CultureInfo.InvariantCulture));
            report.AppendLine(
                "AccessoriesSelectables=" +
                selectableCount.ToString(CultureInfo.InvariantCulture) +
                "; NativeDroneSelectables=" +
                droneSelectables.Length.ToString(CultureInfo.InvariantCulture) +
                "; NativeDroneGraphics=" +
                droneGraphics.Length.ToString(CultureInfo.InvariantCulture));
            report.AppendLine(
                "ProductRow=" +
                rowBounds.Format() +
                "; Drone=" +
                droneBounds.Format());
            for (int index = 0;
                 index < productSlots.Length;
                 index++)
            {
                report.AppendLine(
                    "ProductSlot" +
                    index.ToString(CultureInfo.InvariantCulture) +
                    "=" +
                    ReadMoreEquipmentSlotsWorldBounds(
                        productSlots[index]).Format());
            }
            report.AppendLine(
                "Intersections=none; NativeTailOrder=exact; NavigationProjection=native-plus-three; ViewportContainment=exact; IgnoreLayout=true; PerFramePolling=none");
            report.AppendLine("SelectableRects:");
            foreach (object selectable in officialSlots
                .Concat(backpackSelectables)
                .Concat(droneSelectables)
                .Concat(productSlots)
                .Distinct())
            {
                report.AppendLine(
                    "- " +
                    DescribeMoreEquipmentSlotsObject(selectable) +
                    " " +
                    ReadMoreEquipmentSlotsWorldBounds(
                        selectable).Format());
            }
            return report.ToString().TrimEnd();
        }

        private void CaptureMoreEquipmentSlotsReuseIdentity(
            object productRuntime)
        {
            object session = RequireMoreEquipmentSlotsSession(
                productRuntime);
            moreEquipmentSlots100ReuseSession = session;
            moreEquipmentSlots100ReuseRow =
                RequireMemberForFixture(session, "ProductRowRoot");
            moreEquipmentSlots100ReuseSlots =
                ReadMoreEquipmentSlotsProductSlots(productRuntime);
        }

        private bool TryObserveMoreEquipmentSlotsStableReopenSurface(
            object productRuntime,
            int screenWidth,
            int screenHeight,
            out string pendingReason)
        {
            object session = RequireMoreEquipmentSlotsSession(
                productRuntime);
            object row = RequireMemberForFixture(
                session,
                "ProductRowRoot");
            object drone = RequireMemberForFixture(
                session,
                "NativeDronePanel");
            MoreEquipmentUiBounds rowBounds =
                ReadMoreEquipmentSlotsWorldBounds(row);
            MoreEquipmentUiBounds droneBounds =
                ReadMoreEquipmentSlotsWorldBounds(drone);
            if (!ReadActiveForFixture(row) ||
                !IsMoreEquipmentSlotsInsideViewport(
                    rowBounds,
                    screenWidth,
                    screenHeight) ||
                !IsMoreEquipmentSlotsInsideViewport(
                    droneBounds,
                    screenWidth,
                    screenHeight))
            {
                moreEquipmentSlots100ReopenStableFrames = 0;
                moreEquipmentSlots100LastReopenRowBounds = null;
                moreEquipmentSlots100LastReopenDroneBounds = null;
                pendingReason =
                    "Waiting for the reopened equipment panel to enter the 1024x768 viewport. row=" +
                    rowBounds.Format() +
                    "; drone=" +
                    droneBounds.Format() +
                    ".";
                return false;
            }

            bool stable =
                moreEquipmentSlots100LastReopenRowBounds.HasValue &&
                moreEquipmentSlots100LastReopenDroneBounds.HasValue &&
                AreMoreEquipmentSlotsBoundsStable(
                    moreEquipmentSlots100LastReopenRowBounds.Value,
                    rowBounds) &&
                AreMoreEquipmentSlotsBoundsStable(
                    moreEquipmentSlots100LastReopenDroneBounds.Value,
                    droneBounds);
            moreEquipmentSlots100ReopenStableFrames = stable
                ? moreEquipmentSlots100ReopenStableFrames + 1
                : 1;
            moreEquipmentSlots100LastReopenRowBounds = rowBounds;
            moreEquipmentSlots100LastReopenDroneBounds = droneBounds;
            if (moreEquipmentSlots100ReopenStableFrames < 5)
            {
                pendingReason =
                    "Waiting for five consecutive stable in-viewport frames after reopening EquipmentBarUiState. stableFrames=" +
                    moreEquipmentSlots100ReopenStableFrames.ToString(
                        CultureInfo.InvariantCulture) +
                    "; row=" +
                    rowBounds.Format() +
                    "; drone=" +
                    droneBounds.Format() +
                    ".";
                return false;
            }

            pendingReason = string.Empty;
            return true;
        }

        private static bool AreMoreEquipmentSlotsBoundsStable(
            MoreEquipmentUiBounds before,
            MoreEquipmentUiBounds after) =>
            Math.Abs(before.MinX - after.MinX) <= 0.1f &&
            Math.Abs(before.MinY - after.MinY) <= 0.1f &&
            Math.Abs(before.MaxX - after.MaxX) <= 0.1f &&
            Math.Abs(before.MaxY - after.MaxY) <= 0.1f;

        private void RequireMoreEquipmentSlotsReuseIdentity(
            object productRuntime)
        {
            object session = RequireMoreEquipmentSlotsSession(
                productRuntime);
            object row = RequireMemberForFixture(
                session,
                "ProductRowRoot");
            object[] slots = ReadMoreEquipmentSlotsProductSlots(
                productRuntime);
            if (!ReferenceEquals(
                    session,
                    moreEquipmentSlots100ReuseSession) ||
                !ReferenceEquals(
                    row,
                    moreEquipmentSlots100ReuseRow) ||
                slots.Length !=
                    moreEquipmentSlots100ReuseSlots.Length)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots recreated its Product UI generation across panel close/reopen.");
            }
            for (int index = 0; index < slots.Length; index++)
            {
                if (!ReferenceEquals(
                        slots[index],
                        moreEquipmentSlots100ReuseSlots[index]))
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots recreated Product slot " +
                        index.ToString(CultureInfo.InvariantCulture) +
                        " across panel close/reopen.");
                }
            }
        }

        private void RequireMoreEquipmentSlotsClosedButRetained(
            object productRuntime,
            string boundary)
        {
            object diagnostics = InvokeNoArgResultForFixture(
                productRuntime,
                "GetDiagnosticsSnapshot");
            int roots = ReadIntForFixture(
                diagnostics,
                "RootCount");
            int clones = ReadIntForFixture(
                diagnostics,
                "CloneCount");
            bool visible = ReadBoolForFixture(
                diagnostics,
                "UiVisible");
            if (roots != 1 || clones != 3 || visible)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots panel " +
                    boundary +
                    " did not hide and retain one reusable Product row. roots=" +
                    roots.ToString(CultureInfo.InvariantCulture) +
                    "; clones=" +
                    clones.ToString(CultureInfo.InvariantCulture) +
                    "; visible=" +
                    visible +
                    ".");
            }
            RequireMoreEquipmentSlotsReuseIdentity(productRuntime);
        }

        private void WriteMoreEquipmentSlots100Summary(string status)
        {
            if (string.IsNullOrWhiteSpace(
                    moreEquipmentSlots100SummaryPath))
            {
                return;
            }
            var text = new StringBuilder();
            text.AppendLine(
                "Captured=" +
                DateTimeOffset.Now.ToString(
                    "o",
                    CultureInfo.InvariantCulture));
            text.AppendLine("Status=" + status);
            text.AppendLine("UiOwner=product-1.0-dynamic-row");
            text.AppendLine("EvidenceOwner=qa");
            text.AppendLine("InputOwner=runner-real-B/Escape");
            text.AppendLine(
                "OriginalResolution=" +
                moreEquipmentSlots100OriginalWidth.ToString(
                    CultureInfo.InvariantCulture) +
                "x" +
                moreEquipmentSlots100OriginalHeight.ToString(
                    CultureInfo.InvariantCulture) +
                "; fullscreen=" +
                moreEquipmentSlots100OriginalFullscreen);
            text.AppendLine(
                "NativeUiFixture=passive " +
                moreEquipmentSlots100OriginalPassiveCount.ToString(
                    CultureInfo.InvariantCulture) +
                "->1->2->3->4->5->" +
                moreEquipmentSlots100OriginalPassiveCount.ToString(
                    CultureInfo.InvariantCulture) +
                "; backpack " +
                moreEquipmentSlots100OriginalBackpackCount.ToString(
                    CultureInfo.InvariantCulture) +
                "->40->" +
                moreEquipmentSlots100OriginalBackpackCount.ToString(
                    CultureInfo.InvariantCulture));
            text.AppendLine(
                "ConfigurationCycle=" +
                moreEquipmentSlots100ConfigReceipt);
            text.AppendLine(
                "Screenshots=equipment-slots-1920x1080-dynamic-row.png|equipment-slots-1024x768-dynamic-row.png|equipment-slots-1024x768-reopen.png");
            text.AppendLine();
            text.AppendLine(
                string.Join(
                    Environment.NewLine +
                    Environment.NewLine,
                    moreEquipmentSlots100Reports));
            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    moreEquipmentSlots100SummaryPath) ?? ".");
            File.WriteAllText(
                moreEquipmentSlots100SummaryPath,
                text.ToString());
        }

        private static object RequireMoreEquipmentSlotsSession(
            object productRuntime) =>
            ReadMember(productRuntime, "uiSession") ??
            throw new InvalidOperationException(
                "MoreEquipmentSlots has not created its singleton UI session.");

        private static object[] ReadMoreEquipmentSlotsProductSlots(
            object productRuntime)
        {
            object? uiLeases = ReadMember(
                productRuntime,
                "uiLeases");
            return EnumerateObjects(uiLeases)
                .Select(lease => ReadMember(lease, "Slot"))
                .Where(slot => slot != null)
                .Cast<object>()
                .ToArray();
        }

        private static object RequireMemberForFixture(
            object instance,
            string name) =>
            ReadMember(instance, name) ??
            throw new InvalidOperationException(
                instance.GetType().FullName +
                " exposes no " +
                name +
                " member.");

        private static MethodInfo RequireMethodForFixture(
            Type type,
            string name,
            int parameterCount)
        {
            for (Type? current = type;
                 current != null;
                 current = current.BaseType)
            {
                MethodInfo? method = current.GetMethods(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.Static)
                    .FirstOrDefault(candidate =>
                        candidate.Name == name &&
                        candidate.GetParameters().Length ==
                            parameterCount);
                if (method != null)
                    return method;
            }
            throw new MissingMethodException(
                type.FullName,
                name);
        }

        private static object InvokeNoArgResultForFixture(
            object instance,
            string methodName) =>
            RequireMethodForFixture(
                instance.GetType(),
                methodName,
                parameterCount: 0).Invoke(
                    instance,
                    null) ??
            throw new InvalidOperationException(
                methodName +
                " returned null.");

        private static void InvokeNoArgForFixture(
            object instance,
            string methodName) =>
            RequireMethodForFixture(
                instance.GetType(),
                methodName,
                parameterCount: 0).Invoke(
                    instance,
                    null);

        private static string InvokeStringForFixture(
            object instance,
            string methodName,
            object argument) =>
            RequireMethodForFixture(
                instance.GetType(),
                methodName,
                parameterCount: 1).Invoke(
                    instance,
                    new[] { argument }) as string ?? string.Empty;

        private static int ReadIntForFixture(
            object instance,
            string name) =>
            Convert.ToInt32(
                ReadMember(instance, name) ?? -1,
                CultureInfo.InvariantCulture);

        private static bool ReadBoolForFixture(
            object instance,
            string name) =>
            Convert.ToBoolean(
                ReadMember(instance, name) ?? false,
                CultureInfo.InvariantCulture);

        private static int ReadStaticInt(Type type, string name) =>
            Convert.ToInt32(
                type.GetProperty(
                    name,
                    BindingFlags.Public |
                    BindingFlags.Static)?.GetValue(null) ?? 0,
                CultureInfo.InvariantCulture);

        private static bool ReadStaticBool(Type type, string name) =>
            Convert.ToBoolean(
                type.GetProperty(
                    name,
                    BindingFlags.Public |
                    BindingFlags.Static)?.GetValue(null) ?? false,
                CultureInfo.InvariantCulture);

        private static Type RequireMoreEquipmentSlotsRuntimeType(
            string fullName)
        {
            foreach (string assembly in new[]
            {
                "UnityEngine.CoreModule",
                "UnityEngine.UI",
                "UnityEngine"
            })
            {
                Type? type = Type.GetType(
                    fullName + ", " + assembly,
                    throwOnError: false);
                if (type != null)
                    return type;
            }
            throw new TypeLoadException(fullName);
        }

        private static void ForceMoreEquipmentSlotsCanvasLayout()
        {
            Type canvas = RequireMoreEquipmentSlotsRuntimeType(
                "UnityEngine.Canvas");
            canvas.GetMethod(
                "ForceUpdateCanvases",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null)?.Invoke(null, null);
        }

        private static object[] GetComponentsInChildrenForFixture(
            object target,
            Type componentType)
        {
            object authority = ReadMember(target, "gameObject") ??
                target;
            MethodInfo method = RequireMethodForFixture(
                authority.GetType(),
                "GetComponentsInChildren",
                parameterCount: 2);
            object? value = method.Invoke(
                authority,
                new object[]
                {
                    componentType,
                    true
                });
            return EnumerateObjects(value).ToArray();
        }

        private static void AddIfPresent(
            ICollection<object> targets,
            object? value)
        {
            if (value != null)
                targets.Add(value);
        }

        private static MoreEquipmentUiBounds
            ReadMoreEquipmentSlotsWorldBounds(object target)
        {
            object rectTransform =
                ReadMember(target, "rectTransform") ??
                ReadMember(target, "transform") ??
                ReadMember(
                    ReadMember(target, "gameObject") ?? target,
                    "transform") ??
                throw new InvalidOperationException(
                    DescribeMoreEquipmentSlotsObject(target) +
                    " exposes no RectTransform.");
            Type vector3 = RequireMoreEquipmentSlotsRuntimeType(
                "UnityEngine.Vector3");
            Array corners = Array.CreateInstance(vector3, 4);
            RequireMethodForFixture(
                rectTransform.GetType(),
                "GetWorldCorners",
                parameterCount: 1).Invoke(
                    rectTransform,
                    new object[] { corners });
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            foreach (object corner in corners)
            {
                float x = Convert.ToSingle(
                    ReadMember(corner, "x") ?? 0f,
                    CultureInfo.InvariantCulture);
                float y = Convert.ToSingle(
                    ReadMember(corner, "y") ?? 0f,
                    CultureInfo.InvariantCulture);
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
            return new MoreEquipmentUiBounds(
                minX,
                minY,
                maxX,
                maxY);
        }

        private static void RequireMoreEquipmentSlotsLocalSize(
            object target,
            float expectedWidth,
            float expectedHeight,
            string label)
        {
            object rectTransform =
                ReadMember(target, "rectTransform") ??
                ReadMember(target, "transform") ??
                throw new InvalidOperationException(
                    label +
                    " exposes no RectTransform.");
            object rect = ReadMember(rectTransform, "rect") ??
                throw new InvalidOperationException(
                    label +
                    " exposes no local rect.");
            float width = Convert.ToSingle(
                ReadMember(rect, "width") ?? 0f,
                CultureInfo.InvariantCulture);
            float height = Convert.ToSingle(
                ReadMember(rect, "height") ?? 0f,
                CultureInfo.InvariantCulture);
            if (Math.Abs(width - expectedWidth) > 0.1f ||
                Math.Abs(height - expectedHeight) > 0.1f)
            {
                throw new InvalidOperationException(
                    label +
                    " changed native 112x112 size. actual=" +
                    width.ToString(
                        "0.###",
                        CultureInfo.InvariantCulture) +
                    "x" +
                    height.ToString(
                        "0.###",
                        CultureInfo.InvariantCulture) +
                    ".");
            }
        }

        private static bool ReadMoreEquipmentSlotsIgnoreLayout(
            object target)
        {
            object gameObject = ReadMember(target, "gameObject") ??
                target;
            Type layoutElement = RequireMoreEquipmentSlotsRuntimeType(
                "UnityEngine.UI.LayoutElement");
            MethodInfo getComponent = RequireMethodForFixture(
                gameObject.GetType(),
                "GetComponent",
                parameterCount: 1);
            object? component = getComponent.Invoke(
                gameObject,
                new object[] { layoutElement });
            return component != null &&
                ReadBoolForFixture(component, "ignoreLayout");
        }

        private static bool ReadActiveForFixture(object target)
        {
            object gameObject = ReadMember(target, "gameObject") ??
                target;
            object? active = ReadMember(
                gameObject,
                "activeInHierarchy") ??
                ReadMember(gameObject, "activeSelf");
            return active != null &&
                Convert.ToBoolean(
                    active,
                    CultureInfo.InvariantCulture);
        }

        private static void RequireNoMoreEquipmentSlotsIntersection(
            object product,
            IEnumerable<object> forbidden,
            string label)
        {
            MoreEquipmentUiBounds productBounds =
                ReadMoreEquipmentSlotsWorldBounds(product);
            foreach (object native in forbidden
                .Where(item => item != null)
                .Distinct())
            {
                if (!ReadActiveForFixture(native))
                    continue;
                MoreEquipmentUiBounds nativeBounds =
                    ReadMoreEquipmentSlotsWorldBounds(native);
                if (IntersectsBounds(
                        productBounds,
                        nativeBounds,
                        0.5f))
                {
                    throw new InvalidOperationException(
                        label +
                        " intersects non-drone native UI " +
                        DescribeMoreEquipmentSlotsObject(native) +
                        ". product=" +
                        productBounds.Format() +
                        "; native=" +
                        nativeBounds.Format() +
                        ".");
                }
            }
        }

        private static bool IntersectsBounds(
            MoreEquipmentUiBounds left,
            MoreEquipmentUiBounds right,
            float epsilon) =>
            left.MinX < right.MaxX - epsilon &&
            left.MaxX > right.MinX + epsilon &&
            left.MinY < right.MaxY - epsilon &&
            left.MaxY > right.MinY + epsilon;

        private static void RequireMoreEquipmentSlotsViewportContainment(
            IEnumerable<object> targets,
            int screenWidth,
            int screenHeight)
        {
            foreach (object target in targets
                .Where(item => item != null)
                .Distinct())
            {
                MoreEquipmentUiBounds bounds =
                    ReadMoreEquipmentSlotsWorldBounds(target);
                if (!IsMoreEquipmentSlotsInsideViewport(
                        bounds,
                        screenWidth,
                        screenHeight))
                {
                    throw new InvalidOperationException(
                        DescribeMoreEquipmentSlotsObject(target) +
                        " escaped the current viewport. screen=" +
                        screenWidth.ToString(
                            CultureInfo.InvariantCulture) +
                        "x" +
                        screenHeight.ToString(
                            CultureInfo.InvariantCulture) +
                        "; bounds=" +
                        bounds.Format() +
                        ".");
                }
            }
        }

        private static bool IsMoreEquipmentSlotsInsideViewport(
            MoreEquipmentUiBounds bounds,
            int screenWidth,
            int screenHeight) =>
            bounds.MinX >= -1f &&
            bounds.MinY >= -1f &&
            bounds.MaxX <= screenWidth + 1f &&
            bounds.MaxY <= screenHeight + 1f;

        private static string DescribeMoreEquipmentSlotsObject(
            object target)
        {
            object gameObject = ReadMember(target, "gameObject") ??
                target;
            return ReadMember(gameObject, "name") as string ??
                target.GetType().FullName ??
                target.GetType().Name;
        }

        private static bool IsNonEmptyFile(string path) =>
            File.Exists(path) &&
            new FileInfo(path).Length > 0;

        private readonly struct MoreEquipmentUiBounds
        {
            internal MoreEquipmentUiBounds(
                float minX,
                float minY,
                float maxX,
                float maxY)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }

            internal float MinX { get; }

            internal float MinY { get; }

            internal float MaxX { get; }

            internal float MaxY { get; }

            internal string Format() =>
                "[" +
                MinX.ToString("0.###", CultureInfo.InvariantCulture) +
                "," +
                MinY.ToString("0.###", CultureInfo.InvariantCulture) +
                " -> " +
                MaxX.ToString("0.###", CultureInfo.InvariantCulture) +
                "," +
                MaxY.ToString("0.###", CultureInfo.InvariantCulture) +
                "]";
        }
    }
}
