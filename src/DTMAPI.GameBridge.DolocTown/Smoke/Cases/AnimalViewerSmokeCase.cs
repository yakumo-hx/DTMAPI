using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private bool TryAutoOpenAnimalPanel()
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                List<object> animals = FindAnimalsForSmoke();
                if (animals.Count == 0)
                    throw new InvalidOperationException("No save animal was available for animal panel UI smoke.");

                EnsureAnimalProgressForSmoke(animals);
                if (!TryFindAnimalPanelRoomForSmoke(animals, out object? room, out string source) || room == null)
                    throw new InvalidOperationException("No animal room was available for animal panel UI smoke.");

                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? animalPanelUiState = patcher.ResolveType("DolocTown.AnimalPanelUiState, Assembly-CSharp");
                if (dolocApi == null || animalPanelUiState == null)
                    throw new MissingMemberException("DolocAPI or AnimalPanelUiState was not visible.");

                MethodInfo? enterUi = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "EnterUI" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
                MethodInfo? handleStartUpArgs = animalPanelUiState.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "HandleStartUpArgs")
                            return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(room);
                    });
                if (enterUi == null || handleStartUpArgs == null)
                    throw new MissingMethodException("AnimalPanelUiState EnterUI/HandleStartUpArgs path was not found.");

                Type delegateType = typeof(Func<,>).MakeGenericType(animalPanelUiState, typeof(bool));
                ParameterExpression stateParameter = Expression.Parameter(animalPanelUiState, "state");
                Expression roomConstant = Expression.Constant(room, handleStartUpArgs.GetParameters()[0].ParameterType);
                MethodCallExpression call = Expression.Call(stateParameter, handleStartUpArgs, roomConstant);
                Delegate startUpDelegate = Expression.Lambda(delegateType, call, stateParameter).Compile();

                runtime.SetHookStatus("Smoke.AnimalPanelUi", "pending", "DolocAPI.EnterUI(AnimalPanelUiState)", "Opening official AnimalPanel UI; waiting for AnimalViewer.Show evidence.");
                object? state = enterUi.MakeGenericMethod(animalPanelUiState).Invoke(null, new object[] { startUpDelegate });
                if (state == null)
                    throw new InvalidOperationException("AnimalPanelUiState could not be entered.");
                TryRefreshAnimalPanelForSmoke(state);

                runtime.RuntimeMonitor.Log("Smoke automation opened animal panel UI source=" + source + " animals=" + animals.Count + ".");
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke animal panel UI open failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AnimalPanelUi", "failed", "AnimalPanelUiState", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private void TryRefreshAnimalPanelForSmoke(object state)
        {
            try
            {
                object? panel = ReadMember(state, "panel");
                MethodInfo? refreshView = panel?.GetType().GetMethod("RefreshView", BindingFlags.Public | BindingFlags.Instance);
                MethodInfo? refreshViewer = panel?.GetType().GetMethod("RefreshViewer", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo? select = panel?.GetType().GetMethod("Select", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
                refreshView?.Invoke(panel, null);
                int evidenceIndex = panel == null ? 0 : FindAnimalProgressDataIndex(panel);
                refreshViewer?.Invoke(panel, new object[] { evidenceIndex });
                select?.Invoke(panel, new object[] { evidenceIndex });
                refreshViewer?.Invoke(panel, new object[] { evidenceIndex });
                if (panel != null)
                    runtime.RuntimeMonitor.Log("Smoke automation refreshed animal panel viewer selection panel=" + panel.GetType().FullName + " evidenceIndex=" + evidenceIndex + " refreshView=" + (refreshView != null) + " refreshViewer=" + (refreshViewer != null) + " select=" + (select != null) + ".");
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke animal panel refresh failed.", ex.ToString());
            }
        }

        private int FindAnimalProgressDataIndex(object panel)
        {
            object? currentDatas = ReadMember(panel, "currentDatas") ?? ReadMember(panel, "<currentDatas>k__BackingField");
            if (currentDatas is Array array)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    object? data = array.GetValue(i);
                    if (AnimalViewerService != null && AnimalViewerService.HasAnimalProgressRowsForSmoke(data, out _))
                        return i;
                }
            }
            return 0;
        }

        private bool TryExerciseAnimalViewerForSmoke()
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                List<object> animals = FindAnimalsForSmoke();
                if (animals.Count == 0)
                    throw new InvalidOperationException("No save animal was available in this save/location.");

                Type? dataType = patcher.ResolveType("DolocTown.UI.AnimalFullInfoData, Assembly-CSharp");
                if (dataType == null)
                    throw new MissingMemberException("AnimalFullInfoData was not visible.");

                string lastDescription = string.Empty;
                foreach (object animal in animals)
                {
                    if (!TryConstructAnimalViewerData(dataType, animal, out object? data, out string stateDescription))
                        continue;
                    lastDescription = stateDescription;
                    if (AnimalViewerService != null && AnimalViewerService.HasAnimalProgressRowsForSmoke(data, out string rowSummary))
                    {
                        runtime.RuntimeMonitor.Log("Smoke exercise AnimalViewerRendering OK animal=" + GetAnimalId(animal) + " rows=" + rowSummary + " stateDescription=" + stateDescription.Replace(Environment.NewLine, " | "));
                        runtime.SetHookStatus("Smoke.AnimalViewerRendering", "verified", "AnimalFullInfoData(Animal)", "Constructed animal viewer data and observed independent hidden-produce progress row.");
                        return true;
                    }
                }

                foreach (object animal in animals)
                {
                    if (!TrySeedAnimalHusbandryForSmoke(animal, out string seedSummary))
                        continue;
                    runtime.RuntimeMonitor.Log("Smoke exercise seeded transient animal husbandry progress for " + seedSummary + ".");

                    if (!TryConstructAnimalViewerData(dataType, animal, out object? data, out string stateDescription))
                        continue;
                    lastDescription = stateDescription;
                    if (AnimalViewerService != null && AnimalViewerService.HasAnimalProgressRowsForSmoke(data, out string rowSummary))
                    {
                        runtime.RuntimeMonitor.Log("Smoke exercise AnimalViewerRendering OK animal=" + GetAnimalId(animal) + " rows=" + rowSummary + " stateDescription=" + stateDescription.Replace(Environment.NewLine, " | "));
                        runtime.SetHookStatus("Smoke.AnimalViewerRendering", "verified", "AnimalFullInfoData(Animal)", "Constructed animal viewer data and observed independent hidden-produce progress row after transient in-memory husbandry progress seed.");
                        return true;
                    }
                }

                throw new InvalidOperationException("Animal viewer data did not include independent special produce progress rows across " + animals.Count + " animal(s). lastStateDescription=" + lastDescription);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke animal viewer exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AnimalViewerRendering", "failed", "AnimalFullInfoData(Animal)", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private void EnsureAnimalProgressForSmoke(List<object> animals)
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dataType = patcher.ResolveType("DolocTown.UI.AnimalFullInfoData, Assembly-CSharp");
                if (dataType == null)
                    return;

                foreach (object animal in animals)
                {
                    if (TryConstructAnimalViewerData(dataType, animal, out object? data, out _) && AnimalViewerService != null && AnimalViewerService.HasAnimalProgressRowsForSmoke(data, out _))
                        return;
                }

                foreach (object animal in animals)
                {
                    if (!TrySeedAnimalHusbandryForSmoke(animal, out string seedSummary))
                        continue;
                    runtime.RuntimeMonitor.Log("Smoke exercise seeded transient animal husbandry progress for real UI path " + seedSummary + ".");
                    if (TryConstructAnimalViewerData(dataType, animal, out object? data, out _) && AnimalViewerService != null && AnimalViewerService.HasAnimalProgressRowsForSmoke(data, out _))
                        return;
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke animal progress preparation failed.", ex.ToString());
            }
        }

        private static bool HasAnimalProgressMarker(string stateDescription)
        {
            return !string.IsNullOrWhiteSpace(stateDescription) &&
                (stateDescription.IndexOf("Special produce:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 stateDescription.IndexOf("隐藏产物:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 stateDescription.IndexOf("隐藏产物：", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 (stateDescription.IndexOf("<color=#", StringComparison.OrdinalIgnoreCase) >= 0 &&
                  stateDescription.IndexOf("█", StringComparison.OrdinalIgnoreCase) >= 0 &&
                  stateDescription.IndexOf("/", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private bool TryConstructAnimalViewerData(Type dataType, object animal, out object? data, out string stateDescription)
        {
            data = null;
            stateDescription = string.Empty;
            ConstructorInfo? constructor = dataType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(c =>
                {
                    ParameterInfo[] p = c.GetParameters();
                    return p.Length == 1 && p[0].ParameterType.IsInstanceOfType(animal);
                });
            if (constructor == null)
                throw new MissingMethodException("AnimalFullInfoData(Animal) constructor was not found.");

            data = constructor.Invoke(new[] { animal });
            stateDescription = dataType.GetField("stateDescription", BindingFlags.Public | BindingFlags.Instance)?.GetValue(data) as string ?? string.Empty;
            return data != null;
        }

        private bool TrySeedAnimalHusbandryForSmoke(object animal, out string summary)
        {
            summary = string.Empty;
            string animalId = GetAnimalId(animal);
            if (string.IsNullOrWhiteSpace(animalId))
                return false;

            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocConfig = patcher.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? tbHusbandry = tables?.GetType().GetProperty("TbHusbandry", BindingFlags.Public | BindingFlags.Instance)?.GetValue(tables);
            object? info = tbHusbandry?.GetType().GetMethod("GetOrDefault", BindingFlags.Public | BindingFlags.Instance)?.Invoke(tbHusbandry, new object[] { animalId });
            object? dataList = info?.GetType().GetProperty("HusbandryDatas", BindingFlags.Public | BindingFlags.Instance)?.GetValue(info);
            object? first = FirstFromEnumerable(dataList);
            if (first == null)
                return false;

            string outputId = first.GetType().GetProperty("Output", BindingFlags.Public | BindingFlags.Instance)?.GetValue(first) as string ?? string.Empty;
            object? thresholdValue = first.GetType().GetProperty("Threshold", BindingFlags.Public | BindingFlags.Instance)?.GetValue(first);
            if (string.IsNullOrWhiteSpace(outputId) || thresholdValue == null)
                return false;

            int threshold = Convert.ToInt32(thresholdValue);
            if (threshold <= 0)
                return false;

            MethodInfo? setHusbandryValue = animal.GetType().GetMethod("DEBUG_SetHusbandryValue", BindingFlags.Public | BindingFlags.Instance);
            if (setHusbandryValue == null)
                return false;

            int value = Math.Max(1, threshold - 1);
            setHusbandryValue.Invoke(animal, new object[] { outputId, value });
            summary = animalId + "/" + outputId + "=" + value + "/" + threshold;
            return true;
        }

        private static string GetAnimalId(object animal)
        {
            return animal.GetType().GetProperty("protoName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(animal) as string ?? animal.GetType().Name;
        }

        private List<object> FindAnimalsForSmoke()
        {
            patcher ??= new HarmonyReflectionPatcher(runtime);
            var animals = new List<object>();
            Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
            object? room = dolocApi?.GetProperty("CurrentRoom", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            AddAnimalsFromRoom(animals, room);
            AddAnimalsFromArchive(animals, dolocApi);
            return animals;
        }

        private bool TryFindAnimalPanelRoomForSmoke(List<object> animals, out object? room, out string source)
        {
            foreach (object animal in animals)
            {
                object? animalRoom = ReadMember(animal, "currentRoom");
                if (HasAnimalsInRoom(animalRoom))
                {
                    room = animalRoom;
                    source = "animal.currentRoom";
                    return true;
                }
            }

            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
            object? currentRoom = dolocApi?.GetProperty("CurrentRoom", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (HasAnimalsInRoom(currentRoom))
            {
                room = currentRoom;
                source = "DolocAPI.CurrentRoom";
                return true;
            }

            object? archive = dolocApi?.GetProperty("archiveHandle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? mainFarm = archive?.GetType().GetProperty("MainFarm", BindingFlags.Public | BindingFlags.Instance)?.GetValue(archive);
            if (HasAnimalsInRoom(mainFarm))
            {
                room = mainFarm;
                source = "archiveHandle.MainFarm";
                return true;
            }

            object? farmData = archive?.GetType().GetProperty("farmData", BindingFlags.Public | BindingFlags.Instance)?.GetValue(archive);
            object? fallbackFarm = farmData?.GetType().GetProperty("MainFarm", BindingFlags.Public | BindingFlags.Instance)?.GetValue(farmData);
            if (HasAnimalsInRoom(fallbackFarm))
            {
                room = fallbackFarm;
                source = "archiveHandle.farmData.MainFarm";
                return true;
            }

            room = null;
            source = string.Empty;
            return false;
        }

        private bool HasAnimalsInRoom(object? room)
        {
            if (room == null)
                return false;
            var animals = new List<object>();
            AddAnimalsFromRoom(animals, room);
            return animals.Count > 0;
        }

        private void AddAnimalsFromArchive(List<object> animals, Type? dolocApi)
        {
            object? archive = dolocApi?.GetProperty("archiveHandle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? mainFarm = archive?.GetType().GetProperty("MainFarm", BindingFlags.Public | BindingFlags.Instance)?.GetValue(archive);
            AddAnimalsFromRoom(animals, mainFarm);

            object? farmData = archive?.GetType().GetProperty("farmData", BindingFlags.Public | BindingFlags.Instance)?.GetValue(archive);
            object? fallbackFarm = farmData?.GetType().GetProperty("MainFarm", BindingFlags.Public | BindingFlags.Instance)?.GetValue(farmData);
            AddAnimalsFromRoom(animals, fallbackFarm);
        }

        private void AddAnimalsFromRoom(List<object> animals, object? room)
        {
            if (room == null)
                return;

            AddAnimalsFromEnumerable(animals, room.GetType().GetProperty("AllAnimals", BindingFlags.Public | BindingFlags.Instance)?.GetValue(room));

            object? animalSystem = room.GetType().GetProperty("animalSystem", BindingFlags.Public | BindingFlags.Instance)?.GetValue(room);
            AddAnimalsFromEnumerable(animals, animalSystem?.GetType().GetProperty("Animals", BindingFlags.Public | BindingFlags.Instance)?.GetValue(animalSystem));

            object? manager = room.GetType().GetProperty("DM_animal", BindingFlags.Public | BindingFlags.Instance)?.GetValue(room);
            AddAnimalsFromEnumerable(animals, manager?.GetType().GetProperty("AllAnimals", BindingFlags.Public | BindingFlags.Instance)?.GetValue(manager));
        }

        private static void AddAnimalsFromEnumerable(List<object> animals, object? value)
        {
            if (!(value is IEnumerable enumerable))
                return;
            foreach (object item in enumerable)
            {
                if (item != null && !animals.Contains(item))
                    animals.Add(item);
            }
        }

        private static object? FirstFromEnumerable(object? value)
        {
            if (!(value is IEnumerable enumerable))
                return null;
            foreach (object item in enumerable)
                return item;
            return null;
        }
    }
}
