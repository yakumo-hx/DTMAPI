using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    [DataContract]
    internal sealed class Batch6AutoFishingManagerLifecycleEvidence
    {
        [DataMember(Name = "schemaVersion", Order = 1)] internal int SchemaVersion { get; set; } = 1;
        [DataMember(Name = "caseId", Order = 2)] internal string CaseId { get; set; } = Batch6AutoFishingManagerLifecycleSettings.CaseId;
        [DataMember(Name = "runId", Order = 3)] internal string RunId { get; set; } = string.Empty;
        [DataMember(Name = "mode", Order = 4)] internal string Mode { get; set; } = string.Empty;
        [DataMember(Name = "status", Order = 5)] internal string Status { get; set; } = "Pending";
        [DataMember(Name = "startedAtUtc", Order = 6)] internal DateTimeOffset StartedAtUtc { get; set; }
        [DataMember(Name = "completedAtUtc", Order = 7)] internal DateTimeOffset? CompletedAtUtc { get; set; }
        [DataMember(Name = "expectedProductRoot", Order = 8)] internal string ExpectedProductRoot { get; set; } = string.Empty;
        [DataMember(Name = "disabledMarkerPath", Order = 9)] internal string DisabledMarkerPath { get; set; } = string.Empty;
        [DataMember(Name = "expectedDisabledMarkerSha256", Order = 10)] internal string ExpectedDisabledMarkerSha256 { get; set; } = string.Empty;
        [DataMember(Name = "expectedPackageSha256", Order = 11)] internal string ExpectedPackageSha256 { get; set; } = string.Empty;
        [DataMember(Name = "expectedEntryDllSha256", Order = 12)] internal string ExpectedEntryDllSha256 { get; set; } = string.Empty;
        [DataMember(Name = "expectedManifestSha256", Order = 13)] internal string ExpectedManifestSha256 { get; set; } = string.Empty;
        [DataMember(Name = "expectedReferencePolicySha256", Order = 14)] internal string ExpectedReferencePolicySha256 { get; set; } = string.Empty;
        [DataMember(Name = "package", Order = 15)] internal Batch6AutoFishingPackageProvenance? Package { get; set; }
        [DataMember(Name = "initial", Order = 16)] internal Batch6AutoFishingObservation? Initial { get; set; }
        [DataMember(Name = "activeWithNativeCast", Order = 17)] internal Batch6AutoFishingObservation? ActiveWithNativeCast { get; set; }
        [DataMember(Name = "afterFirstReload", Order = 18)] internal Batch6AutoFishingObservation? AfterFirstReload { get; set; }
        [DataMember(Name = "afterSecondReload", Order = 19)] internal Batch6AutoFishingObservation? AfterSecondReload { get; set; }
        [DataMember(Name = "coldDisabled", Order = 20)] internal Batch6AutoFishingObservation? ColdDisabled { get; set; }
        [DataMember(Name = "nativeCastBaseline", Order = 21)] internal long NativeCastBaseline { get; set; }
        [DataMember(Name = "nativeCastDelta", Order = 22)] internal long NativeCastDelta { get; set; }
        [DataMember(Name = "reloadRequestCount", Order = 23)] internal int ReloadRequestCount { get; set; }
        [DataMember(Name = "workshopReloadReceiptCount", Order = 24)] internal int WorkshopReloadReceiptCount { get; set; }
        [DataMember(Name = "markerCreateObserved", Order = 25)] internal bool MarkerCreateObserved { get; set; }
        [DataMember(Name = "markerRemoveObserved", Order = 26)] internal bool MarkerRemoveObserved { get; set; }
        [DataMember(Name = "sameProcessReentryBlocked", Order = 27)] internal bool SameProcessReentryBlocked { get; set; }
        [DataMember(Name = "coldDisabledNotRestartRequired", Order = 28)] internal bool ColdDisabledNotRestartRequired { get; set; }
        [DataMember(Name = "cleanupVerified", Order = 29)] internal bool CleanupVerified { get; set; }
        [DataMember(Name = "details", Order = 30)] internal string Details { get; set; } = string.Empty;
        [DataMember(Name = "failureCode", Order = 31)] internal string FailureCode { get; set; } = string.Empty;
    }

    internal sealed class Batch6AutoFishingManagerLifecycleEvidenceWriter
    {
        private readonly string resultPath;
        private readonly Batch6AutoFishingManagerLifecycleEvidence evidence;

        internal Batch6AutoFishingManagerLifecycleEvidenceWriter(
            string runtimeEvidenceRoot,
            string runId,
            Batch6AutoFishingManagerLifecycleSettings settings,
            DateTimeOffset startedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(runtimeEvidenceRoot) || !Path.IsPathRooted(runtimeEvidenceRoot))
                throw new InvalidDataException("Batch6AutoFishingManagerLifecycle requires an absolute Runtime evidence root.");
            if (string.IsNullOrWhiteSpace(runId) || runId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidDataException("Batch6AutoFishingManagerLifecycle runId is not safe for an evidence directory.");
            string directory = Path.Combine(runtimeEvidenceRoot, "AUTO-FISHING-MANAGER", runId);
            Directory.CreateDirectory(directory);
            resultPath = Path.Combine(directory, "auto-fishing-manager-lifecycle.json");
            evidence = new Batch6AutoFishingManagerLifecycleEvidence
            {
                RunId = runId,
                Mode = settings.Mode,
                StartedAtUtc = startedAtUtc,
                ExpectedProductRoot = settings.ExpectedProductRoot,
                DisabledMarkerPath = settings.DisabledMarkerPath,
                ExpectedDisabledMarkerSha256 = settings.ExpectedDisabledMarkerSha256,
                ExpectedPackageSha256 = settings.ExpectedPackageSha256,
                ExpectedEntryDllSha256 = settings.ExpectedEntryDllSha256,
                ExpectedManifestSha256 = settings.ExpectedManifestSha256,
                ExpectedReferencePolicySha256 = settings.ExpectedReferencePolicySha256
            };
            Write();
        }

        internal string ResultPath => resultPath;
        internal Batch6AutoFishingManagerLifecycleEvidence Evidence => evidence;

        internal void BindPackage(Batch6AutoFishingPackageProvenance package)
        {
            evidence.Package = package ?? throw new ArgumentNullException(nameof(package));
            Write();
        }

        internal void SetInitial(Batch6AutoFishingObservation observation, long castBaseline)
        {
            evidence.Initial = observation ?? throw new ArgumentNullException(nameof(observation));
            evidence.NativeCastBaseline = castBaseline;
            Write();
        }

        internal void SetActiveWithNativeCast(Batch6AutoFishingObservation observation)
        {
            evidence.ActiveWithNativeCast = observation ?? throw new ArgumentNullException(nameof(observation));
            evidence.NativeCastDelta = observation.CastAppliedCount - evidence.NativeCastBaseline;
            Write();
        }

        internal void MarkMarkerCreateObserved()
        {
            evidence.MarkerCreateObserved = true;
            Write();
        }

        internal void MarkMarkerRemoveObserved()
        {
            evidence.MarkerRemoveObserved = true;
            Write();
        }

        internal void RecordReloadRequest()
        {
            evidence.ReloadRequestCount++;
            Write();
        }

        internal void RecordWorkshopReloadReceipt()
        {
            evidence.WorkshopReloadReceiptCount++;
            Write();
        }

        internal void SetAfterFirstReload(Batch6AutoFishingObservation observation)
        {
            evidence.AfterFirstReload = observation ?? throw new ArgumentNullException(nameof(observation));
            Write();
        }

        internal void SetAfterSecondReload(Batch6AutoFishingObservation observation, bool reentryBlocked)
        {
            evidence.AfterSecondReload = observation ?? throw new ArgumentNullException(nameof(observation));
            evidence.SameProcessReentryBlocked = reentryBlocked;
            Write();
        }

        internal void SetColdDisabled(Batch6AutoFishingObservation observation, bool notRestartRequired)
        {
            evidence.ColdDisabled = observation ?? throw new ArgumentNullException(nameof(observation));
            evidence.ColdDisabledNotRestartRequired = notRestartRequired;
            Write();
        }

        internal void Complete(string details, DateTimeOffset completedAtUtc)
        {
            evidence.Status = "Passed";
            evidence.CleanupVerified = true;
            evidence.Details = details ?? string.Empty;
            evidence.CompletedAtUtc = completedAtUtc;
            Write();
        }

        internal void Fail(string code, string details, DateTimeOffset completedAtUtc)
        {
            evidence.Status = "Failed";
            evidence.CleanupVerified = false;
            evidence.FailureCode = code ?? string.Empty;
            evidence.Details = details ?? string.Empty;
            evidence.CompletedAtUtc = completedAtUtc;
            Write();
        }

        private void Write()
        {
            string temp = resultPath + ".tmp";
            var serializer = new DataContractJsonSerializer(typeof(Batch6AutoFishingManagerLifecycleEvidence));
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                serializer.WriteObject(stream, evidence);
            if (File.Exists(resultPath))
                File.Delete(resultPath);
            File.Move(temp, resultPath);
        }
    }
}
