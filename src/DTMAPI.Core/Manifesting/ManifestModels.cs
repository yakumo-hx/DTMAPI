using System.Collections.Generic;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Manifesting
{
    [DataContract]
    public sealed class ManifestModel : IManifest
    {
        [DataMember(Name = "Name")] public string Name { get; set; } = string.Empty;
        [DataMember(Name = "Author")] public string Author { get; set; } = string.Empty;
        [DataMember(Name = "Version")] public string Version { get; set; } = "0.0.0";
        [DataMember(Name = "Description")] public string Description { get; set; } = string.Empty;
        [DataMember(Name = "UniqueID")] public string UniqueID { get; set; } = string.Empty;
        [DataMember(Name = "EntryDll")] public string EntryDll { get; set; } = string.Empty;
        [DataMember(Name = "EntryType")] public string EntryType { get; set; } = string.Empty;
        [DataMember(Name = "MinimumDTMApiVersion")] public string MinimumDTMApiVersion { get; set; } = string.Empty;
        [DataMember(Name = "MinimumApiVersion")] public string MinimumApiVersionAlias { get; set; } = string.Empty;
        [DataMember(Name = "MinimumGameVersion")] public string MinimumGameVersion { get; set; } = string.Empty;
        [DataMember(Name = "Type")] public string Type { get; set; } = "CodeMod";
        [DataMember(Name = "Dependencies")] public List<ManifestDependencyModel> DependencyModels { get; set; } = new List<ManifestDependencyModel>();
        [DataMember(Name = "UpdateKeys")] public List<string> UpdateKeyModels { get; set; } = new List<string>();

        IReadOnlyList<IManifestDependency> IManifest.Dependencies => DependencyModels;
        IReadOnlyList<string> IManifest.UpdateKeys => UpdateKeyModels;

        public void Normalize()
        {
            Name = Name ?? string.Empty;
            Author = Author ?? string.Empty;
            Version = string.IsNullOrWhiteSpace(Version) ? "0.0.0" : Version;
            Description = Description ?? string.Empty;
            UniqueID = UniqueID ?? string.Empty;
            EntryDll = EntryDll ?? string.Empty;
            EntryType = EntryType ?? string.Empty;
            MinimumDTMApiVersion = string.IsNullOrWhiteSpace(MinimumDTMApiVersion) ? MinimumApiVersionAlias ?? string.Empty : MinimumDTMApiVersion;
            MinimumGameVersion = MinimumGameVersion ?? string.Empty;
            Type = string.IsNullOrWhiteSpace(Type) ? "CodeMod" : Type;
            DependencyModels = DependencyModels ?? new List<ManifestDependencyModel>();
            UpdateKeyModels = UpdateKeyModels ?? new List<string>();
            foreach (ManifestDependencyModel dependency in DependencyModels)
                dependency.Normalize();
        }
    }

    [DataContract]
    public sealed class ManifestDependencyModel : IManifestDependency
    {
        private bool required = true;
        private bool requiredAssigned;

        [DataMember(Name = "UniqueID")] public string UniqueID { get; set; } = string.Empty;
        [DataMember(Name = "MinimumVersion")] public string MinimumVersion { get; set; } = string.Empty;
        [DataMember(Name = "Required")] private bool? RequiredJson { get; set; }
        [DataMember(Name = "IsRequired")] private bool? IsRequiredJson { get; set; }

        public bool Required
        {
            get => required;
            set
            {
                required = value;
                requiredAssigned = true;
            }
        }

        public void Normalize()
        {
            UniqueID = UniqueID ?? string.Empty;
            MinimumVersion = MinimumVersion ?? string.Empty;
            if (RequiredJson.HasValue || IsRequiredJson.HasValue)
                required = (RequiredJson ?? true) && (IsRequiredJson ?? true);
            else if (!requiredAssigned)
                required = true;
        }
    }

    public sealed class DiscoveredMod
    {
        public DiscoveredMod(
            ManifestModel manifest,
            string rootPath,
            string manifestPath,
            string source,
            ulong? workshopId,
            bool officialEnabled,
            bool canDtmApiToggle,
            string officialId,
            bool officialEnablementManaged,
            string enablementReason)
        {
            Manifest = manifest;
            RootPath = rootPath;
            ManifestPath = manifestPath;
            Source = source;
            WorkshopId = workshopId;
            OfficialEnabled = officialEnabled;
            CanDtmApiToggle = canDtmApiToggle;
            OfficialId = officialId ?? string.Empty;
            OfficialEnablementManaged = officialEnablementManaged;
            EnablementReason = enablementReason ?? string.Empty;
        }

        public ManifestModel Manifest { get; }
        public string RootPath { get; }
        public string ManifestPath { get; }
        public string Source { get; }
        public ulong? WorkshopId { get; }
        public bool OfficialEnabled { get; }
        public bool CanDtmApiToggle { get; }
        public string OfficialId { get; }
        public bool OfficialEnablementManaged { get; }
        public string EnablementReason { get; }
    }
}
