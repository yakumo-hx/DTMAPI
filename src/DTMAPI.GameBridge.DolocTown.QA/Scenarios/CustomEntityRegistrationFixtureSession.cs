using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// A QA-only, owner-bound registration transaction. It deliberately exposes
    /// no runtime entity creation, equipment, command, movement, or world verb.
    /// </summary>
    internal sealed class CustomEntityRegistrationFixtureSession : IDisposable
    {
        private readonly DtmApiRuntime runtime;
        private readonly ManifestModel owner;
        private readonly Func<string, string, int> removeOwner;
        private bool disposed;

        internal CustomEntityRegistrationFixtureSession(
            DtmApiRuntime runtime,
            string runId,
            Func<string, string, int>? removeOwner = null)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            if (string.IsNullOrWhiteSpace(runId))
                throw new ArgumentException("A QA runId is required.", nameof(runId));

            owner = new ManifestModel
            {
                Name = "DTMAPI QA Custom Entity Registration",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.QA." + runId + ".CustomEntity",
                Type = "QA"
            };
            this.removeOwner = removeOwner ?? runtime.CustomEntities.RemoveOwner;
        }

        internal string OwnerUniqueId => owner.UniqueID;

        internal int RemovedCount { get; private set; }

        internal CustomAnimalRegistrationResult RegisterSpecies(CustomAnimalSpeciesDefinition definition)
        {
            EnsureActive();
            return runtime.CustomEntities.RegisterSpecies(owner, definition);
        }

        internal CustomMonsterRegistrationResult RegisterMonster(CustomMonsterDefinition definition)
        {
            EnsureActive();
            return runtime.CustomEntities.RegisterMonster(owner, definition);
        }

        internal CustomMonsterRegistrationResult RegisterSpawnTable(CustomMonsterSpawnTableDefinition definition)
        {
            EnsureActive();
            return runtime.CustomEntities.RegisterSpawnTable(owner, definition);
        }

        internal CustomAttackRegistrationResult RegisterAttack(CustomAttackDefinition definition)
        {
            EnsureActive();
            return runtime.CustomEntities.RegisterAttack(owner, definition);
        }

        internal CustomDroneRegistrationResult RegisterDrone(CustomDroneDefinition definition)
        {
            EnsureActive();
            return runtime.CustomEntities.RegisterDrone(owner, definition);
        }

        internal CustomEntityFamilySnapshot GetAnimalSnapshot() => GetSnapshot(runtime.CustomEntities.GetAnimalSnapshot);

        internal CustomEntityFamilySnapshot GetMonsterSnapshot() => GetSnapshot(runtime.CustomEntities.GetMonsterSnapshot);

        internal CustomEntityFamilySnapshot GetAttackSnapshot() => GetSnapshot(runtime.CustomEntities.GetAttackSnapshot);

        internal CustomEntityFamilySnapshot GetDroneSnapshot() => GetSnapshot(runtime.CustomEntities.GetDroneSnapshot);

        internal CustomEntityCapabilityStatus GetAnimalStatus() => GetStatus(runtime.CustomEntities.GetAnimalStatus);

        internal CustomEntityCapabilityStatus GetMonsterStatus() => GetStatus(runtime.CustomEntities.GetMonsterStatus);

        internal CustomEntityCapabilityStatus GetAttackStatus() => GetStatus(runtime.CustomEntities.GetAttackStatus);

        internal CustomEntityCapabilityStatus GetDroneStatus() => GetStatus(runtime.CustomEntities.GetDroneStatus);

        internal void VerifyClean()
        {
            if (!disposed)
                throw new InvalidOperationException("The registration session must be disposed before cleanup verification.");
            if (runtime.CustomEntities.GetAnimalSnapshot(owner.UniqueID).RegisteredDefinitionCount != 0 ||
                runtime.CustomEntities.GetMonsterSnapshot(owner.UniqueID).RegisteredDefinitionCount != 0 ||
                runtime.CustomEntities.GetAttackSnapshot(owner.UniqueID).RegisteredDefinitionCount != 0 ||
                runtime.CustomEntities.GetDroneSnapshot(owner.UniqueID).RegisteredDefinitionCount != 0)
            {
                throw new InvalidOperationException("The owner-bound QA registration session left definitions after Dispose.");
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            int removed = removeOwner(owner.UniqueID, "QA owner-bound registration session disposed");
            RemovedCount = removed;
            disposed = true;
        }

        private CustomEntityFamilySnapshot GetSnapshot(Func<string?, CustomEntityFamilySnapshot> read)
        {
            EnsureActive();
            return read(owner.UniqueID);
        }

        private CustomEntityCapabilityStatus GetStatus(Func<string?, CustomEntityCapabilityStatus> read)
        {
            EnsureActive();
            return read(owner.UniqueID);
        }

        private void EnsureActive()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(CustomEntityRegistrationFixtureSession));
        }
    }
}
