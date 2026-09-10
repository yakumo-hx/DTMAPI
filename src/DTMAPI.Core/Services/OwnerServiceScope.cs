using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Services
{
    // Internal factory seam: production uses only the fixed platform catalog below.
    // This is never exposed through the public Mod registry or author helper.
    internal sealed class OwnerServiceScope
    {
        private readonly Action ensureOwnerActive;
        private readonly Action ensureRuntimeThread;
        private readonly Func<Type, Action, object?> resolve;
        private readonly Dictionary<Type, object> instances = new Dictionary<Type, object>();
        private readonly HashSet<Type> constructing = new HashSet<Type>();
        private bool closed;
        private bool closing;

        internal OwnerServiceScope(Action ensureOwnerActive, Action ensureRuntimeThread,
            Func<Type, Action, object?> resolve)
        {
            this.ensureOwnerActive = ensureOwnerActive;
            this.ensureRuntimeThread = ensureRuntimeThread;
            this.resolve = resolve;
        }

        internal void EnsureActive()
        {
            ensureRuntimeThread();
            if (closed) throw new ObjectDisposedException(nameof(OwnerServiceScope), "The Mod owner service scope is closed.");
            ensureOwnerActive();
        }

        internal TService? GetService<TService>() where TService : class
        {
            EnsureActive();
            Type contract = typeof(TService);
            if (instances.TryGetValue(contract, out object value)) return (TService)value;
            if (!constructing.Add(contract)) throw new InvalidOperationException("Recursive platform service construction: " + contract.FullName);
            try
            {
                object? created = resolve(contract, EnsureActive);
                if (created == null) { EnsureActive(); return null; }
                // Retain resources before checking reentrant owner closure so cleanup can retry.
                instances.Add(contract, created);
                if (!(created is TService)) throw new InvalidOperationException("Platform service factory returned the wrong contract: " + contract.FullName);
                EnsureActive();
                return (TService)created;
            }
            finally { constructing.Remove(contract); }
        }

        internal ModOwnerCleanupParticipantResult Close()
        {
            ensureRuntimeThread();
            closed = true;
            if (closing) return new ModOwnerCleanupParticipantResult(0, instances.Count, "Optional service cleanup already in progress.");
            closing = true;
            int removed = 0, failures = 0;
            try
            {
                foreach (Type contract in instances.Keys.ToArray())
                {
                    try
                    {
                        (instances[contract] as IDisposable)?.Dispose();
                        instances.Remove(contract);
                        removed++;
                    }
                    catch { failures++; }
                }
            }
            finally { closing = false; }
            return new ModOwnerCleanupParticipantResult(removed, instances.Count + constructing.Count, failures,
                "Optional service scope closed; failed disposals remain for owner cleanup retry.");
        }
    }

    internal sealed class OwnerServiceManager : IModOwnerCleanupParticipant
    {
        private readonly PlatformRuntimeServices? platform;
        internal OwnerServiceManager(PlatformRuntimeServices? platform = null) { this.platform = platform; }
        private readonly Dictionary<string, OwnerServiceScope> scopes = new Dictionary<string, OwnerServiceScope>(StringComparer.OrdinalIgnoreCase);
        public string ParticipantId => "Core.OptionalPlatformServices";

        internal OwnerServiceScope Create(string owner, Action ensureOwnerActive, Action ensureRuntimeThread,
            string? packageRoot = null, string? globalDataRoot = null,
            ConfigService? config = null, InputService? input = null, TranslationService? translation = null,
            Action? ensureReflectionOwnerAlive = null)
        {
            ensureRuntimeThread();
            ensureOwnerActive();
            if (scopes.ContainsKey(owner)) throw new InvalidOperationException("An optional service scope already exists for " + owner + ".");
            var scope = new OwnerServiceScope(ensureOwnerActive, ensureRuntimeThread,
                (contract, guard) => contract == typeof(IReflectionHelper) && ensureReflectionOwnerAlive != null
                    ? new Reflection.OwnerReflectionService(ensureReflectionOwnerAlive)
                    : ResolvePlatformService(contract, guard, owner, packageRoot, globalDataRoot, config, input, translation));
            scopes.Add(owner, scope);
            return scope;
        }

        // Fixed platform catalog. No registration, assignability, name matching or Mod API lookup.
        private object? ResolvePlatformService(Type contract, Action ensureActive,
            string owner, string? packageRoot, string? globalDataRoot, ConfigService? config, InputService? input, TranslationService? translation)
        {
            if (contract == typeof(IOwnerFileHelper) && packageRoot != null) return new OwnerFileService(packageRoot, ensureActive);
            if (contract == typeof(IGlobalDataHelper) && globalDataRoot != null) return new GlobalDataService(owner, globalDataRoot, ensureActive);
            if (contract == typeof(IVersionedConfigHelper)) return config?.CreateVersioned(owner, ensureActive);
            if (contract == typeof(IDtmInputDiagnostics)) return input?.CreateDiagnostics(owner, ensureActive);
            if (contract == typeof(IDtmTranslations)) return translation?.CreateOwnerBound(ensureActive);
            return platform?.Resolve(owner, contract);
        }

        public ModOwnerCleanupParticipantResult RemoveOwner(string ownerId, ModOwnerCleanupReason reason)
        {
            if (!scopes.TryGetValue(ownerId, out OwnerServiceScope scope)) return new ModOwnerCleanupParticipantResult(0, "No optional service scope.");
            ModOwnerCleanupParticipantResult result = scope.Close();
            if (result.RemainingResources == 0) scopes.Remove(ownerId);
            return result;
        }
    }
}
