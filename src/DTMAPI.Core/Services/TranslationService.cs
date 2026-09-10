using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Services
{
    internal sealed partial class RuntimeLanguageSource
    {
        private static readonly TimeSpan NativeSampleInterval = TimeSpan.FromMilliseconds(250);
        private readonly object gate = new object();
        private Func<string?>? nativeLanguageProvider;
        private string cachedNativeLanguage = string.Empty;
        private DateTimeOffset nextNativeSampleAtUtc = DateTimeOffset.MinValue;

        internal RuntimeLanguageSource(Func<string?>? nativeLanguageProvider = null)
        {
            this.nativeLanguageProvider = nativeLanguageProvider;
        }

        internal void SetNativeLanguageProvider(Func<string?>? provider)
        {
            lock (gate)
            {
                nativeLanguageProvider = provider;
                cachedNativeLanguage = string.Empty;
                nextNativeSampleAtUtc = DateTimeOffset.MinValue;
            }
        }

        internal string GetRequestedLanguage()
        {
            string requested = Environment.GetEnvironmentVariable("DTMAPI_LANGUAGE") ??
                Environment.GetEnvironmentVariable("DTMAPI_UI_LANGUAGE") ??
                string.Empty;
            if (!string.IsNullOrWhiteSpace(requested) &&
                !requested.Trim().Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                return requested;
            }

            string native = GetNativeLanguage();
            return string.IsNullOrWhiteSpace(native)
                ? CultureInfo.CurrentUICulture.Name
                : native;
        }

        private string GetNativeLanguage()
        {
            lock (gate)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                if (now < nextNativeSampleAtUtc)
                    return cachedNativeLanguage;

                nextNativeSampleAtUtc = now + NativeSampleInterval;
                try
                {
                    cachedNativeLanguage = nativeLanguageProvider?.Invoke()?.Trim() ?? string.Empty;
                }
                catch
                {
                    cachedNativeLanguage = string.Empty;
                }
                return cachedNativeLanguage;
            }
        }
    }

    internal sealed partial class TranslationService : ITranslationHelper
    {
        private static readonly string[][] AliasGroups =
        {
            new[] { "english", "en" },
            new[] { "german", "de" },
            new[] { "french", "fr" },
            new[] { "japanese", "ja", "jp" },
            new[] { "koreana", "korean", "ko" },
            new[] { "brazilian", "pt_br" },
            new[] { "russian", "ru" },
            new[] { "schinese", "zh_cn", "zh_hans", "chinese" },
            new[] { "tchinese", "zh_tw", "zh_hant" }
        };

        private readonly RuntimeLanguageSource languageSource;
        private readonly object catalogGate = new object();
        private readonly Dictionary<string, string> catalogPaths;
        private readonly Dictionary<string, CatalogLoadResult> catalogs =
            new Dictionary<string, CatalogLoadResult>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedCatalogFailures =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Action<string>? logWarning;

        public TranslationService(string modRootPath, string language)
            : this(modRootPath, new RuntimeLanguageSource(() => language))
        {
        }

        internal TranslationService(
            string modRootPath,
            RuntimeLanguageSource languageSource,
            Action<string>? logWarning = null)
        {
            this.languageSource = languageSource ?? throw new ArgumentNullException(nameof(languageSource));
            this.logWarning = logWarning;
            catalogPaths = IndexCatalogs(Path.Combine(modRootPath ?? string.Empty, "i18n"));
        }

        public string Language => ResolveCatalog(languageSource.GetRequestedLanguage()).Name;

        public string Get(string key, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(key))
                return fallback ?? string.Empty;

            ResolvedCatalog primary = ResolveCatalog(languageSource.GetRequestedLanguage());
            if (primary.Values.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value))
                return value;

            ResolvedCatalog english = ResolveEnglishCatalog();
            if (english.Values.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
                return value;

            return string.IsNullOrWhiteSpace(fallback) ? key : fallback;
        }

        public static string DetectLanguage()
        {
            return NormalizeLanguage(new RuntimeLanguageSource().GetRequestedLanguage());
        }

        public static string NormalizeLanguage(string language)
        {
            string normalized = NormalizeLocale(language);
            if (normalized.Length == 0 || normalized == "auto")
                return "english";

            foreach (string[] group in AliasGroups)
            {
                if (group.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                    return group[0];
            }

            int separator = normalized.IndexOf('_');
            if (separator > 0)
            {
                string baseLanguage = normalized.Substring(0, separator);
                foreach (string[] group in AliasGroups)
                {
                    if (group.Contains(baseLanguage, StringComparer.OrdinalIgnoreCase))
                        return group[0];
                }
                return normalized;
            }

            if (normalized == "zh")
                return "schinese";
            if (normalized == "pt")
                return "brazilian";
            return normalized;
        }

        private ResolvedCatalog ResolveCatalog(string requestedLanguage)
        {
            foreach (string candidate in BuildCandidates(requestedLanguage))
            {
                if (!catalogPaths.TryGetValue(candidate, out string path))
                    continue;
                CatalogLoadResult loaded = GetCatalog(path);
                if (loaded.IsAvailable)
                    return new ResolvedCatalog(Path.GetFileNameWithoutExtension(path), loaded.Values);
            }
            return ResolveEnglishCatalog();
        }

        private ResolvedCatalog ResolveEnglishCatalog()
        {
            foreach (string candidate in ExpandAliasGroup("english"))
            {
                if (!catalogPaths.TryGetValue(candidate, out string path))
                    continue;
                CatalogLoadResult loaded = GetCatalog(path);
                if (loaded.IsAvailable)
                    return new ResolvedCatalog(Path.GetFileNameWithoutExtension(path), loaded.Values);
            }
            return new ResolvedCatalog("english", EmptyValues());
        }

        private IEnumerable<string> BuildCandidates(string requestedLanguage)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string exact = NormalizeLocale(requestedLanguage);
            foreach (string candidate in ExpandCandidate(exact))
            {
                if (seen.Add(candidate))
                    yield return candidate;
            }

            int separator = exact.IndexOf('_');
            string baseLanguage = separator > 0 ? exact.Substring(0, separator) : exact;
            if (baseLanguage == "zh")
                baseLanguage = "schinese";
            else if (baseLanguage == "pt")
                baseLanguage = "brazilian";
            foreach (string candidate in ExpandCandidate(baseLanguage))
            {
                if (seen.Add(candidate))
                    yield return candidate;
            }

            foreach (string candidate in ExpandAliasGroup("english"))
            {
                if (seen.Add(candidate))
                    yield return candidate;
            }
        }

        private static IEnumerable<string> ExpandCandidate(string candidate)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
                yield return candidate;
            foreach (string alias in ExpandAliasGroup(candidate))
            {
                if (!alias.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                    yield return alias;
            }
        }

        private static IEnumerable<string> ExpandAliasGroup(string candidate)
        {
            string normalized = NormalizeLocale(candidate);
            foreach (string[] group in AliasGroups)
            {
                if (!group.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                    continue;
                foreach (string alias in group)
                    yield return alias;
                yield break;
            }
            if (!string.IsNullOrWhiteSpace(normalized))
                yield return normalized;
        }

        private CatalogLoadResult GetCatalog(string path)
        {
            CatalogLoadResult result;
            bool shouldLog;
            lock (catalogGate)
            {
                if (catalogs.TryGetValue(path, out CatalogLoadResult cached))
                    return cached;

                result = ReadDictionary(path);
                catalogs[path] = result;
                shouldLog = !result.IsAvailable &&
                    loggedCatalogFailures.Add(path);
            }
            if (shouldLog)
                logWarning?.Invoke("DTMAPI translation catalog is unavailable and will be skipped: " + path + ".");
            return result;
        }

        private static Dictionary<string, string> IndexCatalogs(string i18nPath)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(i18nPath))
                return result;
            foreach (string path in Directory.GetFiles(i18nPath, "*.json", SearchOption.TopDirectoryOnly)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                string stem = NormalizeLocale(Path.GetFileNameWithoutExtension(path));
                if (stem.Length > 0 && !result.ContainsKey(stem))
                    result.Add(stem, path);
            }
            return result;
        }

        private static string NormalizeLocale(string language)
        {
            return (language ?? string.Empty).Trim().Replace('-', '_').ToLowerInvariant();
        }

        private static CatalogLoadResult ReadDictionary(string path)
        {
            if (!File.Exists(path))
                return new CatalogLoadResult(false, EmptyValues());

            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(
                        typeof(Dictionary<string, string>),
                        new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
                    if (serializer.ReadObject(stream) is Dictionary<string, string> values)
                        return new CatalogLoadResult(true, new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase));
                }
            }
            catch
            {
            }
            return new CatalogLoadResult(false, EmptyValues());
        }

        private static Dictionary<string, string> EmptyValues() =>
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly struct CatalogLoadResult
        {
            internal CatalogLoadResult(bool isAvailable, Dictionary<string, string> values)
            {
                IsAvailable = isAvailable;
                Values = values;
            }

            internal bool IsAvailable { get; }
            internal Dictionary<string, string> Values { get; }
        }

        private readonly struct ResolvedCatalog
        {
            internal ResolvedCatalog(string name, Dictionary<string, string> values)
            {
                Name = name;
                Values = values;
            }

            internal string Name { get; }
            internal Dictionary<string, string> Values { get; }
        }
    }
}
