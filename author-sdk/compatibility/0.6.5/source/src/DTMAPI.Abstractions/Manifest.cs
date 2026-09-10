using System.Collections.Generic;

namespace DTMAPI.Abstractions
{
    [DtmApiStatus(DtmApiStatus.Stable, Since = "0.1.0")]
    public interface IManifest
    {
        string Name { get; }
        string Author { get; }
        string Version { get; }
        string Description { get; }
        string UniqueID { get; }
        string EntryDll { get; }
        string EntryType { get; }
        string MinimumDTMApiVersion { get; }
        string MinimumGameVersion { get; }
        string Type { get; }
        IReadOnlyList<IManifestDependency> Dependencies { get; }
        IReadOnlyList<string> UpdateKeys { get; }
    }

    [DtmApiStatus(DtmApiStatus.Stable, Since = "0.1.0")]
    public interface IManifestDependency
    {
        string UniqueID { get; }
        string MinimumVersion { get; }
        bool Required { get; }
    }

    internal sealed class EmptyManifest : IManifest
    {
        public static readonly EmptyManifest Instance = new EmptyManifest();
        public string Name => "Unknown";
        public string Author => "Unknown";
        public string Version => "0.0.0";
        public string Description => string.Empty;
        public string UniqueID => "Unknown.Unknown";
        public string EntryDll => string.Empty;
        public string EntryType => string.Empty;
        public string MinimumDTMApiVersion => string.Empty;
        public string MinimumGameVersion => string.Empty;
        public string Type => "CodeMod";
        public IReadOnlyList<IManifestDependency> Dependencies => new IManifestDependency[0];
        public IReadOnlyList<string> UpdateKeys => new string[0];
    }
}
