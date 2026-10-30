using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using Native = DTMAPI.DebugConsole.DebugConsoleNativeAccess;

namespace DTMAPI.DebugConsole
{
    // Product-local results: failures can overlap a confirmed new maturity after a native notification throws.
    internal sealed class RoomCropMaturityResult
    {
        public int Radius { get; set; }
        public int CandidateBasins { get; set; }
        public int GrassBasins { get; set; }
        public List<CropMaturityObservation> Targets { get; } = new List<CropMaturityObservation>();
        public string FailureReason { get; set; } = string.Empty;
        public int CropsMatured => Targets.Count(t => t.NewlyMature && t.Family != "grass");
        public int GrassMatured => Targets.Count(t => t.NewlyMature && t.Family == "grass");
        public int AlreadyMature => Targets.Count(t => t.Outcome == "already-mature");
        public int EmptyOrDead => Targets.Count(t => t.Outcome == "empty-or-dead");
        public int Invalidated => Targets.Count(t => t.Outcome == "invalidated");
        public int Failed => Targets.Count(t => t.Outcome == "failed");
        public int Unexecuted => Targets.Count(t => t.Outcome == "unexecuted");
        public bool Success => FailureReason.Length == 0 && Failed == 0 && Invalidated == 0 && Unexecuted == 0;
    }

    internal sealed class CropMaturityObservation
    {
        public int EquipmentId { get; set; }
        public string Family { get; set; } = string.Empty;
        public int GrassIndex { get; set; } = -1;
        public int? BeforeLevel { get; set; }
        public int? AfterLevel { get; set; }
        public bool NewlyMature { get; set; }
        public bool? FruitReady { get; set; }
        public string Outcome { get; set; } = "unexecuted";
        public string Error { get; set; } = string.Empty;
    }

    internal sealed partial class DebugConsoleNativeActions
    {
        private IDtmRuntimeContext? cropMaturityContext;
        private bool maturingCrops;
        internal void SetCropMaturityContext(IDtmRuntimeContext context) => cropMaturityContext = context;

