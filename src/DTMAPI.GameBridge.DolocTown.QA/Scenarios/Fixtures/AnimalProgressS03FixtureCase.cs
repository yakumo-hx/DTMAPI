using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        internal bool AnimalProgressS03Enabled { get; set; }
        private bool animalProgressS03Complete;
        private AnimalProgressS03State? animalProgressS03;

        // Explicit NoNativeSave acceptance. The animal is constructed from the
        // loaded AnimalPack definition but is never added to a room or archive.
        private sealed class AnimalProgressS03State
        {
            internal object Table = null!, OriginalOutputs = null!, Viewer = null!, Data = null!, Animal = null!, Tables = null!;
            internal object LongTitleItem = null!, Root = null!;
            internal object Panel = null!, OriginalAnimals = null!, Alpaca = null!;
            internal int OriginalCapacity;
            internal string OriginalTitle = "", OriginalLanguage = "";
            internal MethodInfo Show = null!, SwitchLanguage = null!;
            internal int Stage;
            internal DateTimeOffset ChangedAt;
            internal string? Screenshot;
            internal DateTimeOffset ScreenshotAt;
            internal readonly string[] Ids = { "petrified_hatch_egg", "wool_grease", "fertile_mole_egg", "polymer_oilfloater_egg", "sack_hatch" };
            internal readonly int[] Values = { 0, 45, 90, 60, 30 };
            internal int RootId;
            internal int[] RowIds = Array.Empty<int>();
        }

        private G4FixtureStepResult ObserveAnimalProgressS03ForFixture(object panel, List<object> animals, string screenshotPath)
        {
            try
            {
                if (animalProgressS03 == null)
                {
                    PrepareAnimalProgressS03(panel, animals);
                    return G4FixtureStepResult.Pending("S03 native alpaca selected; checking its own wool output and native row alignment before the separate bridge hatch case.");
                }
                var s = animalProgressS03;
                if ((DateTimeOffset.UtcNow - s.ChangedAt).TotalSeconds < 0.4)
                    return G4FixtureStepResult.Pending("S03 waiting for the next-frame guard and layout; stage=" + s.Stage);
                if (s.Stage < 0)
                {
                    string output = s.Stage == -3 ? "wool_grease" : "petrified_hatch_egg";
                    VerifyNativeAnimalOutput(s, output);
                    AssertAnimalNativeAlignment(s.Viewer, s.Root);
                    var actualShot = ObserveScreenshotForFixture(Path.ChangeExtension(screenshotPath, s.Stage == -3 ? "s03-alpaca.png" : "s03-hatch.png"), ref s.Screenshot, ref s.ScreenshotAt);
                    if (!actualShot.Completed || !actualShot.Succeeded) return actualShot;
                    s.Screenshot = null;
                    if (s.Stage == -3)
                    {
                        SelectAnimalS03(s, 1);
                        s.Stage = -2; s.ChangedAt = DateTimeOffset.UtcNow;
                        return G4FixtureStepResult.Pending("S03 real alpaca has only wool grease; native selection now displays the bridge hatch.");
                    }
                    PrepareAnimalS03Stress(s, animals);
                    return G4FixtureStepResult.Pending("S03 bridge hatch has only petrified hatch egg; now starting explicitly named temporary multi-output layout data.");
                }
                AssertAnimalS03(ReadAnimalProductObservationSummary().Contains("nextFrameGuard=completed"), "next-frame guard did not complete");
                AssertAnimalS03(InstanceId(AnimalProgressRoot(s.Viewer)!) == s.RootId, "refresh recreated the owned root");
                if (s.Stage == 0)
                {
                    VerifyAnimalS03Rows(s);
                    if (s.Screenshot == null)
                        runtime.RuntimeMonitor.Log("S03 layout root=" + DescribeAnimalRect(s.Root) + "; row=" + DescribeAnimalRect(AnimalProgressRows(s.Root)[0]) + "; mood=" + DescribeAnimalRect(ReadMember(s.Viewer, "moodBar")!));
                    AssertAnimalRowVisible(AnimalProgressRows(s.Root)[0]);
                    var shot = ObserveScreenshotForFixture(Path.ChangeExtension(screenshotPath, "s03-top.png"), ref s.Screenshot, ref s.ScreenshotAt);
                    if (!shot.Completed || !shot.Succeeded) return shot;
                    object scroll = Component(s.Root, "UnityEngine.UI.ScrollRect, UnityEngine.UI");
                    AssertAnimalS03(SetMemberValue(scroll, "verticalNormalizedPosition", 0f), "scroll-to-last-row failed");
                    s.Screenshot = null; s.Stage = 1; s.ChangedAt = DateTimeOffset.UtcNow;
                    return G4FixtureStepResult.Pending("S03 first screenshot captured; scrolling to the fifth output.");
                }
                if (s.Stage == 1)
                {
                    object scroll = Component(s.Root, "UnityEngine.UI.ScrollRect, UnityEngine.UI");
                    AssertAnimalS03(Convert.ToSingle(ReadMember(scroll, "verticalNormalizedPosition")) < 0.01f, "last output was not reachable by scrolling");
                    AssertAnimalRowVisible(AnimalProgressRows(s.Root)[4]);
                    var shot = ObserveScreenshotForFixture(Path.ChangeExtension(screenshotPath, "s03-bottom.png"), ref s.Screenshot, ref s.ScreenshotAt);
                    if (!shot.Completed || !shot.Succeeded) return shot;
                    s.SwitchLanguage.Invoke(null, new object[] { "en" });
                    SetMemberValue(s.LongTitleItem, "Title", "A deliberately long output name from the real custom animal table");
                    s.Show.Invoke(s.Viewer, new[] { s.Data });
                    s.Stage = 2; s.ChangedAt = DateTimeOffset.UtcNow;
                    return G4FixtureStepResult.Pending("S03 fifth output captured; checking English after native language change.");
                }
                VerifyAnimalS03Rows(s);
                if (s.Stage == 2)
                {
                    s.SwitchLanguage.Invoke(null, new object[] { "zh-CN" });
                    SetMemberValue(s.LongTitleItem, "Title", "切换语言后自定义产物的新中文名称");
                    s.Show.Invoke(s.Viewer, new[] { s.Data });
                    s.Stage = 3; s.ChangedAt = DateTimeOffset.UtcNow;
                    return G4FixtureStepResult.Pending("S03 English names verified; checking Chinese after another native language change.");
                }
                RestoreAnimalProgressS03Fixture();
                FindMethod(panel.GetType(), "RefreshView", 0)!.Invoke(panel, null);
                runtime.RuntimeMonitor.Log("Smoke Animal S03 semantics verified: realAlpacaOnly=wool_grease; bridgeHatchOnly=petrified_hatch_egg; nativeSelectionMatchesViewer=true; nativeRowAlignment=true; detachedFromArchive=true; temporaryStressOutputs=5; hiddenZeroProgress=true; invalidThresholdOmitted=true; progressOrder=90/60,60/60,45/60,30/60,0/60; longCustomName=true; languages=en,zh-CN; repeatedShow=50; rapidSelection=20; stableRows=true; zeroOutputs=true; parentChangeAndSiblingRestored=true; scrollLast=true; fixtureRestored=true.");
                return G4FixtureStepResult.Verified("Animal S03 real custom bridge, five outputs, scrolling, stable rows, language and restoration verified.");
            }
            catch (Exception ex)
            {
                RestoreAnimalProgressS03Fixture();
                return G4FixtureStepResult.Failed("Animal S03: " + ex);
            }
        }

        private void PrepareAnimalProgressS03(object panel, List<object> animals)
        {
            var s = new AnimalProgressS03State();
            animalProgressS03 = s;
            Type api = ResolveType("DolocAPI, Assembly-CSharp")!;
            s.Tables = ReadStaticMember(ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp"), "Tables")!;
            s.OriginalLanguage = (string)ReadMember(s.Tables, "_currentL10nId")!;
            s.SwitchLanguage = FindMethod(api, "SwitchLanguage", 1)!;
            object animalTable = ReadMember(s.Tables, "TbAnimal")!;
            object proto = FindMethod(animalTable.GetType(), "GetOrDefault", 1)!.Invoke(animalTable, new object[] { "hatch" })
                ?? throw new InvalidOperationException("The real AnimalPack hatch definition is required.");
            object husbandry = ReadMember(s.Tables, "TbHusbandry")!;
            s.Table = FindMethod(husbandry.GetType(), "GetOrDefault", 1)!.Invoke(husbandry, new object[] { "hatch" })!;
            s.OriginalOutputs = ReadMember(s.Table, "HusbandryDatas")!;
            IList original = (IList)s.OriginalOutputs;
            AssertAnimalS03(original.Count == 1 && ReadStringMember(original[0]!, "Output", "") == "petrified_hatch_egg", "hatch did not come from the real merged bridge table");
            object itemTable = ReadMember(s.Tables, "TbItem")!;
            foreach (string id in s.Ids)
                AssertAnimalS03(FindMethod(itemTable.GetType(), "GetOrDefault", 1)!.Invoke(itemTable, new object[] { id }) != null, "custom output missing: " + id);
            s.LongTitleItem = FindMethod(itemTable.GetType(), "GetOrDefault", 1)!.Invoke(itemTable, new object[] { "sack_hatch" })!;
            s.OriginalTitle = (string)ReadMember(s.LongTitleItem, "Title")!;
            Type animalType = ResolveType("DolocTown.Animal, Assembly-CSharp")!;
            object birthday = ReadMember(animals[0], "birthdayInfo")!;
            s.Animal = Activator.CreateInstance(animalType, new[] { proto, birthday })!;
            s.Alpaca = animals.First(a => ReadStringMember(a, "protoName", "") == "goat");
            SetMemberValue(s.Animal, "currentRoom", ReadMember(s.Alpaca, "currentRoom")!);
            SetMemberValue(s.Animal, "homeRoom", ReadMember(s.Alpaca, "homeRoom")!);
            s.Panel = panel;
            s.OriginalAnimals = ReadMember(g4AnimalUiOwnedState!, "currentAnimals")!;
            s.OriginalCapacity = ReadIntMember(panel, "totalCapacity", -1);
            var visibleAnimals = (IList)Activator.CreateInstance(s.OriginalAnimals.GetType())!;
            visibleAnimals.Add(s.Alpaca); visibleAnimals.Add(s.Animal);
            // This list belongs only to the QA-owned UI state. The hatch is
            // never inserted into native room or save membership.
            SetMemberValue(g4AnimalUiOwnedState!, "currentAnimals", visibleAnimals);
            FindMethod(panel.GetType(), "SetTotalCapacity", 1)!.Invoke(panel, new object[] { 2 });
            s.Viewer = ReadMember(panel, "Viewer")!;
            s.Show = FindMethod(s.Viewer.GetType(), "Show", 1)!;
            SelectAnimalS03(s, 0);
            s.Stage = -3; s.ChangedAt = DateTimeOffset.UtcNow;
        }

        private void SelectAnimalS03(AnimalProgressS03State s, int index)
        {
            FindMethod(s.Panel.GetType(), "RefreshView", 0)!.Invoke(s.Panel, null);
            FindMethod(s.Panel.GetType(), "Select", 1)!.Invoke(s.Panel, new object[] { index });
            AssertAnimalS03(ReadIntMember(s.Panel, "selectedIndex", -1) == index, "native animal selection did not change");
            s.Data = ((Array)ReadMember(s.Panel, "currentDatas")!).GetValue(index)!;
            s.Root = AnimalProgressRoot(s.Viewer) ?? throw new InvalidOperationException("No owned view for selected native animal.");
        }

        private void PrepareAnimalS03Stress(AnimalProgressS03State s, List<object> animals)
        {
            IList original = (IList)s.OriginalOutputs;
            SetMemberValue(s.LongTitleItem, "Title", "一只来自真实自定义畜牧桥的哈奇所产生的特别长名称产物");
            var outputs = (IList)Activator.CreateInstance(original.GetType())!;
            MethodInfo clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!;
            for (int i = 0; i < 7; i++)
            {
                object row = clone.Invoke(original[0], null)!;
                SetMemberValue(row, "Output", i < 5 ? s.Ids[i] : i == 5 ? "sack_drecko" : s.Ids[0]);
                SetMemberValue(row, "Threshold", i == 5 ? 0 : 60);
                outputs.Add(row);
            }
            SetMemberValue(s.Table, "HusbandryDatas", outputs);
            SetMemberValue(s.Animal, "customName", "临时多产物布局验收");
            var values = (IDictionary)ReadMember(s.Animal, "husbandryValues")!;
            for (int i = 0; i < s.Ids.Length; i++) values[s.Ids[i]] = s.Values[i];
            Type dataType = ResolveType("DolocTown.UI.AnimalFullInfoData, Assembly-CSharp")!;
            SelectAnimalS03(s, 1);
            int firstRoot = InstanceId(s.Root);
            int[] firstRows = AnimalProgressRows(s.Root).Select(InstanceId).ToArray();
            for (int i = 0; i < 50; i++) s.Show.Invoke(s.Viewer, new[] { s.Data });
            for (int i = 0; i < 20; i++)
            {
                object other = Activator.CreateInstance(dataType, new[] { animals[i % animals.Count] })!;
                s.Show.Invoke(s.Viewer, new[] { other });
                s.Show.Invoke(s.Viewer, new[] { s.Data });
            }
            AssertAnimalS03(InstanceId(AnimalProgressRoot(s.Viewer)!) == firstRoot && firstRows.SequenceEqual(AnimalProgressRows(s.Root).Select(InstanceId)),
                "selection churn recreated rows: root=" + firstRoot + "->" + InstanceId(AnimalProgressRoot(s.Viewer)!) + "; before=" + string.Join(",", firstRows) + "; after=" + string.Join(",", AnimalProgressRows(s.Root).Select(InstanceId)));
            SetMemberValue(s.Data, "visible", false);
            s.Show.Invoke(s.Viewer, new[] { s.Data });
            AssertAnimalS03(AnimalProgressRoot(s.Viewer) == null, "hidden animal retained visible rows");
            SetMemberValue(s.Data, "visible", true);
            s.Show.Invoke(s.Viewer, new[] { s.Data });
            AssertAnimalS03(AnimalProgressRoot(s.Viewer) != null, "hidden selection discarded other native row bindings");
            SetMemberValue(s.Table, "HusbandryDatas", Activator.CreateInstance(original.GetType())!);
            s.Show.Invoke(s.Viewer, new[] { s.Data });
            AssertAnimalS03(AnimalProgressRoot(s.Viewer) == null, "zero outputs retained visible rows");
            SetMemberValue(s.Table, "HusbandryDatas", outputs);
            s.Show.Invoke(s.Viewer, new[] { s.Data });

            // Reparent only the native template temporarily; keep other native
            // children untouched, then restore it before screenshots.
            object mood = ReadMember(s.Viewer, "moodBar")!;
            object transform = ReadMember(mood, "transform")!;
            object parent = ReadMember(transform, "parent")!;
            object originalPosition = ReadMember(transform, "localPosition")!;
            object originalScale = ReadMember(transform, "localScale")!;
            object originalAnchoredPosition = ReadMember(transform, "anchoredPosition")!;
            object originalSize = ReadMember(transform, "sizeDelta")!;
            int originalSibling = (int)FindMethod(transform.GetType(), "GetSiblingIndex", 0)!.Invoke(transform, null)!;
            object temp = Activator.CreateInstance(ResolveType("UnityEngine.GameObject, UnityEngine.CoreModule")!, new object[] { "DTMAPI.QA.AnimalS03Parent" })!;
            object tempTransform = ReadMember(temp, "transform")!;
            MethodInfo reparent = transform.GetType().GetMethod("SetParent", new[] { ResolveType("UnityEngine.Transform, UnityEngine.CoreModule")!, typeof(bool) })!;
            try
            {
                reparent.Invoke(tempTransform, new[] { parent, (object)false });
                reparent.Invoke(transform, new[] { tempTransform, (object)false });
                s.Show.Invoke(s.Viewer, new[] { s.Data });
                AssertAnimalS03(AnimalProgressRoot(s.Viewer) != null, "changed parent did not receive the view");
            }
            finally
            {
                reparent.Invoke(transform, new[] { parent, (object)false });
                FindMethod(transform.GetType(), "SetSiblingIndex", 1)!.Invoke(transform, new object[] { originalSibling });
                SetMemberValue(transform, "localPosition", originalPosition);
                SetMemberValue(transform, "localScale", originalScale);
                SetMemberValue(transform, "sizeDelta", originalSize);
                SetMemberValue(transform, "anchoredPosition", originalAnchoredPosition);
                s.Show.Invoke(s.Viewer, new[] { s.Data });
                ResolveType("UnityEngine.Object, UnityEngine.CoreModule")!.GetMethod("Destroy", new[] { ResolveType("UnityEngine.Object, UnityEngine.CoreModule")! })!.Invoke(null, new[] { temp });
            }
            s.Root = AnimalProgressRoot(s.Viewer)!;
            s.RootId = InstanceId(s.Root); s.RowIds = AnimalProgressRows(s.Root).Select(InstanceId).ToArray();
            VerifyAnimalS03Rows(s);
            s.Stage = 0;
            s.ChangedAt = DateTimeOffset.UtcNow;
        }

        private static void VerifyNativeAnimalOutput(AnimalProgressS03State s, string id)
        {
            var rows = AnimalProgressRows(s.Root);
            AssertAnimalS03(rows.Count == 1, "native species must have only its own one output: " + id);
            object items = ReadMember(s.Tables, "TbItem")!;
            object item = FindMethod(items.GetType(), "GetOrDefault", 1)!.Invoke(items, new object[] { id })!;
            object bar = Component(rows[0], "DolocTown.UI.ProgressBar, Assembly-CSharp");
            AssertAnimalS03(ReadStringMember(ReadMember(bar, "txtTitle")!, "text", "") == ReadStringMember(item, "Title", ""), "output crossed species boundary: " + id);
            object animal = s.Stage == -3 ? s.Alpaca : s.Animal;
            var values = (IDictionary)ReadMember(animal, "husbandryValues")!;
            int value = values.Contains(id) ? Convert.ToInt32(values[id]) : 0;
            int threshold = s.Stage == -3 ? 80 : 60;
            AssertAnimalS03(ReadStringMember(ReadMember(bar, "txtProgress")!, "text", "") == value + "/" + threshold, "native species progress/threshold mismatch: " + id);
            object selectedAnimal = ReadMember(s.Data, "title")!;
            AssertAnimalS03(ReadStringMember(ReadMember(s.Viewer, "title")!, "text", "") == (string)selectedAnimal, "viewer title disagrees with native selected animal");
            AssertAnimalRowVisible(rows[0]);
        }

        private static void AssertAnimalNativeAlignment(object viewer, object root)
        {
            object energy = ReadMember(viewer, "energyBar")!, mood = ReadMember(viewer, "moodBar")!;
            object row = Component(AnimalProgressRows(root)[0], "DolocTown.UI.ProgressBar, Assembly-CSharp");
            float Coordinate(object value, string axis) => Convert.ToSingle(ReadMember(ReadMember(ReadMember(value, "transform")!, "position")!, axis));
            float energyY = Coordinate(ReadMember(energy, "txtProgress")!, "y");
            float moodY = Coordinate(ReadMember(mood, "txtProgress")!, "y");
            float rowY = Coordinate(ReadMember(row, "txtProgress")!, "y");
            AssertAnimalS03(Math.Abs((energyY - moodY) - (moodY - rowY)) < 0.1f, "native stats and output must be consecutively spaced");
            AssertAnimalS03(Math.Abs(Coordinate(ReadMember(mood, "txtProgress")!, "x") - Coordinate(ReadMember(row, "txtProgress")!, "x")) < 0.1f &&
                Math.Abs(Coordinate(ReadMember(mood, "progressBackground")!, "x") - Coordinate(ReadMember(row, "progressBackground")!, "x")) < 0.1f,
                "output bar and value must align with the native mood row");
        }

        private void VerifyAnimalS03Rows(AnimalProgressS03State s)
        {
            var rows = AnimalProgressRows(s.Root);
            AssertAnimalS03(rows.Count == 5 && rows.Select(InstanceId).SequenceEqual(s.RowIds), "all five stable rows required");
            var expected = s.Ids.Select((id, index) => new { Id = id, Value = s.Values[index] }).OrderByDescending(x => x.Value).ToArray();
            object items = ReadMember(s.Tables, "TbItem")!;
            for (int i = 0; i < rows.Count; i++)
            {
                object bar = Component(rows[i], "DolocTown.UI.ProgressBar, Assembly-CSharp");
                object item = FindMethod(items.GetType(), "GetOrDefault", 1)!.Invoke(items, new object[] { expected[i].Id })!;
                string actual = (string)ReadMember(ReadMember(bar, "txtTitle")!, "text")!;
                string title = (string)ReadMember(item, "Title")!;
                string progress = (string)ReadMember(ReadMember(bar, "txtProgress")!, "text")!;
                AssertAnimalS03(actual == title && progress == expected[i].Value + "/60", "visible row mismatch: " + actual + "/" + progress + " expected=" + title);
            }
        }
        private void RestoreAnimalProgressS03Fixture()
        {
            var s = animalProgressS03;
            if (s == null) return;
            if (s.OriginalOutputs != null) SetMemberValue(s.Table, "HusbandryDatas", s.OriginalOutputs);
            if (s.SwitchLanguage != null && s.OriginalLanguage.Length > 0) s.SwitchLanguage.Invoke(null, new object[] { s.OriginalLanguage });
            if (s.LongTitleItem != null) SetMemberValue(s.LongTitleItem, "Title", s.OriginalTitle);
            if (s.OriginalAnimals != null && g4AnimalUiOwnedState != null)
            {
                SetMemberValue(g4AnimalUiOwnedState, "currentAnimals", s.OriginalAnimals);
                FindMethod(s.Panel.GetType(), "SetTotalCapacity", 1)!.Invoke(s.Panel, new object[] { s.OriginalCapacity });
                FindMethod(s.Panel.GetType(), "RefreshView", 0)!.Invoke(s.Panel, null);
                FindMethod(s.Panel.GetType(), "Select", 1)!.Invoke(s.Panel, new object[] { 0 });
            }
            animalProgressS03 = null;
        }
        private static object? AnimalProgressRoot(object viewer)
        {
            object parent = ReadMember(ReadMember(ReadMember(viewer, "moodBar")!, "transform")!, "parent")!;
            return Children(parent).Select(t => ReadMember(t, "gameObject")!).SingleOrDefault(g =>
                ReadStringMember(g, "name", "") == "DTMAPI.AnimalProduceProgress" && ReadMember(g, "activeSelf") is bool active && active);
        }
        private static List<object> AnimalProgressRows(object root)
        {
            object transform = ReadMember(root, "transform")!;
            object content = Children(transform).Single(t => ReadStringMember(t, "name", "") == "Rows");
            return Children(content).Select(t => ReadMember(t, "gameObject")!).Where(g => ReadMember(g, "activeSelf") is bool active && active).ToList();
        }
        private static IEnumerable<object> Children(object transform)
        {
            int count = Convert.ToInt32(ReadMember(transform, "childCount"));
            for (int i = 0; i < count; i++) yield return FindMethod(transform.GetType(), "GetChild", 1)!.Invoke(transform, new object[] { i })!;
        }
        private static object Component(object gameObject, string type) => gameObject.GetType().GetMethod("GetComponent", new[] { typeof(Type) })!.Invoke(gameObject, new object[] { ResolveType(type)! })!;
        private static int InstanceId(object value) => (int)FindMethod(value.GetType(), "GetInstanceID", 0)!.Invoke(value, null)!;
        private static void AssertAnimalRowVisible(object row)
        {
            object bar = Component(row, "DolocTown.UI.ProgressBar, Assembly-CSharp");
            object title = ReadMember(bar, "txtTitle")!;
            object renderer = ReadMember(title, "canvasRenderer")!;
            object generator = ReadMember(title, "cachedTextGenerator")!;
            AssertAnimalS03(!(ReadMember(renderer, "cull") is bool culled && culled) &&
                Convert.ToInt32(ReadMember(generator, "characterCountVisible")) > 0,
                "owned output title is clipped or has no rendered characters; row=" + DescribeAnimalRect(row) + "; title=" + DescribeAnimalRect(title));
        }
        private static string DescribeAnimalRect(object value)
        {
            object rect = ReadMember(value, "transform")!;
            return "rect=" + ReadMember(rect, "rect") + ", pos=" + ReadMember(rect, "localPosition") + ", size=" + ReadMember(rect, "sizeDelta") + ", scale=" + ReadMember(rect, "localScale");
        }
        private static void AssertAnimalS03(bool ok, string detail) { if (!ok) throw new InvalidOperationException(detail); }
    }
}
