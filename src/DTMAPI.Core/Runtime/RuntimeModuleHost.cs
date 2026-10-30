using System;
using System.Collections.Generic;
using DTMAPI.Abstractions.Internal;

namespace DTMAPI.Core.Runtime
{
    // Fixed Runtime composition supplies modules and contexts. Domain methods are
    // never discovered by reflection; only the component factory is a load boundary.
    internal sealed class RuntimeModuleHost : IModOwnerCleanupParticipant
    {
        private readonly Dictionary<string, IRuntimeModule> modules = new Dictionary<string, IRuntimeModule>(StringComparer.OrdinalIgnoreCase);
        public string ParticipantId => "Core.RuntimeModules";
        internal void Register(IRuntimeModule module, IRuntimeModuleContext context)
        {
            modules.Add(context.ProviderId, module);
            try { module.Register(context); }
            catch
            {
                // Retain a partially initialized module until its own cleanup succeeds.
                module.BeginClose();
                throw;
            }
        }
        internal void BeginClose(string provider)
        { if (modules.TryGetValue(provider, out var module)) module.BeginClose(); }
        internal RuntimeModuleCleanup TryClose(string provider)
        {
            if (!modules.TryGetValue(provider, out var module)) return new RuntimeModuleCleanup(0);
            module.BeginClose();
            var result = module.TryClose();
            if (result.Released) modules.Remove(provider);
            return result;
        }
        public ModOwnerCleanupParticipantResult RemoveOwner(string ownerId, ModOwnerCleanupReason reason)
        {
            bool existed = modules.ContainsKey(ownerId);
            var result = TryClose(ownerId);
            return new ModOwnerCleanupParticipantResult(existed && result.Released ? 1 : 0, result.Remaining, result.RestartRequired ? 1 : 0, result.Diagnostic);
        }
    }
}
