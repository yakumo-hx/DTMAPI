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
        if (typeName.Contains("CameraZoom", StringComparison.Ordinal))
            return "ICameraZoomApi and ICameraViewApi are both frozen; do not add new consumers or migrate between them. Existing ICameraZoomApi binary compatibility remains available. Players seeking playable zoom should install the first-party Zoom ProductNative mod; this is a product choice, not a replacement public API.";
        return "Consult the DTMAPI Author SDK migration report and current public API matrix before rebuilding this mod.";
    }
}
