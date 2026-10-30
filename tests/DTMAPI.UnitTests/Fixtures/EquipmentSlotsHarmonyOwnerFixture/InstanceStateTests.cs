using System;
using System.Collections;
using System.Linq;
using DTMAPI.MoreEquipmentSlots;
using DolocTown;

namespace DTMAPI.EquipmentSlotsHarmonyOwnerFixture
{
    internal static partial class Program
    {
        private static void ProductNativeItemStateRoundTrips()
        {
            ResetOwners();
            DolocAPI.ResetInventory();
            DolocAPI.archiveHandle = new FixtureArchive(new DolocTown.GameData.AgentEquipmentManager());
            var items = new EquipmentSlotNativeItem(typeof(DolocAPI));
            var placement = new EquipmentSlotNativePlacement(new DolocTownItemPlacementGateway());
            foreach (string id in new[] { "fixture-shield", "fixture-herb-package", "fixture-cooldown" })
            {
                var item = (Item)items.Create(id);
                if (item is ItemHatShield shield) shield.SetShieldValue(3);
                if (item is ItemHerbPackage herb) herb.SetFilled(false);
                if (item is ItemCooldown cooldown) cooldown.cdDuration = 123;
                var slot = new EquipmentSlotStorageEntry
                {
                    ItemId = id, ItemState = items.Capture(item),
                    IsShield = item is ItemHatShield, ShieldValue = item is ItemHatShield ? 3 : 0,
                    ShieldMaxValue = item is ItemHatShield ? 10 : 0
                };
                Assert(placement.PlaceOneWithImmediateEvidence(slot).Success, "Stateful placement did not use the native Item overload.");
                var restored = (Item)items.FindBackpack(id)!;
                Assert(restored is ItemHatShield s ? s.ShieldValue == 3 :
                    restored is ItemHerbPackage h ? !h.isFilled : ((ItemCooldown)restored).cdDuration == 123,
                    "Stateful native backpack round trip regenerated default state.");
                Assert(ReferenceEquals(items.Withdraw(restored), restored), "Withdrawal did not preserve the selected instance.");
                DolocAPI.BackpackPlacementAvailable = false;
                int mails = DolocAPI.SendItemAsEmailCallCount;
                Assert(!placement.PlaceOneWithImmediateEvidence(slot).Success && DolocAPI.SendItemAsEmailCallCount == mails,
                    "Stateful recovery used an ID-only mail destination.");
                DolocAPI.BackpackPlacementAvailable = true;
            }
            DolocAPI.ResetInventory();
        }

        private static void ProductEffectsRetainUnchangedItems()
        {
            ResetOwners();
            var legacy = new HarmonyLib.Harmony(MoreEquipmentSlotsProductContract.LegacyHarmonyOwner);
            legacy.Patch(ReloadParamsTarget, postfix: new HarmonyLib.HarmonyMethod(typeof(Program), nameof(LegacyInstanceOwnerPostfix)));
            var rejected = CreateProductRuntime();
            bool blocked = false;
            try { rejected.Configure(new MoreEquipmentSlotsConfig { Enabled = true }, "legacy owner conflict"); }
            catch (InvalidOperationException) { blocked = true; }
            Assert(blocked && rejected.InstalledPatchCount == 0, "New owner overlapped a retained published owner.");
            legacy.UnpatchSelf();
            rejected.DeactivateOwner("RuntimeShutdown");
            using (var fixture = new EmbeddedFixture("unchanged-functions"))
            {
                fixture.Load();
                var manager = fixture.Archive.farmData.agentData.agentEquipment;
                var runtime = fixture.Runtime;
                var document = fixture.Document;
                DolocAPI.SetItemCount("fixture-herb-package", 1);
                var herb = (ItemHerbPackage)DolocAPI.Instances.Single();
                herb.SetFilled(false);
                Assert(runtime.EquipFromBackpack(herb.name, 0), "Actual empty herb package could not equip.");
                var functions = (IDictionary)EquipmentSlotNativeItem.Read(manager, "functions")!;
                var effect = (AgentEquipmentFunction)functions[herb]!;
                Assert(effect.Created == 1 && ReferenceEquals(effect.Item, herb), "Effect did not own the exact equipped item.");
                DolocAPI.SetItemCount("fixture-cooldown", 1);
                var cooldown = (ItemCooldown)DolocAPI.Instances.Single();
                cooldown.cdDuration = 81;
                Assert(runtime.EquipFromBackpack(cooldown.name, 1), "Cooldown item could not equip.");
                runtime.Configure(new MoreEquipmentSlotsConfig { Enabled = true }, "unchanged settings");
                Assert(ReferenceEquals(functions[herb], effect) && effect.Created == 1 && effect.Removed == 0,
                    "Another slot or a settings refresh rebuilt the herb function.");
                cooldown.cdDuration = 79;
                Assert(runtime.RequestUnequip(1), "Cooldown item could not return to backpack.");
                Assert(((ItemCooldown)DolocAPI.Instances.Single()).cdDuration == 79,
                    "Native in-place consumption was not captured on unequip.");
                Assert(runtime.RequestUnequip(0) && !((ItemHerbPackage)DolocAPI.Instances.Single(i => i is ItemHerbPackage)).isFilled && effect.Removed == 1,
                    "Unequip lost empty state or skipped native OnRemove.");
                Assert(document.Slots.All(s => !s.IsOccupied), "Unsaved gameplay mutated the committed projection.");
                runtime.ReturnedToTitle();
                Assert(runtime.BuildLifecycleSummary().EndsWith("roots=0", StringComparison.Ordinal), "Title retained native item roots.");
                runtime.DeactivateOwner("RuntimeShutdown");
            }
        }
        private static void LegacyInstanceOwnerPostfix() { }
    }
}
