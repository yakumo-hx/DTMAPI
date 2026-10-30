using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DTMAPI.Abstractions;
using DTMAPI.MoreEquipmentSlots;
using DolocTown;

namespace DTMAPI.EquipmentSlotsHarmonyOwnerFixture
{
    internal static partial class Program
    {
        // This host serializes its own native variable envelope. It exercises the
        // linked product's save protocol, not DolocTown's actual SaveGame writer.
        private sealed class EmbeddedFixture : IDisposable
        {
            internal readonly string Root, ConfigPath, NativePath, SourcePath;
            internal readonly IProtectedSourceFiles Sources;
            internal readonly MoreEquipmentSlotsNativeRuntime Runtime;
            internal ColdFixtureArchive Archive;
            internal long? Epoch = 1;
            internal int Slot => Archive.archiveIndex;
            internal EmbeddedFixture(string label, int slot = 2, string name = "fixture-player")
            {
                ResetOwners();
                DolocAPI.ResetInventory();
                Root = CreateFixtureScenarioRoot("mes");
                // The older cold fixtures use extended paths for their migration
                // artifacts. Core's protected-source boundary requires a drive path.
                if (Root.StartsWith(@"\\?\", StringComparison.Ordinal)) Root = Root.Substring(4);
                ConfigPath = Path.Combine(Root, "config", "config.json");
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                SourcePath = Path.Combine(Root, "config", "protected-items", "equipment-slots",
                    "slot-" + slot, "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json");
                NativePath = Path.Combine(Root, "native.sav");
                Archive = NewArchive(slot, name);
                DolocAPI.archiveHandle = Archive;
                DolocAPI.dataPersistenceManager = new ColdFixtureDataPersistenceManager(NativePath);
                Type service = Assembly.Load("DTMAPI.Core").GetType("DTMAPI.Core.Services.ProtectedSourceFileService", true)!;
                Sources = (IProtectedSourceFiles)Activator.CreateInstance(service,
                    BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new object[] { Path.GetDirectoryName(ConfigPath)!, (Action)(() => { }), (Action)(() => { }) }, null)!;
                Runtime = new MoreEquipmentSlotsNativeRuntime(NullMonitor.Instance, ConfigPath);
                Runtime.SetArchiveContext(() => Epoch);
                Runtime.SetProtectedSources(Sources);
                Runtime.Configure(new MoreEquipmentSlotsConfig(), label);
                WriteNative();
            }
            internal EquipmentSlotEmbeddedSession Session =>
                (EquipmentSlotEmbeddedSession)GetField(Runtime, "embeddedSession").GetValue(Runtime)!;
            internal List<EquipmentSlotStorageEntry>? Working =>
                (List<EquipmentSlotStorageEntry>?)GetField(Runtime, "workingSlots").GetValue(Runtime);
            internal EquipmentSlotStorageDocument Document =>
                (EquipmentSlotStorageDocument)GetField(Runtime, "document").GetValue(Runtime)!;
            internal string Staged => (string)Archive.cityData!.dialogueManager.variableStorage.variables[EquipmentSlotEmbeddedArchive.Key];
            internal void Load() => Runtime.OnSaveLoaded(Slot, false);
            internal void SetCurrent(IReadOnlyList<EquipmentSlotStorageEntry> slots)
            { Archive.cityData!.dialogueManager.variableStorage.SetValue(EquipmentSlotEmbeddedArchive.Key, EquipmentSlotEmbeddedPayload.Encode(slots)); }
            internal void Seed(IReadOnlyList<EquipmentSlotStorageEntry> slots)
            { SetCurrent(slots); WriteNative(); Load(); Assert(Session.State == EquipmentSlotEmbeddedState.Ready, "Complete native seed did not enter normal Working."); }
            internal void WriteSource(EquipmentSlotStorageDocument document)
            {
                // Author a legacy input, as NormalBackend does. This is not an
                // invocation of the retired normal Product sidecar writer.
                Directory.CreateDirectory(Path.GetDirectoryName(SourcePath)!);
                using (var stream = File.Create(SourcePath))
                    new DataContractJsonSerializer(typeof(EquipmentSlotStorageDocument)).WriteObject(stream, document);
            }
            internal void WriteNative(string? overridePayload = null)
            {
                var values = new Dictionary<string, object>(Archive.cityData!.dialogueManager.variableStorage.variables);
                if (overridePayload != null) values[EquipmentSlotEmbeddedArchive.Key] = overridePayload;
                using (var stream = File.Create(NativePath))
                    new DataContractJsonSerializer(typeof(FixtureSaveEnvelope),
                        new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true }).WriteObject(stream,
                        new FixtureSaveEnvelope { City = new FixtureCityEnvelope { Dialogue = new FixtureDialogueEnvelope
                        { Storage = new FixtureStorageEnvelope { Variables = values } } } });
            }
            internal void Save()
            { Runtime.OnSaveSaving(Slot); WriteNative(); Runtime.OnSaveSaved(Slot); }
            internal List<EquipmentSlotStorageEntry> DiskSlots()
            {
                bool exists = EquipmentSlotEmbeddedArchive.ReadDisk(NativePath, out object? raw);
                Assert(EquipmentSlotEmbeddedPayload.Read(exists, raw, out var slots, out _) == EquipmentSlotEmbeddedReadKind.Supported,
                    "Own host disk envelope did not contain a supported native payload.");
                return slots!;
            }
            internal void ColdLoad()
            {
                Assert(EquipmentSlotEmbeddedArchive.ReadDisk(NativePath, out var raw), "Cold load needs a committed native key.");
                Runtime.ReturnedToTitle();
                var oldCity = Archive.cityData;
                Archive = NewArchive(Slot, Archive.farmData.agentData.playerName);
                Assert(!ReferenceEquals(oldCity, Archive.cityData), "Cold host reused the previous city object.");
                Archive.cityData!.dialogueManager.variableStorage.variables[EquipmentSlotEmbeddedArchive.Key] = raw!;
                Epoch = Epoch!.Value + 1;
                DolocAPI.archiveHandle = Archive;
                Load();
            }
            public void Dispose()
            {
                Runtime.DeactivateOwner("RuntimeShutdown");
                ((IDisposable)Sources).Dispose();
                AssertOwnerCount(ProductOwner, 0);
                DolocAPI.ResetInventory();
            }
        }
        private static ColdFixtureArchive NewArchive(int slot, string name) =>
            new ColdFixtureArchive(slot, name, name, new DolocTown.GameData.AgentEquipmentManager());
        private static EquipmentSlotStorageDocument CompleteEmbeddedDocument(int slot = 2, string name = "fixture-player")
        {
            var document = EquipmentSlotDocumentStore.CreateEmpty(new EquipmentSlotSaveScope
            { ArchiveIndex = slot, PlayerName = name, CustomPlayerName = name, TotalGameSeconds = 1 });
            var native = new EquipmentSlotNativeItem(typeof(DolocAPI));
            string[] ids = { "fixture-shield", "fixture-herb-package", "fixture-cooldown" };
            for (int i = 0; i < ids.Length; i++)
            {
                var item = (Item)native.Create(ids[i]);
                if (item is ItemHatShield shield) shield.SetShieldValue(8);
                if (item is ItemHerbPackage herb) herb.SetFilled(false);
                if (item is ItemCooldown cooldown) cooldown.cdDuration = 77;
                document.Slots[i] = new EquipmentSlotStorageEntry { Index = i, ItemId = ids[i], DisplayName = ids[i],
                    ItemState = native.Capture(item), IsShield = i == 0, ShieldValue = i == 0 ? 8 : 0,
                    ShieldMaxValue = i == 0 ? 10 : 0, ShieldDefend = i == 0 ? 1 : 0, DefenseBonus = i == 0 ? 3 : 0 };
            }
            return document;
        }
        private static EquipmentSlotStorageDocument OneEmbeddedItem(string id = "fixture-shield", int shieldValue = 5)
        {
            var document = CompleteEmbeddedDocument();
            foreach (var entry in document.Slots) entry.Clear();
            var native = new EquipmentSlotNativeItem(typeof(DolocAPI));
            var item = (Item)native.Create(id);
            if (item is ItemHatShield shield) shield.SetShieldValue(shieldValue);
            document.Slots[0] = new EquipmentSlotStorageEntry { Index = 0, ItemId = id, DisplayName = id,
                ItemState = native.Capture(item), IsShield = item is ItemHatShield,
                ShieldValue = item is ItemHatShield ? shieldValue : 0, ShieldMaxValue = item is ItemHatShield ? 10 : 0 };
            return document;
        }
        private static void RequireFailure<T>(Action action, string diagnostic) where T : Exception
        {
            try { action(); }
            catch (T ex) { Assert(ex.Message.IndexOf(diagnostic, StringComparison.Ordinal) >= 0,
                "Wrong failure diagnostic: " + ex.Message); return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ": " + diagnostic);
        }
        [DataContract] private sealed class FixtureSaveEnvelope
        { [DataMember(Name = "cityData")] public FixtureCityEnvelope City = null!; }
        [DataContract] private sealed class FixtureCityEnvelope
        { [DataMember(Name = "dialogueManager")] public FixtureDialogueEnvelope Dialogue = null!; }
        [DataContract] private sealed class FixtureDialogueEnvelope
        { [DataMember(Name = "variableStorage")] public FixtureStorageEnvelope Storage = null!; }
        [DataContract] private sealed class FixtureStorageEnvelope
        { [DataMember(Name = "variables")] public Dictionary<string, object> Variables = null!; }
        private sealed class ColdFixtureCity
        { public ColdFixtureDialogue dialogueManager { get; } = new ColdFixtureDialogue(); }
        private sealed class ColdFixtureDialogue
        { public ColdFixtureVariables variableStorage { get; } = new ColdFixtureVariables(); }
        private sealed class ColdFixtureVariables
        {
            public Dictionary<string, object> variables { get; } = new Dictionary<string, object>();
            public void SetValue(string key, string value) => variables[key] = value;
        }

