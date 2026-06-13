using System;
using System.Linq;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private bool TryExerciseFishRoeTooltipForSmoke()
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                string? roeItemId = FindFishRoeItemId();
                if (string.IsNullOrWhiteSpace(roeItemId))
                    throw new MissingMemberException("Could not find an ItemFunctionFishRoe item id.");
                string nonNullRoeItemId = roeItemId!;

                MethodInfo? generateItem = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] p = m.GetParameters();
                        return m.Name == "GenerateItem" &&
                            p.Length >= 1 &&
                            p.Length <= 2 &&
                            p[0].ParameterType == typeof(string) &&
                            (p.Length == 1 || p[1].ParameterType == typeof(int));
                    });
                if (generateItem == null)
                    throw new MissingMethodException("DolocAPI.GenerateItem(string, int) was not found.");

                object? item = generateItem.GetParameters().Length == 1
                    ? generateItem.Invoke(null, new object[] { nonNullRoeItemId })
                    : generateItem.Invoke(null, new object[] { nonNullRoeItemId, 1 });
                if (item == null)
                    throw new InvalidOperationException("DolocAPI.GenerateItem returned null for " + nonNullRoeItemId + ".");

                item.GetType().GetMethod("SetFishName", BindingFlags.Public | BindingFlags.Instance)?.Invoke(item, new object[] { "fish" });
                string title = item.GetType().GetProperty("title", BindingFlags.Public | BindingFlags.Instance)?.GetValue(item) as string ?? string.Empty;
                string description = item.GetType().GetProperty("description", BindingFlags.Public | BindingFlags.Instance)?.GetValue(item) as string ?? string.Empty;
                string detail = item.GetType().GetMethod("GetDetailInfo", BindingFlags.Public | BindingFlags.Instance)?.Invoke(item, null) as string ?? string.Empty;
                bool ok = title.IndexOf(" (", StringComparison.Ordinal) >= 0 || description.IndexOf("Hatches:", StringComparison.Ordinal) >= 0 || detail.IndexOf("Hatches:", StringComparison.Ordinal) >= 0;
                if (!ok)
                    throw new InvalidOperationException("Fish roe display hooks did not append expected text. title=" + title + " detail=" + detail);

                runtime.RuntimeMonitor.Log("Smoke exercise FishRoeTooltip OK item=" + nonNullRoeItemId + " title=" + title + " detail=" + detail.Replace(Environment.NewLine, " | "));
                runtime.SetHookStatus("Smoke.FishRoeTooltip", "verified", "ItemFishRoe title/description/detail", "Generated fish roe item and observed decorated tooltip text.");
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke fish roe tooltip exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.FishRoeTooltip", "failed", "ItemFishRoe title/description/detail", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }
    }
}