        public RoomCropMaturityResult MatureAllCrops(IManifest owner, int radius)
        {
            var result = new RoomCropMaturityResult { Radius = DebugConsoleConfig.NormalizeCropRadius(radius) };
            if (maturingCrops)
            {
                result.FailureReason = "maturity-request-already-running";
                return result;
            }
            maturingCrops = true;
            try
            {
                IDtmRuntimeContext context = cropMaturityContext ?? throw new InvalidOperationException("missing-runtime-context");
                DtmRuntimeContextSnapshot epoch = context.Snapshot;
                object archive = Native.Archive ?? throw new InvalidOperationException("missing-archive");
                object room = Native.CurrentRoom ?? throw new InvalidOperationException("missing-current-room");
                object manager = CropRead(room, "DM_equipment");
                bool Current() => context.IsMainThread && IsCurrentSaveSession &&
                    context.Snapshot.IsWorldReady && epoch.IsWorldReady &&
                    context.Snapshot.RuntimeInstanceId == epoch.RuntimeInstanceId &&
                    context.Snapshot.SaveSessionEpoch == epoch.SaveSessionEpoch &&
                    context.Snapshot.WorldEpoch == epoch.WorldEpoch &&
                    ReferenceEquals(archive, Native.Archive) && ReferenceEquals(room, Native.CurrentRoom) &&
                    ReferenceEquals(manager, Native.Read(room, "DM_equipment"));
                if (!Current()) throw new InvalidOperationException("inactive-save-or-room");
                // Room.AgentPositionCell is refreshed by the native timed room update and can lag movement or load.
                // Read the existing live same-room conversion once, including when the Y modal has paused updates.
                object api = Native.Resolve("DolocAPI, Assembly-CSharp") ?? throw new InvalidOperationException("missing-native-api");
                (int x, int y) center = CropCell(CropRead(api, "AgentRoomCellPosition"));
                object[] equipments = ((IEnumerable)CropRead(manager, "AllEquipments")).Cast<object>()
                    .Where(e => e != null).Distinct(CropReferenceComparer.Instance).ToArray();
                var targets = new List<CropTarget>();
                foreach (object equipment in equipments)
                {
                    string? family = CropFamily(equipment);
                    if (family == null || !ReferenceEquals(room, Native.Read(equipment, "CurrentRoom")) ||
                        CropBool(equipment, "IsRemoved")) continue;
                    (int x, int y) anchor = CropCell(CropRead(equipment, "Anchor"));
                    long distance = CropDistance(equipment, center, anchor);
                    if (distance > (long)result.Radius * result.Radius) continue;
                    result.CandidateBasins++;
                    int id = CropInt(equipment, "id");
                    if (family == "grass")
                    {
                        result.GrassBasins++;
                        Array? grasses = Native.Read(equipment, "crops") as Array;
                        if (grasses == null || grasses.Length == 0)
                            targets.Add(new CropTarget(equipment, null, family, id, distance, anchor, grasses, -1));
                        else
                            for (int i = 0; i < grasses.Length; i++)
                                targets.Add(new CropTarget(equipment, grasses.GetValue(i), family, id, distance, anchor, grasses, i));
                    }
                    else targets.Add(new CropTarget(equipment, Native.Read(equipment, "Crop"), family, id, distance, anchor, null, -1));
                }
                targets = targets.OrderBy(t => t.Distance).ThenBy(t => t.X).ThenBy(t => t.Y)
                    .ThenBy(t => t.Observation.EquipmentId).ThenBy(t => t.Observation.GrassIndex).ToList();
                result.Targets.AddRange(targets.Select(t => t.Observation));
                foreach (CropTarget target in targets)
                {
                    if (!Current()) { result.FailureReason = "save-room-or-owner-ended"; break; }
                    CropMaturityObservation observation = target.Observation;
                    try
                    {
                        if (!CropTargetCurrent(target, room, manager))
                        { observation.Outcome = "invalidated"; continue; }
                        if (target.Crop == null || CropDead(target))
                        { observation.Outcome = "empty-or-dead"; continue; }
                        int level = CropLevel(target);
                        int matureLevel = CropInt(CropRead(target.Crop, target.Family == "tree" ? "protoTree" : "seedProto"),
                            target.Family == "tree" ? "MaxLevel" : "MatureLevel");
                        if (matureLevel < 0 || level < 0 || level > matureLevel)
                            throw new InvalidOperationException("invalid-native-growth-level");
                        observation.BeforeLevel = level;
                        observation.AfterLevel = level;
                        bool wasMature = CropMature(target);
                        if (wasMature)
                        {
                            if (level != matureLevel) throw new InvalidOperationException("inconsistent-native-maturity");
                            ObserveFruit(target);
                            observation.Outcome = "already-mature";
                            continue;
                        }
                        Exception? failure = null;
                        try
                        {
                            if (target.Family == "crop")
                                CropMethod(target.Crop, "DEBUG_SetLevel", typeof(bool), typeof(int))
                                    .Invoke(target.Crop, new object[] { CropBool(target.Equipment, "IsRender"), matureLevel });
                            else CropMethod(target.Crop, "DEBUG_SetLevel", typeof(int))
                                .Invoke(target.Crop, new object[] { matureLevel });
                        }
                        catch (Exception error) { failure = error.GetBaseException(); }
                        // Read the same object even if notification code ended the session. Never replay or undo it.
                        observation.AfterLevel = CropLevel(target);
                        observation.NewlyMature = !CropDead(target) && CropMature(target) && observation.AfterLevel == matureLevel;
                        ObserveFruit(target);
                        bool attached = Current() && CropTargetCurrent(target, room, manager);
                        if (failure != null) throw failure;
                        if (!attached) observation.Outcome = "invalidated";
                        else if (!observation.NewlyMature) throw new InvalidOperationException("native-maturity-postcondition-failed");
                        else observation.Outcome = "matured";
                    }
                    catch (Exception error)
                    {
                        observation.Outcome = "failed";
                        observation.Error = error.GetBaseException().Message;
                        runtime.Error("mature-crop-" + observation.EquipmentId, error.GetBaseException());
                    }
                }
                if (result.FailureReason.Length == 0 && result.Failed > 0) result.FailureReason = "partial-native-failure";
                else if (result.FailureReason.Length == 0 && result.Invalidated > 0) result.FailureReason = "candidate-invalidated";
            }
            catch (Exception error)
            {
                result.FailureReason = error.GetBaseException().Message;
                runtime.Error("mature-crops", error.GetBaseException());
            }
            finally { maturingCrops = false; }
            LogMutation("mature-crops", result.Success,
                "radius=" + result.Radius + " basins=" + result.CandidateBasins + " grassBasins=" + result.GrassBasins +
                " targets=" + result.Targets.Count + " newCrops=" + result.CropsMatured + " newGrass=" + result.GrassMatured +
                " already=" + result.AlreadyMature + " emptyDead=" + result.EmptyOrDead + " invalid=" + result.Invalidated +
                " failed=" + result.Failed + " unexecuted=" + result.Unexecuted + " reason=" + result.FailureReason);
            return result;
        }

