using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AnimalContent;

namespace DTMAPI.RuntimeModules.Animals
{
    internal interface IAnimalFileApi { IAnimalRegistration Replace(IAnimalRegistration? previous, AnimalDefinitionBatch definitions); }
    internal interface IAnimalDefinitionBackend : IDisposable
    {
        IAnimalRegistration Register(AnimalDefinition[] definitions, Func<string, PackageResourceReference> resource, Action guard);
    }
    internal sealed partial class AnimalService
    {
        private readonly Dictionary<string, DefinitionRegistration> species = new Dictionary<string, DefinitionRegistration>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DefinitionRegistration> animators = new Dictionary<string, DefinitionRegistration>(StringComparer.OrdinalIgnoreCase);
        private IAnimalRegistration RegisterDefinitions(IRuntimeModuleOwnerContext owner, AnimalDefinitionBatch input, IAnimalRegistration? previous)
        {
            owner.Guard();
            if (closed) throw new ObjectDisposedException("Animals");
            if (!context.Runtime.IsMainThread) throw new InvalidOperationException("AnimalsWrongThread");
            var candidate = AnimalDefinitions.Validate(input);
            var prior = (previous as DefinitionHandle)?.Registration;
            if (previous != null && (prior == null || prior.Service != this || prior.Owner != owner.Manifest.UniqueID || prior.Closed))
                throw new InvalidOperationException("Animal replacement belongs to a different source or generation.");
            foreach (var row in candidate)
            {
                if (species.TryGetValue(row.SpeciesId, out var existing) && existing != prior) throw new InvalidDataException("AnimalSpeciesConflict: " + row.SpeciesId);
                foreach (string key in AnimalDefinitions.Keys(row))
                    if (animators.TryGetValue(key, out existing) && existing != prior) throw new InvalidDataException("AnimalAnimatorConflict: " + key);
                foreach (string path in new[] { row.AnimatorBundle, row.FrameManifest }.Where(p => p.Length != 0)) owner.Reference(path).Inspect();
            }
            var scope = owner.CreateContribution("animal-definitions"); scope.UseProvider(AnimalModule.ProviderId);
            var registration = new DefinitionRegistration(this, owner.Manifest.UniqueID, scope, candidate);
            try
            {
                scope.Activate(() =>
                {
                    registration.Native = Backend().Register(candidate, owner.Reference, scope.Guard);
                    return registration;
                }, value =>
                {
                    scope.Guard();
                    foreach (var row in candidate) { species[row.SpeciesId] = value; foreach (string key in AnimalDefinitions.Keys(row)) animators[key] = value; }
                });
                return new DefinitionHandle(registration);
            }
            catch { scope.Close(); scope.TryClose(out _); throw; }
        }
        private sealed class DefinitionHandle : IAnimalRegistration
        {
            internal readonly DefinitionRegistration Registration;
            internal DefinitionHandle(DefinitionRegistration value) { Registration = value; }
            public AnimalRegistrationSnapshot GetSnapshot() => Registration.GetSnapshot();
            public void Dispose() => Registration.ReleaseScope();
        }
        private sealed class DefinitionRegistration : IAnimalRegistration
        {
            internal readonly AnimalService Service;
            internal readonly string Owner;
            private readonly IRuntimeContribution scope;
            private readonly AnimalDefinition[] rows;
            internal IAnimalRegistration? Native;
            internal bool Closed;
            internal DefinitionRegistration(AnimalService service, string owner, IRuntimeContribution scope, AnimalDefinition[] rows)
            { Service = service; Owner = owner; this.scope = scope; this.rows = rows; }
            public AnimalRegistrationSnapshot GetSnapshot() => Closed ? new AnimalRegistrationSnapshot(AnimalRegistrationState.Released) : Native?.GetSnapshot() ?? new AnimalRegistrationSnapshot(AnimalRegistrationState.WaitingNative);
            public void Dispose()
            {
                // Also called by the common Owner scope; removing indexes is conditional on this exact generation.
                Closed = true; Native?.Dispose(); Native = null;
                foreach (var row in rows)
                {
                    if (Service.species.TryGetValue(row.SpeciesId, out var value) && value == this) Service.species.Remove(row.SpeciesId);
                    foreach (string key in AnimalDefinitions.Keys(row))
                        if (Service.animators.TryGetValue(key, out value) && value == this) Service.animators.Remove(key);
                }
            }
            internal void ReleaseScope() { scope.Close(); if (!scope.TryClose(out string reason)) throw new InvalidOperationException(reason); }
        }
    }
    internal static class AnimalDefinitions
    {
        internal static IEnumerable<string> Keys(AnimalDefinition row) => new[] { row.AdultAnimatorKey, row.ChildAnimatorKey }.Where(k => k.Length != 0);
        internal static AnimalDefinition[] Validate(AnimalDefinitionBatch input)
        {
            if (input?.Definitions == null || input.Definitions.Length == 0) throw new InvalidDataException("Animal definitions are required.");
            var species = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = input.Definitions.Select(Copy).ToArray();
            foreach (var row in result)
            {
                if (row.SpeciesId.Length == 0 || !species.Add(row.SpeciesId)) throw new InvalidDataException("AnimalSpeciesConflict: " + row.SpeciesId);
                string ai = row.AiTemplate.Length == 0 ? row.TemplateSpeciesId : row.AiTemplate;
                if (ai.Length != 0 && ai != row.SpeciesId && !new[] { "chicken", "goat", "marsh_pangolin", "slime" }.Contains(ai)) throw new InvalidDataException("AnimalAiTemplate: " + ai);
                if (row.AnimatorMode != "" && row.AnimatorMode != "assetBundle" && row.AnimatorMode != "pngSpriteOverride") throw new InvalidDataException("AnimalAnimatorMode: " + row.AnimatorMode);
                foreach (string key in Keys(row)) if (!keys.Add(key)) throw new InvalidDataException("AnimalAnimatorConflict: " + key);
                if (row.AnimatorMode != "" && !Keys(row).Any()) throw new InvalidDataException("Animal animator key required.");
                if (row.AnimatorMode == "assetBundle" && (row.AnimatorBundle.Length == 0 || (row.AdultAnimatorKey.Length == 0) != (row.AdultAnimatorAsset.Length == 0) || (row.ChildAnimatorKey.Length == 0) != (row.ChildAnimatorAsset.Length == 0))) throw new InvalidDataException("Animal assetBundle requires matching animator keys/assets and bundle.");
                if (row.AnimatorMode == "pngSpriteOverride" && (row.TemplateSpritePrefix.Length == 0 || row.CustomSpritePrefix.Length == 0)) throw new InvalidDataException("Animal PNG sprite prefixes required.");
                foreach (string path in new[] { row.AnimatorBundle, row.FrameManifest }.Where(p => p.Length != 0))
                    if (path.StartsWith("/") || path.Contains("\\") || path.Contains(":") || path.Split('/').Any(s => s == "" || s == "." || s == "..")) throw new InvalidDataException("AnimalResourcePath: " + path);
            }
            return result;
        }
        private static string Text(string value) => value != null && !value.Any(char.IsControl) ? value.Trim() : throw new InvalidDataException("Animal field must be non-null text.");
        private static AnimalDefinition Copy(AnimalDefinition value)
        {
            if (value == null) throw new InvalidDataException("Animal definition is null.");
            return new AnimalDefinition { SpeciesId = Text(value.SpeciesId), TemplateSpeciesId = Text(value.TemplateSpeciesId), AiTemplate = Text(value.AiTemplate), AnimatorMode = Text(value.AnimatorMode),
                AdultAnimatorKey = Text(value.AdultAnimatorKey), ChildAnimatorKey = Text(value.ChildAnimatorKey), AnimatorBundle = Text(value.AnimatorBundle), AdultAnimatorAsset = Text(value.AdultAnimatorAsset), ChildAnimatorAsset = Text(value.ChildAnimatorAsset),
                FrameManifest = Text(value.FrameManifest), TemplateSpritePrefix = Text(value.TemplateSpritePrefix), CustomSpritePrefix = Text(value.CustomSpritePrefix) };
        }
    }
}
