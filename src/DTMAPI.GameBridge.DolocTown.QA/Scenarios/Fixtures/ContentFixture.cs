using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private const double MinePowerThresholdForFixture = 10d;
        private string newContentOilItemMetadataSummary = string.Empty;
        private string newContentOilCoalDropSummary = string.Empty;
        private string newContentMineOfficialJsonSummary = string.Empty;
        private object? newContentPendingMine;
        private object? newContentPendingMineSecond;
        private object? newContentPendingMineReplacement;
        private object? newContentPendingMineProductRuntime;
        private object? newContentPendingMineRoom;
        private bool newContentPendingMineOnly;
        private int newContentPendingMineStage;
        private int newContentPendingMineFirstIndex;
        private int newContentPendingMineSecondIndex;
        private int newContentPendingMineReplacementIndex;
        private int newContentPendingMineStableDue;
        private int newContentPendingMineExpectedFirstObserved;
        private bool newContentPendingMineFirstIndexMutated;
        private bool newContentPendingMineReplacementIndexMutated;
        private bool newContentPendingMineSecondRemoved;
        private string newContentPendingMineCreateSummary = string.Empty;
        private string newContentPendingMineScaleSummary = string.Empty;
        private readonly List<string> newContentPendingMineMatrixEvidence =
            new List<string>();
        private DateTimeOffset newContentPendingMineDeadline = DateTimeOffset.MinValue;

        private string EnsureNewContentEvidenceDir()
        {
            if (!string.IsNullOrWhiteSpace(newContentEvidenceDir))
                return newContentEvidenceDir!;

            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            newContentEvidenceDir = Path.Combine(runtime.Paths.EvidencePath, "NEWCONTENT-025", timestamp);
            Directory.CreateDirectory(newContentEvidenceDir);
            return newContentEvidenceDir;
        }

        private string CaptureMinePlacementEvidenceForFixture(object mine, string createSummary)
        {
            try
            {
                string evidenceDir = EnsureNewContentEvidenceDir();
                string screenshotPath = Path.Combine(evidenceDir, "mine-placed-dtmapi-mine.png");
                bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
                string equipmentSummary = DescribeEquipmentForFixture(mine);
                string summary = "playerItem=dtmapi_mine ItemEquipment, placed=" + equipmentSummary + ", create={" + createSummary + "}, screenshot=" + (screenshotRequested ? screenshotPath : "unavailable");
                File.WriteAllText(Path.Combine(evidenceDir, "mine-placement-summary.txt"),
                    "Captured=" + DateTimeOffset.Now.ToString("o", System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                    "ScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                    "Screenshot=" + screenshotPath + Environment.NewLine +
                    "Create=" + createSummary + Environment.NewLine +
                    "Equipment=" + equipmentSummary + Environment.NewLine);
                runtime.RuntimeMonitor.Log("Mine placement evidence " + (screenshotRequested ? "OK" : "unavailable") + " " + summary + ".");
                runtime.SetHookStatus("Smoke.NewContentMinePlacement", screenshotRequested ? "verified" : "pending", "ItemEquipment dtmapi_mine + IEquipmentHost.CreateEquipment + UnityEngine.ScreenCapture", summary);
                return summary;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Mine placement evidence capture failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.NewContentMinePlacement", "failed", "ItemEquipment dtmapi_mine + IEquipmentHost.CreateEquipment", ex.GetType().Name + ": " + ex.Message);
                return "failed:" + ex.GetType().Name + ":" + ex.Message;
            }
        }

        private string CaptureEquipmentHatTableForFixture()
        {
            try
            {
                EquipmentHatTableDiagnostic diagnostic = BuildEquipmentHatTableDiagnosticForFixture();
                string evidenceDir = GetEvidenceDirectory("g5/equipment-slots");
                string jsonPath = Path.Combine(evidenceDir, "equipment-hat-table.json");
                string csvPath = Path.Combine(evidenceDir, "equipment-hat-table.csv");

                using (FileStream stream = File.Create(jsonPath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(EquipmentHatTableDiagnostic));
                    serializer.WriteObject(stream, diagnostic);
                }

                File.WriteAllText(csvPath, BuildEquipmentHatTableCsv(diagnostic));

                string summary = "hats=" + diagnostic.HatCount.ToString(CultureInfo.InvariantCulture) +
                    ", itemHatRows=" + diagnostic.ItemHatCount.ToString(CultureInfo.InvariantCulture) +
                    ", hatsWithoutItems=" + diagnostic.HatsWithoutItemRows.ToString(CultureInfo.InvariantCulture) +
                    ", itemRowsWithoutHatInfo=" + diagnostic.ItemRowsWithoutHatInfo.ToString(CultureInfo.InvariantCulture) +
                    ", json=" + jsonPath +
                    ", csv=" + csvPath;
                string status = diagnostic.HatCount > 0 ? "verified" : "failed";
                runtime.RuntimeMonitor.Log("Smoke equipment hat table diagnostic " + status + " " + summary + ".");
                runtime.SetHookStatus(
                    "Smoke.EquipmentHatTable",
                    status,
                    "DolocConfig.Tables.TbHat/TbItem read-only enumeration",
                    summary);
                return summary;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Equipment hat table diagnostic failed.", ex.ToString());
                runtime.SetHookStatus(
                    "Smoke.EquipmentHatTable",
                    "failed",
                    "DolocConfig.Tables.TbHat/TbItem read-only enumeration",
                    ex.GetType().Name + ": " + ex.Message);
                return "failed:" + ex.GetType().Name + ":" + ex.Message;
            }
        }

        private EquipmentHatTableDiagnostic BuildEquipmentHatTableDiagnosticForFixture()
        {
            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocConfig = patcher.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig == null ? null : ReadStaticMember(dolocConfig, "Tables");
            object? tbHat = tables == null ? null : ReadMember(tables, "TbHat");
            object? tbItem = tables == null ? null : ReadMember(tables, "TbItem");
            object? hatList = tbHat == null ? null : ReadMember(tbHat, "DataList");
            object? itemList = tbItem == null ? null : ReadMember(tbItem, "DataList");

            var itemRows = new List<EquipmentHatItemDiagnosticRow>();
            var itemRowsByHatId = new Dictionary<string, List<EquipmentHatItemDiagnosticRow>>(StringComparer.OrdinalIgnoreCase);
            foreach (object proto in EnumerateObjects(itemList))
            {
                string itemId = ReadAnyStringMember(proto, string.Empty, "Id", "id");
                object? function = ReadAnyMember(proto, "Function", "function");
                string functionType = function?.GetType().Name ?? "none";
                if (functionType.IndexOf("ItemFunctionHat", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                string hatId = function == null ? string.Empty : ReadAnyStringMember(function, string.Empty, "HatId", "hatId", "hat_id");
                object? hatIdRef = function == null ? null : ReadAnyMember(function, "HatId_Ref", "hatId_Ref", "HatIdRef");
                var itemRow = new EquipmentHatItemDiagnosticRow
                {
                    ItemId = itemId,
                    FunctionType = functionType,
                    HatId = hatId,
                    HatIdRefResolved = hatIdRef != null
                };
                itemRows.Add(itemRow);

                if (string.IsNullOrWhiteSpace(hatId))
                    continue;
                if (!itemRowsByHatId.TryGetValue(hatId, out List<EquipmentHatItemDiagnosticRow>? rows))
                {
                    rows = new List<EquipmentHatItemDiagnosticRow>();
                    itemRowsByHatId[hatId] = rows;
                }
                rows.Add(itemRow);
            }

            var hatRows = new List<EquipmentHatDiagnosticRow>();
            foreach (object hatInfo in EnumerateObjects(hatList))
            {
                string hatId = ReadAnyStringMember(hatInfo, string.Empty, "Id", "id");
                string skill = ReadAnyStringMember(hatInfo, string.Empty, "Skill", "skill");
                int defense = ReadAnyIntMember(hatInfo, 0, "Defense", "defense");
                object? skillRef = ReadAnyMember(hatInfo, "Skill_Ref", "skill_ref", "SkillRef");
                object? skillFunction = skillRef == null ? null : ReadAnyMember(skillRef, "Function", "function");
                var row = new EquipmentHatDiagnosticRow
                {
                    HatInfoId = hatId,
                    Skill = skill,
                    Defense = defense,
                    SkillRefId = skillRef == null ? string.Empty : ReadAnyStringMember(skillRef, string.Empty, "Id", "id"),
                    SkillRefGearEntry = skillRef == null ? string.Empty : (ReadAnyMember(skillRef, "GearEntry", "gearEntry", "gear_entry")?.ToString() ?? string.Empty),
                    SkillRefFunctionType = skillFunction?.GetType().Name ?? string.Empty,
                    ItemRows = itemRowsByHatId.TryGetValue(hatId, out List<EquipmentHatItemDiagnosticRow>? rows)
                        ? rows.OrderBy(item => item.ItemId, StringComparer.OrdinalIgnoreCase).ToList()
                        : new List<EquipmentHatItemDiagnosticRow>()
                };
                hatRows.Add(row);
            }

            HashSet<string> knownHatIds = new HashSet<string>(
                hatRows.Select(row => row.HatInfoId).Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
            List<EquipmentHatItemDiagnosticRow> orphanItemRows = itemRows
                .Where(row => string.IsNullOrWhiteSpace(row.HatId) || !knownHatIds.Contains(row.HatId))
                .OrderBy(row => row.ItemId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            hatRows = hatRows
                .OrderBy(row => row.HatInfoId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new EquipmentHatTableDiagnostic
            {
                CapturedAt = DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture),
                Source = "DolocConfig.Tables.TbHat.DataList + DolocConfig.Tables.TbItem.DataList",
                HatCount = hatRows.Count,
                ItemHatCount = itemRows.Count,
                HatsWithoutItemRows = hatRows.Count(row => row.ItemRows.Count == 0),
                ItemRowsWithoutHatInfo = orphanItemRows.Count,
                Hats = hatRows,
                ItemRowsWithoutHatInfoDetails = orphanItemRows
            };
        }

        private static string BuildEquipmentHatTableCsv(EquipmentHatTableDiagnostic diagnostic)
        {
            var lines = new List<string>
            {
                "HatInfo.Id,Skill,Defense,Skill_Ref.Id,Skill_Ref.GearEntry,Skill_Ref.Function.GetType().Name,TbItem[itemId],TbItem.Function.GetType().Name,ItemFunctionHatBase.HatId,HatId_Ref!=null"
            };

            foreach (EquipmentHatDiagnosticRow hat in diagnostic.Hats)
            {
                if (hat.ItemRows.Count == 0)
                {
                    lines.Add(string.Join(",", new[]
                    {
                        CsvCell(hat.HatInfoId),
                        CsvCell(hat.Skill),
                        hat.Defense.ToString(CultureInfo.InvariantCulture),
                        CsvCell(hat.SkillRefId),
                        CsvCell(hat.SkillRefGearEntry),
                        CsvCell(hat.SkillRefFunctionType),
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty
                    }));
                    continue;
                }

                foreach (EquipmentHatItemDiagnosticRow item in hat.ItemRows)
                {
                    lines.Add(string.Join(",", new[]
                    {
                        CsvCell(hat.HatInfoId),
                        CsvCell(hat.Skill),
                        hat.Defense.ToString(CultureInfo.InvariantCulture),
                        CsvCell(hat.SkillRefId),
                        CsvCell(hat.SkillRefGearEntry),
                        CsvCell(hat.SkillRefFunctionType),
                        CsvCell(item.ItemId),
                        CsvCell(item.FunctionType),
                        CsvCell(item.HatId),
                        item.HatIdRefResolved ? "true" : "false"
                    }));
                }
            }

            foreach (EquipmentHatItemDiagnosticRow item in diagnostic.ItemRowsWithoutHatInfoDetails)
            {
                lines.Add(string.Join(",", new[]
                {
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    CsvCell(item.ItemId),
                    CsvCell(item.FunctionType),
                    CsvCell(item.HatId),
                    item.HatIdRefResolved ? "true" : "false"
                }));
            }

            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        private static string CsvCell(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
        }

        private FixtureAttemptResult TryExerciseNewContentApisForFixture(
            bool mineOnly,
            bool? expectOilAbsentOverride = null,
            bool? oilOnlyOverride = null)
        {
            bool expectOilAbsent = !mineOnly && (expectOilAbsentOverride ?? false);
            bool oilOnly = !mineOnly && (oilOnlyOverride ?? false);
            oilOnlyFixtureMode = oilOnly;
            string newContentStage = mineOnly ? "MineOfficialJson" : (expectOilAbsent ? "OilAbsent" : "OilItemMetadata");
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                if (expectOilAbsent)
                {
                    string oilAbsentSummary = VerifyOilAbsentForFixture(dolocApi);
                    runtime.RuntimeMonitor.Log("Smoke exercise NewContentOilAbsent OK " + oilAbsentSummary);
                    runtime.SetHookStatus("Smoke.NewContentOilAbsent", "verified", "DolocAPI.QueryItemProto + DolocConfig.Tables.TbItemSpawn base LUT + DTMAPI diagnostics", oilAbsentSummary);
                    return FixtureAttemptResult.Succeeded;
                }

                if (mineOnly && mineOfficialTechTreeUiOpenRequested && !mineOfficialTechTreeUiEvidenceCaptured)
                    return CaptureMineOfficialTechTreeUiEvidenceForFixture(dolocApi);
                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    string recovery = mineOnly && mineOfficialTechTreeUiEvidenceCaptured
                        ? TryRecoverNormalStateAfterMineTechTreeUiForFixture(dolocApi)
                        : "not-applicable";
                    runtime.SetHookStatus("Smoke.NewContentMineProduction", "pending", "NormalGameState", "Waiting for NormalGameState before creating a temporary dtmapi_mine. recovery={" + recovery + "}");
                    return FixtureAttemptResult.Pending;
                }

                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                    throw new InvalidOperationException("CurrentRoom was not available for new-content smoke.");

                if (!mineOnly && ReadBoolMember(room, "IsInHouse", false))
                {
                    if (!autoExerciseOneActionMainFarmRequested && TryEnterMainFarmForOneActionSmoke(dolocApi, room, out string transitionSummary))
                    {
                        runtime.SetHookStatus("Smoke.NewContentOilCoalDrop", "pending", "DolocAPI.EnterFarm", transitionSummary);
                        return FixtureAttemptResult.Pending;
                    }

                    runtime.SetHookStatus("Smoke.NewContentOilCoalDrop", "pending", "DolocAPI.EnterFarm", "Waiting for outdoor farm transition before official coal_mine merged-LUT/native-world-drop smoke. room=" + DescribeRoomForFixture(room));
                    return FixtureAttemptResult.Pending;
                }

                if (!mineOnly && string.IsNullOrWhiteSpace(newContentOilItemMetadataSummary))
                    newContentOilItemMetadataSummary = TryExerciseOilItemMetadataForFixture(dolocApi);
                string oilItemMetadataSummary = mineOnly ? "skipped-mine-only" : newContentOilItemMetadataSummary;
                newContentStage = mineOnly ? "MineOfficialJson" : "OilCoalDrop";
                if (!mineOnly && string.IsNullOrWhiteSpace(newContentOilCoalDropSummary))
                    newContentOilCoalDropSummary = TryExerciseOilCoalDropForFixture(dolocApi, room);
                string oilCoalDropSummary = mineOnly ? "skipped-mine-only" : newContentOilCoalDropSummary;
                if (oilOnly)
                {
                    string oilOnlySummary = "oilItemMetadata={" + oilItemMetadataSummary + "}, oilCoalDrop={" + oilCoalDropSummary + "}";
                    runtime.RuntimeMonitor.Log("Smoke exercise NewContentOilOnly OK " + oilOnlySummary);
                    runtime.SetHookStatus("Smoke.NewContentOilOnly", "verified", "official Oil JSON -> merged native LUT -> ToolCollider.HandleTools -> CurrentRoom.DM_dropitem", oilOnlySummary);
                    return FixtureAttemptResult.Succeeded;
                }

                newContentStage = "MineOfficialJson";
                if (string.IsNullOrWhiteSpace(newContentMineOfficialJsonSummary))
                    newContentMineOfficialJsonSummary = TryExerciseMineOfficialJsonForFixture(dolocApi);
                string mineOfficialJsonSummary = newContentMineOfficialJsonSummary;
                if (mineOnly && !mineOfficialTechTreeUiEvidenceCaptured)
                    return OpenMineOfficialTechTreeUiForFixture(dolocApi, mineOfficialJsonSummary);

                newContentStage = "MineProduction";
                string equipmentSlotsSummary = mineOnly
                    ? "skipped-mine-only"
                    : "migrated-to-dedicated-MoreEquipmentSlots-ProductNative-fixture";

                if (newContentPendingMine == null)
                {
                    BeginPendingMineProductionForFixture(dolocApi, room, mineOnly);
                    runtime.SetHookStatus("Smoke.NewContentMineProduction", "pending", "Mine ProductNative UpdateTicked", "Transient Mine created; waiting across real Unity frames until the session-derived ProductNative scheduler observes the placed equipment.");
                    return FixtureAttemptResult.Pending;
                }

                if (newContentPendingMineOnly != mineOnly)
                    throw new InvalidOperationException("Pending Mine production mode changed before its transaction completed.");

                FixtureAttemptResult matrixResult =
                    AdvancePendingMineAcceptanceForFixture(
                        dolocApi,
                        out string matrixSummary);
                if (matrixResult == FixtureAttemptResult.Pending)
                {
                    return FixtureAttemptResult.Pending;
                }
                if (matrixResult != FixtureAttemptResult.Succeeded)
                    return matrixResult;

                string nativeInventory = DescribeMineNativeInventoryForFixture(newContentPendingMine);
                if (!ContainsIgnoreCase(nativeInventory, "capacity=16") ||
                    !ContainsIgnoreCase(nativeInventory, "lineCapacity=4"))
                {
                    throw new InvalidOperationException("Mine ProductNative produced but native inventory geometry was not the Mine-owned 16-slot/4-line contract. inventory={" + nativeInventory + "}");
                }

                object completedMine = newContentPendingMine;
                newContentPendingMineScaleSummary =
                    ObserveMineVisualAndOwnerForFixture(completedMine);
                if (!ContainsIgnoreCase(
                        newContentPendingMineScaleSummary,
                        "rendererScale=2x2"))
                {
                    throw new InvalidOperationException(
                        "Mine ProductNative did not apply the reviewed 2x placed-equipment scale. observation={" +
                        newContentPendingMineScaleSummary + "}");
                }
                string mineDescription = DescribeEquipmentForFixture(completedMine);
                string minePlacementEvidenceSummary = CaptureMinePlacementEvidenceForFixture(completedMine, newContentPendingMineCreateSummary);
                string summary = "mode=" + (mineOnly ? "mine-only" : "all-new-content") + ", oilItemMetadata={" + oilItemMetadataSummary + "}, oilCoalDrop={" + oilCoalDropSummary + "}, mineOfficialJson={" + mineOfficialJsonSummary + "}, equipmentSlots={" + equipmentSlotsSummary + "}, matrix={" + matrixSummary + "}, minePlacement={" + minePlacementEvidenceSummary + "}, scaleContainment={" + newContentPendingMineScaleSummary + "}, mine={" + mineDescription + "}, create={" + newContentPendingMineCreateSummary + "}, nativeInventory={" + nativeInventory + "}";
                CleanupPendingMineProductionForFixture("completed");
                runtime.RuntimeMonitor.Log("Smoke exercise NewContentMineProduction OK " + summary);
                runtime.SetHookStatus("Smoke.NewContentMineProduction", "verified", "Mine ProductNative session scheduler -> native ElectronicComponentAppliance/LinearInventory", summary);
                return FixtureAttemptResult.Succeeded;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                TryCleanupPendingMineProductionAfterFailure("target invocation failure");
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke new-content mine production failed.", ex.InnerException.ToString());
                runtime.SetHookStatus("Smoke.NewContent" + newContentStage, "failed", "NewContent smoke", ex.InnerException.GetType().Name + ": " + ex.InnerException.Message);
                return FixtureAttemptResult.Failed;
            }
            catch (Exception ex)
            {
                TryCleanupPendingMineProductionAfterFailure("fixture failure");
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke new-content mine production failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.NewContent" + newContentStage, "failed", "NewContent smoke", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private void BeginPendingMineProductionForFixture(Type dolocApi, object room, bool mineOnly)
        {
            object? transientMine =
                TryCreateTransientEquipmentForFixture(
                    dolocApi,
                    room,
                    "DolocTown.Equipment",
                    new[] { "dtmapi_mine" },
                    out string firstCreateSummary);
            if (transientMine == null)
                throw new InvalidOperationException("Could not create the first transient dtmapi_mine. " + firstCreateSummary);

            // Publish the transient owner before any later probe can fail so the catch/Close
            // paths retain a retryable receipt instead of silently orphaning world state.
            newContentPendingMine = transientMine;
            newContentPendingMineProductRuntime =
                GetMineProductRuntimeForFixture();
            newContentPendingMineRoom = room;
            newContentPendingMineOnly = mineOnly;
            newContentPendingMineCreateSummary = "first={" + firstCreateSummary + "}";

            if (!ReadStringMember(transientMine, "Name", string.Empty).Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("First transient equipment was not dtmapi_mine. target=" + DescribeEquipmentForFixture(transientMine) + ", create={" + firstCreateSummary + "}");

            object? secondMine =
                TryCreateTransientEquipmentForFixture(
                    dolocApi,
                    room,
                    "DolocTown.Equipment",
                    new[] { "dtmapi_mine" },
                    out string secondCreateSummary);
            if (secondMine == null)
                throw new InvalidOperationException("Could not create the second transient dtmapi_mine. " + secondCreateSummary);
            newContentPendingMineSecond = secondMine;
            newContentPendingMineCreateSummary += ", second={" + secondCreateSummary + "}";

            newContentPendingMineFirstIndex =
                ReadIntMember(transientMine, "index", int.MinValue);
            newContentPendingMineSecondIndex =
                ReadIntMember(secondMine, "index", int.MinValue);
            newContentPendingMineScaleSummary =
                ObserveMineVisualAndOwnerForFixture(transientMine);
            newContentPendingMineStage = 0;
            newContentPendingMineStableDue = -1;
            newContentPendingMineExpectedFirstObserved = -1;
            newContentPendingMineFirstIndexMutated = false;
            newContentPendingMineReplacementIndexMutated = false;
            newContentPendingMineSecondRemoved = false;
            newContentPendingMineMatrixEvidence.Clear();
            newContentPendingMineDeadline = DateTimeOffset.UtcNow.AddSeconds(12);
        }

        private FixtureAttemptResult AdvancePendingMineAcceptanceForFixture(
            Type dolocApi,
            out string summary)
        {
            object first =
                newContentPendingMine ??
                throw new InvalidOperationException("The first pending Mine was lost.");
            object second =
                newContentPendingMineSecond ??
                throw new InvalidOperationException("The second pending Mine was lost before its dismantle stage.");
            summary = string.Join(
                "; ",
                newContentPendingMineMatrixEvidence.ToArray());

            if (newContentPendingMineStage == 0)
            {
                if (ObserveMineSchedulerEntryCountForFixture() < 2)
                    return WaitForPendingMineStageOrThrow("two-Mine discovery", out summary);

                string firstSchedule =
                    ObserveMineScheduleForFixture(first);
                string secondSchedule =
                    ObserveMineScheduleForFixture(second);
                if (!ContainsIgnoreCase(firstSchedule, "present=True") ||
                    !ContainsIgnoreCase(secondSchedule, "present=True"))
                {
                    return WaitForPendingMineStageOrThrow(
                        "identity-backed two-Mine scheduler entries",
                        out summary);
                }

                TimeSkipResult timeSkip =
                    timeDebugApi.SkipToNextWeatherPeriod(
                        CreateFixtureManifest());
                if (!timeSkip.Success)
                {
                    throw new InvalidOperationException(
                        "Native pass-time failed after the two Mine entries armed. reason=" +
                        timeSkip.FailureReason + ", message=" +
                        timeSkip.Message);
                }
                string firstDrain =
                    DrainTransientMinePowerThroughNativeOwnerForFixture(
                        first);
                string secondDrain =
                    DrainTransientMinePowerThroughNativeOwnerForFixture(
                        second);
                newContentPendingMineMatrixEvidence.Add(
                    "twoMines=2, timeSkip={" + timeSkip.Message +
                    "}, lowPowerPrepared={" + firstDrain + "|" +
                    secondDrain + "}");
                AdvancePendingMineStage(
                    1,
                    "Waiting for both overdue, uncharged Mines to reach the low-power preflight.");
                return FixtureAttemptResult.Pending;
            }

            if (newContentPendingMineStage == 1)
            {
                string firstSchedule =
                    ObserveMineScheduleForFixture(first);
                string secondSchedule =
                    ObserveMineScheduleForFixture(second);
                if (!ContainsIgnoreCase(firstSchedule, "block=low-power") ||
                    !ContainsIgnoreCase(secondSchedule, "block=low-power"))
                {
                    return WaitForPendingMineStageOrThrow(
                        "two low-power preflights",
                        out summary);
                }
                if (ReadMineFilledSlotsForFixture(first) != 0 ||
                    ReadMineFilledSlotsForFixture(second) != 0)
                {
                    throw new InvalidOperationException(
                        "An uncharged Mine produced output before native low-power preflight.");
                }

                FillMineInventoryForFixture(dolocApi, first, "stone");
                string firstCharge =
                    ChargeTransientMineThroughNativeOwnerForFixture(first);
                string secondCharge =
                    ChargeTransientMineThroughNativeOwnerForFixture(second);
                double fullPowerBefore =
                    ReadMinePowerForFixture(first);
                double producingPowerBefore =
                    ReadMinePowerForFixture(second);
                newContentPendingMineMatrixEvidence.Add(
                    "lowPower=both-empty/no-output, firstSchedule={" +
                    firstSchedule + "}, secondSchedule={" +
                    secondSchedule + "}");
                newContentPendingMineMatrixEvidence.Add(
                    "preparedFullAndCharged=firstPower:" +
                    FormatRatio(fullPowerBefore) + ", secondPower:" +
                    FormatRatio(producingPowerBefore) + ", owners={" +
                    firstCharge + "|" + secondCharge + "}");
                AdvancePendingMineStage(
                    2,
                    "Waiting for full-storage refusal and the second Mine's successful production.");
                return FixtureAttemptResult.Pending;
            }

            if (newContentPendingMineStage == 2)
            {
                string firstSchedule =
                    ObserveMineScheduleForFixture(first);
                int secondFilled =
                    ReadMineFilledSlotsForFixture(second);
                if (!ContainsIgnoreCase(firstSchedule, "block=full-storage") ||
                    secondFilled <= 0)
                {
                    return WaitForPendingMineStageOrThrow(
                        "full-storage refusal plus independent second-Mine production",
                        out summary);
                }
                if (ReadMineFilledSlotsForFixture(first) != 16)
                {
                    throw new InvalidOperationException(
                        "The full Mine inventory changed during preflight refusal. inventory={" +
                        DescribeMineNativeInventoryForFixture(first) + "}");
                }
                double fullPowerAfter =
                    ReadMinePowerForFixture(first);
                double producingPowerAfter =
                    ReadMinePowerForFixture(second);
                if (fullPowerAfter < MinePowerThresholdForFixture)
                {
                    throw new InvalidOperationException(
                        "The full Mine consumed native power before refusing production. power=" +
                        FormatRatio(fullPowerAfter) + ".");
                }
                newContentPendingMineMatrixEvidence.Add(
                    "fullStorage=16/16/no-power-consumption, schedule={" +
                    firstSchedule + "}");
                newContentPendingMineMatrixEvidence.Add(
                    "independentProduction=secondFilled:" + secondFilled +
                    ", secondPowerAfter:" +
                    FormatRatio(producingPowerAfter));

                string disabled =
                    ConfigureMineEnabledForFixture(false);
                if (!ContainsIgnoreCase(disabled, "status=disabled") ||
                    !ContainsIgnoreCase(disabled, "active=False") ||
                    !ContainsIgnoreCase(disabled, "hooks=0/3") ||
                    !ContainsIgnoreCase(disabled, "scheduler=0"))
                {
                    throw new InvalidOperationException(
                        "Mine hot disable did not clear runtime ownership. status={" +
                        disabled + "}");
                }
                string reenabled =
                    ConfigureMineEnabledForFixture(true);
                if (!ContainsIgnoreCase(reenabled, "status=active") ||
                    !ContainsIgnoreCase(reenabled, "active=True") ||
                    !ContainsIgnoreCase(reenabled, "hooks=3/3") ||
                    !ContainsIgnoreCase(reenabled, "scheduler=0"))
                {
                    throw new InvalidOperationException(
                        "Mine hot re-enable did not reactivate a fresh session scheduler. status={" +
                        reenabled + "}");
                }
                newContentPendingMineMatrixEvidence.Add(
                    "hotDisableReenable={" + disabled + " -> " +
                    reenabled + "}");
                AdvancePendingMineStage(
                    3,
                    "Waiting for fresh scheduler entries after re-enable.");
                return FixtureAttemptResult.Pending;
            }

            if (newContentPendingMineStage == 3)
            {
                if (ObserveMineSchedulerEntryCountForFixture() < 2)
                    return WaitForPendingMineStageOrThrow("post-re-enable two-Mine discovery", out summary);
                string firstSchedule =
                    ObserveMineScheduleForFixture(first);
                string secondSchedule =
                    ObserveMineScheduleForFixture(second);
                newContentPendingMineStableDue =
                    ReadSummaryInt(firstSchedule, "nextDueTU", -1);
                if (newContentPendingMineStableDue < 0 ||
                    !ContainsIgnoreCase(secondSchedule, "present=True"))
                {
                    return WaitForPendingMineStageOrThrow("post-re-enable identity schedules", out summary);
                }

                int movedIndex =
                    newContentPendingMineFirstIndex == int.MinValue
                        ? 900001
                        : checked(newContentPendingMineFirstIndex + 900000);
                if (!WriteIntMember(first, "index", movedIndex))
                    throw new InvalidOperationException("Could not mutate the first Mine index for the move-stability probe.");
                newContentPendingMineFirstIndexMutated = true;
                newContentPendingMineMatrixEvidence.Add(
                    "freshAfterReenable=first{" + firstSchedule +
                    "}, second{" + secondSchedule + "}");
                AdvancePendingMineStage(
                    4,
                    "Waiting for one authoritative poll with the same Mine object at a changed index.");
                return FixtureAttemptResult.Pending;
            }

            if (newContentPendingMineStage == 4)
            {
                string movedSchedule =
                    ObserveMineScheduleForFixture(first);
                int movedDue =
                    ReadSummaryInt(movedSchedule, "nextDueTU", -1);
                if (!ContainsIgnoreCase(movedSchedule, "present=True") ||
                    movedDue != newContentPendingMineStableDue ||
                    ObserveMineSchedulerEntryCountForFixture() != 2)
                {
                    return WaitForPendingMineStageOrThrow(
                        "same-object move/index stability",
                        out summary);
                }
                RestorePendingMineIndicesForFixture();
                newContentPendingMineMatrixEvidence.Add(
                    "moveIndexStable=due:" + movedDue +
                    ", scheduleCount:2");
                if (!TryRemoveTransientEquipmentForFixture(
                        newContentPendingMineRoom,
                        second))
                {
                    throw new InvalidOperationException(
                        "Could not dismantle the second transient Mine.");
                }
                newContentPendingMineSecondRemoved = true;
                AdvancePendingMineStage(
                    5,
                    "Waiting for the dismantled Mine identity to be pruned.");
                return FixtureAttemptResult.Pending;
            }

            if (newContentPendingMineStage == 5)
            {
                string removedSchedule =
                    ObserveMineScheduleForFixture(second);
                if (ObserveMineSchedulerEntryCountForFixture() != 1 ||
                    !ContainsIgnoreCase(removedSchedule, "present=False"))
                {
                    return WaitForPendingMineStageOrThrow(
                        "dismantled identity pruning",
                        out summary);
                }

                object? replacement =
                    TryCreateTransientEquipmentForFixture(
                        dolocApi,
                        newContentPendingMineRoom!,
                        "DolocTown.Equipment",
                        new[] { "dtmapi_mine" },
                        out string replacementSummary);
                if (replacement == null)
                {
                    throw new InvalidOperationException(
                        "Could not create the replacement Mine. " +
                        replacementSummary);
                }
                newContentPendingMineReplacement = replacement;
                newContentPendingMineReplacementIndex =
                    ReadIntMember(replacement, "index", int.MinValue);
                newContentPendingMineExpectedFirstObserved =
                    ReadCurrentTotalTusForFixture(dolocApi);
                if (newContentPendingMineSecondIndex != int.MinValue &&
                    !WriteIntMember(
                        replacement,
                        "index",
                        newContentPendingMineSecondIndex))
                {
                    throw new InvalidOperationException(
                        "Could not expose the dismantled Mine index on the replacement identity.");
                }
                newContentPendingMineReplacementIndexMutated =
                    newContentPendingMineSecondIndex != int.MinValue;
                newContentPendingMineMatrixEvidence.Add(
                    "dismantled=pruned, replacement={" +
                    replacementSummary + "}, reusedIndex=" +
                    newContentPendingMineSecondIndex);
                AdvancePendingMineStage(
                    6,
                    "Waiting for the replacement object at the reused index to receive a fresh due entry.");
                return FixtureAttemptResult.Pending;
            }

            if (newContentPendingMineStage == 6)
            {
                object replacement =
                    newContentPendingMineReplacement ??
                    throw new InvalidOperationException("The replacement Mine was lost.");
                string replacementSchedule =
                    ObserveMineScheduleForFixture(replacement);
                int firstObserved =
                    ReadSummaryInt(replacementSchedule, "firstObservedTU", -1);
                int nextDue =
                    ReadSummaryInt(replacementSchedule, "nextDueTU", -1);
                int cycles =
                    ReadSummaryInt(replacementSchedule, "cycles", -1);
                if (!ContainsIgnoreCase(replacementSchedule, "present=True") ||
                    ObserveMineSchedulerEntryCountForFixture() != 2)
                {
                    return WaitForPendingMineStageOrThrow(
                        "fresh replacement identity",
                        out summary);
                }
                if (firstObserved < newContentPendingMineExpectedFirstObserved ||
                    nextDue <= firstObserved ||
                    cycles != 0)
                {
                    throw new InvalidOperationException(
                        "The replacement Mine inherited stale scheduling state. expectedFirstObserved>=" +
                        newContentPendingMineExpectedFirstObserved +
                        ", schedule={" + replacementSchedule + "}");
                }
                RestorePendingMineIndicesForFixture();
                newContentPendingMineMatrixEvidence.Add(
                    "indexReuseFresh={" + replacementSchedule + "}");
                summary = string.Join(
                    "; ",
                    newContentPendingMineMatrixEvidence.ToArray());
                return FixtureAttemptResult.Succeeded;
            }

            throw new InvalidOperationException(
                "Unknown pending Mine matrix stage " +
                newContentPendingMineStage + ".");
        }

        private FixtureAttemptResult WaitForPendingMineStageOrThrow(
            string expectation,
            out string summary)
        {
            summary = string.Join(
                "; ",
                newContentPendingMineMatrixEvidence.ToArray());
            if (DateTimeOffset.UtcNow < newContentPendingMineDeadline)
            {
                runtime.SetHookStatus(
                    "Smoke.NewContentMineProduction",
                    "pending",
                    "Mine ProductNative bounded acceptance matrix",
                    "stage=" + newContentPendingMineStage +
                    ", waitingFor=" + expectation +
                    ", scheduler=" +
                    ObserveMineSchedulerEntryCountForFixture() +
                    ", runtime={" +
                    ObserveMineRuntimeSummaryForFixture() + "}");
                return FixtureAttemptResult.Pending;
            }
            throw new TimeoutException(
                "Mine bounded acceptance timed out. stage=" +
                newContentPendingMineStage + ", expected=" +
                expectation + ", evidence={" + summary +
                "}, runtime={" +
                ObserveMineRuntimeSummaryForFixture() + "}");
        }

        private void AdvancePendingMineStage(
            int stage,
            string detail)
        {
            newContentPendingMineStage = stage;
            newContentPendingMineDeadline =
                DateTimeOffset.UtcNow.AddSeconds(12);
            runtime.SetHookStatus(
                "Smoke.NewContentMineProduction",
                "pending",
                "Mine ProductNative bounded acceptance matrix",
                "stage=" + stage + ", " + detail);
        }

        private string CleanupPendingMineProductionForFixture(string reason)
        {
            RestorePendingMineIndicesForFixture();
            object? first = newContentPendingMine;
            object? second = newContentPendingMineSecond;
            object? replacement = newContentPendingMineReplacement;
            object? room = newContentPendingMineRoom;
            var failures = new List<string>();
            if (replacement != null &&
                !TryRemoveTransientEquipmentForFixture(room, replacement))
                failures.Add("replacement");
            if (second != null &&
                !newContentPendingMineSecondRemoved &&
                !TryRemoveTransientEquipmentForFixture(room, second))
                failures.Add("second");
            if (first != null &&
                !TryRemoveTransientEquipmentForFixture(room, first))
                failures.Add("first");
            if (failures.Count > 0)
                throw new InvalidOperationException("Could not remove pending QA Mine equipment during " + (reason ?? string.Empty) + ": " + string.Join(",", failures.ToArray()) + ".");
            newContentPendingMine = null;
            newContentPendingMineSecond = null;
            newContentPendingMineReplacement = null;
            newContentPendingMineProductRuntime = null;
            newContentPendingMineRoom = null;
            newContentPendingMineOnly = false;
            newContentPendingMineStage = 0;
            newContentPendingMineFirstIndex = int.MinValue;
            newContentPendingMineSecondIndex = int.MinValue;
            newContentPendingMineReplacementIndex = int.MinValue;
            newContentPendingMineStableDue = -1;
            newContentPendingMineExpectedFirstObserved = -1;
            newContentPendingMineFirstIndexMutated = false;
            newContentPendingMineReplacementIndexMutated = false;
            newContentPendingMineSecondRemoved = false;
            newContentPendingMineCreateSummary = string.Empty;
            newContentPendingMineScaleSummary = string.Empty;
            newContentPendingMineMatrixEvidence.Clear();
            newContentPendingMineDeadline = DateTimeOffset.MinValue;
            return "reason=" + (reason ?? string.Empty) +
                ", removed=" +
                new[] { first, second, replacement }
                    .Count(value => value != null)
                    .ToString(CultureInfo.InvariantCulture);
        }

        private void TryCleanupPendingMineProductionAfterFailure(string reason)
        {
            try
            {
                CleanupPendingMineProductionForFixture(reason);
            }
            catch (Exception cleanupError)
            {
                runtime.RuntimeMonitor.Log("G5 pending Mine cleanup did not complete reason=" + reason + " error=" + cleanupError.GetType().Name + ": " + cleanupError.Message + ".", LogLevel.Warn);
            }
        }

        private FixtureAttemptResult OpenMineOfficialTechTreeUiForFixture(Type dolocApi, string mineOfficialJsonSummary)
        {
            string treeId = ExtractMineTechTreeId(mineOfficialJsonSummary);
            string nodeId = "dtmapi_mine";
            MethodInfo? jumpTechTreeNode = FindMethod(dolocApi, "JumpTechTreeNode", 2);
            if (jumpTechTreeNode == null)
                throw new MissingMethodException("DolocAPI.JumpTechTreeNode(string,string) was not found.");

            mineOfficialTechTreeUiTreeId = treeId;
            mineOfficialTechTreeUiNodeId = nodeId;
            mineOfficialTechTreeUiOpenRequested = true;
            mineOfficialTechTreeUiOpenAt = DateTimeOffset.Now;
            runtime.SetHookStatus("Smoke.NewContentMineOfficialTechTreeUi", "pending", "DolocAPI.JumpTechTreeNode + TechTreeUiState", "Opening official tech tree node tree=" + treeId + ", node=" + nodeId + " for visual evidence.");
            runtime.RuntimeMonitor.Log("Mine official tech tree UI opening tree=" + treeId + " node=" + nodeId + " via DolocAPI.JumpTechTreeNode.");
            jumpTechTreeNode.Invoke(null, new object[] { treeId, nodeId });
            return FixtureAttemptResult.Pending;
        }

        private FixtureAttemptResult CaptureMineOfficialTechTreeUiEvidenceForFixture(Type dolocApi)
        {
            if ((DateTimeOffset.Now - mineOfficialTechTreeUiOpenAt).TotalSeconds < 1.75)
            {
                runtime.SetHookStatus("Smoke.NewContentMineOfficialTechTreeUi", "pending", "DolocAPI.JumpTechTreeNode + TechTreeUiState", "Waiting for official tech tree panel render before screenshot. tree=" + mineOfficialTechTreeUiTreeId + ", node=" + mineOfficialTechTreeUiNodeId + ".");
                return FixtureAttemptResult.Pending;
            }

            try
            {
                string evidenceDir = EnsureNewContentEvidenceDir();
                string screenshotPath = Path.Combine(evidenceDir, "mine-official-tech-tree-dtmapi-mine.png");
                bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
                string closeSummary = CloseMineOfficialTechTreeUiForFixture(dolocApi);
                mineOfficialTechTreeUiEvidenceCaptured = true;
                mineOfficialTechTreeUiClosedAt = DateTimeOffset.Now;

                string summary = "tree=" + mineOfficialTechTreeUiTreeId +
                    ", node=" + mineOfficialTechTreeUiNodeId +
                    ", screenshot=" + (screenshotRequested ? screenshotPath : "unavailable") +
                    ", close={" + closeSummary + "}";
                File.AppendAllText(Path.Combine(evidenceDir, "mine-official-tech-tree-summary.txt"),
                    "Captured=" + DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture) + Environment.NewLine +
                    "Tree=" + mineOfficialTechTreeUiTreeId + Environment.NewLine +
                    "Node=" + mineOfficialTechTreeUiNodeId + Environment.NewLine +
                    "ScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                    "Screenshot=" + screenshotPath + Environment.NewLine +
                    "Close=" + closeSummary + Environment.NewLine);
                runtime.RuntimeMonitor.Log("Mine official tech tree UI evidence " + (screenshotRequested ? "OK" : "unavailable") + " " + summary + ".");
                runtime.SetHookStatus("Smoke.NewContentMineOfficialTechTreeUi", screenshotRequested ? "verified" : "pending", "DolocAPI.JumpTechTreeNode + TechTreeUiState + UnityEngine.ScreenCapture", summary);
                return FixtureAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Mine official tech tree UI evidence capture failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.NewContentMineOfficialTechTreeUi", "failed", "DolocAPI.JumpTechTreeNode + TechTreeUiState", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private string CloseMineOfficialTechTreeUiForFixture(Type dolocApi)
        {
            Type? techTreeUiState = patcher?.ResolveType("DolocTown.TechTreeUiState, Assembly-CSharp");
            if (techTreeUiState == null)
                return "skipped:missing-TechTreeUiState";

            object? userInput = ReadStaticMember(dolocApi, "userInput");
            object? currentState = userInput == null ? null : ReadMember(userInput, "CurrentState");
            if (currentState != null && techTreeUiState.IsAssignableFrom(currentState.GetType()))
            {
                MethodInfo? popState = FindMethod(userInput!.GetType(), "PopState", 0);
                if (popState != null && popState.Invoke(userInput, null) is bool popped && popped)
                    return "popped-current-TechTreeUiState";
            }

            MethodInfo? removeUiState = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(method => method.Name.Equals("RemoveUiState", StringComparison.Ordinal) && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
            if (removeUiState == null)
                return "skipped:missing-DolocAPI.RemoveUiState";

            removeUiState.MakeGenericMethod(techTreeUiState).Invoke(null, null);
            return "removed-TechTreeUiState";
        }

        private string TryRecoverNormalStateAfterMineTechTreeUiForFixture(Type dolocApi)
        {
            object? userInput = ReadStaticMember(dolocApi, "userInput");
            object? currentState = userInput == null ? null : ReadMember(userInput, "CurrentState");
            string currentStateName = currentState?.GetType().FullName ?? "null";
            double secondsSinceClose = mineOfficialTechTreeUiClosedAt == default ? -1 : (DateTimeOffset.Now - mineOfficialTechTreeUiClosedAt).TotalSeconds;
            Type? techTreeUiState = patcher?.ResolveType("DolocTown.TechTreeUiState, Assembly-CSharp");
            if (userInput != null && currentState != null && techTreeUiState != null && techTreeUiState.IsAssignableFrom(currentState.GetType()))
            {
                MethodInfo? popState = FindMethod(userInput.GetType(), "PopState", 0);
                if (popState != null && popState.Invoke(userInput, null) is bool popped)
                    return "retry-pop-TechTreeUiState popped=" + popped + ", secondsSinceClose=" + FormatRatio(secondsSinceClose);
            }

            if ((DateTimeOffset.Now - lastMineOfficialTechTreeUiRecoveryLogAt).TotalSeconds >= 5)
            {
                lastMineOfficialTechTreeUiRecoveryLogAt = DateTimeOffset.Now;
                runtime.RuntimeMonitor.Log("Mine official tech tree UI recovery waiting currentState=" + currentStateName + " secondsSinceClose=" + FormatRatio(secondsSinceClose) + ".");
            }

            return "currentState=" + currentStateName + ", secondsSinceClose=" + FormatRatio(secondsSinceClose);
        }

        private static string ExtractMineTechTreeId(string summary)
        {
            const string marker = "tree=";
            int start = summary.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return "industrial_techtree";

            start += marker.Length;
            int end = start;
            while (end < summary.Length && summary[end] != ',' && summary[end] != '}' && !char.IsWhiteSpace(summary[end]))
                end++;

            string tree = summary.Substring(start, end - start).Trim();
            return string.IsNullOrWhiteSpace(tree) ? "industrial_techtree" : tree;
        }

        private static string ObserveMineProductNativeOwnerForFixture()
        {
            Batch6HarmonyOwnerInventory owner =
                Batch6AdvancedHarmonyOwnerObserver.Observe(
                    "DTMAPI.Mine",
                    "dtmapi.mod.dtmapi.minemod",
                    new[]
                    {
                        new Batch6HarmonyPatchTarget(
                            "DolocTown.EquipmentRenderer",
                            "OnReuse",
                            0),
                        new Batch6HarmonyPatchTarget(
                            "DolocTown.EquipmentBuilder",
                            "CreateIndicator",
                            0),
                        new Batch6HarmonyPatchTarget(
                            "DolocTown.EquipmentBuilder",
                            "TurnIndicator",
                            0)
                    });
            if (!owner.IsComplete)
            {
                throw new InvalidOperationException(
                    "Mine ProductNative exact-owner inventory is incomplete. loaded=" +
                    owner.ProductAssemblyLoaded +
                    ", targets=" + owner.ExactOwnerTargetCount +
                    "/" + owner.ExpectedTargetCount +
                    ", patches=" + owner.ExactOwnerPatchCount +
                    "/" + owner.ExpectedPatchCount +
                    ", details={" + owner.Details + "}");
            }

            return "assemblyLoaded=True, owner=dtmapi.mod.dtmapi.minemod, targets=" +
                owner.ExactOwnerTargetCount + "/" +
                owner.ExpectedTargetCount + ", patches=" +
                owner.ExactOwnerPatchCount + "/" +
                owner.ExpectedPatchCount + ", details={" +
                owner.Details + "}";
        }

        private static int
            ObserveMineSchedulerEntryCountForFixture()
        {
            object productRuntime =
                GetMineProductRuntimeForFixture();
            PropertyInfo? schedulerCount =
                productRuntime.GetType().GetProperty(
                    "SchedulerEntryCount",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
            if (schedulerCount == null)
            {
                throw new InvalidOperationException(
                    "Mine ProductNative scheduler observation is unavailable.");
            }
            return Convert.ToInt32(
                schedulerCount.GetValue(
                    productRuntime,
                    null),
                CultureInfo.InvariantCulture);
        }

        private static string ObserveMineScheduleForFixture(
            object equipment)
        {
            object productRuntime =
                GetMineProductRuntimeForFixture();
            MethodInfo? observe =
                productRuntime.GetType().GetMethod(
                    "ObserveMineScheduleForFixture",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
            if (observe == null)
            {
                throw new MissingMethodException(
                    productRuntime.GetType().FullName,
                    "ObserveMineScheduleForFixture(object)");
            }
            return Convert.ToString(
                       observe.Invoke(
                           productRuntime,
                           new[] { equipment }),
                       CultureInfo.InvariantCulture) ??
                   "present=False";
        }

        private string ConfigureMineEnabledForFixture(
            bool enabled)
        {
            object productRuntime =
                newContentPendingMineProductRuntime ??
                GetMineProductRuntimeForFixture();
            Type runtimeType = productRuntime.GetType();
            FieldInfo? configField =
                runtimeType.GetField(
                    "config",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);
            FieldInfo? manifestField =
                runtimeType.GetField(
                    "manifest",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);
            FieldInfo? oilField =
                runtimeType.GetField(
                    "isOilAvailable",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);
            object? currentConfig =
                configField?.GetValue(productRuntime);
            object? manifest =
                manifestField?.GetValue(productRuntime);
            object? oilAvailability =
                oilField?.GetValue(productRuntime);
            MethodInfo? copy =
                currentConfig?.GetType().GetMethod(
                    "Copy",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
            object? nextConfig =
                copy?.Invoke(currentConfig, null);
            PropertyInfo? enabledProperty =
                nextConfig?.GetType().GetProperty(
                    "Enabled",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
            MethodInfo? configure =
                runtimeType.GetMethod(
                    "Configure",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
            if (nextConfig == null ||
                manifest == null ||
                oilAvailability == null ||
                enabledProperty == null ||
                !enabledProperty.CanWrite ||
                configure == null)
            {
                throw new InvalidOperationException(
                    "Mine configuration lifecycle fixture seam is unavailable.");
            }
            enabledProperty.SetValue(
                nextConfig,
                enabled,
                null);
            configure.Invoke(
                productRuntime,
                new[]
                {
                    nextConfig,
                    manifest,
                    oilAvailability,
                    (object)("bounded QA Enabled=" + enabled)
                });
            MethodInfo? summary =
                runtimeType.GetMethod(
                    "BuildStatusSummary",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
            return Convert.ToString(
                       summary?.Invoke(
                           productRuntime,
                           null),
                       CultureInfo.InvariantCulture) ??
                   "unavailable";
        }

        private static double ReadMinePowerForFixture(
            object mine)
        {
            object? component =
                ReadMember(
                    mine,
                    "IElectronicComponent");
            return component == null
                ? double.NaN
                : ReadDoubleMember(
                    component,
                    "power",
                    double.NaN);
        }

        private static void FillMineInventoryForFixture(
            Type dolocApi,
            object mine,
            string itemId)
        {
            object? inventory =
                ReadMember(mine, "inventory");
            if (inventory == null)
                throw new InvalidOperationException("Mine inventory was unavailable.");
            MethodInfo? read =
                FindMethod(
                    inventory.GetType(),
                    "Read",
                    1);
            MethodInfo? place =
                FindMethod(
                    inventory.GetType(),
                    "PlaceItemAt",
                    2);
            int capacity =
                ReadIntMember(
                    inventory,
                    "capacity",
                    0);
            if (read == null || place == null || capacity != 16)
            {
                throw new InvalidOperationException(
                    "Mine inventory did not expose the reviewed 16-slot LinearInventory contract.");
            }
            for (int slot = 0; slot < capacity; slot++)
            {
                if (read.Invoke(
                        inventory,
                        new object[] { slot }) != null)
                    continue;
                object? item =
                    GenerateItemForFixture(
                        dolocApi,
                        itemId,
                        1);
                if (item == null)
                {
                    throw new InvalidOperationException(
                        "Could not generate " + itemId +
                        " for Mine full-storage preflight.");
                }
                object? leftover =
                    place.Invoke(
                        inventory,
                        new[] { (object)slot, item });
                if (leftover != null &&
                    ReadIntMember(leftover, "count", 1) > 0)
                {
                    throw new InvalidOperationException(
                        "Mine inventory rejected the full-storage fixture item at slot " +
                        slot + ".");
                }
            }
            if (ReadMineFilledSlotsForFixture(mine) != capacity)
            {
                throw new InvalidOperationException(
                    "Mine full-storage fixture did not fill all slots. inventory={" +
                    DescribeMineNativeInventoryForFixture(mine) + "}");
            }
        }

        private static int ReadCurrentTotalTusForFixture(
            Type dolocApi)
        {
            object? archive =
                ReadStaticMember(
                    dolocApi,
                    "archiveHandle");
            object? dateNow =
                archive == null
                    ? null
                    : ReadMember(archive, "DateNow");
            object? timeData =
                archive == null
                    ? null
                    : ReadMember(archive, "timeData");
            dateNow ??=
                timeData == null
                    ? null
                    : ReadMember(timeData, "dateNow");
            int totalTus =
                dateNow == null
                    ? -1
                    : ReadIntMember(
                        dateNow,
                        "TotalTUs",
                        -1);
            if (totalTus < 0)
                throw new InvalidOperationException("Current native TotalTUs was unavailable.");
            return totalTus;
        }

        private static int ReadSummaryInt(
            string summary,
            string key,
            int fallback)
        {
            string marker = key + "=";
            int start =
                summary.IndexOf(
                    marker,
                    StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return fallback;
            start += marker.Length;
            int end = start;
            if (end < summary.Length && summary[end] == '-')
                end++;
            while (end < summary.Length &&
                   char.IsDigit(summary[end]))
                end++;
            return int.TryParse(
                summary.Substring(start, end - start),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int value)
                ? value
                : fallback;
        }

        private void RestorePendingMineIndicesForFixture()
        {
            if (newContentPendingMineFirstIndexMutated &&
                newContentPendingMine != null)
            {
                if (!WriteIntMember(
                        newContentPendingMine,
                        "index",
                        newContentPendingMineFirstIndex))
                {
                    throw new InvalidOperationException(
                        "Could not restore the first Mine index after the move-stability probe.");
                }
                newContentPendingMineFirstIndexMutated = false;
            }
            if (newContentPendingMineReplacementIndexMutated &&
                newContentPendingMineReplacement != null)
            {
                if (!WriteIntMember(
                        newContentPendingMineReplacement,
                        "index",
                        newContentPendingMineReplacementIndex))
                {
                    throw new InvalidOperationException(
                        "Could not restore the replacement Mine index after the reuse probe.");
                }
                newContentPendingMineReplacementIndexMutated = false;
            }
        }

        private static string
            ObserveMineRuntimeSummaryForFixture()
        {
            object productRuntime =
                GetMineProductRuntimeForFixture();
            MethodInfo? summary =
                productRuntime.GetType().GetMethod(
                    "BuildStatusSummary",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
            if (summary == null)
            {
                throw new MissingMethodException(
                    productRuntime.GetType().FullName,
                    "BuildStatusSummary()");
            }
            return Convert.ToString(
                       summary.Invoke(
                           productRuntime,
                           null),
                       CultureInfo.InvariantCulture) ??
                   "unavailable";
        }

        private static object
            GetMineProductRuntimeForFixture()
        {
            Assembly? assembly =
                AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(candidate =>
                        string.Equals(
                            candidate.GetName().Name,
                            "DTMAPI.Mine",
                            StringComparison.Ordinal));
            Type? callbacks =
                assembly?.GetType(
                    "DTMAPI.Mine.MineCallbacks",
                    throwOnError: false,
                    ignoreCase: false);
            object? productRuntime =
                callbacks?.GetField(
                    "runtime",
                    BindingFlags.Static |
                    BindingFlags.NonPublic)
                    ?.GetValue(null);
            return productRuntime ??
                throw new InvalidOperationException(
                    "Mine ProductNative runtime observation is unavailable.");
        }

        private static string
            ChargeTransientMineThroughNativeOwnerForFixture(
                object mine)
        {
            object? component =
                ReadMember(
                    mine,
                    "IElectronicComponent");
            if (component == null)
            {
                throw new InvalidOperationException(
                    "Transient Mine has no native electronic component.");
            }
            MethodInfo? chargeToFull =
                FindMethod(
                    component.GetType(),
                    "ChargeToFull",
                    0);
            if (chargeToFull == null)
            {
                throw new MissingMethodException(
                    component.GetType().FullName,
                    "ChargeToFull()");
            }
            chargeToFull.Invoke(
                component,
                null);
            return "owner=" +
                component.GetType().FullName +
                ", method=ChargeToFull(), reviewedThreshold=10";
        }

        private static string
            DrainTransientMinePowerThroughNativeOwnerForFixture(
                object mine)
        {
            object? component =
                ReadMember(
                    mine,
                    "IElectronicComponent");
            if (component == null)
            {
                throw new InvalidOperationException(
                    "Transient Mine has no native electronic component.");
            }
            MethodInfo? launch =
                FindMethod(
                    component.GetType(),
                    "Launch",
                    0);
            if (launch == null)
            {
                throw new MissingMethodException(
                    component.GetType().FullName,
                    "Launch()");
            }
            int consumed = 0;
            while (consumed < 1024)
            {
                object? result =
                    launch.Invoke(
                        component,
                        null);
                if (!(result is bool launched) || !launched)
                    break;
                consumed++;
            }
            if (consumed >= 1024)
            {
                throw new InvalidOperationException(
                    "Native Mine power drain exceeded its bounded Launch() preparation.");
            }
            double remaining =
                ReadDoubleMember(
                    component,
                    "power",
                    double.NaN);
            if (!double.IsNaN(remaining) &&
                remaining >= MinePowerThresholdForFixture)
            {
                throw new InvalidOperationException(
                    "Native Mine power drain did not reach the low-power precondition. remaining=" +
                    FormatRatio(remaining) + ".");
            }
            return "owner=" + component.GetType().FullName +
                ", method=Launch(), successfulDrains=" + consumed +
                ", remaining=" + FormatRatio(remaining);
        }

        private static string ObserveMineVisualAndOwnerForFixture(
            object mine)
        {
            string owner = ObserveMineProductNativeOwnerForFixture();
            object? renderer = ReadMember(mine, "Renderer");
            object? transform =
                renderer == null
                    ? null
                    : ReadMember(renderer, "transform");
            object? scale =
                transform == null
                    ? null
                    : ReadMember(transform, "localScale");
            string scaleSummary =
                scale == null
                    ? "unavailable"
                    : Math.Abs(ReadDoubleMember(scale, "x", 0d))
                            .ToString("0.##", CultureInfo.InvariantCulture) +
                        "x" +
                        Math.Abs(ReadDoubleMember(scale, "y", 0d))
                            .ToString("0.##", CultureInfo.InvariantCulture);
            return "rendererScale=" + scaleSummary +
                ", exactOwner={" + owner + "}";
        }

        private static int ReadMineFilledSlotsForFixture(
            object? mine)
        {
            object? inventory =
                mine == null
                    ? null
                    : ReadMember(mine, "inventory");
            return inventory == null
                ? -1
                : ReadIntMember(inventory, "filledCount", -1);
        }

        private static string DescribeMineNativeInventoryForFixture(
            object? mine)
        {
            object? inventory =
                mine == null
                    ? null
                    : ReadMember(mine, "inventory");
            if (inventory == null)
                return "inventory=missing";

            int capacity =
                ReadIntMember(inventory, "capacity", -1);
            int filled =
                ReadIntMember(inventory, "filledCount", -1);
            int lineCapacity =
                mine == null
                    ? -1
                    : ReadIntMember(mine, "lineCapacity", -1);
            var items = new List<string>();
            for (int slot = 0; slot < capacity; slot++)
            {
                object? item =
                    ReadInventoryItemForFixture(inventory, slot);
                if (item == null)
                    continue;
                string itemId =
                    FirstNonEmpty(
                        ReadStringMember(item, "name", string.Empty),
                        ReadStringMember(item, "Name", string.Empty),
                        item.GetType().Name);
                int count =
                    ReadIntMember(item, "count", 0);
                items.Add(
                    slot.ToString(CultureInfo.InvariantCulture) +
                    ":" + itemId + "x" +
                    count.ToString(CultureInfo.InvariantCulture));
            }

            return "filled=" + filled +
                ", capacity=" + capacity +
                ", lineCapacity=" + lineCapacity +
                ", items=" +
                (items.Count == 0
                    ? "none"
                    : string.Join("|", items.ToArray()));
        }

        private static string DescribeMineNativeTechRouteForFixture(
            Type dolocApi)
        {
            object? assets = ReadStaticMember(dolocApi, "assets");
            object? techTrees =
                assets == null
                    ? null
                    : ReadMember(assets, "techTrees");
            object? graphs =
                techTrees == null
                    ? null
                    : ReadMember(techTrees, "AllTreeGraphs");
            if (!(graphs is IEnumerable enumerableGraphs))
                return "pending:missing-native-techtrees";

            foreach (object graph in enumerableGraphs)
            {
                object? nodesObject = ReadMember(graph, "nodes");
                if (!(nodesObject is IEnumerable nodes))
                    continue;
                object? mineNode = null;
                object? parentNode = null;
                object? commanderNode = null;
                foreach (object node in nodes)
                {
                    string id =
                        ReadStringMember(node, "id", string.Empty);
                    if (id.Equals(
                            "dtmapi_mine",
                            StringComparison.OrdinalIgnoreCase))
                        mineNode = node;
                    if (id.Equals(
                            "alloy_material",
                            StringComparison.OrdinalIgnoreCase))
                        parentNode = node;
                    object? data = ReadMember(node, "data");
                    string title =
                        data == null
                            ? string.Empty
                            : ReadStringMember(
                                data,
                                "Title",
                                string.Empty);
                    if (ContainsIgnoreCase(title, "指挥官") ||
                        ContainsIgnoreCase(title, "Commander"))
                        commanderNode = node;
                }
                if (mineNode == null)
                    continue;

                object? mineData = ReadMember(mineNode, "data");
                object? minePos = ReadMember(mineNode, "pos");
                object? parentPos =
                    parentNode == null
                        ? null
                        : ReadMember(parentNode, "pos");
                object? commanderPos =
                    commanderNode == null
                        ? null
                        : ReadMember(commanderNode, "pos");
                int x =
                    minePos == null
                        ? int.MinValue
                        : ReadIntMember(minePos, "x", int.MinValue);
                int y =
                    minePos == null
                        ? int.MinValue
                        : ReadIntMember(minePos, "y", int.MinValue);
                bool rightOfParent =
                    parentPos != null &&
                    x > ReadIntMember(
                        parentPos,
                        "x",
                        int.MaxValue);
                bool aboveCommander =
                    commanderPos != null &&
                    y < ReadIntMember(
                        commanderPos,
                        "y",
                        int.MinValue);
                object? equipmentEntries =
                    mineData == null
                        ? null
                        : ReadMember(mineData, "equipments");
                object? recipeEntries =
                    mineData == null
                        ? null
                        : ReadMember(mineData, "recipes");
                object? costs =
                    mineData == null
                        ? null
                        : ReadMember(mineData, "costs");
                int equipmentCount =
                    CountMineEnumerableItemsForFixture(
                        equipmentEntries);
                int recipeCount =
                    CountMineEnumerableItemsForFixture(recipeEntries);
                return "verified-native, node=dtmapi_mine" +
                    ", tree=" +
                    ReadStringMember(
                        graph,
                        "id",
                        "industrial_techtree") +
                    ", parent=alloy_material" +
                    ", rightOfParent=" + rightOfParent +
                    ", aboveCommander=" + aboveCommander +
                    ", unlockEntries=" +
                    (equipmentCount == 0 && recipeCount == 1
                        ? "recipe-only"
                        : "mixed") +
                    ", equipmentEntries=" + equipmentCount +
                    ", recipeEntries=" + recipeCount +
                    ", costValues=" +
                    DescribeMineTechCostsForFixture(costs);
            }

            return "pending:missing-node=dtmapi_mine";
        }

        private static int CountMineEnumerableItemsForFixture(
            object? value)
        {
            if (!(value is IEnumerable items))
                return 0;
            int count = 0;
            foreach (object _ in items)
                count++;
            return count;
        }

        private static string DescribeMineTechCostsForFixture(
            object? value)
        {
            if (!(value is IEnumerable costs))
                return "none";
            var values = new List<string>();
            foreach (object? cost in costs)
            {
                if (cost == null)
                    continue;
                object? type = ReadMember(cost, "type");
                string id =
                    type == null
                        ? "unknown"
                        : FirstNonEmpty(
                            ReadStringMember(
                                type,
                                "id",
                                string.Empty),
                            ReadStringMember(
                                type,
                                "Id",
                                string.Empty),
                            type.ToString() ?? "unknown");
                values.Add(
                    id + ":" +
                    ReadIntMember(cost, "count", -1)
                        .ToString(
                            CultureInfo.InvariantCulture));
            }
            return values.Count == 0
                ? "none"
                : string.Join("|", values.ToArray());
        }


        private string TryExerciseMineOfficialJsonForFixture(Type dolocApi)
        {
            if (!TryGetNativeItemProto(dolocApi, "dtmapi_mine", out object? itemProto, out string itemProbe))
            {
                string reloadDetail;
                if (!TryReloadOfficialModsAndConfig("Smoke.NewContentMineOfficialJson", "missing dtmapi_mine before Mine metadata smoke", out reloadDetail))
                    throw new InvalidOperationException("dtmapi_mine is not present in the native item table before Mine metadata smoke, and official reload failed. probe={" + itemProbe + "}, reload={" + reloadDetail + "}");

                if (!TryGetNativeItemProto(dolocApi, "dtmapi_mine", out itemProto, out itemProbe))
                    throw new InvalidOperationException("dtmapi_mine is not present in the native item table after official reload. probe={" + itemProbe + "}, reload={" + reloadDetail + "}");
            }

            object? equipmentProto = QueryEquipmentProtoForFixture(dolocApi, "dtmapi_mine");
            object? wellProto = QueryEquipmentProtoForFixture(dolocApi, "well");
            if (!TryGetNativeRecipeProto(dolocApi, "dtmapi_mine", out object? recipeProto, out string recipeProbe))
                throw new InvalidOperationException("dtmapi_mine recipe is not present in the native recipe table. probe={" + recipeProbe + "}");
            object? generatedMineItem = GenerateItemForFixture(dolocApi, "dtmapi_mine");
            string generatedMineItemType = generatedMineItem == null ? "null" : generatedMineItem.GetType().FullName ?? generatedMineItem.GetType().Name;
            bool generatedMineIsItemEquipment = generatedMineItem != null && IsTypeOrBase(generatedMineItem.GetType(), "DolocTown.ItemEquipment");

            object? recipeGroup = GetDolocConfigDataMapValueForFixture("TbRecipeGroup", "equipment_workbench");
            IContentItemInfo? sourceInfo = runtime.GetIndexedContentItem("dtmapi_mine");
            object equipmentObject = equipmentProto ?? new object();
            object wellObject = wellProto ?? new object();
            object recipeObject = recipeProto ?? new object();

            string title = FirstNonEmpty(ReadAnyStringMember(itemProto!, string.Empty, "Title", "title"), "dtmapi_mine");
            string description = ReadAnyStringMember(itemProto!, string.Empty, "DescriptionBasic", "description_basic");
            string subType = ReadAnyStringMember(itemProto!, string.Empty, "SubType", "sub_type");
            string itemFunction = ReadAnyMember(itemProto!, "Function", "function")?.GetType().Name ?? "none";
            int overlay = ReadAnyIntMember(itemProto!, -1, "Overlay", "overlay");
            int buyingPrice = ReadAnyIntMember(itemProto!, -1, "BuyingPrice", "buying_price");
            bool viewable = ReadAnyBoolMember(itemProto!, false, "Viewable", "viewable");
            string indexedIcon = sourceInfo?.IconAssetKey ?? string.Empty;

            (int mineWidth, int mineHeight) = ReadVector2IntForFixture(ReadAnyMember(equipmentObject, "CoverSize", "cover_size"));
            (int wellWidth, int wellHeight) = ReadVector2IntForFixture(ReadAnyMember(wellObject, "CoverSize", "cover_size"));
            string sceneAsset = ReadAnyStringMember(ReadAnyMember(equipmentObject, "SceneAsset", "scene_asset") ?? new object(), string.Empty, "AssetUrl", "url");
            object? equipmentFunctionObject = ReadAnyMember(equipmentObject, "Function", "function");
            string equipmentFunction = equipmentFunctionObject?.GetType().Name ?? "none";
            int caseTotalCapacity = ReadAnyIntMember(equipmentFunctionObject ?? new object(), -1, "TotalCapacity", "total_capacity");
            int caseLineCapacity = ReadAnyIntMember(equipmentFunctionObject ?? new object(), -1, "LineCapacity", "line_capacity");
            object? electronicComponentObject = ReadAnyMember(equipmentObject, "ElectronicComponent", "electronic_component");
            string electronicComponent = electronicComponentObject?.GetType().Name ?? "none";
            int electronicThreshold = ReadAnyIntMember(electronicComponentObject ?? new object(), -1, "Threshold", "threshold");
            string outputItem = ReadAnyStringMember(ReadAnyMember(recipeObject, "OutputItem", "output_item") ?? new object(), string.Empty, "itemName", "ItemName", "item_name");
            int techPoint = ReadAnyIntMember(recipeObject, -1, "TechPoint", "tech_point");
            bool defaultUnlock = ReadAnyBoolMember(recipeObject, false, "DefaultUnlock", "default_unlock");
            bool groupIncludesRecipe = recipeGroup != null && ReadStringValues(ReadAnyMember(recipeGroup, "RecipeIds", "recipe_ids")).Any(v => v.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase));
            string inputs = DescribeCountItemsForFixture(ReadAnyMember(recipeObject, "InputItems", "input_items"));
            string nativeTechTreeSummary = DescribeMineNativeTechRouteForFixture(dolocApi);
            string productNativeOwnerSummary = ObserveMineProductNativeOwnerForFixture();

            var failures = new List<string>();
            if (equipmentProto == null)
                failures.Add("missing-equipment-proto");
            if (wellProto == null)
                failures.Add("missing-well-proto");
            if (sourceInfo != null &&
                !sourceInfo.SourceKind.Equals(
                    "DTMAPI",
                    StringComparison.OrdinalIgnoreCase))
            {
                failures.Add(
                    "unexpected-indexed-source:" +
                    sourceInfo.SourceKind);
            }
            if (!ContainsIgnoreCase(title, "矿井") && !ContainsIgnoreCase(title, "Mine"))
                failures.Add("missing-title:" + title);
            if (!ContainsIgnoreCase(description, "水井") && !ContainsIgnoreCase(description, "well"))
                failures.Add("missing-well-description");
            if (!subType.Equals("equipment_ornament", StringComparison.OrdinalIgnoreCase))
                failures.Add("unexpected-subtype:" + subType);
            if (!itemFunction.Equals("ItemFunctionEquipment", StringComparison.OrdinalIgnoreCase))
                failures.Add("unexpected-item-function:" + itemFunction);
            if (!generatedMineIsItemEquipment)
                failures.Add("generated-item-not-ItemEquipment:" + generatedMineItemType);
            if (overlay <= 0 || buyingPrice <= 0 || !viewable)
                failures.Add("item-not-viewable-buyable-stackable:overlay=" + overlay + ", buy=" + buyingPrice + ", viewable=" + viewable);
            if (sourceInfo != null &&
                !ContainsIgnoreCase(
                    indexedIcon,
                    "icon_item_well"))
                failures.Add("missing-indexed-icon:" + indexedIcon);
            if (mineWidth != 8 || mineHeight != 6)
                failures.Add("mine-cover-size=" + mineWidth + "x" + mineHeight);
            if (wellWidth <= 0 || wellHeight <= 0 || mineWidth != wellWidth * 2 || mineHeight != wellHeight * 2)
                failures.Add("not-double-well-cover:mine=" + mineWidth + "x" + mineHeight + ", well=" + wellWidth + "x" + wellHeight);
            if (!ContainsIgnoreCase(sceneAsset, "sprite_equipment_well"))
                failures.Add("scene-asset=" + sceneAsset);
            if (!equipmentFunction.Equals("EquipmentFuncCase", StringComparison.OrdinalIgnoreCase))
                failures.Add("unexpected-equipment-function:" + equipmentFunction);
            if (caseTotalCapacity != 16 || caseLineCapacity != 4)
                failures.Add("unexpected-case-storage:" + caseTotalCapacity + "/" + caseLineCapacity);
            if (!electronicComponent.Equals("EComProtoAppliance", StringComparison.OrdinalIgnoreCase) || electronicThreshold != 10)
                failures.Add("unexpected-electronic-component:" + electronicComponent + "/" + electronicThreshold);
            if (!outputItem.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase))
                failures.Add("recipe-output=" + outputItem);
            bool hasOilRecipe = CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "crude_oil", 10) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "metal_framework", 10) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "engine_core", 5) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "steel_ingot", 20);
            bool hasFallbackRecipe = CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "metal_framework", 15) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "engine_core", 10) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "steel_ingot", 20) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "coal", 100);
            if (!hasOilRecipe && !hasFallbackRecipe)
                failures.Add("recipe-inputs=" + inputs);
            if (techPoint != 1 || defaultUnlock)
                failures.Add("recipe-tech-default:tech=" + techPoint + ", defaultUnlock=" + defaultUnlock);
            if (!groupIncludesRecipe)
                failures.Add("equipment_workbench-missing-dtmapi_mine");
            if (!ContainsIgnoreCase(nativeTechTreeSummary, "node=dtmapi_mine") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "parent=alloy_material") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "rightOfParent=True") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "aboveCommander=True") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "unlockEntries=recipe-only") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "equipmentEntries=0") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "recipeEntries=1") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "costValues=") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, ":1") ||
                ContainsIgnoreCase(nativeTechTreeSummary, "pending") ||
                ContainsIgnoreCase(nativeTechTreeSummary, "failed"))
            {
                failures.Add("native-tech-route=" + nativeTechTreeSummary);
            }
            string summary = "item=" + title +
                ", contentIndex=" + (sourceInfo == null ? "supplemental-unavailable" : sourceInfo.SourceKind + "/" + sourceInfo.SourceId + "/" + sourceInfo.SourceModTitle) +
                ", indexedIcon=" + (sourceInfo == null ? "supplemental-unavailable" : indexedIcon) +
                ", itemFunction=" + itemFunction +
                ", subtype=" + subType +
                ", cover=" + mineWidth + "x" + mineHeight +
                ", baseWellCover=" + wellWidth + "x" + wellHeight +
                ", sceneAsset=" + sceneAsset +
                ", equipmentFunction=" + equipmentFunction +
                ", caseStorage=" + caseTotalCapacity + "/" + caseLineCapacity +
                ", electronicComponent=" + electronicComponent + "/" + electronicThreshold +
                ", generatedItemType=" + generatedMineItemType +
                ", recipeOutput=" + outputItem +
                ", recipeInputs=" + inputs +
                ", techPoint=" + techPoint +
                ", defaultUnlock=" + defaultUnlock +
                ", recipeGroup=equipment_workbench includes=" + groupIncludesRecipe +
                ", nativeTech={" + nativeTechTreeSummary + "}" +
                ", productNativeOwner={" + productNativeOwnerSummary + "}" +
                ", probes=item{" + itemProbe + "}, recipe{" + recipeProbe + "}";
            if (failures.Count > 0)
                throw new InvalidOperationException("Mine official JSON/ProductNative smoke failed: " + string.Join(", ", failures) + ". " + summary);

            runtime.RuntimeMonitor.Log("Smoke exercise NewContentMineOfficialJson OK " + summary);
            runtime.SetHookStatus("Smoke.NewContentMineOfficialJson", "verified", "DolocConfig item/equipment/recipe/group + ProductNative exact Harmony owner", summary);
            return summary;
        }

        private static bool TryQueryNativeItemProto(Type dolocApi, string itemId, out string detail)
        {
            return TryGetNativeItemProto(dolocApi, itemId, out _, out detail);
        }

        private static bool TryGetNativeRecipeProto(Type dolocApi, string recipeId, out object? proto, out string detail)
        {
            detail = string.Empty;
            proto = null;
            try
            {
                MethodInfo? queryRecipeProto = dolocApi.GetMethod("QueryRecipeProto", BindingFlags.Public | BindingFlags.Static);
                if (queryRecipeProto == null)
                {
                    detail = "DolocAPI.QueryRecipeProto was not found.";
                    return false;
                }

                object?[] args = new object?[] { recipeId, null };
                bool found = queryRecipeProto.Invoke(null, args) is bool ok && ok && args[1] != null;
                proto = found ? args[1] : null;
                detail = found ? "found " + recipeId + " in DolocConfig.Tables.TbRecipe." : "missing " + recipeId + " in DolocConfig.Tables.TbRecipe.";
                return found;
            }
            catch (Exception ex)
            {
                Exception root = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                detail = root.GetType().Name + ": " + root.Message;
                return false;
            }
        }

        private static bool TryGetNativeItemProto(Type dolocApi, string itemId, out object? proto, out string detail)
        {
            detail = string.Empty;
            proto = null;
            try
            {
                MethodInfo? queryItemProto = dolocApi.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
                if (queryItemProto == null)
                {
                    detail = "DolocAPI.QueryItemProto was not found.";
                    return false;
                }

                object?[] args = new object?[] { itemId, null };
                bool found = queryItemProto.Invoke(null, args) is bool ok && ok && args[1] != null;
                proto = found ? args[1] : null;
                detail = found ? "found " + itemId + " in DolocConfig.Tables.TbItem." : "missing " + itemId + " in DolocConfig.Tables.TbItem.";
                return found;
            }
            catch (Exception ex)
            {
                Exception root = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                detail = root.GetType().Name + ": " + root.Message;
                return false;
            }
        }

        private int FindHighestNativeFuelEnergyExcept(string excludedItemId, out string itemId)
        {
            itemId = string.Empty;
            int highest = 0;
            HashSet<string> indexedContentIds = new HashSet<string>(
                runtime.GetIndexedContentItems()
                    .Select(i => i.ItemId)
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocConfig = patcher.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? tbItem = tables == null ? null : ReadMember(tables, "TbItem");
            object? dataList = tbItem == null ? null : ReadMember(tbItem, "DataList");
            if (!(dataList is IEnumerable enumerable))
                return highest;

            foreach (object proto in enumerable)
            {
                string id = ReadAnyStringMember(proto, string.Empty, "Id", "id");
                if (string.IsNullOrWhiteSpace(id) || id.Equals(excludedItemId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (indexedContentIds.Contains(id))
                    continue;

                int energy = ReadAnyIntMember(proto, 0, "ElectricEnergy", "electric_energy");
                if (energy > highest)
                {
                    highest = energy;
                    itemId = id;
                }
            }

            return highest;
        }

        private object? GetDolocConfigDataMapValueForFixture(string tableName, string id)
        {
            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocConfig = patcher.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig == null ? null : ReadStaticMember(dolocConfig, "Tables");
            object? table = tables == null ? null : ReadMember(tables, tableName);
            object? dataMap = table == null ? null : ReadMember(table, "DataMap");
            foreach (object entry in EnumerateObjects(dataMap))
            {
                object? key = ReadMember(entry, "Key");
                if (key == null || !id.Equals(key.ToString(), StringComparison.OrdinalIgnoreCase))
                    continue;
                return ReadMember(entry, "Value");
            }

            return null;
        }

        private static string DescribeCountItemsForFixture(object? items)
        {
            List<string> parts = new List<string>();
            foreach (object item in EnumerateObjects(items))
            {
                string itemId = ReadAnyStringMember(item, string.Empty, "itemName", "ItemName", "item_name");
                int count = ReadAnyIntMember(item, 0, "itemCount", "ItemCount", "item_count");
                if (!string.IsNullOrWhiteSpace(itemId))
                    parts.Add(itemId + "x" + count);
            }

            return parts.Count == 0 ? "none" : string.Join("|", parts);
        }

        private static bool CountItemsContain(object? items, string itemId, int minimumCount)
        {
            foreach (object item in EnumerateObjects(items))
            {
                string currentId = ReadAnyStringMember(item, string.Empty, "itemName", "ItemName", "item_name");
                int count = ReadAnyIntMember(item, 0, "itemCount", "ItemCount", "item_count");
                if (currentId.Equals(itemId, StringComparison.OrdinalIgnoreCase) && count >= minimumCount)
                    return true;
            }

            return false;
        }

        private static IEnumerable<object> EnumerateObjects(object? value)
        {
            if (value is IEnumerable enumerable && !(value is string))
            {
                foreach (object? item in enumerable)
                {
                    if (item != null)
                        yield return item;
                }
            }
        }

        private object? TryCreateTransientEquipmentForFixture(Type dolocApi, object room, string targetTypeName, IReadOnlyList<string> equipmentIds, out string summary)
        {
            summary = string.Empty;
            Type? hostType = patcher?.ResolveType("DolocTown.IEquipmentHost, Assembly-CSharp");
            if (hostType == null || !hostType.IsInstanceOfType(room))
            {
                summary = "current room is not an IEquipmentHost. room=" + DescribeRoomForFixture(room);
                return null;
            }

            MethodInfo? createEquipment = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateEquipment" && m.GetParameters().Length == 6);
            if (createEquipment == null)
            {
                summary = "IEquipmentHost.CreateEquipment was not found.";
                return null;
            }

            List<string> notes = new List<string>();
            int attempted = 0;
            foreach (string equipmentId in equipmentIds)
            {
                object? proto = QueryEquipmentProtoForFixture(dolocApi, equipmentId);
                if (proto == null)
                {
                    notes.Add(equipmentId + ":missing-proto");
                    continue;
                }

                object? coverSize = ReadMember(proto, "CoverSize");
                int width = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "x", 1));
                int height = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "y", 1));
                foreach ((int x, int y) in EnumerateSmokeEquipmentAnchors(dolocApi, room, width, height))
                {
                    attempted++;
                    object? anchor = CreateVector2IntForFixture(x, y);
                    if (anchor == null)
                        continue;
                    if (!AreEquipmentCellsEmptyForFixture(hostType, room, x, y, width, height))
                        continue;

                    object? worldPosition = CreateEquipmentWorldPositionForFixture(room, x, y, width);
                    if (worldPosition == null)
                        continue;

                    try
                    {
                        object? equipment = createEquipment.Invoke(room, new object?[] { worldPosition, anchor, proto, false, null, -1 });
                        if (equipment == null)
                        {
                            notes.Add(equipmentId + "@" + x + "," + y + ":null");
                            continue;
                        }

                        if (IsTypeOrBase(equipment.GetType(), targetTypeName))
                        {
                            summary = "transient:" + equipmentId + "@" + x + "," + y + ", attempted=" + attempted + ", room=" + DescribeRoomForFixture(room);
                            return equipment;
                        }

                        notes.Add(equipmentId + "@" + x + "," + y + ":wrong-type=" + equipment.GetType().FullName);
                        TryRemoveTransientEquipmentForFixture(room, equipment);
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        notes.Add(equipmentId + "@" + x + "," + y + ":" + ex.InnerException.GetType().Name);
                    }
                    catch (Exception ex)
                    {
                        notes.Add(equipmentId + "@" + x + "," + y + ":" + ex.GetType().Name);
                    }
                }
            }

            summary = "transient-create-failed targetType=" + targetTypeName + ", attempted=" + attempted + ", notes=" + string.Join("|", notes.Take(8));
            return null;
        }

        private object? TryCreateTransientEquipmentNoRenderForFixture(Type dolocApi, object room, string targetTypeName, IReadOnlyList<string> equipmentIds, out string summary)
        {
            summary = string.Empty;
            Type? hostType = patcher?.ResolveType("DolocTown.IEquipmentHost, Assembly-CSharp");
            if (hostType == null || !hostType.IsInstanceOfType(room))
            {
                summary = "room is not an IEquipmentHost. room=" + DescribeRoomForFixture(room);
                return null;
            }

            MethodInfo? createEquipmentNoRender = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateEquipmentNoRender" && m.GetParameters().Length == 4);
            if (createEquipmentNoRender == null)
            {
                summary = "IEquipmentHost.CreateEquipmentNoRender was not found.";
                return null;
            }

            List<string> notes = new List<string>();
            int attempted = 0;
            foreach (string equipmentId in equipmentIds)
            {
                object? proto = QueryEquipmentProtoForFixture(dolocApi, equipmentId);
                if (proto == null)
                {
                    notes.Add(equipmentId + ":missing-proto");
                    continue;
                }

                object? coverSize = ReadMember(proto, "CoverSize");
                int width = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "x", 1));
                int height = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "y", 1));
                foreach ((int x, int y) in EnumerateSmokeEquipmentAnchors(dolocApi, room, width, height))
                {
                    attempted++;
                    object? anchor = CreateVector2IntForFixture(x, y);
                    object? worldPosition = CreateEquipmentWorldPositionForFixture(room, x, y, width);
                    if (anchor == null || worldPosition == null)
                        continue;
                    if (!AreEquipmentCellsEmptyForFixture(hostType, room, x, y, width, height))
                        continue;

                    try
                    {
                        object? equipment = createEquipmentNoRender.Invoke(room, new object?[] { worldPosition, anchor, proto, false });
                        if (equipment == null)
                        {
                            notes.Add(equipmentId + "@" + x + "," + y + ":null");
                            continue;
                        }

                        if (IsTypeOrBase(equipment.GetType(), targetTypeName))
                        {
                            summary = "transient-no-render:" + equipmentId + "@" + x + "," + y + ", attempted=" + attempted + ", room=" + DescribeRoomForFixture(room);
                            return equipment;
                        }

                        notes.Add(equipmentId + "@" + x + "," + y + ":wrong-type=" + equipment.GetType().FullName);
                        TryRemoveTransientEquipmentForFixture(room, equipment);
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        notes.Add(equipmentId + "@" + x + "," + y + ":" + ex.InnerException.GetType().Name);
                    }
                    catch (Exception ex)
                    {
                        notes.Add(equipmentId + "@" + x + "," + y + ":" + ex.GetType().Name);
                    }
                }
            }

            summary = "transient-no-render-create-failed targetType=" + targetTypeName + ", attempted=" + attempted + ", notes=" + string.Join("|", notes.Take(8));
            return null;
        }

        private object? FindChestLocatorSmokeBuildingRoom(Type dolocApi, object archive, out string summary)
        {
            object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
            object? mainFarm = ReadMember(archive, "MainFarm");
            object? buildingManager = mainFarm == null ? null : ReadMember(mainFarm, "DM_building");
            object? buildings = buildingManager == null ? null : ReadMember(buildingManager, "Buildings");
            int scanned = 0;
            var samples = new List<string>();

            if (buildings is IEnumerable enumerable)
            {
                foreach (object? building in enumerable)
                {
                    if (building == null)
                        continue;
                    scanned++;
                    object? room = ReadMember(building, "room");
                    if (room == null)
                    {
                        samples.Add("building" + scanned + ":no-room");
                        continue;
                    }
                    samples.Add(DescribeRoomForFixture(room));
                    if (ReferenceEquals(room, currentRoom))
                        continue;
                    if (ReadMember(room, "DM_equipment") == null)
                        continue;

                    summary = "selected=" + DescribeRoomForFixture(room) + ", scanned=" + scanned + ", current=" + (currentRoom == null ? "none" : DescribeRoomForFixture(currentRoom));
                    return room;
                }
            }

            summary = "scanned=" + scanned + ", current=" + (currentRoom == null ? "none" : DescribeRoomForFixture(currentRoom)) + ", samples=" + string.Join(" | ", samples.Take(5));
            return null;
        }

        private string SelectZeroBaselineSmokeItemId(Type dolocApi, IReadOnlyList<string> candidates, out int baseline, out string summary)
        {
            baseline = 0;
            var notes = new List<string>();
            foreach (string candidate in candidates)
            {
                object? item = GenerateItemForFixture(dolocApi, candidate, 1);
                if (item == null)
                {
                    notes.Add(candidate + ":missing");
                    continue;
                }

                int count = CountNativeItemForFixture(dolocApi, candidate, checkBox: true);
                notes.Add(candidate + ":baseline=" + count);
                if (count == 0)
                {
                    baseline = count;
                    summary = string.Join("|", notes);
                    return candidate;
                }
            }

            summary = string.Join("|", notes);
            return string.Empty;
        }

        private static int CountNativeItemForFixture(Type dolocApi, string itemId, bool checkBox)
        {
            MethodInfo? countItem = dolocApi.GetMethod("CountItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null);
            object? result = countItem == null ? null : countItem.Invoke(null, new object[] { itemId, checkBox });
            return result is int value ? value : -1;
        }

        private static bool CostNativeItemForFixture(Type dolocApi, string itemId, int count, bool checkBox)
        {
            MethodInfo? costItem = dolocApi.GetMethod("CostItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(bool) }, null);
            object? result = costItem == null ? null : costItem.Invoke(null, new object[] { itemId, count, checkBox });
            return result is bool value && value;
        }

        private bool AreEquipmentCellsEmptyForFixture(Type hostType, object room, int anchorX, int anchorY, int width, int height)
        {
            MethodInfo? getEquipment = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "GetEquipment")
                        return false;
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType.FullName == "UnityEngine.Vector2Int";
                });
            if (getEquipment == null)
                return false;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    object? cell = CreateVector2IntForFixture(anchorX + x, anchorY + y);
                    if (cell == null)
                        return false;
                    object? existing = getEquipment.Invoke(room, new[] { cell });
                    if (existing != null)
                        return false;
                }
            }
            return true;
        }

        private object? CreateEquipmentWorldPositionForFixture(object room, int anchorX, int anchorY, int width)
        {
            object? roomPosition = ReadMember(room, "RoomPosition");
            double roomX = ReadDoubleMember(roomPosition, "x", 0);
            double roomY = ReadDoubleMember(roomPosition, "y", 0);
            double worldX = roomX + (anchorX + width * 0.5) * 1.5;
            double worldY = roomY + anchorY * 1.5;
            return CreateVector3ForFixture(worldX, worldY, 0);
        }

        private object? CreateVector2IntForFixture(int x, int y)
        {
            Type? vector2Int = patcher?.ResolveType("UnityEngine.Vector2Int, UnityEngine.CoreModule") ??
                patcher?.ResolveType("UnityEngine.Vector2Int, UnityEngine");
            return vector2Int == null ? null : Activator.CreateInstance(vector2Int, new object[] { x, y });
        }

        private object? CreateVector2ForFixture(double x, double y)
        {
            Type? vector2 = patcher?.ResolveType("UnityEngine.Vector2, UnityEngine.CoreModule") ??
                patcher?.ResolveType("UnityEngine.Vector2, UnityEngine");
            return vector2 == null ? null : Activator.CreateInstance(vector2, new object[] { (float)x, (float)y });
        }

        private object? CreateVector3ForFixture(double x, double y, double z)
        {
            Type? vector3 = patcher?.ResolveType("UnityEngine.Vector3, UnityEngine.CoreModule") ??
                patcher?.ResolveType("UnityEngine.Vector3, UnityEngine");
            return vector3 == null ? null : Activator.CreateInstance(vector3, new object[] { (float)x, (float)y, (float)z });
        }

        private bool TryRemoveTransientEquipmentForFixture(object? room, object equipment)
        {
            try
            {
                Type? hostType = patcher?.ResolveType("DolocTown.IEquipmentHost, Assembly-CSharp");
                MethodInfo? removeEquipment = hostType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "RemoveEquipment" && m.GetParameters().Length == 4);
                MethodInfo? getEquipment = hostType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "GetEquipment")
                            return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType.FullName == "UnityEngine.Vector2Int";
                    });
                object? anchor = ReadMember(equipment, "Anchor");
                if (room == null || hostType == null || !hostType.IsInstanceOfType(room) || removeEquipment == null || getEquipment == null || anchor == null)
                    return false;

                object? proto = ReadMember(equipment, "proto") ?? ReadMember(equipment, "Proto");
                object? coverSize = proto == null ? null : ReadMember(proto, "CoverSize");
                int width = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "x", 1));
                int height = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "y", 1));
                int anchorX = ReadIntMember(anchor, "x", int.MinValue);
                int anchorY = ReadIntMember(anchor, "y", int.MinValue);
                if (anchorX == int.MinValue || anchorY == int.MinValue)
                    return false;

                removeEquipment.Invoke(room, new object?[] { equipment, false, false, true });
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        object? cell = CreateVector2IntForFixture(anchorX + x, anchorY + y);
                        if (cell == null)
                            return false;
                        object? remaining = getEquipment.Invoke(room, new[] { cell });
                        if (ReferenceEquals(remaining, equipment))
                            return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke transient equipment removal failed.", ex.ToString());
                return false;
            }
        }

        private static string DescribeEquipmentForFixture(object equipment)
        {
            object? anchor = ReadMember(equipment, "Anchor");
            object? proto = ReadMember(equipment, "proto") ?? ReadMember(equipment, "Proto");
            object? coverSize = proto == null ? null : ReadMember(proto, "CoverSize");
            object? sceneAsset = proto == null ? null : ReadMember(proto, "SceneAsset");
            object? function = proto == null ? null : ReadMember(proto, "Function");
            object? renderer = ReadMember(equipment, "Renderer");
            object? transform = renderer == null ? null : ReadMember(renderer, "transform");
            object? localScale = transform == null ? null : ReadMember(transform, "localScale");
            object? inventory = ReadMember(equipment, "inventory");
            string name = ReadStringMember(equipment, "Name", ReadStringMember(equipment, "Title", equipment.GetType().Name));
            int index = ReadIntMember(equipment, "index", -1);
            string anchorText = anchor == null ? "unknown" : ReadIntMember(anchor, "x", 0) + "," + ReadIntMember(anchor, "y", 0);
            string coverText = coverSize == null ? "unknown" : ReadIntMember(coverSize, "x", 0) + "x" + ReadIntMember(coverSize, "y", 0);
            string sceneText = sceneAsset == null ? "unknown" : FirstNonEmpty(ReadStringMember(sceneAsset, "AssetUrl", string.Empty), sceneAsset.ToString() ?? string.Empty);
            string scaleText = localScale == null ? "unknown" : ReadDoubleMember(localScale, "x", 0).ToString("0.##") + "x" + ReadDoubleMember(localScale, "y", 0).ToString("0.##");
            string storageText = inventory == null
                ? "none"
                : ReadIntMember(inventory, "filledCount", 0) + "/" + ReadIntMember(inventory, "capacity", 0) + "/line=" + ReadIntMember(equipment, "lineCapacity", 0);
            return name + "/" + (equipment.GetType().FullName ?? equipment.GetType().Name) + "/index=" + index + "/anchor=" + anchorText + "/cover=" + coverText + "/scene=" + sceneText + "/function=" + (function == null ? "unknown" : function.GetType().Name) + "/rendererScale=" + scaleText + "/storage=" + storageText;
        }

        [DataContract]
        private sealed class EquipmentHatTableDiagnostic
        {
            [DataMember(Order = 0)] public string CapturedAt { get; set; } = string.Empty;
            [DataMember(Order = 1)] public string Source { get; set; } = string.Empty;
            [DataMember(Order = 2)] public int HatCount { get; set; }
            [DataMember(Order = 3)] public int ItemHatCount { get; set; }
            [DataMember(Order = 4)] public int HatsWithoutItemRows { get; set; }
            [DataMember(Order = 5)] public int ItemRowsWithoutHatInfo { get; set; }
            [DataMember(Order = 6)] public List<EquipmentHatDiagnosticRow> Hats { get; set; } = new List<EquipmentHatDiagnosticRow>();
            [DataMember(Order = 7)] public List<EquipmentHatItemDiagnosticRow> ItemRowsWithoutHatInfoDetails { get; set; } = new List<EquipmentHatItemDiagnosticRow>();
        }

        [DataContract]
        private sealed class EquipmentHatDiagnosticRow
        {
            [DataMember(Order = 0)] public string HatInfoId { get; set; } = string.Empty;
            [DataMember(Order = 1)] public string Skill { get; set; } = string.Empty;
            [DataMember(Order = 2)] public int Defense { get; set; }
            [DataMember(Order = 3)] public string SkillRefId { get; set; } = string.Empty;
            [DataMember(Order = 4)] public string SkillRefGearEntry { get; set; } = string.Empty;
            [DataMember(Order = 5)] public string SkillRefFunctionType { get; set; } = string.Empty;
            [DataMember(Order = 6)] public List<EquipmentHatItemDiagnosticRow> ItemRows { get; set; } = new List<EquipmentHatItemDiagnosticRow>();
        }

        [DataContract]
        private sealed class EquipmentHatItemDiagnosticRow
        {
            [DataMember(Order = 0)] public string ItemId { get; set; } = string.Empty;
            [DataMember(Order = 1)] public string FunctionType { get; set; } = string.Empty;
            [DataMember(Order = 2)] public string HatId { get; set; } = string.Empty;
            [DataMember(Order = 3)] public bool HatIdRefResolved { get; set; }
        }
    }
}
