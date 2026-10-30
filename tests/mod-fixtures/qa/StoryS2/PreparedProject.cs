using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown;
using DolocTown.Config;
using Google.Protobuf;
using UnityEngine;
using Yarn.Unity;
using TextLineProvider = DolocTown.TextLineProvider;
using Object = UnityEngine.Object;

namespace DTMAPI.Internal.StoryS2
{
    internal sealed class PreparedProject : IDisposable
    {
        internal static int LiveObjects { get; private set; }
        internal static int ObserveUnityObjects() => Resources.FindObjectsOfTypeAll<YarnProject>().Count(p => p.name == "DTMAPI.StoryS2.Project") +
            Resources.FindObjectsOfTypeAll<Localization>().Count(l => l.name.StartsWith("DTMAPI.StoryS2.Locale.", StringComparison.Ordinal));
        internal readonly YarnProject Original;
        internal readonly TextLineProvider OriginalProvider;
        internal YarnProject Project = null!;
        internal TextLineProvider Provider = null!;
        private readonly List<Object> owned = new List<Object>();
        private T Own<T>(T asset) where T : Object { owned.Add(asset); LiveObjects++; return asset; }
        private PreparedProject(YarnProject original, TextLineProvider provider) { Original = original; OriginalProvider = provider; }
        internal static PreparedProject Build(YarnProject original, TextLineProvider provider, Candidate candidate, NativeBoundary boundary, Action<string> fault)
        {
            var result = new PreparedProject(original, provider);
            try
            {
                Candidate.Require(ReferenceEquals(boundary.ProviderProject.GetValue(provider), original), "S2-NATIVE-PROVIDER-IDENTITY");
                var merged = candidate.Merge(original.Program);
                // Fresh ScriptableObject: none of YarnProject's lazy caches or
                // serialized dictionary references can alias the native asset.
                result.Project = result.Own(ScriptableObject.CreateInstance<YarnProject>());
                result.Project.name = "DTMAPI.StoryS2.Project";
                result.Project.hideFlags = HideFlags.HideAndDontSave;
                result.Project.compiledYarnProgram = merged.ToByteArray();
                result.Project.localizationType = original.localizationType;
                result.Project.searchAssembliesForActions = new List<string>(original.searchAssembliesForActions);
                fault("project");
                var locales = new Dictionary<Localization, Localization>();
                foreach (var source in original.localizations.Concat(new[] { original.baseLocalization }).Distinct())
                {
                    if (source == null) throw new InvalidOperationException("S2-NATIVE-LOCALIZATION-MISSING");
                    // Unity copies serialized strings/assets. Explicitly replace
                    // the runtime dictionary, preserving all native overrides.
                    var clone = result.Own(Object.Instantiate(source));
                    clone.name = "DTMAPI.StoryS2.Locale." + source.LocaleCode;
                    clone.hideFlags = HideFlags.HideAndDontSave;
                    boundary.RuntimeStrings.SetValue(clone, new Dictionary<string, string>((Dictionary<string, string>)boundary.RuntimeStrings.GetValue(source)!));
                    foreach (var line in candidate.Lines.Values)
                    {
                        string id = (string)line["id"]!;
                        Candidate.Require(!source.ContainsLocalizedString(id) && original.lineMetadata.GetMetadata(id) == null && string.IsNullOrEmpty(DolocConfig.Tables.GetText(id)), "S2-NATIVE-TEXT-COLLISION:" + id);
                        clone.AddLocalizedString(id, candidate.Text(line, source.LocaleCode));
                    }
                    locales.Add(source, clone);
                    fault("localization");
                }
                result.Project.localizations = original.localizations.Select(l => locales[l]).ToList();
                result.Project.baseLocalization = locales[original.baseLocalization];
                var metadata = original.lineMetadata.GetLineIDs().Select(id => new KeyValuePair<string, string[]>(id, original.lineMetadata.GetMetadata(id))).ToList();
                metadata.AddRange(candidate.Lines.Values.Select(line => new KeyValuePair<string, string[]>((string)line["id"]!, line["metadata"]!.Select(t => (string)t!).ToArray())));
                result.Project.lineMetadata = boundary.Metadata(metadata);
                result.Provider = new TextLineProvider(result.Project) { textLanguageCode = provider.textLanguageCode };
                // Force both lazy caches before publication; they belong solely
                // to this clone, never to a mutated native project.
                Candidate.Require(result.Project.Program.Nodes.Count == merged.Nodes.Count && result.Project.InitialValues.Count == merged.InitialValues.Count, "S2-PROJECT-CACHE");
                fault("metadata");
                return result;
            }
            catch { result.Dispose(); throw; }
        }
        public void Dispose()
        {
            foreach (var asset in owned) { if (asset != null) Object.DestroyImmediate(asset); LiveObjects--; }
            owned.Clear();
        }
    }
}
