using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using Native = DTMAPI.DebugConsole.DebugConsoleNativeAccess;

namespace DTMAPI.DebugConsole
{
    internal sealed partial class DebugConsoleNativeActions
    {
        public TimeSkipResult AdvanceTime(
            IManifest owner,
            AdvancedTimeAdvanceKind kind,
            int amount)
        {
            amount = Math.Max(1, Math.Min(52, amount));
            object? global = Native.Read(
                Native.Resolve("DolocAPI, Assembly-CSharp"),
                "GlobalParameter");
            int hoursPerDay = Math.Max(1, Native.Int(global, "Day2Hour", 24));
            int minutesPerHour = Math.Max(1, Native.Int(global, "Hour2Min", 60));
            int days = kind == AdvancedTimeAdvanceKind.Week
                ? amount * 7
                : kind == AdvancedTimeAdvanceKind.Month
                    ? amount * 28
                    : amount;
            int minutes = checked(days * hoursPerDay * minutesPerHour);
            return AdvanceMinutes(
                owner,
                minutes,
                kind.ToString().ToLowerInvariant(),
                GetTimeState().Hour);
        }

        public DebugValueResult AddMoney(IManifest owner, int amount)
        {
            amount = Math.Max(1, Math.Min(100000, amount));
            var result = new DebugValueResult
            {
                ValueId = "money",
                RequestedDelta = amount
            };
            try
            {
                object? archive = Native.Archive;
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                if (archive == null || api == null)
                    return Fail(result, "missing-archive", "Native archive is unavailable.");
                result.BeforeValue = Native.Int(archive, "CurrentMoney");
                MethodInfo? command = Native.Method(api, "Command_AddMoney", 1, true);
                if (command != null)
                    command.Invoke(null, new object[] { amount });
                else if (!Native.Write(archive, "CurrentMoney", result.BeforeValue + amount))
                    return Fail(result, "missing-money-owner", "Native money command and CurrentMoney setter are unavailable.");
                result.AfterValue = Native.Int(
                    archive,
                    "CurrentMoney",
                    result.BeforeValue + amount);
                result.Success = result.AfterValue >= result.BeforeValue + amount;
                result.Message = "Money +" + amount +
                    " before=" + result.BeforeValue +
                    " after=" + result.AfterValue + ".";
                LogMutation("money", result.Success, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("money", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        public DebugValueResult AddTechPoint(
            IManifest owner,
            string pointTypeId,
            int amount)
        {
            pointTypeId = (pointTypeId ?? string.Empty).Trim();
            amount = Math.Max(1, Math.Min(1000, amount));
            var result = new DebugValueResult
            {
                ValueId = pointTypeId,
                RequestedDelta = amount
            };
            try
            {
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                Type? type = Native.Resolve(
                    "DolocTown.Config.TechTree.TechPointType, Assembly-CSharp");
                if (api == null || type?.IsEnum != true)
                    return Fail(result, "missing-tech-point-owner", "Native tech-point owner is unavailable.");
                if (!Enum.GetNames(type).Any(name =>
                        name.Equals(pointTypeId, StringComparison.OrdinalIgnoreCase)))
                    return Fail(result, "not-whitelisted", "Unknown tech-point type.");
                object value = Enum.Parse(type, pointTypeId, true);
                ReadTechPoint(value, out int before, out _);
                result.BeforeValue = before;
                MethodInfo? add = ResolveAddTechPointMethod(api);
                if (add == null)
                    return Fail(result, "missing-add-tech-point", "DolocAPI.AddTechPoint is unavailable.");
                add.Invoke(null, new[] { value, (object)amount });
                ReadTechPoint(value, out int after, out _);
                result.AfterValue = after;
                result.Success = after >= before + amount;
                result.Message = pointTypeId + " +" + amount +
                    " before=" + before +
                    " after=" + after + ".";
                LogMutation("tech-point", result.Success, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("tech-point", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        public DebugCommandResult UnlockAllTechTrees(IManifest owner)
        {
            var result = new DebugCommandResult
            {
                CommandId = "unlock-all-tech-trees"
            };
            try
            {
                object? farm = Native.Read(Native.Archive, "farmData");
                object? unlocked = Native.Read(farm, "unlockedTechTree");
                if (unlocked == null)
                    return Fail(result, "missing-tech-tree-owner", "Native unlockedTechTree collection is unavailable.");
                int before = Native.Count(unlocked);
                foreach (object proto in Native.Enumerate(Native.TableList("TbTechTree")))
                {
                    string id = Native.TextFirst(proto, "Id", "id");
                    if (id.Length > 0 && Native.Add(unlocked, id))
                        result.AffectedCount++;
                }
                result.Success = result.AffectedCount > 0 ||
                    Native.Count(unlocked) >= before;
                result.Message = "Tech trees affected=" +
                    result.AffectedCount + ".";
                LogMutation("unlock-tech-trees", result.Success, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("unlock-tech-trees", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        public SpawnActionResult SpawnMonster(
            IManifest owner,
            string monsterId,
            int count) =>
            SpawnMonsterNative(owner, monsterId, count);

        BridgeFeatureStatus IAdvancedActions.GetStatus() =>
            Status(runtime.NativeOwnerLabel +
                " fixed advanced-action allowlist; no parser or arbitrary native command surface.");

        private static void ReadTechPoint(
            object typeValue,
            out int points,
            out int level)
        {
            points = 0;
            level = 0;
            Type? operation = Native.Resolve(
                "DolocTown.GameData.ArchiveOperationGlobal, Assembly-CSharp");
            object? archive = Native.Archive;
            MethodInfo? getPoint = operation?.GetMethods(
                    BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                    method.Name == "GetTechPoint" &&
                    method.GetParameters().Length == 2);
            MethodInfo? getLevel = operation?.GetMethods(
                    BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                    method.Name == "GetTechLevel" &&
                    method.GetParameters().Length == 2);
            object? point = getPoint?.Invoke(null, new[] { archive, typeValue });
            object? currentLevel = getLevel?.Invoke(null, new[] { archive, typeValue });
            if (point != null)
                points = Convert.ToInt32(point, CultureInfo.InvariantCulture);
            if (currentLevel != null)
                level = Convert.ToInt32(currentLevel, CultureInfo.InvariantCulture);
        }

        private static MethodInfo? ResolveAddTechPointMethod(Type api)
        {
            if (!ReferenceEquals(addTechPointApiType, api))
            {
                addTechPointApiType = api;
                addTechPointMethod = null;
                addTechPointMethodResolved = false;
            }
            if (addTechPointMethodResolved)
                return addTechPointMethod;
            foreach (MethodInfo method in api.GetMethods(
                BindingFlags.Public | BindingFlags.Static))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == "AddTechPoint" &&
                    parameters.Length == 2 &&
                    parameters[1].ParameterType == typeof(int))
                {
                    addTechPointMethod = method;
                    break;
                }
            }
            addTechPointMethodResolved = true;
            return addTechPointMethod;
        }

        private static TimeScaleDebugResult Fail(
            TimeScaleDebugResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }

        private static DebugValueResult Fail(
            DebugValueResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }

        private static DebugCommandResult Fail(
            DebugCommandResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }

    }
}
