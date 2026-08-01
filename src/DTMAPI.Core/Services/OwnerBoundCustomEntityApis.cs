#pragma warning disable CS0618 // Core intentionally preserves owner-bound facades for the frozen CustomEntity registry ABI.
using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Services
{
    internal static class OwnerBoundCustomEntityApis
    {
        internal static IOwnerBoundApiFactory ForAnimal(ICustomAnimalApi api) =>
            new TypedFactory<ICustomAnimalApi>((owner, active) => new AnimalFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForMonster(ICustomMonsterApi api) =>
            new TypedFactory<ICustomMonsterApi>((owner, active) => new MonsterFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForAttack(ICustomAttackApi api) =>
            new TypedFactory<ICustomAttackApi>((owner, active) => new AttackFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForDrone(ICustomDroneApi api) =>
            new TypedFactory<ICustomDroneApi>((owner, active) => new DroneFacade(api, owner, active));

        private sealed class TypedFactory<TApi> : IOwnerBoundApiFactory where TApi : class
        {
            private readonly Func<IManifest, Action, TApi> create;
            public TypedFactory(Func<IManifest, Action, TApi> create) { this.create = create; }
            public object CreateOwnerBoundApi(Type apiType, IManifest consumer, Action ensureOwnerActive)
            {
                if (apiType != typeof(TApi))
                    throw new InvalidOperationException("Owner-bound custom-entity factory expected contract '" + (typeof(TApi).FullName ?? typeof(TApi).Name) + "'.");
                return create(consumer, ensureOwnerActive);
            }
        }

        private abstract class FacadeBase : IOwnerBoundApiFacade
        {
            private readonly Action ensureOwnerActive;

            protected FacadeBase(IManifest owner, Action ensureOwnerActive)
            {
                Owner = owner ?? throw new ArgumentNullException(nameof(owner));
                this.ensureOwnerActive = ensureOwnerActive ?? throw new ArgumentNullException(nameof(ensureOwnerActive));
            }

            protected IManifest Owner { get; }
            protected void EnsureActive() => ensureOwnerActive();

            protected void EnsureOwner(IManifest supplied)
            {
                ensureOwnerActive();
                if (supplied == null || !string.Equals(supplied.UniqueID, Owner.UniqueID, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Custom-entity API for owner '" + Owner.UniqueID + "' can't mutate resources for another owner.");
            }

            public abstract void Deactivate();
        }

        private sealed class OwnedEvent<TArgs> where TArgs : EventArgs
        {
            private readonly Action<EventHandler<TArgs>> subscribe;
            private readonly Action<EventHandler<TArgs>> unsubscribe;
            private readonly List<EventHandler<TArgs>> handlers = new List<EventHandler<TArgs>>();
            private readonly object gate = new object();

            public OwnedEvent(Action<EventHandler<TArgs>> subscribe, Action<EventHandler<TArgs>> unsubscribe)
            {
                this.subscribe = subscribe;
                this.unsubscribe = unsubscribe;
            }

            public void Add(EventHandler<TArgs>? handler)
            {
                if (handler == null)
                    return;
                subscribe(handler);
                lock (gate)
                    handlers.Add(handler);
            }

            public void Remove(EventHandler<TArgs>? handler)
            {
                if (handler == null)
                    return;
                bool found = false;
                lock (gate)
                {
                    for (int index = handlers.Count - 1; index >= 0; index--)
                    {
                        if (!Equals(handlers[index], handler))
                            continue;
                        handlers.RemoveAt(index);
                        found = true;
                        break;
                    }
                }
                if (found)
                    unsubscribe(handler);
            }

            public void Clear()
            {
                EventHandler<TArgs>[] snapshot;
                lock (gate)
                {
                    snapshot = handlers.ToArray();
                    handlers.Clear();
                }
                foreach (EventHandler<TArgs> handler in snapshot)
                {
                    try
                    {
                        unsubscribe(handler);
                    }
                    catch
                    {
                        // Continue detaching the remaining owner callbacks. The
                        // facade cache root is removed even if one unsubscribe fails.
                    }
                }
            }
        }

        private sealed class AnimalFacade : FacadeBase, ICustomAnimalApi
        {
            private readonly ICustomAnimalApi inner;
            private readonly OwnedEvent<CustomAnimalLifecycleEventArgs> lifecycle;
            public AnimalFacade(ICustomAnimalApi inner, IManifest owner, Action active) : base(owner, active)
            {
                this.inner = inner;
                lifecycle = new OwnedEvent<CustomAnimalLifecycleEventArgs>(handler => inner.LifecycleChanged += handler, handler => inner.LifecycleChanged -= handler);
            }
            public event EventHandler<CustomAnimalLifecycleEventArgs>? LifecycleChanged { add { EnsureActive(); lifecycle.Add(value); } remove { EnsureActive(); lifecycle.Remove(value); } }
            public CustomAnimalRegistrationResult RegisterSpecies(IManifest owner, CustomAnimalSpeciesDefinition definition) { EnsureOwner(owner); return inner.RegisterSpecies(Owner, definition); }
            public CustomEntityUnregisterResult UnregisterSpecies(IManifest owner, string speciesId) { EnsureOwner(owner); return inner.UnregisterSpecies(Owner, speciesId); }
            public IReadOnlyList<CustomAnimalSpeciesDefinition> GetSpeciesDefinitions(string? ownerUniqueId = null) { EnsureActive(); return inner.GetSpeciesDefinitions(ownerUniqueId); }
            public CustomAnimalSpeciesDefinition? GetSpeciesDefinition(string speciesId) { EnsureActive(); return inner.GetSpeciesDefinition(speciesId); }
            public CustomAnimalSpawnResult RequestSpawn(IManifest owner, CustomAnimalSpawnRequest request) { EnsureOwner(owner); return inner.RequestSpawn(Owner, request); }
            public CustomEntityRequestResult RequestRemove(IManifest owner, CustomEntityHandle animalHandle, string reason) { EnsureOwner(owner); return inner.RequestRemove(Owner, animalHandle, reason); }
            public IReadOnlyList<CustomAnimalInstanceSnapshot> GetAnimalInstances(string? ownerUniqueId = null) { EnsureActive(); return inner.GetAnimalInstances(ownerUniqueId); }
            public CustomAnimalInstanceSnapshot? GetAnimalInstance(CustomEntityHandle handle) { EnsureActive(); return inner.GetAnimalInstance(handle); }
            public CustomEntityFamilySnapshot GetSnapshot(string? ownerUniqueId = null) { EnsureActive(); return inner.GetSnapshot(ownerUniqueId); }
            public CustomEntityCapabilityStatus GetStatus(string? ownerUniqueId = null) { EnsureActive(); return inner.GetStatus(ownerUniqueId); }
            public override void Deactivate() => lifecycle.Clear();
        }

        private sealed class MonsterFacade : FacadeBase, ICustomMonsterApi
        {
            private readonly ICustomMonsterApi inner;
            private readonly OwnedEvent<CustomMonsterLifecycleEventArgs> lifecycle;
            public MonsterFacade(ICustomMonsterApi inner, IManifest owner, Action active) : base(owner, active)
            {
                this.inner = inner;
                lifecycle = new OwnedEvent<CustomMonsterLifecycleEventArgs>(handler => inner.LifecycleChanged += handler, handler => inner.LifecycleChanged -= handler);
            }
            public event EventHandler<CustomMonsterLifecycleEventArgs>? LifecycleChanged { add { EnsureActive(); lifecycle.Add(value); } remove { EnsureActive(); lifecycle.Remove(value); } }
            public CustomMonsterRegistrationResult RegisterMonster(IManifest owner, CustomMonsterDefinition definition) { EnsureOwner(owner); return inner.RegisterMonster(Owner, definition); }
            public CustomMonsterRegistrationResult RegisterSpawnTable(IManifest owner, CustomMonsterSpawnTableDefinition spawnTable) { EnsureOwner(owner); return inner.RegisterSpawnTable(Owner, spawnTable); }
            public CustomEntityUnregisterResult UnregisterMonster(IManifest owner, string monsterId) { EnsureOwner(owner); return inner.UnregisterMonster(Owner, monsterId); }
            public IReadOnlyList<CustomMonsterDefinition> GetMonsterDefinitions(string? ownerUniqueId = null) { EnsureActive(); return inner.GetMonsterDefinitions(ownerUniqueId); }
            public CustomMonsterDefinition? GetMonsterDefinition(string monsterId) { EnsureActive(); return inner.GetMonsterDefinition(monsterId); }
            public CustomMonsterSpawnResult RequestSpawn(IManifest owner, CustomMonsterSpawnRequest request) { EnsureOwner(owner); return inner.RequestSpawn(Owner, request); }
            public CustomEntityRequestResult RequestDespawn(IManifest owner, CustomEntityHandle monsterHandle, string reason) { EnsureOwner(owner); return inner.RequestDespawn(Owner, monsterHandle, reason); }
            public IReadOnlyList<CustomMonsterInstanceSnapshot> GetMonsterInstances(string? ownerUniqueId = null) { EnsureActive(); return inner.GetMonsterInstances(ownerUniqueId); }
            public CustomMonsterInstanceSnapshot? GetMonsterInstance(CustomEntityHandle handle) { EnsureActive(); return inner.GetMonsterInstance(handle); }
            public CustomEntityFamilySnapshot GetSnapshot(string? ownerUniqueId = null) { EnsureActive(); return inner.GetSnapshot(ownerUniqueId); }
            public CustomEntityCapabilityStatus GetStatus(string? ownerUniqueId = null) { EnsureActive(); return inner.GetStatus(ownerUniqueId); }
            public override void Deactivate() => lifecycle.Clear();
        }

        private sealed class AttackFacade : FacadeBase, ICustomAttackApi
        {
            private readonly ICustomAttackApi inner;
            private readonly OwnedEvent<CustomAttackLifecycleEventArgs> lifecycle;
            public AttackFacade(ICustomAttackApi inner, IManifest owner, Action active) : base(owner, active)
            {
                this.inner = inner;
                lifecycle = new OwnedEvent<CustomAttackLifecycleEventArgs>(handler => inner.LifecycleChanged += handler, handler => inner.LifecycleChanged -= handler);
            }
            public event EventHandler<CustomAttackLifecycleEventArgs>? LifecycleChanged { add { EnsureActive(); lifecycle.Add(value); } remove { EnsureActive(); lifecycle.Remove(value); } }
            public CustomAttackRegistrationResult RegisterAttack(IManifest owner, CustomAttackDefinition definition) { EnsureOwner(owner); return inner.RegisterAttack(Owner, definition); }
            public CustomEntityUnregisterResult UnregisterAttack(IManifest owner, string attackId) { EnsureOwner(owner); return inner.UnregisterAttack(Owner, attackId); }
            public IReadOnlyList<CustomAttackDefinition> GetAttackDefinitions(string? ownerUniqueId = null) { EnsureActive(); return inner.GetAttackDefinitions(ownerUniqueId); }
            public CustomAttackDefinition? GetAttackDefinition(string attackId) { EnsureActive(); return inner.GetAttackDefinition(attackId); }
            public CustomAttackSpawnResult SpawnProjectile(IManifest owner, CustomAttackSpawnRequest request) { EnsureOwner(owner); return inner.SpawnProjectile(Owner, request); }
            public CustomAttackSpawnResult ExecuteAttack(IManifest owner, CustomAttackSpawnRequest request) { EnsureOwner(owner); return inner.ExecuteAttack(Owner, request); }
            public CustomEntityRequestResult RequestExpire(IManifest owner, CustomEntityHandle attackHandle, string reason) { EnsureOwner(owner); return inner.RequestExpire(Owner, attackHandle, reason); }
            public IReadOnlyList<CustomAttackInstanceSnapshot> GetActiveAttacks(string? ownerUniqueId = null) { EnsureActive(); return inner.GetActiveAttacks(ownerUniqueId); }
            public CustomAttackInstanceSnapshot? GetAttackInstance(CustomEntityHandle handle) { EnsureActive(); return inner.GetAttackInstance(handle); }
            public CustomEntityFamilySnapshot GetSnapshot(string? ownerUniqueId = null) { EnsureActive(); return inner.GetSnapshot(ownerUniqueId); }
            public CustomEntityCapabilityStatus GetStatus(string? ownerUniqueId = null) { EnsureActive(); return inner.GetStatus(ownerUniqueId); }
            public override void Deactivate() => lifecycle.Clear();
        }

        private sealed class DroneFacade : FacadeBase, ICustomDroneApi
        {
            private readonly ICustomDroneApi inner;
            private readonly OwnedEvent<CustomDroneLifecycleEventArgs> lifecycle;
            public DroneFacade(ICustomDroneApi inner, IManifest owner, Action active) : base(owner, active)
            {
                this.inner = inner;
                lifecycle = new OwnedEvent<CustomDroneLifecycleEventArgs>(handler => inner.LifecycleChanged += handler, handler => inner.LifecycleChanged -= handler);
            }
            public event EventHandler<CustomDroneLifecycleEventArgs>? LifecycleChanged { add { EnsureActive(); lifecycle.Add(value); } remove { EnsureActive(); lifecycle.Remove(value); } }
            public CustomDroneRegistrationResult RegisterDrone(IManifest owner, CustomDroneDefinition definition) { EnsureOwner(owner); return inner.RegisterDrone(Owner, definition); }
            public CustomEntityUnregisterResult UnregisterDrone(IManifest owner, string droneId) { EnsureOwner(owner); return inner.UnregisterDrone(Owner, droneId); }
            public IReadOnlyList<CustomDroneDefinition> GetDroneDefinitions(string? ownerUniqueId = null) { EnsureActive(); return inner.GetDroneDefinitions(ownerUniqueId); }
            public CustomDroneDefinition? GetDroneDefinition(string droneId) { EnsureActive(); return inner.GetDroneDefinition(droneId); }
            public CustomDroneSummonResult RequestSummon(IManifest owner, CustomDroneSummonRequest request) { EnsureOwner(owner); return inner.RequestSummon(Owner, request); }
            public CustomEntityRequestResult RequestDismiss(IManifest owner, CustomEntityHandle droneHandle, string reason) { EnsureOwner(owner); return inner.RequestDismiss(Owner, droneHandle, reason); }
            public CustomDroneEquipmentResult Equip(IManifest owner, CustomEntityHandle droneHandle, CustomDroneEquipmentRequest request) { EnsureOwner(owner); return inner.Equip(Owner, droneHandle, request); }
            public CustomDroneCommandResult SetMode(IManifest owner, CustomEntityHandle droneHandle, CustomDroneCommandRequest request) { EnsureOwner(owner); return inner.SetMode(Owner, droneHandle, request); }
            public IReadOnlyList<CustomDroneInstanceSnapshot> GetDroneInstances(string? ownerUniqueId = null) { EnsureActive(); return inner.GetDroneInstances(ownerUniqueId); }
            public CustomDroneInstanceSnapshot? GetDroneInstance(CustomEntityHandle handle) { EnsureActive(); return inner.GetDroneInstance(handle); }
            public CustomEntityFamilySnapshot GetSnapshot(string? ownerUniqueId = null) { EnsureActive(); return inner.GetSnapshot(ownerUniqueId); }
            public CustomEntityCapabilityStatus GetStatus(string? ownerUniqueId = null) { EnsureActive(); return inner.GetStatus(ownerUniqueId); }
            public override void Deactivate() => lifecycle.Clear();
        }
    }
}
