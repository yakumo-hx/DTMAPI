using DTMAPI.Abstractions;
using System;
using System.IO;

namespace DTMAPI.UnitTests;

public static class RuntimeCodeModProbeContracts
{
    public const string AtomicCheckpointParticipantProviderId = "DTMAPI.Tests.AtomicCheckpointParticipant";
}

public interface IUnitProbeApi
{
    string Owner { get; }
}

public sealed class UnitProbeApi : IUnitProbeApi
{
    public UnitProbeApi(string owner) => Owner = owner;

    public string Owner { get; }
}

public interface IAtomicCheckpointParticipantApi
{
    void RegisterOwner(string ownerId);
}

public sealed class HotLoadProbeConfig
{
    public bool Enabled { get; set; } = true;
}

public sealed class HotLoadProbeMod : DtmMod
{
    public override void Entry(IDtmHelper helper)
    {
        helper.ModRegistry.RegisterApi<IUnitProbeApi>(new UnitProbeApi(helper.ModManifest.UniqueID));
        helper.Events.GameLoop.UpdateTicked += (_, _) => { };
        helper.Input.RegisterButton("F12");
        helper.Config.RegisterMigration<HotLoadProbeConfig>(helper.ModManifest, _ => { });

        string configPath = helper.Config.GetConfigPath(helper.ModManifest);
        string dir = Path.GetDirectoryName(configPath) ?? string.Empty;
        Directory.CreateDirectory(dir);
        string marker = Path.Combine(dir, helper.ModManifest.UniqueID + ".hotload.txt");
        int count = File.Exists(marker) && int.TryParse(File.ReadAllText(marker), out int existing) ? existing : 0;
        File.WriteAllText(marker, (count + 1).ToString());

        IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
        if (menu != null)
        {
            menu.Register(helper.ModManifest, () => { }, () => { });
            menu.AddParagraph(helper.ModManifest, () => "Hot-load probe config page.");
        }
    }
}

public sealed class AtomicOwnerProbeMod : DtmMod, IDisposable
{
    public static int ConstructorCount;
    public static int EntryCount;
    public static int DisposeCount;
    public static int DisposeFailuresRemaining;
    public static int PrepareFailuresRemaining;
    public static string LastDeactivationReason = string.Empty;
    public static string LastPrepareReason = string.Empty;

    public AtomicOwnerProbeMod()
    {
        ConstructorCount++;
    }

    public override void Entry(IDtmHelper helper)
    {
        EntryCount++;
        helper.Events.GameLoop.UpdateTicked += (_, _) => { };
        helper.Input.RegisterButton("F12");
        helper.Config.RegisterMigration<HotLoadProbeConfig>(helper.ModManifest, _ => { });
        helper.ModRegistry.RegisterApi<IUnitProbeApi>(new UnitProbeApi(helper.ModManifest.UniqueID));
        helper.ModRegistry
            .GetApi<IAtomicCheckpointParticipantApi>(RuntimeCodeModProbeContracts.AtomicCheckpointParticipantProviderId)
            ?.RegisterOwner(helper.ModManifest.UniqueID);
        IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
        if (menu != null)
        {
            menu.Register(helper.ModManifest, () => { }, () => { });
            menu.AddParagraph(helper.ModManifest, () => "Atomic owner probe.");
        }
    }

    public void Dispose()
    {
        DisposeCount++;
        if (DisposeFailuresRemaining > 0)
        {
            DisposeFailuresRemaining--;
            throw new InvalidOperationException("atomic-owner-dispose-probe");
        }
    }

    internal void DtmApiDeactivateOwner(string reason)
    {
        LastDeactivationReason = reason ?? string.Empty;
        Dispose();
    }

    internal void DtmApiPrepareOwnerDeactivation(
        string reason)
    {
        LastPrepareReason = reason ?? string.Empty;
        if (PrepareFailuresRemaining > 0)
        {
            PrepareFailuresRemaining--;
            throw new InvalidOperationException(
                "atomic-owner-prepare-probe");
        }
    }
}

public sealed class ThrowingEntryProbeMod : DtmMod
{
    public override void Entry(IDtmHelper helper)
    {
        throw new InvalidOperationException("throwing-entry-probe");
    }
}

public sealed class ThrowAfterRegisterProbeMod : DtmMod
{
    public static int UpdateCalls;

    public override void Entry(IDtmHelper helper)
    {
        UpdateCalls = 0;
        helper.Events.GameLoop.UpdateTicked += (_, _) => UpdateCalls++;
        helper.Input.RegisterButton("F11");
        helper.ModRegistry.RegisterApi<IUnitProbeApi>(new UnitProbeApi(helper.ModManifest.UniqueID));

        IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
        if (menu != null)
        {
            menu.Register(helper.ModManifest, () => { }, () => { });
            menu.AddParagraph(helper.ModManifest, () => "This page must be removed after Entry failure.");
        }

#pragma warning disable CS0618 // Frozen legacy API is intentional in this cleanup fixture.
        ICustomAnimalApi? animals = helper.ModRegistry.GetApi<ICustomAnimalApi>("DTMAPI");
        animals?.RegisterSpecies(helper.ModManifest, new CustomAnimalSpeciesDefinition
        {
            SpeciesId = helper.ModManifest.UniqueID + ".Animal"
        });
#pragma warning restore CS0618

        throw new InvalidOperationException("throw-after-register-probe");
    }
}

public sealed class MonitorErrorProbeMod : DtmMod
{
    public override void Entry(IDtmHelper helper)
    {
        helper.Monitor.Log("monitor-error-probe caught internal failure", LogLevel.Error);
    }
}

public sealed class ApiOwnerProbeMod : DtmMod
{
    public override void Entry(IDtmHelper helper)
    {
        helper.ModRegistry.RegisterApi<IUnitProbeApi>(new UnitProbeApi(helper.ModManifest.UniqueID));
    }
}