        private static void ProductNewGameUsesEmbeddedScopeAndExactSourceCleanup()
        {
            using (var f = new EmbeddedFixture("newgame-boundaries", 11, ""))
            {
                var source = CompleteEmbeddedDocument(11, "old-owner");
                f.WriteSource(source);
                byte[] sourceBytes = File.ReadAllBytes(f.SourcePath), nativeBytes = File.ReadAllBytes(f.NativePath);
                RequireFailure<InvalidDataException>(() => f.Runtime.OnSaveLoaded(10, true),
                    "MES embedded entry requires the initialized archive slot and active save epoch.");
                Assert(f.Epoch == 1 && f.Working == null && EqualBytes(sourceBytes, File.ReadAllBytes(f.SourcePath)) &&
                    EqualBytes(nativeBytes, File.ReadAllBytes(f.NativePath)), "Valid-epoch slot drift changed either authority.");
                RequireFailure<InvalidDataException>(() => f.Runtime.OnSaveLoaded(null, true),
                    "native current archive already exists: " + f.NativePath);
                Assert(f.Working == null && EqualBytes(sourceBytes, File.ReadAllBytes(f.SourcePath)) &&
                    EqualBytes(nativeBytes, File.ReadAllBytes(f.NativePath)), "Existing native current rejection changed either authority.");
                File.Delete(f.SourcePath);
                File.Delete(f.NativePath);
                f.Runtime.OnSaveLoaded(null, true);
                Assert(f.Session.State == EquipmentSlotEmbeddedState.Ready && f.Working!.Count == 3 &&
                    f.Working.All(s => !s.IsOccupied) && f.Runtime.InstalledPatchCount == 5,
                    "Readable absent key and absent sources did not start three clean NewGame slots: " + f.Session.State + "; " + f.Session.SourceProblem);
                var city = f.Archive.cityData;
                f.Archive.farmData.agentData.playerName = "new-owner";
                f.Archive.farmData.agentData.customPlayerName = "new-owner";
                f.Archive.baseDataOnLoad.customPlayerName = "new-owner";
                f.Runtime.OnSaveSaving(11);
                Assert(ReferenceEquals(city, f.Archive.cityData) && f.Document.Scope.PlayerName == "new-owner" &&
                    f.Epoch == 1 && !File.Exists(f.NativePath) && !File.Exists(f.SourcePath),
                    "NewGame Stage did not rebind post-dialogue names within the same archive/epoch without disk writes.");
                f.WriteNative(); f.Runtime.OnSaveSaved(11); f.ColdLoad();
                Assert(f.Session.State == EquipmentSlotEmbeddedState.Ready && f.Working!.All(s => !s.IsOccupied) &&
                    f.Document.Scope.PlayerName == "new-owner" && f.Epoch == 2,
                    "First NewGame native payload did not cold-load under a new city and epoch.");
            }
            foreach (string newOwner in new[] { "same-owner", "different-owner" })
            using (var f = new EmbeddedFixture("newgame-retained-" + newOwner, 11, newOwner))
            {
                var source = CompleteEmbeddedDocument(11, "same-owner");
                f.WriteSource(source); File.Copy(f.SourcePath, f.SourcePath + ".previous");
                string sibling = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(f.SourcePath))!, "slot-10", "sibling.json");
                Directory.CreateDirectory(Path.GetDirectoryName(sibling)!); File.WriteAllText(sibling, "sibling");
                byte[] before = File.ReadAllBytes(f.SourcePath);
                File.Delete(f.NativePath); f.Runtime.OnSaveLoaded(null, true);
                Assert(f.Working == null && EqualBytes(before, File.ReadAllBytes(f.SourcePath)) && File.Exists(f.SourcePath + ".previous") &&
                    File.ReadAllText(sibling) == "sibling" && DolocAPI.CostItemCallCount == 0,
                    "Reused NewGame slot automatically adopted or deleted its old owner's authority.");
                if (newOwner == "same-owner")
                {
                    Assert(f.Session.State == EquipmentSlotEmbeddedState.NeedsChoice, "Complete same-owner source needs an explicit group choice.");
                    f.Runtime.DeferEmbeddedChoice();
                    Assert(f.Session.Deferred && f.Working == null, "Defer auto-installed old equipment.");
                    f.Runtime.ReopenEmbeddedChoice(); f.Runtime.ChooseEmbedded(true);
                    string committedProjection = EquipmentSlotEmbeddedPayload.Encode(f.Document.Slots);
                    f.Runtime.OnSaveSaving(11);
                    Assert(!File.Exists(f.NativePath) && File.Exists(f.SourcePath), "Stage promoted disk authority or cleaned its source.");
                    RequireFailure<FileNotFoundException>(() => f.Runtime.OnSaveSaved(11), "The native current save archive is missing.");
                    Assert(File.Exists(f.SourcePath) && EquipmentSlotEmbeddedPayload.Encode(f.Document.Slots) == committedProjection &&
                        (bool)GetField(f.Runtime, "newGamePending").GetValue(f.Runtime)!, "Failed native save promoted Working or removed source.");
                    f.WriteNative(EquipmentSlotEmbeddedPayload.Encode(EquipmentSlotStorageDocument.CreateEmptySlots()));
                    f.Runtime.OnSaveSaved(11);
                    Assert(File.Exists(f.SourcePath) && EquipmentSlotEmbeddedPayload.Encode(f.Document.Slots) == committedProjection &&
                        (bool)GetField(f.Runtime, "newGamePending").GetValue(f.Runtime)!, "Wrong disk payload promoted Working or removed source.");
                    f.WriteNative(); f.Runtime.OnSaveSaved(11);
                    Assert(!File.Exists(f.SourcePath) && File.Exists(f.SourcePath + ".previous") &&
                        Directory.Exists(Path.GetDirectoryName(f.SourcePath)) && File.ReadAllText(sibling) == "sibling",
                        "Exact native commit did not clean only the explicitly selected primary source.");
                    f.ColdLoad();
                    Assert(f.Session.State == EquipmentSlotEmbeddedState.NeedsChoice && f.Session.Current!.Count == 3,
                        "Preserved previous authority did not require a fresh explicit choice after cold load.");
                }
                else Assert(f.Session.State == EquipmentSlotEmbeddedState.Unavailable && f.Session.SourceProblem.Length > 0,
                    "Wrong-scope old owner was treated as empty Working.");
            }
            using (var f = new EmbeddedFixture("newgame-missing-instance"))
            {
                var source = CompleteEmbeddedDocument(); source.Slots[1].ItemState = null; f.WriteSource(source);
                byte[] before = File.ReadAllBytes(f.SourcePath); File.Delete(f.NativePath);
                f.Runtime.OnSaveLoaded(null, true);
                Assert(f.Session.State == EquipmentSlotEmbeddedState.Unavailable && f.Working == null &&
                    EqualBytes(before, File.ReadAllBytes(f.SourcePath)), "Missing historical instance became empty NewGame authority.");
            }
            using (var f = new EmbeddedFixture("native-key-controls"))
            {
                f.Load(); Assert(f.Session.State == EquipmentSlotEmbeddedState.Ready, "Readable key absence was not established.");
                f.Archive.cityData!.dialogueManager.variableStorage.variables[EquipmentSlotEmbeddedArchive.Key] = "{\"schema\":99}";
                f.Load(); Assert(f.Session.State == EquipmentSlotEmbeddedState.Unavailable && f.Working == null,
                    "Present unknown native payload was treated as key absence.");
                f.Archive.cityData = null;
                RequireFailure<InvalidDataException>(f.Load, "Missing native archive member: cityData");
                Assert(f.Working == null, "Unreadable native city was treated as readable absence.");
            }
        }

