using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DTMAPI.MoreEquipmentSlots;

namespace DTMAPI.UnitTests
{
    internal static class ItemStateAndEffectsTests
    {
        internal static void RunAll()
        {
            SnapshotBoundaries();
            EffectLifecycle();
        }

        private static void SnapshotBoundaries()
        {
            var document = new EquipmentSlotStorageDocument
            {
                Scope = new EquipmentSlotSaveScope { ArchiveIndex = 2, PlayerName = "state-fixture", CustomPlayerName = "state-fixture", TotalGameSeconds = 10 }
            };
            document.Slots[0].ItemId = "herb";
            document.Slots[0].ItemState = new EquipmentSlotItemState
            {
                InstanceId = Guid.NewGuid().ToString("N"), NativeType = "DolocTown.ItemHerbPackage", Filled = true
            };
            var working = EquipmentSlotGameplayCandidateCoordinator.CloneSlots(document.Slots);
            working[0].ItemState!.Filled = false;
            Check(document.Slots[0].ItemState!.Filled == true, "Working consumption changed committed state.");
            EquipmentSlotGameplayCandidateCoordinator.Prepare(document, working, "before",
                id => new NativeRecoveryObservation("before", 0, 0));
            working[0].ItemState!.Filled = true;
            Check(document.GameplayCandidate!.WorkingSlots[0].ItemState!.Filled == false,
                "Candidate shares mutable state with Working.");
            string root = Path.Combine(Environment.GetEnvironmentVariable("DTMAPI_TEST_SESSION_ROOT")!, "item-state");
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "sidecar.json");
            var store = new EquipmentSlotDocumentStore();
            store.WriteAtomic(path, document);
            Check(EquipmentSlotDocumentStore.TryReadValidated(path, document.Scope, out var read, out _), "New state could not round-trip.");
            Check(read!.Slots[0].ItemState!.Filled == true && read.GameplayCandidate!.WorkingSlots[0].ItemState!.Filled == false,
                "Persistence merged committed and candidate item state.");
            read.GameplayCandidate!.NativeExpectations.Clear();
            bool rejected = false;
            try { store.WriteAtomic(path, read); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Missing outgoing/incoming item evidence was accepted.");
            document.GameplayCandidate = null;
            document.Slots[1] = document.Slots[0].Clone(1);
            rejected = false;
            try { store.WriteAtomic(path, document); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Two slots sharing one native identity were accepted.");
            document.Slots[1].Clear();
            EquipmentSlotTransactionCoordinator.PrepareRecovery(document, new[] { 0 }, EquipmentSlotTransactionOrigin.OwnerRecovery);
            Check(document.Journal!.Escrow[0].ItemState!.Filled == true, "Recovery lost the item snapshot.");
            var recovered = document.Journal.Escrow[0].ToSlot();
            recovered.ItemState!.Filled = false;
            Check(document.Journal.Escrow[0].ItemState!.Filled == true, "Recovery placement shares its durable snapshot.");
            document.Journal.Escrow[0].ItemState!.Cooldown = 5;
            rejected = false;
            try { store.WriteAtomic(path, document); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Contradictory native item state was accepted.");
        }

        private static void EffectLifecycle()
        {
            var effects = new EquipmentSlotEffects();
            IDictionary native = new Hashtable();
            object first = new object(), second = new object(), third = new object();
            var created = new List<Function>();
            Func<object, string, object> factory = (item, skill) => { var f = new Function(); created.Add(f); return f; };
            var desired = new[] { Input(0, first), Input(1, second) };
            effects.Reconcile(native, desired, factory);
            Check(created.Count == 2 && created[0].Creates == 1 && created[1].Creates == 1, "Native creation lifecycle missing.");
            effects.Reconcile(native, desired, factory);
            Check(created.Count == 2, "Unchanged effects were rebuilt.");
            effects.Reconcile(native, new[] { Input(0, third), Input(1, second) }, factory);
            Check(created.Count == 3 && created[0].Removes == 1 && created[1].Removes == 0,
                "Changing one slot rebuilt another effect.");
            created[1].FailRemove = true;
            Throws(effects.Clear);
            Check(effects.Count == 1 && created[2].Removes == 1 && native.Count == 0,
                "Failed removal lost retry ownership or blocked other releases.");
            effects.Clear();
            effects.Clear();
            Check(effects.Count == 0 && created[1].Removes == 2 && created[2].Removes == 1,
                "Removal retry did not converge exactly once after success.");
            var bad = new Function { FailCreate = true, FailRemove = true };
            Throws(() => effects.Reconcile(native, new[] { Input(0, first) }, (i, s) => bad));
            Check(effects.Count == 1 && native.Count == 0, "Failed creation remained active or lost cleanup ownership.");
            effects.Clear();
            Check(effects.Count == 0 && native.Count == 0, "Partial creation could not be cleaned up.");
            effects.Reconcile(native, new[] { Input(0, first) }, factory);
            object external = new object();
            native[first] = external;
            effects.Clear();
            Check(ReferenceEquals(native[first], external), "Cleanup removed an external replacement.");
        }

        private static EquipmentSlotEffects.EffectInput Input(int index, object item) => new EquipmentSlotEffects.EffectInput(index, item, "skill");
        private static void Throws(Action action)
        {
            try { action(); } catch (AggregateException) { return; }
            throw new InvalidOperationException("Expected lifecycle failure was lost.");
        }
        private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        private sealed class Function
        {
            internal int Creates, Removes;
            internal bool FailCreate, FailRemove;
            public void OnCreate() { Creates++; if (FailCreate) { FailCreate = false; throw new InvalidOperationException("create"); } }
            public void OnRemove() { Removes++; if (FailRemove) { FailRemove = false; throw new InvalidOperationException("remove"); } }
        }
    }
}
