using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Services
{
    internal sealed partial class RuntimeLanguageSource
    {
        internal event Action? LanguageChanged;
        private string? publishedLanguage;
        // Called by the existing Runtime Update on its main thread. Native lookup already
        // has a 250ms cache; no additional timer, frame driver or author-thread callback.
        internal void PollLanguageChanges()
        {
            if (LanguageChanged == null) return;
            string language = GetRequestedLanguage().Trim().Replace('-', '_').ToLowerInvariant();
            if (publishedLanguage == language) return;
            publishedLanguage = language;
            LanguageChanged?.Invoke();
        }
    }

    internal sealed partial class TranslationService
    {
        internal IDtmTranslations CreateOwnerBound(Action ensureActive) => new OwnerTranslations(this, ensureActive);

        private sealed class OwnerTranslations : IDtmTranslations, IDisposable
        {
            private readonly TranslationService inner;
            private readonly Action ensureActive;
            private readonly List<Subscription> subscriptions = new List<Subscription>();
            private string lastRequested;
            private bool closed;
            internal OwnerTranslations(TranslationService inner, Action ensureActive)
            { this.inner = inner; this.ensureActive = ensureActive; lastRequested = Requested(); }
            private string Requested() => NormalizeLocale(inner.languageSource.GetRequestedLanguage());
            private void Check() { ensureActive(); if (closed) throw new ObjectDisposedException(nameof(IDtmTranslations)); }
            public DtmLanguageSnapshot Snapshot { get { Check(); return new DtmLanguageSnapshot(Requested(), inner.Language); } }
            public DtmTranslationResult Get(string key, IReadOnlyDictionary<string, string>? parameters = null, string fallback = "")
            {
                Check();
                if (parameters != null && parameters.Count > 64) throw new ArgumentOutOfRangeException(nameof(parameters));
                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var pair in parameters ?? new Dictionary<string, string>())
                {
                    if (pair.Key.Length > 128 || (pair.Value?.Length ?? 0) > 8192) throw new ArgumentOutOfRangeException(nameof(parameters));
                    values.Add(pair.Key, pair.Value ?? string.Empty);
                }
                string template = inner.Get(key, fallback);
                if (template.Length > 65536) throw new InvalidOperationException("Translation template exceeds the supported size.");
                var text = new StringBuilder();
                var missing = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < template.Length; i++)
                {
                    char current = template[i];
                    if ((current == '{' || current == '}') && i + 1 < template.Length && template[i + 1] == current)
                    { text.Append(current); i++; }
                    else if (current == '{')
                    {
                        int end = template.IndexOf('}', i + 1);
                        if (end < 0) { text.Append(current); continue; }
                        string name = template.Substring(i + 1, end - i - 1);
                        if (values.TryGetValue(name, out string value)) text.Append(value);
                        else { text.Append(template, i, end - i + 1); missing.Add(name); }
                        i = end;
                    }
                    else text.Append(current);
                    if (text.Length > 65536) throw new InvalidOperationException("Formatted translation exceeds the supported size.");
                }
                return new DtmTranslationResult(text.ToString(), missing.OrderBy(v => v, StringComparer.Ordinal));
            }
            public IDisposable SubscribeLanguageChanged(Action<DtmLanguageSnapshot> callback)
            {
                Check();
                if (callback == null) throw new ArgumentNullException(nameof(callback));
                if (subscriptions.Count >= 128) throw new InvalidOperationException("Owner language subscription capacity reached.");
                if (subscriptions.Count == 0)
                { lastRequested = Requested(); inner.languageSource.LanguageChanged += Notify; }
                var subscription = new Subscription(this, callback);
                subscriptions.Add(subscription);
                return subscription;
            }
            private void Notify()
            {
                if (closed) return;
                string language = Requested();
                if (language == lastRequested) return;
                lastRequested = language;
                var snapshot = new DtmLanguageSnapshot(language, inner.Language);
                foreach (var item in subscriptions.ToArray())
                {
                    if (closed) break;
                    if (item.Callback == null) continue;
                    try { item.Callback(snapshot); }
                    catch (Exception ex) { inner.logWarning?.Invoke("Language callback failed: " + ex.GetType().Name); }
                }
            }
            private void Remove(Subscription subscription)
            {
                if (subscription.Callback == null) return;
                Check();
                subscription.Clear();
                subscriptions.Remove(subscription);
                if (subscriptions.Count == 0) inner.languageSource.LanguageChanged -= Notify;
            }
            public void Dispose()
            {
                if (closed) return;
                closed = true;
                inner.languageSource.LanguageChanged -= Notify;
                foreach (var item in subscriptions) item.Clear();
                subscriptions.Clear();
            }
            private sealed class Subscription : IDisposable
            {
                private OwnerTranslations? owner;
                internal Action<DtmLanguageSnapshot>? Callback;
                internal Subscription(OwnerTranslations owner, Action<DtmLanguageSnapshot> callback) { this.owner = owner; Callback = callback; }
                internal void Clear() { owner = null; Callback = null; }
                public void Dispose() { owner?.Remove(this); }
            }
        }
    }
}