        private static void ProductTraitsCommitOnlyExactEmbeddedNativeSave()
        {
            foreach (bool shutdown in new[] { false, true })
            using (var f = new EmbeddedFixture("traits-discard-" + shutdown))
            {
                var source = CompleteEmbeddedDocument(); f.SetCurrent(source.Slots); f.WriteNative();
                byte[] committed = File.ReadAllBytes(f.NativePath);
                ItemFactory.ShieldMaxValue = 6;
                try
                {
                    f.Load(); AssertRefreshedEmbeddedWorking(f, source);
                    if (shutdown) f.Runtime.DeactivateOwner("RuntimeShutdown"); else f.Runtime.ReturnedToTitle();
                    Assert(EqualBytes(committed, File.ReadAllBytes(f.NativePath)), "Unsaved trait refresh reached disk.");
                    if (!shutdown) { ItemFactory.ShieldMaxValue = 10; f.ColdLoad(); Assert(f.Session.Current![0].ShieldValue == 8,
                        "Title discard changed cold committed shield charge."); }
                }
                finally { ItemFactory.ShieldMaxValue = 10; }
            }
            using (var f = new EmbeddedFixture("traits-commit"))
            {
                var source = CompleteEmbeddedDocument(); f.SetCurrent(source.Slots); f.WriteNative();
                byte[] committed = File.ReadAllBytes(f.NativePath);
                ItemFactory.ShieldMaxValue = 6;
                try
                {
                    f.Load(); AssertRefreshedEmbeddedWorking(f, source); f.Runtime.OnSaveSaving(2);
                    Assert(EqualBytes(committed, File.ReadAllBytes(f.NativePath)), "Trait Stage wrote disk.");
                    f.WriteNative(); f.Runtime.OnSaveSaved(2); f.ColdLoad();
                    var slots = f.DiskSlots();
                    Assert(slots[0].DefenseBonus == 7 && slots[0].ShieldMaxValue == 6 && slots[0].ShieldValue == 6 && slots[0].ShieldDefend == 2 &&
                        slots[1].ItemState!.Filled == false && slots[2].ItemState!.Cooldown == 77 &&
                        slots.Select(s => s.ItemState!.InstanceId).SequenceEqual(source.Slots.Select(s => s.ItemState!.InstanceId)),
                        "Committed native refresh lost shield traits, filled/cooldown state or instance identities.");
                }
                finally { ItemFactory.ShieldMaxValue = 10; }
            }
        }
        private static void AssertRefreshedEmbeddedWorking(EmbeddedFixture f, EquipmentSlotStorageDocument source) =>
            Assert(f.Working![0].DefenseBonus == 7 && f.Working[0].ShieldMaxValue == 6 && f.Working[0].ShieldValue == 6 &&
                f.Working[0].ShieldDefend == 2 && f.Document.Slots[0].ShieldValue == 8 && source.Slots[0].DefenseBonus == 3 &&
                (bool)GetField(f.Runtime, "workingDirty").GetValue(f.Runtime)!, "Load trait refresh did not preserve committed native projection.");

