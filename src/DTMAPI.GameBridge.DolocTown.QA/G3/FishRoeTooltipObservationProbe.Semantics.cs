using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed partial class FishRoeTooltipObservationProbe
    {
        private string VerifyTitleSemantics(Type dolocApi, MethodInfo generateItem, string roeItemId)
        {
            object tables = ResolveType("DolocTown.Config.DolocConfig", "Assembly-CSharp")?
                .GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                ?? throw new InvalidOperationException("Native tables unavailable.");
            string originalLanguage = tables.GetType().GetField("_currentL10nId", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(tables) as string
                ?? throw new InvalidOperationException("Native language identity unavailable.");
            MethodInfo switchLanguage = dolocApi.GetMethod("SwitchLanguage", BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMethodException("DolocAPI.SwitchLanguage");
            var fishIds = new List<string>();
            object fishTable = RequiredProperty(tables, "TbFarmFish");
            foreach (object entry in (IEnumerable)RequiredProperty(fishTable, "DataMap"))
            {
                if (RequiredProperty(entry, "Key") is string id && !string.IsNullOrWhiteSpace(id))
                    fishIds.Add(id);
                if (fishIds.Count == 2) break;
            }
            if (fishIds.Count != 2) throw new InvalidOperationException("Two native hatchable fish are required.");

            object NewItem(string id) => generateItem.Invoke(null, generateItem.GetParameters().Length == 1
                ? new object[] { id } : new object[] { id, 1 }) ?? throw new InvalidOperationException("Could not generate " + id);
            var observations = new List<string>();
            try
            {
                foreach (string language in new[] { "zh-CN", "en", "zh-CN" })
                {
                    switchLanguage.Invoke(null, new object[] { language });
                    foreach (string fishId in fishIds)
                    {
                        object roe = NewItem(roeItemId);
                        roe.GetType().GetMethod("SetFishName")!.Invoke(roe, new object[] { fishId });
                        object fry = roe.GetType().GetMethod("ToFry")!.Invoke(roe, null)
                            ?? throw new InvalidOperationException("Native ToFry returned null.");
                        object fish = fry.GetType().GetMethod("ToFishItem")!.Invoke(fry, null)
                            ?? throw new InvalidOperationException("Native ToFishItem returned null.");
                        if (RequiredText(roe, "fishName") != fishId || RequiredText(fry, "fishName") != fishId || RequiredText(fish, "name") != fishId)
                            throw new InvalidOperationException("Roe, native fry and grown fish identities differ.");

                        string fishTitle = RequiredText(fish, "title");
                        if (fishTitle != RequiredText(RequiredProperty(fish, "proto"), "Title"))
                            throw new InvalidOperationException("An ordinary fish item title was changed.");
                        object proto = RequiredProperty(roe, "proto");
                        string originalTitle = RequiredText(proto, "Title");
                        string nativeDescription = RequiredText(proto, "Description");
                        string beforeDetail = ReadDetail(roe);
                        string actualTitle = RequiredText(roe, "title");
                        string expectedTitle = productDisabled ? originalTitle : originalTitle + " (" + fishTitle + ")";
                        if (actualTitle != expectedTitle ||
                            RequiredText(roe, "title") != actualTitle ||
                            RequiredText(roe, "description") != nativeDescription || beforeDetail != ReadDetail(roe))
                            throw new InvalidOperationException("Exact hatch title, description, detail or repeated-query check failed: " + actualTitle);
                        observations.Add(language + ":" + fishId + "=" + actualTitle);

                        // Native SetFishName normalizes unknown IDs. Bypass only
                        // this transient object's setter validation to test a
                        // malformed/third-party roe; never add it to a save.
                        roe.GetType().GetProperty("fishName")!.GetSetMethod(true)!.Invoke(roe, new object[] { "__dtmapi_missing_fish__" });
                        if (RequiredText(roe, "title") != originalTitle || RequiredText(roe, "description") != nativeDescription || ReadDetail(roe) != beforeDetail)
                            throw new InvalidOperationException("Unresolved fish ID did not preserve native text.");
                    }
                }
            }
            finally { switchLanguage.Invoke(null, new object[] { originalLanguage }); }

            string details = "item=" + roeItemId + "; hatchTitles=" + string.Join(" | ", observations) +
                "; nativeFryAndFish=true; unknownIdFallback=true; descriptionAndDetailUnchanged=true; repeatedQuery=true; languageRestored=" + originalLanguage +
                "; productEnabled=" + !productDisabled + "; productOwnerPatches=" + (productDisabled ? 0 : 1) + "; transientOnly=true; owner=qa; fallback=false";
            access.Log("Fish roe S01 semantics verified " + details + ".");
            return details;
        }

        private static object RequiredProperty(object instance, string property) => instance.GetType().GetProperty(property)?.GetValue(instance)
            ?? throw new MissingMemberException(instance.GetType().FullName, property);
        private static string RequiredText(object instance, string property) => RequiredProperty(instance, property) as string
            ?? throw new InvalidOperationException(property + " was not a string.");
        private static string ReadDetail(object item) => item.GetType().GetMethod("GetDetailInfo")!.Invoke(item, null) as string ?? string.Empty;
    }
}
