using System.Collections.Generic;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Manifesting
{
    [DataContract]
    public sealed class ManifestModel : IManifest
    {
        private string type = "CodeMod";
        private string codeModKind = string.Empty;

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
        [DataMember(Name = "Type")]
        public string Type
        {
            get => type;
            set
            {
                TypeWasDeclared = true;
                DeclaredTypeValue = value ?? string.Empty;
                type = value ?? string.Empty;
            }
        }

        [DataMember(Name = "CodeModKind")]
        public string CodeModKind
        {
            get => codeModKind;
            set
            {
                CodeModKindWasDeclared = true;
                DeclaredCodeModKindValue = value ?? string.Empty;
                codeModKind = value ?? string.Empty;
            }
        }
        [DataMember(Name = "Dependencies")] public List<ManifestDependencyModel> DependencyModels { get; set; } = new List<ManifestDependencyModel>();
        [DataMember(Name = "UpdateKeys")] public List<string> UpdateKeyModels { get; set; } = new List<string>();

        IReadOnlyList<IManifestDependency> IManifest.Dependencies => DependencyModels;
        IReadOnlyList<string> IManifest.UpdateKeys => UpdateKeyModels;

        [IgnoreDataMember] internal bool TypeWasDeclared { get; private set; }
        [IgnoreDataMember] internal string DeclaredTypeValue { get; private set; } = string.Empty;
        [IgnoreDataMember] internal bool CodeModKindWasDeclared { get; private set; }
        [IgnoreDataMember] internal string DeclaredCodeModKindValue { get; private set; } = string.Empty;

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
            type = string.IsNullOrWhiteSpace(type) ? "CodeMod" : type;
            codeModKind = codeModKind ?? string.Empty;
            DependencyModels = DependencyModels ?? new List<ManifestDependencyModel>();
            UpdateKeyModels = UpdateKeyModels ?? new List<string>();
            foreach (ManifestDependencyModel dependency in DependencyModels)
            {
                dependency.Normalize();
                if (dependency.HasRequiredAliasConflict)
                {
                    throw new SerializationException(
                        "Dependency '" + dependency.UniqueID + "' declares conflicting Required and IsRequired values. " +
                        "Required is the canonical field; remove IsRequired or make both values agree.");
                }
            }
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

        internal bool HasRequiredAliasConflict =>
            RequiredJson.HasValue &&
            IsRequiredJson.HasValue &&
            RequiredJson.Value != IsRequiredJson.Value;

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
            if (RequiredJson.HasValue)
                required = RequiredJson.Value;
            else if (IsRequiredJson.HasValue)
                required = IsRequiredJson.Value;
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
            string enablementReason,
            bool nativeSubscriptionVerified = false,
            string selectionReason = "",
            ManagedModClassification? classification = null)
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
            NativeSubscriptionVerified = nativeSubscriptionVerified;
            SelectionReason = selectionReason ?? string.Empty;
            Classification = classification ?? ManagedModClassifier.ClassifyDeclaredIdentity(manifest);
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
        public bool NativeSubscriptionVerified { get; }
        public string SelectionReason { get; }
        public ManagedModClassification Classification { get; }

        internal DiscoveredMod WithSelectionReason(string selectionReason)
        {
            return new DiscoveredMod(
                Manifest,
                RootPath,
                ManifestPath,
                Source,
                WorkshopId,
                OfficialEnabled,
                CanDtmApiToggle,
                OfficialId,
                OfficialEnablementManaged,
                EnablementReason,
                NativeSubscriptionVerified,
                selectionReason,
                Classification);
        }
    }
}
