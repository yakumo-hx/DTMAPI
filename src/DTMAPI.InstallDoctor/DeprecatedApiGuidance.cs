using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Tooling.Metadata;

namespace DTMAPI.InstallDoctor;

internal static class DeprecatedApiGuidance
{
    private static readonly string[] KnownRetiredOrDeprecatedTypes =
    {
        "DTMAPI.Abstractions.ILampControlApi",
        "DTMAPI.Abstractions.LampManualToggleOptions",
        "DTMAPI.Abstractions.LampManualToggleRegisterResult",
        "DTMAPI.Abstractions.LampManualToggleState",
        "DTMAPI.Abstractions.IFishingAutomationApi",
        "DTMAPI.Abstractions.FishingAutomationOptions",
        "DTMAPI.Abstractions.FishingAutomationState",
        "DTMAPI.Abstractions.FishingAutomationStrategy",
        "DTMAPI.Abstractions.ICameraZoomApi"
    };

    public static IReadOnlyList<DoctorFinding> Find(PeMetadataInspection inspection, IReadOnlyList<ObsoleteDeclaration> authoritativeCatalog)
    {
        var findings = new List<DoctorFinding>();
        var obsoleteTypes = new Dictionary<string, ObsoleteDeclaration>(StringComparer.Ordinal);
        foreach (ObsoleteDeclaration declaration in authoritativeCatalog)
        {
            if (declaration.SymbolKind.Equals("Type", StringComparison.Ordinal) && declaration.DeclaringType.Length > 0)
                obsoleteTypes[declaration.DeclaringType] = declaration;
        }

        foreach (string known in KnownRetiredOrDeprecatedTypes)
        {
            if (!obsoleteTypes.ContainsKey(known))
            {
                obsoleteTypes[known] = new ObsoleteDeclaration
                {
                    SymbolKind = "Type",
                    DeclaringType = known
                };
            }
        }

        foreach (string referencedType in inspection.ReferencedTypes.Distinct(StringComparer.Ordinal))
        {
            if (!obsoleteTypes.TryGetValue(referencedType, out ObsoleteDeclaration? declaration))
                continue;
            findings.Add(new DoctorFinding
            {
                Code = referencedType.Contains("Lamp", StringComparison.Ordinal) ? "deprecated-lamp-api" : "deprecated-dtmapi-api",
                Severity = DoctorSeverity.Warning,
                Path = inspection.Path,
                Message = "The assembly references obsolete DTMAPI API " + referencedType + "." +
                          (declaration.Message.Length == 0 ? string.Empty : " " + declaration.Message),
                Guidance = GuidanceFor(referencedType)
            });
        }

        foreach (ObsoleteDeclaration declaration in authoritativeCatalog.Where(value => !value.SymbolKind.Equals("Type", StringComparison.Ordinal)))
        {
            string reference = declaration.DeclaringType + "::" + declaration.SymbolName;
            if (!inspection.ReferencedMembers.Contains(reference, StringComparer.Ordinal))
                continue;
            findings.Add(new DoctorFinding
            {
                Code = "deprecated-dtmapi-member",
                Severity = DoctorSeverity.Warning,
                Path = inspection.Path,
                Message = "The assembly references obsolete DTMAPI member " + reference + "." +
                          (declaration.Message.Length == 0 ? string.Empty : " " + declaration.Message),
                Guidance = GuidanceFor(declaration.DeclaringType)
            });
        }

        return findings
            .GroupBy(value => value.Code + "\0" + value.Message, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(value => value.Code, StringComparer.Ordinal)
            .ThenBy(value => value.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static string GuidanceFor(string typeName)
    {
        if (typeName.Contains("Lamp", StringComparison.Ordinal))
            return "Remove the Lamp API dependency. DTMAPI 0.5.5 retains only a deterministic retired-disabled ABI shell; it installs no hook and performs no lighting behavior. A future Lamp feature requires a separate native-owner/API project.";
        if (typeName.Contains("FishingAutomation", StringComparison.Ordinal))
            return "IFishingAutomationApi and its DTOs are frozen; do not add new consumers. Existing binary compatibility remains available until a separately approved breaking change. Players seeking supported fishing automation should install the self-contained Yuuka.DTMAPI.AutoFishing Advanced product; this is a product choice, not a replacement public API. Authors need a separate native-owner review and SDK policy admission for new fishing automation.";
        if (typeName.Contains("CameraZoom", StringComparison.Ordinal) || typeName.Contains("CameraView", StringComparison.Ordinal))
            return "ICameraZoomApi and ICameraViewApi are both frozen; do not add new consumers or migrate between them. Existing ICameraZoomApi binary compatibility remains available. Players seeking playable zoom should install the first-party Zoom ProductNative mod; this is a product choice, not a replacement public API.";
        if (typeName.Contains("CustomAnimal", StringComparison.Ordinal) || typeName.Contains("CustomMonster", StringComparison.Ordinal) ||
            typeName.Contains("CustomAttack", StringComparison.Ordinal) || typeName.Contains("CustomDrone", StringComparison.Ordinal))
            return "Retain the old ABI only for existing consumers. Custom-entity native creation remains blocked; registration success does not create an animal, monster, attack or drone. There is no replacement public native-creation API. Remove the unsupported gameplay request or keep an explicit unavailable path.";
        if (typeName.Contains("ActionCompletion", StringComparison.Ordinal))
            return ProductGuidance("OneActionComplete", "resource-completion policy");
        if (typeName.Contains("ActionSpeed", StringComparison.Ordinal))
            return ProductGuidance("ActionSpeed", "action timing and automatic bottle filling");
        if (typeName.Contains("ItemTooltip", StringComparison.Ordinal))
            return ProductGuidance("FishBreedingAssistant", "item title formatting");
        if (typeName.Contains("AnimalHusbandry", StringComparison.Ordinal) || typeName.Contains("AnimalViewer", StringComparison.Ordinal))
            return ProductGuidance("AnimalHusbandryProgress", "animal progress display");
        if (typeName.Contains("ChestLocator", StringComparison.Ordinal))
            return ProductGuidance("ChestLocatorEnhancer", "chest-location range");
        if (typeName.Contains("MachineProduction", StringComparison.Ordinal) || typeName.Contains("MachineRecipe", StringComparison.Ordinal) ||
            typeName.Contains("MachineDefinition", StringComparison.Ordinal) || typeName.Contains("MachineOutput", StringComparison.Ordinal) ||
            typeName.Contains("MachineRegister", StringComparison.Ordinal))
            return "IMachineProductionApi has no Runtime provider. Its retained interface and DTOs are ABI shells, not a working machine service. The Mine product owns its player behavior and exposes no replacement public machine API. Existing callers must handle provider absence; do not remove persisted data as part of this migration.";
        if (typeName.Contains("EquipmentSlots", StringComparison.Ordinal))
            return ProductGuidance("MoreEquipmentSlots", "equipment slot behavior");
        if (typeName.Contains("SaveSlots", StringComparison.Ordinal))
            return ProductGuidance("MoreSaves", "save slot selection");
        if (typeName.Contains("StrongPlantingGun", StringComparison.Ordinal))
            return "IStrongPlantingGunApi has no compatibility provider. Use the StrongPlantingGun product for player behavior; it exposes no replacement public farming-gun API. Keep an explicit provider-unavailable path in old consumers and preserve any existing owner data.";
        return "Consult the DTMAPI Author SDK migration report and current public API matrix before rebuilding this mod.";
    }

    private static string ProductGuidance(string product, string behavior) =>
        "Keep the frozen ABI for existing binaries; do not add new consumers. The " + product +
        " product provides player-facing " + behavior + ", but exposes no replacement public API for this contract. " +
        "Migrating to the product loses programmatic control through the old API. New native behavior must use the public Advanced SDK contract and separately verified game signatures. No physical removal date is announced by this diagnostic.";
}
