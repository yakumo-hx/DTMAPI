using System;
using System.Linq;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AnimalContent;
using DTMAPI.AudioContent.Hosting;

namespace DTMAPI.RuntimeModules.Animals
{
    internal interface IAnimalVoiceBackend : IDisposable
    {
        IDisposable Register(string owner, int order, AnimalVoiceBinding[] voices, Action guard);
    }
    internal interface IAnimalBackend : IAnimalVoiceBackend, IAnimalDefinitionBackend { }
    internal sealed partial class AnimalService : IRuntimeModule
    {
        private IRuntimeModuleContext context = null!;
        private IAnimalBackend? backend;
        private IAnimalBackend Backend() => backend ?? (backend = context.CreateBackend<IAnimalBackend>("DTMAPI.Modules.Animals", context));
        private bool closed;
        public void Register(IRuntimeModuleContext context)
        { this.context = context; context.RegisterApi<IAnimalContentApi>(owner => new Facade(this, owner)); }
        public void BeginClose() { closed = true; }
        public RuntimeModuleCleanup TryClose()
        {
            BeginClose();
            try { backend?.Dispose(); backend = null; return new RuntimeModuleCleanup(0); }
            catch (Exception ex) { return new RuntimeModuleCleanup(1, ex.GetBaseException().Message); }
        }
        private sealed class Facade : IAnimalContentApi, IAnimalFileApi
        {
            private readonly AnimalService service;
            private readonly IRuntimeModuleOwnerContext owner;
            internal Facade(AnimalService service, IRuntimeModuleOwnerContext owner) { this.service = service; this.owner = owner; }
            public IAnimalRegistration Register(AnimalDefinitionBatch definitions) => service.RegisterDefinitions(owner, definitions, null);
            public IAnimalRegistration Replace(IAnimalRegistration? previous, AnimalDefinitionBatch definitions) => service.RegisterDefinitions(owner, definitions, previous);
            public IDisposable RegisterVoices(AnimalVoiceBinding[] voices)
            {
                owner.Guard();
                if (service.closed) throw new ObjectDisposedException("Animals");
                if (voices == null || voices.Length == 0) throw new ArgumentException("Animal voices are required.");
                var copy = voices.Select(v =>
                {
                    if (v == null || string.IsNullOrWhiteSpace(v.SpeciesId) || string.IsNullOrWhiteSpace(v.NativeEvent) || !Enum.IsDefined(typeof(AnimalStage), v.Stage) ||
                        !(v.Sound is IAudioNativeSound sound) || !(v.Sound is IAudioSoundBinding) || !string.Equals(sound.Owner, owner.Manifest.UniqueID, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("Animal voice requires a species, stage, native event and same-Owner Audio sound.");
                    sound.Guard();
                    return new AnimalVoiceBinding { SpeciesId = v.SpeciesId, Stage = v.Stage, NativeEvent = v.NativeEvent.ToUpperInvariant(), Sound = v.Sound };
                }).ToArray();
                var scope = owner.CreateContribution("animal-voices");
                scope.UseProvider(AnimalModule.ProviderId);
                scope.UseProvider(DTMAPI.AudioContent.AudioModule.ProviderId);
                try
                {
                    var release = new Release(scope);
                    scope.Activate(() => new VoiceRegistration(service.Backend().Register(owner.Manifest.UniqueID, owner.OfficialOrder, copy, scope.Guard)), registration =>
                    {
                        foreach (var sound in copy.Select(v => v.Sound).Distinct()) registration.Bindings.Add(((IAudioSoundBinding)sound).Bind(release));
                    });
                    return release;
                }
                catch { scope.Close(); scope.TryClose(out _); throw; }
            }
        }
        private sealed class VoiceRegistration : IDisposable
        {
            private readonly IDisposable native;
            internal readonly System.Collections.Generic.List<IDisposable> Bindings = new System.Collections.Generic.List<IDisposable>();
            internal VoiceRegistration(IDisposable native) { this.native = native; }
            public void Dispose()
            {
                native.Dispose();
                foreach (var binding in Bindings) binding.Dispose();
                Bindings.Clear();
            }
        }
        private sealed class Release : IDisposable
        {
            private readonly IRuntimeContribution scope;
            internal Release(IRuntimeContribution scope) { this.scope = scope; }
            public void Dispose() { scope.Close(); if (!scope.TryClose(out string reason)) throw new InvalidOperationException(reason); }
        }
    }
}