        private static void SameItemEmbeddedReplacementPreservesInstances()
        {
            using (var f = new EmbeddedFixture("same-id"))
            {
                var source = OneEmbeddedItem(); f.Seed(source.Slots);
                DolocAPI.SetItemCount("fixture-shield", 1);
                var incoming = (ItemHatShield)DolocAPI.Instances.Single(); incoming.SetShieldValue(3);
                byte[] committed = File.ReadAllBytes(f.NativePath);
                Assert(f.Runtime.EquipFromBackpack(incoming.name, 0) && DolocAPI.CostItemCallCount == 1 &&
                    DolocAPI.TryPlaceInBackpackCallCount == 1 && DolocAPI.CountItem(incoming.name, false) == 1 &&
                    !ReferenceEquals(DolocAPI.Instances.Single(), incoming) && ((ItemHatShield)DolocAPI.Instances.Single()).ShieldValue == 5,
                    "Same-ID replacement did not withdraw the selected instance and return exactly one old shield.");
                f.Runtime.OnSaveSaving(2);
                Assert(f.Working![0].ShieldValue == 3 && f.Working[0].ItemState!.InstanceId != source.Slots[0].ItemState!.InstanceId &&
                    EqualBytes(committed, File.ReadAllBytes(f.NativePath)) && f.Document.GameplayCandidate == null && f.Document.Journal == null,
                    "Same-ID Stage changed committed authority or invented a sidecar transaction.");
                f.WriteNative(); f.Runtime.OnSaveSaved(2); f.ColdLoad();
                Assert(f.Working![0].ShieldValue == 3 && f.DiskSlots()[0].ShieldValue == 3, "Same-ID committed incoming shield did not cold-load.");
            }
        }
        private static void ProductIncomingUnknownBlocksReplayAndSave()
        {
            foreach (bool replacement in new[] { false, true })
            using (var f = new EmbeddedFixture("incoming-unknown-" + replacement))
            {
                if (replacement) f.Seed(OneEmbeddedItem("fixture-hat").Slots); else f.Load();
                byte[] committed = File.ReadAllBytes(f.NativePath);
                bool existed = EquipmentSlotEmbeddedArchive.ReadCurrent(out object? before);
                DolocAPI.SetItemCount("fixture-shield", 1); DolocAPI.CostItemThrowAfterMutation = true;
                Assert(!f.Runtime.EquipFromBackpack("fixture-shield", 0) && DolocAPI.GetStoredItemCount("fixture-shield") == 0 &&
                    DolocAPI.CostItemCallCount == 1 && GetField(f.Runtime, "pendingGameplayIncomingWithdrawal").GetValue(f.Runtime) != null,
                    "Mutate-then-throw withdrawal did not retain one current incoming guard.");
                Assert(!f.Runtime.EquipFromBackpack("fixture-shield", 0) && !f.Runtime.RequestUnequip(0), "Unknown incoming allowed a replay.");
                for (int i = 0; i < 2; i++) RequireFailure<InvalidOperationException>(() => f.Runtime.OnSaveSaving(2), "incoming native withdrawal has an unknown outcome");
                RequireFailure<InvalidOperationException>(() => f.Runtime.PrepareOwnerDeactivation("OfficialDisable"), "incoming native withdrawal has an unknown outcome");
                bool existsAfter = EquipmentSlotEmbeddedArchive.ReadCurrent(out object? after);
                Assert(DolocAPI.CostItemCallCount == 1 && DolocAPI.TryPlaceInBackpackCallCount == (replacement ? 1 : 0) &&
                    DolocAPI.GetStoredItemCount("fixture-hat") == (replacement ? 1 : 0) && existed == existsAfter && Equals(before, after) &&
                    EqualBytes(committed, File.ReadAllBytes(f.NativePath)), "Unknown incoming replayed, duplicated outgoing equipment or staged authority.");
            }
        }
        private static void ProductMailOutcomeUnknownReconcilesImmediatelyWithoutReplay()
        {
            using (var f = new EmbeddedFixture("mail-immediate"))
            {
                f.Seed(OneEmbeddedItem("fixture-hat").Slots); DolocAPI.BackpackPlacementAvailable = false;
                var mail = f.Archive.farmData.emailManager; mail.emails = new object();
                Assert(!f.Runtime.RequestUnequip(0) && DolocAPI.SendItemAsEmailCallCount == 0 && f.Runtime.GetSlotItemId(0) == "fixture-hat",
                    "Unreadable mail preflight called a mutator or released Working.");
                mail.emails = Array.Empty<object>(); var exact = ExactItemMail("fixture-hat");
                DolocAPI.SendItemAsEmailOverride = (id, count) =>
                { mail.SetReadSequence(new object(), exact); return id == "fixture-hat" && count == 1; };
                Assert(f.Runtime.RequestUnequip(0) && DolocAPI.SendItemAsEmailCallCount == 1 && f.Runtime.GetSlotItemId(0) == "",
                    "One immediate exact observation did not reconcile mail without replay.");
                f.Save();
                Assert(DolocAPI.SendItemAsEmailCallCount == 1 && !f.DiskSlots()[0].IsOccupied &&
                    EquipmentSlotNativeMailEvidence.CountUnacceptedDtmapiItemMail(typeof(DolocAPI), "fixture-hat") == 1,
                    "Native commit replayed mail or retained a second equipment copy.");
            }
        }
        private static object[] ExactItemMail(string id) => new object[] { new FixtureEmail { Id = "send_item_template",
            emailAttaches = new object[] { new EmailAttachReward { isAccept = false, reward = new RewardItem { itemName = id, itemCount = 1 } } } } };
        private static void ProductOutcomeUnknownNoSaveTitlePreservesAuthorities()
        {
            using (var f = new EmbeddedFixture("mail-no-save"))
            {
                f.Seed(OneEmbeddedItem("fixture-hat").Slots);
                var oldSource = OneEmbeddedItem("fixture-hat"); f.WriteSource(oldSource);
                f.Load();
                Assert(f.Session.State == EquipmentSlotEmbeddedState.NeedsChoice, "No-save source was not captured through ordinary entry.");
                f.Runtime.ChooseEmbedded(true);
                byte[] source = File.ReadAllBytes(f.SourcePath), disk = File.ReadAllBytes(f.NativePath);
                DolocAPI.BackpackPlacementAvailable = false;
                DolocAPI.SendItemAsEmailOverride = (_, _) => { f.Archive.farmData.emailManager.emails = new object(); return false; };
                Assert(!f.Runtime.RequestUnequip(0) && DolocAPI.SendItemAsEmailCallCount == 1 && f.Runtime.GetSlotItemId(0) == "fixture-hat",
                    "Unknown mail did not retain Working ownership.");
                Assert(!f.Runtime.RequestUnequip(0), "Unknown mail allowed a second native attempt.");
                RequireFailure<InvalidOperationException>(() => f.Runtime.OnSaveSaving(2), "pending gameplay placement");
                f.Archive.farmData.emailManager.emails = ExactItemMail("fixture-hat");
                RequireFailure<InvalidOperationException>(() => f.Runtime.OnSaveSaving(2), "pending gameplay placement");
                Assert(DolocAPI.SendItemAsEmailCallCount == 1, "Delayed mail evidence caused replay.");
                f.Runtime.ReturnedToTitle();
                Assert(EqualBytes(source, File.ReadAllBytes(f.SourcePath)) && EqualBytes(disk, File.ReadAllBytes(f.NativePath)) &&
                    GetField(f.Runtime, "pendingGameplayPlacement").GetValue(f.Runtime) == null && f.Runtime.GetDiagnosticsSnapshot().RootCount == 0,
                    "No-save title changed retained/native authority or kept pending roots.");
                f.ColdLoad(); Assert(f.Session.Current![0].ItemId == "fixture-hat", "No-save title lost committed native equipment.");
            }
        }
        private static void ProductLegacyTransactionsRemainUnavailable()
        {
            // The retired normal durable writer is covered by the registered
            // policy/store tests and retained Compatibility tests. Ordinary entry
            // must preserve these bytes and must never replay those transactions.
            foreach (string kind in new[] { "incoming-unknown", "mail-unknown", "ambiguous", "candidate" })
            using (var f = new EmbeddedFixture("legacy-" + kind))
            {
                var document = OneEmbeddedItem();
                if (kind == "candidate") EquipmentSlotGameplayCandidateCoordinator.Prepare(document,
                    EquipmentSlotGameplayCandidateCoordinator.CloneSlots(document.Slots), "old-preimage",
                    _ => new NativeRecoveryObservation("old-preimage", 0, 0));
                else
                {
                    var journal = kind == "incoming-unknown"
                        ? EquipmentSlotTransactionCoordinator.PrepareReplacement(document, 0, OneEmbeddedItem("fixture-hat").Slots[0])
                        : EquipmentSlotTransactionCoordinator.PrepareRecovery(document, new[] { 0 }, EquipmentSlotTransactionOrigin.OwnerRecovery);
                    EquipmentSlotTransactionCoordinator.StartAttempt(document, "old-preimage", _ => new NativeRecoveryObservation("old-preimage", 0, 0));
                    if (kind != "mail-unknown") EquipmentSlotTransactionCoordinator.RecordPlacement(journal, 0,
                        new NativePlacementResult(NativePlacementKind.Backpack, 1, 0, "retained exact outgoing"),
                        new NativeRecoveryObservation("old-preimage", 1, 0));
                }
                f.WriteSource(document); byte[] before = File.ReadAllBytes(f.SourcePath), disk = File.ReadAllBytes(f.NativePath);
                f.Load(); Assert(f.Session.State == EquipmentSlotEmbeddedState.Unavailable && f.Working == null &&
                    f.Session.SourceProblem.Contains("未决 journal/GameplayCandidate"), "Retained " + kind + " gained normal authority.");
                f.Runtime.OnSaveSaving(2); f.Runtime.OnSaveSaved(2);
                Assert(EqualBytes(before, File.ReadAllBytes(f.SourcePath)) && EqualBytes(disk, File.ReadAllBytes(f.NativePath)) &&
                    !EquipmentSlotEmbeddedArchive.HasCurrentKey() && DolocAPI.CostItemCallCount == 0 &&
                    DolocAPI.TryPlaceInBackpackCallCount == 0 && DolocAPI.SendItemAsEmailCallCount == 0,
                    "Ordinary save replayed/promoted or rewrote retained " + kind + ".");
            }
        }
        private static void RunNativeEmbeddedPresenceBlocksColdRecovery()
        {
            foreach (string state in new[] { "empty", "invalid", "unknown", "unreadable" })
            {
                DolocAPI.ResetInventory();
                var archive = NewArchive(2, "fixture-player"); DolocAPI.archiveHandle = archive;
                Assert(!EquipmentSlotEmbeddedArchive.HasCurrentKey(), "Presence negative needs a readable absence control.");
                if (state == "unreadable") archive.cityData = null;
                else archive.cityData!.dialogueManager.variableStorage.variables[EquipmentSlotEmbeddedArchive.Key] =
                    state == "empty" ? EquipmentSlotEmbeddedPayload.Encode(EquipmentSlotStorageDocument.CreateEmptySlots()) :
                    state == "invalid" ? "broken payload" : "{\"schema\":99}";
                string root = CreateFixtureScenarioRoot("ech-native-key");
                var host = new FakeHost(root);
                var core = new DTMAPI.Core.Runtime.DtmApiRuntime(host, new DTMAPI.ModConfigMenu.ConfigMenuRegistry());
                object service = CreateService(core);
                string path = Path.Combine(core.Paths.ConfigPath, "protected-items", "equipment-slots", "slot-2",
                    "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json");
                var source = OneEmbeddedItem("fixture-hat");
                new EquipmentSlotDocumentStore().WriteAtomic(path, source); byte[] before = File.ReadAllBytes(path);
                object?[] arguments = { path, CreateHostScope(service, 2, "fixture-player"), false, 0, "" };
                var result = RequireMethod(service.GetType(), "TryRecoverMoreEquipmentSlotsProductStorage").Invoke(service, arguments);
                var presence = core.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Player.EquipmentSlotsApi");
                Assert(result is bool recovered && !recovered && arguments[2] is bool handled && handled && (int)arguments[3]! == 0 &&
                    (string)arguments[4]! == "Embedded MES data exists or cannot be inspected; legacy source retained without automatic recovery." &&
                    CountProductColdRecoverySessions(service) == 0 && EqualBytes(before, File.ReadAllBytes(path)) &&
                    DolocAPI.CostItemCallCount == 0 && DolocAPI.TryPlaceInBackpackCallCount == 0 && DolocAPI.SendItemAsEmailCallCount == 0 &&
                    presence.Status == "embedded-mes-preserved" &&
                    presence.Details.Contains(state == "unreadable" ? "presence could not be inspected" : "key is present"),
                    "Native " + state + " did not block retained cold recovery with exact evidence and unchanged source.");
            }
            DolocAPI.ResetInventory();
        }
        private static void ProductFailClosedSaveSavingCancelsCoreBoundary()
        {
            foreach (bool incoming in new[] { true, false })
            using (var f = new EmbeddedFixture("core-save-veto-" + incoming))
            {
                if (incoming)
                {
                    f.Load(); DolocAPI.SetItemCount("fixture-shield", 1); DolocAPI.CostItemThrowAfterMutation = true;
                    Assert(!f.Runtime.EquipFromBackpack("fixture-shield", 0), "Core incoming guard was not established.");
                }
                else
                {
                    f.Seed(OneEmbeddedItem("fixture-hat").Slots); DolocAPI.BackpackPlacementAvailable = false;
                    DolocAPI.SendItemAsEmailOverride = (_, _) => { f.Archive.farmData.emailManager.emails = new object(); return false; };
                    Assert(!f.Runtime.RequestUnequip(0), "Core outgoing guard was not established.");
                }
                byte[] disk = File.ReadAllBytes(f.NativePath);
                var core = CreateFixtureCoreRuntime("embedded-core-veto");
                IEventsHelper events = CreateEventsProxy(core, "fixture.embedded.save-veto");
                events.Save.SaveSaving += (_, e) => f.Runtime.OnSaveSaving(e.SaveSlot);
                core.Start();
                Assert(!core.TryNotifySaveSaving(2) && !core.TryNotifySaveSaving(2) &&
                    DolocAPI.CostItemCallCount == (incoming ? 1 : 0) && DolocAPI.SendItemAsEmailCallCount == (incoming ? 0 : 1) &&
                    EqualBytes(disk, File.ReadAllBytes(f.NativePath)), "Current unresolved mutation did not veto Core SaveSaving without replay.");
            }
        }
        private static void ProductRuntimeShutdownDiscardsUnsavedWorkingState()
        {
            foreach (bool equip in new[] { true, false })
            using (var f = new EmbeddedFixture("shutdown-" + equip))
            {
                if (equip) { f.Load(); DolocAPI.SetItemCount("fixture-shield", 1); }
                else f.Seed(OneEmbeddedItem().Slots);
                byte[] before = File.ReadAllBytes(f.NativePath);
                Assert(equip ? f.Runtime.EquipFromBackpack("fixture-shield", 0) : f.Runtime.RequestUnequip(0), "Shutdown Working mutation failed.");
                f.Runtime.DeactivateOwner("RuntimeShutdown"); var s = f.Runtime.GetDiagnosticsSnapshot();
                Assert(EqualBytes(before, File.ReadAllBytes(f.NativePath)) && s.PatchCount == 0 && s.CloneCount == 0 &&
                    s.ListenerCount == 0 && s.FunctionCount == 0 && s.CallbackCount == 0 && s.RootCount == 0 && GetProductCallbackRuntime() == null,
                    "RuntimeShutdown committed unsaved Working or retained owned roots.");
            }
        }
        private static void ProductOwnerDeactivationStagesMemoryAndCleansRoots()
        {
            foreach (string mutation in new[] { "unequip", "equip", "break", "damage" })
            using (var f = new EmbeddedFixture("owner-" + mutation))
            {
                if (mutation == "equip") { f.Load(); DolocAPI.SetItemCount("fixture-shield", 1); Assert(f.Runtime.EquipFromBackpack("fixture-shield", 0), "Owner equip failed."); }
                else
                {
                    f.Seed(OneEmbeddedItem().Slots);
                    if (mutation == "unequip") Assert(f.Runtime.RequestUnequip(0), "Owner unequip failed.");
                    else if (mutation == "break") Assert(!f.Runtime.TryBlockNativeAttack(20, out int blocked) && blocked == 5, "Owner shield break failed.");
                    else Assert(f.Working![0].ShieldDefend == 2 && f.Runtime.TryBlockNativeAttack(5, out int first) && first == 3 &&
                        f.Runtime.TryBlockNativeAttack(3, out int second) && second == 1 && f.Working[0].ShieldValue == 1 &&
                        f.Document.Slots[0].ShieldValue == 5, "Owner repeated native shield damage did not leave Working-only charge.");
                }
                byte[] disk = File.ReadAllBytes(f.NativePath);
                f.Runtime.PrepareOwnerDeactivation("OfficialDisable"); string staged = f.Staged;
                Assert(f.Document.Journal == null && f.Document.GameplayCandidate == null && EqualBytes(disk, File.ReadAllBytes(f.NativePath)),
                    "Official prepare invented recovery authority or committed unsaved gameplay.");
                f.Runtime.DeactivateOwner("OfficialDisable"); var state = f.Runtime.GetDiagnosticsSnapshot();
                Assert(f.Staged == staged && EqualBytes(disk, File.ReadAllBytes(f.NativePath)) && state.PatchCount == 0 && state.RootCount == 0 &&
                    state.FunctionCount == 0 && GetProductCallbackRuntime() == null, "Official owner cleanup retained roots or rewrote native disk.");
            }
        }
        private static void ProductCleanOwnerRestartUsesNativeCommitted()
        {
            using (var f = new EmbeddedFixture("clean-owner"))
            {
                f.Seed(OneEmbeddedItem().Slots); byte[] disk = File.ReadAllBytes(f.NativePath);
                f.Runtime.PrepareOwnerDeactivation("OfficialDisable"); f.Runtime.DeactivateOwner("OfficialDisable");
                Assert(DolocAPI.CostItemCallCount == 0 && DolocAPI.TryPlaceInBackpackCallCount == 0 &&
                    EqualBytes(disk, File.ReadAllBytes(f.NativePath)), "Clean owner synthesized typed recovery or a native item.");
                var restarted = new MoreEquipmentSlotsNativeRuntime(NullMonitor.Instance, f.ConfigPath);
                try
                {
                    restarted.SetArchiveContext(() => f.Epoch); restarted.SetProtectedSources(f.Sources);
                    restarted.Configure(new MoreEquipmentSlotsConfig { Enabled = false }, "same-process fresh owner"); restarted.OnSaveLoaded(2);
                    Assert(restarted.GetSlotItemId(0) == "fixture-shield" && restarted.InstalledPatchCount == 5,
                        "Fresh owner did not use the committed native group with old false config.");
                }
                finally { restarted.DeactivateOwner("RuntimeShutdown"); }
            }
        }
        private static void ProductOldFalseConfigurationKeepsGameplay()
        {
            using (var f = new EmbeddedFixture("actual-entry-old-false"))
            {
                f.Runtime.DeactivateOwner("RuntimeShutdown");
                var helper = new ProductModHelper(Manifest(MoreEquipmentSlotsProductContract.UniqueId), f.ConfigPath, f.Sources, () => f.Epoch, false);
                Assert(!helper.ReadConfig<MoreEquipmentSlotsConfig>().Enabled && File.ReadAllText(f.ConfigPath).Contains("\"Enabled\":false"),
                    "Actual entry fixture did not read old JSON false.");
                var entry = new ModEntry();
                try
                {
                    entry.Entry(helper);
                    var runtime = (MoreEquipmentSlotsNativeRuntime)GetField(entry, "runtime").GetValue(entry)!;
                    helper.RaiseSaveLoaded(2); DolocAPI.SetItemCount("fixture-shield", 1);
                    Assert(runtime.InstalledPatchCount == 5 && runtime.EquipFromBackpack("fixture-shield", 0), "Old false disabled actual Entry/gameplay.");
                    byte[] disk = File.ReadAllBytes(f.NativePath);
                    var requested = (MoreEquipmentSlotsConfig)GetField(entry, "config").GetValue(entry)!;
                    requested.Enabled = false; requested.VerboseLogging = true; Invoke(entry, "SaveConfig");
                    Assert(runtime.InstalledPatchCount == 5 && runtime.GetSlotItemId(0) == "fixture-shield" &&
                        EqualBytes(disk, File.ReadAllBytes(f.NativePath)), "Old disable changed Working/hooks or disk on config save.");
                    entry.DtmApiPrepareOwnerDeactivation("OfficialDisable");
                    Assert(EquipmentSlotEmbeddedArchive.HasCurrentKey() && EqualBytes(disk, File.ReadAllBytes(f.NativePath)), "Official Entry prepare failed memory-only Stage.");
                    entry.DtmApiDeactivateOwner("OfficialDisable");
                    Assert(runtime.GetDiagnosticsSnapshot().RootCount == 0 && runtime.InstalledPatchCount == 0, "Official Entry owner cleanup retained roots.");
                }
                finally { entry.DtmApiDeactivateOwner("RuntimeShutdown"); }
            }
        }
        private static void ProductConfigWriteFailureLeavesCommittedStateUntouched()
        {
            using (var f = new EmbeddedFixture("actual-entry-config-fault"))
            {
                f.Runtime.DeactivateOwner("RuntimeShutdown");
                var helper = new ProductModHelper(Manifest(MoreEquipmentSlotsProductContract.UniqueId), f.ConfigPath, f.Sources, () => f.Epoch);
                var entry = new ModEntry();
                try
                {
                    entry.Entry(helper); helper.RaiseSaveLoaded(2);
                    var runtime = (MoreEquipmentSlotsNativeRuntime)GetField(entry, "runtime").GetValue(entry)!;
                    DolocAPI.SetItemCount("fixture-shield", 1); Assert(runtime.EquipFromBackpack("fixture-shield", 0), "Config-fault actual gameplay failed.");
                    byte[] disk = File.ReadAllBytes(f.NativePath), config = File.ReadAllBytes(f.ConfigPath);
                    var requested = (MoreEquipmentSlotsConfig)GetField(entry, "config").GetValue(entry)!; requested.VerboseLogging = true;
                    helper.ConfigStore.FailNextWrite = true;
                    RequireFailure<IOException>(() => Invoke(entry, "SaveConfig"), "fixture config write failure");
                    Assert(EqualBytes(disk, File.ReadAllBytes(f.NativePath)) && EqualBytes(config, File.ReadAllBytes(f.ConfigPath)) &&
                        runtime.GetSlotItemId(0) == "fixture-shield" && runtime.InstalledPatchCount == 5 &&
                        !((MoreEquipmentSlotsConfig)GetField(entry, "appliedConfig").GetValue(entry)!).VerboseLogging &&
                        !((MoreEquipmentSlotsConfig)GetField(entry, "config").GetValue(entry)!).VerboseLogging && !helper.ConfigStore.Persisted.VerboseLogging,
                        "Actual config-write failure changed gameplay, disk, hooks or visible/applied/persisted logging config.");
                }
                finally { entry.DtmApiDeactivateOwner("RuntimeShutdown"); }
            }
        }
        private sealed class FixtureRuntimeContext : RealProxy
        {
            private readonly Func<long?> epoch;
            internal FixtureRuntimeContext(Func<long?> epoch) : base(typeof(IDtmRuntimeContext)) => this.epoch = epoch;
            public override IMessage Invoke(IMessage message)
            {
                var call = (IMethodCallMessage)message;
                if (call.MethodName != "get_Snapshot") return new ReturnMessage(new MissingMethodException(call.MethodName), call);
                return new ReturnMessage(new DtmRuntimeContextSnapshot(Guid.Empty, 1, DtmRuntimePhase.LoadingSave, epoch(), null),
                    null, 0, call.LogicalCallContext, call);
            }
        }
    }
}