        private static bool CropTargetCurrent(CropTarget target, object room, object manager)
        {
            if (CropBool(target.Equipment, "IsRemoved") || !ReferenceEquals(room, Native.Read(target.Equipment, "CurrentRoom")) ||
                CropInt(target.Equipment, "id") != target.Observation.EquipmentId) return false;
            // Native GetEquipment(int) indexes its compact list, not the recycled equipment id.
            // Recheck membership by reference without expanding the fixed batch or following another object with this id.
            if (!((IEnumerable)CropRead(manager, "AllEquipments")).Cast<object>()
                .Any(e => ReferenceEquals(e, target.Equipment))) return false;
            if (target.Family != "grass") return ReferenceEquals(target.Crop, Native.Read(target.Equipment, "Crop"));
            Array? array = Native.Read(target.Equipment, "crops") as Array;
            return ReferenceEquals(array, target.GrassArray) &&
                (target.Observation.GrassIndex < 0 || (array != null && target.Observation.GrassIndex < array.Length &&
                    ReferenceEquals(target.Crop, array.GetValue(target.Observation.GrassIndex))));
        }

        private static bool CropDead(CropTarget t) => t.Family == "crop" ? CropBool(t.Crop!, "isDead") :
            t.Family == "tree" && CropInt(t.Crop!, "currentHealth") <= 0;
        private static bool CropMature(CropTarget t) => CropBool(t.Crop!, t.Family == "grass" ? "IsMature" : "isMature");
        private static int CropLevel(CropTarget t) => CropInt(t.Crop!, t.Family == "grass" ? "currentLevel" : "CurrentLevel");
        private static void ObserveFruit(CropTarget t)
        {
            if (t.Family == "tree")
                t.Observation.FruitReady = CropBool(t.Crop!, "HasTreeCrop") && CropBool(t.Crop!, "ShouldRepeat") &&
                    CropMature(t) && !CropDead(t) && !CropBool(t.Equipment, "IsRemoved");
        }
        private static string? CropFamily(object equipment)
        {
            for (Type? type = equipment.GetType(); type != null; type = type.BaseType)
            {
                if (type.FullName == "DolocTown.PlantBasinGrass") return "grass";
                if (type.FullName == "DolocTown.PlantBasinTree") return "tree";
                if (type.FullName == "DolocTown.PlantBasin") return "crop";
            }
            return null;
        }
        private static object CropRead(object o, string name) => Native.Read(o, name) ?? throw new MissingMemberException(o.GetType().FullName, name);
        private static int CropInt(object o, string name) => (int)CropRead(o, name);
        private static bool CropBool(object o, string name) => (bool)CropRead(o, name);
        private static (int x, int y) CropCell(object o) => (CropInt(o, "x"), CropInt(o, "y"));
        private static MethodInfo CropMethod(object o, string name, params Type[] parameters) => o.GetType().GetMethod(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null) ??
            throw new MissingMethodException(o.GetType().FullName, name);
        private static long CropDistance(object equipment, (int x, int y) center, (int x, int y) anchor)
        {
            long Squared((int x, int y) p)
            {
                long dx = (long)p.x - center.x, dy = (long)p.y - center.y;
                // Saturate far-away coordinates before squaring; supported radii never exceed 100.
                return Math.Abs(dx) > 100 || Math.Abs(dy) > 100 ? long.MaxValue : dx * dx + dy * dy;
            }
            Array? covered = Native.Read(equipment, "CoveredPositions") as Array;
            if (covered == null || covered.Length == 0) return Squared(anchor);
            long nearest = long.MaxValue;
            foreach (object cell in covered) nearest = Math.Min(nearest, Squared(CropCell(cell)));
            return nearest;
        }
        private sealed class CropTarget
        {
            internal readonly object Equipment;
            internal readonly object? Crop;
            internal readonly string Family;
            internal readonly Array? GrassArray;
            internal readonly long Distance;
            internal readonly int X, Y;
            internal readonly CropMaturityObservation Observation;
            internal CropTarget(object equipment, object? crop, string family, int id, long distance,
                (int x, int y) anchor, Array? grasses, int index)
            {
                Equipment = equipment; Crop = crop; Family = family; GrassArray = grasses; Distance = distance;
                X = anchor.x; Y = anchor.y;
                Observation = new CropMaturityObservation { EquipmentId = id, Family = family, GrassIndex = index };
            }
        }
        private sealed class CropReferenceComparer : IEqualityComparer<object>
        {
            internal static readonly CropReferenceComparer Instance = new CropReferenceComparer();
            public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
