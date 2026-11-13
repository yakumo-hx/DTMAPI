using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DTMAPI.Abstractions;
using Native = DTMAPI.DebugConsole.DebugConsoleNativeAccess;

namespace DTMAPI.DebugConsole
{
    internal interface IMonsterConsoleRuntime
    {
        object? GetMonsterContentApi();
    }
    internal sealed partial class DebugConsoleNativeActions
    {
        private IReadOnlyList<SpawnCatalogOption> ModuleMonsters()
        {
            try { return ReadModuleMonsters(); }
            catch (Exception error)
            {
                runtime.Error("optional-monster-catalog", error);
                return Array.Empty<SpawnCatalogOption>();
            }
        }
        private IReadOnlyList<SpawnCatalogOption> ReadModuleMonsters()
        {
            object? api = (runtime as IMonsterConsoleRuntime)?.GetMonsterContentApi();
            if (api == null) return Array.Empty<SpawnCatalogOption>();
            object? result = api.GetType().GetMethod("GetDefinitions")?.Invoke(api, null);
            if (!Equals(Native.Read(result, "Success"), true)) return Array.Empty<SpawnCatalogOption>();
            var options = new List<SpawnCatalogOption>();
            foreach (object row in Native.Enumerate(Native.Read(result, "Value")))
            {
                bool ready = Native.Text(row, "State") == "Creatable" && Native.CurrentRoom != null;
                options.Add(new SpawnCatalogOption {
                    Id = Native.Text(row, "Id"), DisplayName = Native.Text(row, "DisplayName"),
                    Category = "monster", SourceId = Native.First(Native.Text(row, "OwnerId"), "__mods"), SourceDisplayName = Native.Text(row, "SourceName"),
                    IsModSource = true, IsAvailable = ready, UnavailableReason = ready ? "" : "monster-definition-unavailable" });
            }
            return options;
        }
        private SpawnActionResult SpawnModuleMonster(SpawnCatalogOption option, SpawnDebugResult result)
        {
            object? api = (runtime as IMonsterConsoleRuntime)?.GetMonsterContentApi();
            var method = api?.GetType().GetMethod("SpawnFromCatalog");
            if (api == null || method == null) return FailSpawn("spawn-monster", result, "monster-module-unavailable", "The optional Monster provider is unavailable.");
            object? position = Native.AgentPosition;
            float x = Convert.ToSingle(Native.Read(position, "x"), CultureInfo.InvariantCulture), y = Convert.ToSingle(Native.Read(position, "y"), CultureInfo.InvariantCulture);
            string code = "", reason = "";
            result.DisplayName = option.DisplayName;
            object? manager = Native.Read(Native.CurrentRoom, "DM_monster");
            if (manager == null) return FailSpawn("spawn-monster", result, "native-host-unavailable", "The current room has no monster manager.");
            var before = ManagerSnapshot(manager, "AllMonsters");
            for (int i = 0; i < result.RequestedCount; i++)
            {
                object request = Activator.CreateInstance(method.GetParameters()[0].ParameterType)!;
                request.GetType().GetProperty("DefinitionId")!.SetValue(request, option.Id);
                request.GetType().GetProperty("X")!.SetValue(request, x + (i % 5 - 2) * 1.5f);
                request.GetType().GetProperty("Y")!.SetValue(request, y + 2 + i / 5);
                object? answer = method.Invoke(api, new[] { request });
                if (!Equals(Native.Read(answer, "Success"), true))
                {
                    var diagnostic = Native.Read(answer, "Diagnostic"); code = Native.Text(diagnostic, "Code"); reason = Native.Text(diagnostic, "Message"); break;
                }
                result.SpawnedCount++;
            }
            return FinishSpawn("spawn-monster", result, AddedEntities(before, ManagerSnapshot(manager, "AllMonsters")).Count, code, reason);
        }
    }
}
