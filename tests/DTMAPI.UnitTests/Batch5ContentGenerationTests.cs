using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    internal static class Batch5ContentGenerationTests
    {
        internal static void Run()
        {
            DirtyGenerationCoalescesAndPreservesInFlightSuccessor();
            AbandonedGenerationClosesAndPublishesARecoverableSuccessor();
            CompletionDiagnosticsRemainBoundedAcrossManyOwners();
            MultiDomainDirtyInputStopsAfterCollapseThreshold();
            DuplicateOwnerPrefixCannotHideALateDistinctOwner();
            AuthoritativeOwnerSelectionDoesNotUseDisplayIdentity();
            ContentQuerySkipsInactiveOfficialSourcesAndIsolatesCompatibleInputs();
            ContentQueryRejectRetainsAtomicLastGoodAndStableModSnapshots();
            ContentQueryRejectsAnUnavailableStillActiveOwnerRoot();
            ContentQueryAtomicBarrierPublishesReceiptWithVisibleSnapshot();
            ContentQueryPreCommitFailureRetainsLastGoodAndRequeues();
            ContentQueryStaleCompletionDoesNotPublishPreparedSnapshot();
            ContentQueryPostCommitFailureDoesNotRequeuePublishedGeneration();
            ContentQueryOwnerCleanupAndSuccessorGenerationStayConsistent();
            CustomAnimalsRejectOnlyBadOwnerAndSteadyFramesDoNotBuildCandidates();
            CustomAnimalsAtomicBarrierPublishesReceiptWithVisibleSnapshot();
            CustomAnimalsPreCommitFailureRetainsLastGoodAndRequeues();
            CustomAnimalsStaleCompletionDoesNotPublishPreparedSnapshot();
            CustomAnimalsPostCommitFailureDoesNotRequeuePublishedGeneration();
            OverlongOwnerDirtyRequestRebuildsAudioAndCustomAnimals();
            AudioPreCommitFailureRetainsLastGoodAndRequeues();
            AudioStaleCompletionDoesNotPublishPreparedSnapshot();
            AudioPostCommitFailureDoesNotRequeuePublishedGeneration();
            AudioPublicationFailureDoesNotSkipDemandReconciliation();
            AudioReadOnlyHealthQueriesDoNotCreatePhantomOwnerResources();
            AudioMultiOwnerBatchPublishesOnlyAfterAllOwnersReachATerminalCandidate();
            AudioUpdaterDoesNotTraverseWhenNoEntryIsPending();
        }

        private static void ContentQuerySkipsInactiveOfficialSourcesAndIsolatesCompatibleInputs()
        {
            WithIsolatedPersistentRoot(persistentRoot =>
            {
                string gameDir = NewTempGameDir();
                string enabledCommentsRoot = Path.Combine(persistentRoot, "MODS", "NativeEnabledComments");
                string enabledGoodRoot = Path.Combine(persistentRoot, "MODS", "NativeEnabledGood");
                string enabledBrokenRoot = Path.Combine(persistentRoot, "MODS", "NativeEnabledBroken");
                string disabledBrokenRoot = Path.Combine(persistentRoot, "MODS", "NativeDisabledBroken");
                string unknownBrokenRoot = Path.Combine(persistentRoot, "MODS", "NativeUnknownBroken");
                foreach (string root in new[] { enabledCommentsRoot, enabledGoodRoot, enabledBrokenRoot, disabledBrokenRoot, unknownBrokenRoot })
                    Directory.CreateDirectory(Path.Combine(root, "Content", "Tables"));

                File.WriteAllText(
                    Path.Combine(enabledCommentsRoot, "info.json"),
                    "// official SimpleJSON accepts line comments\r\n{ \"name\": \"Comment-compatible native content\" }",
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                File.WriteAllText(
                    Path.Combine(enabledCommentsRoot, "Content", "Tables", "item_tbitem.json"),
                    "[\r\n" +
                    "  // comment before a valid item\r\n" +
                    "  { \"id\": \"native_comment_item\", \"sub_type\": \"material\", \"title\": { \"text\": \"注释物品\", \"english\": \"Comment Item\" }, \"ui_sprite_asset\": { \"url\": \"https://example.test/icon//inside-string\" } } // trailing comment\r\n" +
                    "]",
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                WriteItem(Path.Combine(enabledGoodRoot, "Content", "Tables", "item_tbitem.json"), "native_good_item", "Native Good Item");
                File.WriteAllText(Path.Combine(enabledBrokenRoot, "Content", "Tables", "item_tbitem.json"), "[{ enabled-broken-json ]", new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(disabledBrokenRoot, "Content", "Tables", "item_tbitem.json"), "[{ disabled-broken-json ]", new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(unknownBrokenRoot, "Content", "Tables", "item_tbitem.json"), "[{ unknown-broken-json ]", new UTF8Encoding(false));

                string saveRoot = Path.Combine(persistentRoot, "SAVE");
                Directory.CreateDirectory(saveRoot);
                File.WriteAllText(
                    Path.Combine(saveRoot, "mod_infos.json"),
                    "{ \"modInfos\": {" +
                    " \"Local.NativeEnabledComments\": { \"id\": \"Local.NativeEnabledComments\", \"enabled\": true, \"priority\": 1, \"source\": \"Local\", \"title\": \"Comments\" }," +
                    " \"Local.NativeEnabledGood\": { \"id\": \"Local.NativeEnabledGood\", \"enabled\": true, \"priority\": 2, \"source\": \"Local\", \"title\": \"Good\" }," +
                    " \"Local.NativeEnabledBroken\": { \"id\": \"Local.NativeEnabledBroken\", \"enabled\": true, \"priority\": 3, \"source\": \"Local\", \"title\": \"Broken\" }," +
                    " \"Local.NativeDisabledBroken\": { \"id\": \"Local.NativeDisabledBroken\", \"enabled\": false, \"priority\": 4, \"source\": \"Local\", \"title\": \"Disabled\" }" +
                    " } }",
                    new UTF8Encoding(false));

                var service = new ContentQueryService(new RuntimePaths(gameDir, gameDir));
                ContentQueryRebuildResult result = service.RebuildCandidate(Array.Empty<DiscoveredMod>(), 1, "official compatibility unit");
                Assert(result.Success, "An unreadable enabled third-party official file must not reject other valid official inputs.");
                Assert(result.SkippedOfficialInputCount == 1, "Only the enabled broken source should be parsed and reported; disabled and unknown sources must be skipped before input reads.");
                Assert(result.OfficialInputDiagnostics.Count == 1 && result.OfficialInputDiagnostics[0].Contains("NativeEnabledBroken", StringComparison.OrdinalIgnoreCase), "The bounded source diagnostic must identify the exact enabled broken source.");
                Assert(!result.OfficialInputDiagnostics.Any(value => value.Contains("NativeDisabledBroken", StringComparison.OrdinalIgnoreCase) || value.Contains("NativeUnknownBroken", StringComparison.OrdinalIgnoreCase)), "Disabled and unknown sources must not leak parse diagnostics because they were never read.");

                result.PublishPreparedSnapshot?.Invoke();
                IContentItemInfo? commentItem = service.GetIndexedItem("native_comment_item");
                Assert(commentItem != null && commentItem.Enabled && commentItem.IconAssetKey == "https://example.test/icon//inside-string", "The official compatibility reader must remove line comments without changing // inside quoted strings.");
                Assert(service.GetIndexedItem("native_good_item") != null, "A second valid enabled source must publish alongside comment-compatible input.");
                Assert(service.GetAnyIndexedItem("disabled-broken-json") == null && service.GetAllIndexedItems().All(item => item.SourceId != "Local.NativeDisabledBroken"), "Disabled official sources must not publish item-level diagnostic rows.");
                Assert(service.LastGoodGeneration == 1 && service.IndexedItemCount == 2 && service.IndexedItemSourceCount == 2, "Valid enabled sources must publish one complete atomic generation after a peer source is isolated.");
            });
        }

        private static void DirtyGenerationCoalescesAndPreservesInFlightSuccessor()
        {
            var generations = new ContentRefreshGenerationService();
            generations.MarkDirty(ContentRefreshDomains.ContentQuery, new[] { "Owner.A" }, "startup");
            generations.MarkDirty(ContentRefreshDomains.ContentQuery, new[] { "Owner.B" }, "workshop");

            Assert(generations.TryGetDirty(ContentRefreshDomains.ContentQuery, out ContentRefreshDirtyBatch first), "The first dirty generation should be consumable.");
            Assert(first.Generation == 1 && first.OwnerIds.SequenceEqual(new[] { "Owner.A", "Owner.B" }, StringComparer.OrdinalIgnoreCase), "Dirty owner requests should coalesce into one generation.");
            Assert(first.Reasons.Count == 2, "Coalesced generation should retain both bounded reasons.");

            generations.MarkDirty(ContentRefreshDomains.ContentQuery, new[] { "Owner.C" }, new string('r', 400));
            generations.MarkDirty(ContentRefreshDomains.ContentQuery, new[] { "Owner.D" }, "owner-activated");
            Assert(generations.IsDirty(ContentRefreshDomains.ContentQuery), "A successor marked while the first generation is in flight must remain observable.");
            Assert(!generations.TryGetDirty(ContentRefreshDomains.ContentQuery, out _), "An in-flight generation must not be handed to a second consumer.");

            bool firstCompleted = generations.Complete(
                first,
                new[]
                {
                    new ContentRefreshCompletion("all", ContentRefreshCompletionStatus.Committed, 0, 1, 1, "first commit")
                });
            Assert(firstCompleted, "The matching in-flight generation should complete.");
            Assert(generations.TryGetDirty(ContentRefreshDomains.ContentQuery, out ContentRefreshDirtyBatch second), "Dirtiness received in flight must be promoted to a successor generation.");
            Assert(second.Generation == 2 && second.OwnerIds.SequenceEqual(new[] { "Owner.C", "Owner.D" }, StringComparer.OrdinalIgnoreCase), "The successor generation must retain every in-flight owner request.");
            Assert(second.Reasons.Count == 2 && second.Reasons.All(reason => reason.Length <= 192), "Successor reasons must be coalesced and individually bounded.");

            generations.Complete(
                second,
                new[]
                {
                    new ContentRefreshCompletion(new string('o', 400), ContentRefreshCompletionStatus.Rejected, 1, 1, 1, new string('d', 900))
                });
            ContentRefreshGenerationSnapshot snapshot = generations.GetSnapshot();
            Assert(!generations.IsDirty(ContentRefreshDomains.ContentQuery), "Completing the successor should leave the domain clean.");
            Assert(snapshot.Receipts.Count == 2, "Both generations should publish one receipt.");
            Assert(snapshot.Receipts.All(receipt => receipt.Domain.Length <= 64 && receipt.OwnerId.Length <= 128 && receipt.Details.Length <= 512 && receipt.FormatSummary().Length <= 1024), "Receipt identities, details and summaries must remain bounded.");
            Assert(snapshot.FormatSummary().Length <= 4096, "Generation snapshot summary must remain bounded.");
        }

        private static void AbandonedGenerationClosesAndPublishesARecoverableSuccessor()
        {
            var generations = new ContentRefreshGenerationService();
            generations.MarkDirty(ContentRefreshDomains.AudioReplacement, new[] { "Owner.A" }, "startup");
            Assert(generations.TryGetDirty(ContentRefreshDomains.AudioReplacement, out ContentRefreshDirtyBatch failed), "The failing consumer should acquire the original generation.");
            generations.MarkDirty(ContentRefreshDomains.AudioReplacement, new[] { "Owner.B" }, "change-during-consumer");

            Assert(generations.AbandonAndRequeue(failed, "injected consumer failure"), "An unexpected consumer failure should close the in-flight generation and requeue its work.");
            Assert(generations.TryGetDirty(ContentRefreshDomains.AudioReplacement, out ContentRefreshDirtyBatch retry), "The domain must remain consumable after an unexpected consumer failure.");
            Assert(retry.Generation > failed.Generation && retry.OwnerIds.SequenceEqual(new[] { "Owner.A", "Owner.B" }, StringComparer.OrdinalIgnoreCase), "The successor must have a new generation and preserve original plus in-flight owner dirtiness.");
            Assert(generations.GetReceipts().Last().Status == ContentRefreshCompletionStatus.Rejected, "The abandoned generation should leave a bounded rejection receipt.");
            Assert(generations.Complete(retry, new[] { new ContentRefreshCompletion("all", ContentRefreshCompletionStatus.Committed, 0, 1, 1, "retry committed") }), "The successor should complete normally.");
            Assert(!generations.IsDirty(ContentRefreshDomains.AudioReplacement), "A successful successor must return the domain to clean state.");
        }

        private static void CompletionDiagnosticsRemainBoundedAcrossManyOwners()
        {
            var generations = new ContentRefreshGenerationService();
            generations.MarkDirty(ContentRefreshDomains.CustomAnimals, new[] { "all" }, "many-owner-refresh");
            Assert(generations.TryGetDirty(ContentRefreshDomains.CustomAnimals, out ContentRefreshDirtyBatch batch), "The many-owner generation should become consumable.");
            var completions = new ContentRefreshCompletionCollector();
            for (int index = 0; index < 1000; index++)
            {
                completions.Add(new ContentRefreshCompletion(
                    "DTMAPI.Tests.Batch5.Completion." + index.ToString("0000"),
                    index % 3 == 0 ? ContentRefreshCompletionStatus.Rejected : ContentRefreshCompletionStatus.Committed,
                    index,
                    index + 1,
                    index + 1,
                    "bounded-completion"));
            }

            Assert(completions.TotalCompletionCount == 1000 && completions.Count == 255, "The completion collector must retain a fixed named projection plus one aggregate.");
            Assert(generations.Complete(batch, completions), "The bounded completion projection should close the generation.");
            IReadOnlyList<ContentRefreshReceipt> receipts = generations.GetReceipts();
            Assert(receipts.Count == 255, "One generation must not manufacture and immediately discard an unbounded receipt list.");
            ContentRefreshReceipt aggregate = receipts.Last();
            Assert(aggregate.Status == ContentRefreshCompletionStatus.Trimmed && aggregate.OwnerId == "all" && aggregate.Details.Contains("trimmedCompletions=746", StringComparison.Ordinal), "The final receipt must report the exact omitted completion aggregate.");
        }

        private static void MultiDomainDirtyInputStopsAfterCollapseThreshold()
        {
            var generations = new ContentRefreshGenerationService();
            int moved = 0;
            IEnumerable<string> ManyOwners()
            {
                for (int index = 0; index < 10_000; index++)
                {
                    moved++;
                    yield return "DTMAPI.Tests.Batch5.SourceOwner." + index.ToString("00000");
                }
            }

            generations.MarkDirty(ContentRefreshDomains.SourceDriven, ManyOwners(), "source-refresh-stress");
            Assert(moved == 257, "A shared multi-domain dirty input must stop after the first owner beyond its 256-name capacity; moved=" + moved + ".");
            foreach (string domain in ContentRefreshDomains.SourceDriven)
            {
                Assert(generations.TryGetDirty(domain, out ContentRefreshDirtyBatch batch), "Every source-driven domain should receive the coalesced dirty generation.");
                Assert(batch.OwnerIds.Count == 1 && batch.OwnerIds[0] == "all", "A source set beyond the named capacity must collapse to one authoritative all-owner request.");
            }
        }

        private static void DuplicateOwnerPrefixCannotHideALateDistinctOwner()
        {
            var generations = new ContentRefreshGenerationService();
            IEnumerable<string> Owners()
            {
                for (int index = 0; index < 257; index++)
                    yield return "Owner.A";
                yield return "Owner.B";
            }

            generations.MarkDirty(ContentRefreshDomains.SourceDriven, Owners(), "duplicate-prefix");
            foreach (string domain in ContentRefreshDomains.SourceDriven)
            {
                Assert(generations.TryGetDirty(domain, out ContentRefreshDirtyBatch batch), "Every source-driven domain should receive the duplicate-prefix refresh.");
                Assert(batch.OwnerIds.SequenceEqual(new[] { "Owner.A", "Owner.B" }, StringComparer.OrdinalIgnoreCase), "A raw Take limit must not silently discard a late distinct owner behind duplicates.");
            }

            var conservative = new ContentRefreshGenerationService();
            conservative.MarkDirty(
                ContentRefreshDomains.SourceDriven,
                Enumerable.Repeat("Owner.A", 5000),
                "hostile-repetition");
            Assert(conservative.TryGetDirty(ContentRefreshDomains.ContentQuery, out ContentRefreshDirtyBatch fallback) &&
                fallback.OwnerIds.Count == 1 && fallback.OwnerIds[0] == "all",
                "Exceeding the raw input scan budget must conservatively refresh all owners instead of truncating unknown late identities.");
        }

        private static void AuthoritativeOwnerSelectionDoesNotUseDisplayIdentity()
        {
            string ordinaryOwner = "Owner." + new string('o', 122);
            string overlongOwner = ordinaryOwner + "x";
            var generations = new ContentRefreshGenerationService();

            generations.MarkDirty(ContentRefreshDomains.AudioReplacement, new[] { ordinaryOwner }, "ordinary-owner");
            Assert(generations.TryGetDirty(ContentRefreshDomains.AudioReplacement, out ContentRefreshDirtyBatch ordinaryBatch), "An ordinary owner should produce a consumable generation.");
            Assert(ordinaryBatch.OwnerIds.Count == 1 && ordinaryBatch.OwnerIds[0] == ordinaryOwner, "An owner that fits the authoritative bound must retain its exact identity in the dirty batch.");
            Assert(generations.Complete(
                ordinaryBatch,
                new[] { new ContentRefreshCompletion(ordinaryOwner, ContentRefreshCompletionStatus.Committed, 0, 1, 1, "ordinary-owner") }),
                "The ordinary-owner generation should complete.");

            generations.MarkDirty(ContentRefreshDomains.AudioReplacement, new[] { overlongOwner }, "overlong-owner");
            Assert(generations.TryGetDirty(ContentRefreshDomains.AudioReplacement, out ContentRefreshDirtyBatch overlongBatch), "An overlong owner should still produce a consumable generation.");
            Assert(overlongBatch.OwnerIds.Count == 1 && overlongBatch.OwnerIds[0] == "all", "An owner beyond the authoritative bound must conservatively select all owners instead of becoming a display hash.");
            Assert(generations.Complete(
                overlongBatch,
                new[] { new ContentRefreshCompletion(overlongOwner, ContentRefreshCompletionStatus.Committed, 1, 2, 2, "overlong-owner") }),
                "The conservative overlong-owner generation should complete.");
            ContentRefreshReceipt receipt = generations.GetReceipts().Last();
            Assert(receipt.OwnerId.Length <= 128 && receipt.OwnerId != overlongOwner, "Receipt/display owner identities must remain bounded independently of authoritative dirty selection.");
        }

        private static void ContentQueryRejectRetainsAtomicLastGoodAndStableModSnapshots()
        {
            WithIsolatedPersistentRoot(persistentRoot =>
            {
                string gameDir = NewTempGameDir();
                const string folder = "Batch5ContentAtomic";
                const string ownerId = "DTMAPI.Tests.Batch5ContentAtomic";
                string packageRoot = Path.Combine(persistentRoot, "MODS", folder);
                string dtmapiRoot = Path.Combine(packageRoot, "Content", "DTMAPI");
                string tableRoot = Path.Combine(packageRoot, "Content", "Tables");
                Directory.CreateDirectory(dtmapiRoot);
                Directory.CreateDirectory(tableRoot);
                WriteManifest(Path.Combine(dtmapiRoot, "manifest.json"), ownerId);
                File.WriteAllText(Path.Combine(packageRoot, "info.json"), "{ \"name\": \"Batch 5 Atomic Content\" }", new UTF8Encoding(false));
                string itemPath = Path.Combine(tableRoot, "item_tbitem.json");
                WriteItem(itemPath, "batch5_atomic_a", "Generation A");
                WriteOfficialModInfos(persistentRoot, "Local." + folder, enabled: true);

                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                Assert(runtime.GetIndexedContentItem("batch5_atomic_a") != null, "Valid ContentQuery generation A should publish.");
                Assert(ReferenceEquals(runtime.LoadedMods, runtime.LoadedMods) && ReferenceEquals(runtime.DiscoveredMods, runtime.DiscoveredMods), "Runtime mod views should return stable read-only snapshots between mutations.");
                Assert(!(runtime.LoadedMods is DiscoveredMod[]) && runtime.LoadedMods.Count > 0, "The public stable view must not expose its authoritative backing array.");
                bool rejectedExternalMutation = false;
                try
                {
                    ((IList<DiscoveredMod>)runtime.LoadedMods)[0] = runtime.LoadedMods[0];
                }
                catch (NotSupportedException)
                {
                    rejectedExternalMutation = true;
                }
                Assert(rejectedExternalMutation && runtime.LoadedMods[0].Manifest.UniqueID == ownerId, "The stable read-only wrapper must reject IList element replacement without changing authoritative publication.");
                IReadOnlyList<DiscoveredMod> loadedBeforeNoOpRefresh = runtime.LoadedMods;
                long generationA = runtime.Content.LastGoodGeneration;
                int buildsAfterA = runtime.Content.CandidateBuildCountForTest;

                File.WriteAllText(itemPath, "[{ invalid-json ]", new UTF8Encoding(false));
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.GetIndexedContentItem("batch5_atomic_a") != null, "A rejected ContentQuery candidate must retain the complete generation A publication.");
                Assert(runtime.Content.LastGoodGeneration == generationA && runtime.Content.CandidateBuildCountForTest == buildsAfterA + 1, "Rejected ContentQuery candidate must not advance last-good generation.");
                Assert(ReferenceEquals(loadedBeforeNoOpRefresh, runtime.LoadedMods), "A source refresh with no loaded-owner mutation must preserve the stable LoadedMods snapshot instance.");
                ContentRefreshReceipt rejected = runtime.ContentRefreshGenerations.GetReceipts().Last(receipt => receipt.Domain == ContentRefreshDomains.ContentQuery);
                Assert(rejected.Status == ContentRefreshCompletionStatus.Rejected && rejected.LastGoodGeneration == generationA, "ContentQuery rejection receipt should name retained last-good generation A.");

                WriteItem(itemPath, "batch5_atomic_b", "Generation B");
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.GetIndexedContentItem("batch5_atomic_a") == null && runtime.GetIndexedContentItem("batch5_atomic_b") != null, "Valid ContentQuery generation B should replace A atomically.");
                Assert(runtime.Content.LastGoodGeneration > generationA, "Valid generation B should advance ContentQuery last-good generation.");
            });
        }

        private static void ContentQueryRejectsAnUnavailableStillActiveOwnerRoot()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                string ownerRoot = Path.Combine(gameDir, "Mods", "ActiveOwner");
                Directory.CreateDirectory(ownerRoot);
                string assetPath = Path.Combine(ownerRoot, "last-good.txt");
                File.WriteAllText(assetPath, "last-good", new UTF8Encoding(false));
                var manifest = new ManifestModel
                {
                    Name = "Active Owner",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.Batch5.ActiveMissingRoot",
                    Type = "ContentPack"
                };
                var activeOwner = new DiscoveredMod(
                    manifest,
                    ownerRoot,
                    Path.Combine(ownerRoot, "manifest.json"),
                    "Local",
                    null,
                    officialEnabled: true,
                    canDtmApiToggle: true,
                    officialId: "Local.ActiveOwner",
                    officialEnablementManaged: false,
                    enablementReason: string.Empty);
                var service = new ContentQueryService(new RuntimePaths(gameDir, gameDir));
                var generations = new ContentRefreshGenerationService();

                generations.MarkDirty(ContentRefreshDomains.ContentQuery, new[] { manifest.UniqueID }, "initial");
                Assert(generations.TryGetDirty(ContentRefreshDomains.ContentQuery, out ContentRefreshDirtyBatch initialBatch), "The initial ContentQuery generation should be consumable.");
                ContentQueryRebuildResult prepared = service.RebuildCandidate(new[] { activeOwner }, initialBatch.Generation, "initial");
                Assert(prepared.Success && service.FindAssets("txt").Count == 0, "Preparing the initial active owner must not publish before the generation authority accepts it.");
                Assert(generations.CompleteWithAtomicCommit(
                    initialBatch,
                    new[] { new ContentRefreshCompletion(manifest.UniqueID, ContentRefreshCompletionStatus.Committed, prepared.PreviousGeneration, prepared.CurrentGeneration, prepared.CurrentGeneration, "initial") },
                    prepared.PublishPreparedSnapshot),
                    "The matching initial ContentQuery generation should accept its prepared publication.");
                Assert(service.FindAssets("txt").Count == 1, "The accepted initial active owner should publish its text asset.");
                Directory.Move(ownerRoot, ownerRoot + ".temporarily-unavailable");

                generations.MarkDirty(ContentRefreshDomains.ContentQuery, new[] { manifest.UniqueID }, "active root unavailable");
                Assert(generations.TryGetDirty(ContentRefreshDomains.ContentQuery, out ContentRefreshDirtyBatch invalidBatch), "The invalid-root ContentQuery generation should be consumable.");
                ContentQueryRebuildResult rejected = service.RebuildCandidate(new[] { activeOwner }, invalidBatch.Generation, "active root unavailable");
                Assert(!rejected.Success && rejected.RetainedPreviousGeneration, "A still-active owner with an unavailable root must reject the candidate generation.");
                Assert(generations.CompleteWithAtomicCommit(
                    invalidBatch,
                    new[] { new ContentRefreshCompletion(manifest.UniqueID, ContentRefreshCompletionStatus.Rejected, rejected.PreviousGeneration, rejected.CurrentGeneration, rejected.CurrentGeneration, rejected.Failure) },
                    rejected.PublishPreparedSnapshot),
                    "The invalid-root generation should close as one rejected terminal result without a publisher.");
                Assert(service.LastGoodGeneration == prepared.CurrentGeneration && service.FindAssets("txt").Count == 1, "The rejected generation must preserve the complete last-good publication.");
                ContentRefreshReceipt invalidReceipt = generations.GetReceipts().Last(receipt => receipt.Domain == ContentRefreshDomains.ContentQuery);
                Assert(invalidReceipt.Status == ContentRefreshCompletionStatus.Rejected && invalidReceipt.LastGoodGeneration == service.LastGoodGeneration, "The invalid-root rejection receipt must name the retained visible ContentQuery generation.");
            });
        }

        private static void ContentQueryAtomicBarrierPublishesReceiptWithVisibleSnapshot()
        {
            WithIsolatedPersistentRoot(persistentRoot =>
            {
                string gameDir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.Batch5Content.Barrier";
                string itemPath = WriteOfficialContentPack(persistentRoot, "Batch5ContentBarrier", ownerId, "content_barrier_a", "Content Barrier A");
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                int receiptsBefore = runtime.ContentRefreshGenerations.GetReceipts().Count(receipt => receipt.Domain == ContentRefreshDomains.ContentQuery);

                WriteItem(itemPath, "content_barrier_b", "Content Barrier B");
                bool barrierObserved = false;
                runtime.Content.PreCommitFaultForTest = _ =>
                {
                    barrierObserved = true;
                    Assert(runtime.GetIndexedContentItem("content_barrier_a") != null && runtime.GetIndexedContentItem("content_barrier_b") == null, "Before the atomic publisher swaps the ContentQuery snapshot, only generation A may be visible.");
                    Assert(runtime.ContentRefreshGenerations.GetReceipts().Count(receipt => receipt.Domain == ContentRefreshDomains.ContentQuery) == receiptsBefore, "The ContentQuery terminal receipt must not publish before its matching snapshot swap.");
                };
                try
                {
                    runtime.NotifyWorkshopModListChanged();
                }
                finally
                {
                    runtime.Content.PreCommitFaultForTest = null;
                }

                ContentRefreshReceipt receipt = runtime.ContentRefreshGenerations.GetReceipts().Last(item => item.Domain == ContentRefreshDomains.ContentQuery);
                Assert(barrierObserved && runtime.GetIndexedContentItem("content_barrier_a") == null && runtime.GetIndexedContentItem("content_barrier_b") != null, "Returning from the ContentQuery atomic commit must expose generation B only.");
                Assert(receipt.Status == ContentRefreshCompletionStatus.Committed && receipt.CurrentGeneration == runtime.Content.LastGoodGeneration && runtime.ContentRefreshGenerations.GetReceipts().Count(item => item.Domain == ContentRefreshDomains.ContentQuery) == receiptsBefore + 1, "The ContentQuery snapshot and exactly one matching terminal receipt must become authoritative at the same boundary.");
            });
        }

        private static void ContentQueryPreCommitFailureRetainsLastGoodAndRequeues()
        {
            WithIsolatedPersistentRoot(persistentRoot =>
            {
                string gameDir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.Batch5Content.PreCommit";
                string itemPath = WriteOfficialContentPack(persistentRoot, "Batch5ContentPreCommit", ownerId, "content_precommit_a", "Content PreCommit A");
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                long generationA = runtime.Content.LastGoodGeneration;

                WriteItem(itemPath, "content_precommit_b", "Content PreCommit B");
                runtime.Content.PreCommitFaultForTest = _ => throw new InvalidOperationException("injected ContentQuery before visible snapshot swap");
                bool threw = false;
                try
                {
                    runtime.NotifyWorkshopModListChanged();
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("injected ContentQuery", StringComparison.Ordinal))
                {
                    threw = true;
                }
                finally
                {
                    runtime.Content.PreCommitFaultForTest = null;
                }

                Assert(threw, "The ContentQuery pre-commit fault must cross the real generation publisher.");
                Assert(runtime.GetIndexedContentItem("content_precommit_a") != null && runtime.GetIndexedContentItem("content_precommit_b") == null && runtime.Content.LastGoodGeneration == generationA, "A pre-commit failure must preserve the complete ContentQuery generation A publication.");
                Assert(runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.ContentQuery), "A ContentQuery pre-commit failure must leave a recoverable successor dirty generation.");
                ContentRefreshReceipt abandoned = runtime.ContentRefreshGenerations.GetReceipts().Last(receipt => receipt.Domain == ContentRefreshDomains.ContentQuery);
                Assert(abandoned.Status == ContentRefreshCompletionStatus.Rejected && abandoned.Details.Contains("Unexpected consumer failure", StringComparison.Ordinal), "The failed ContentQuery generation must be explicitly abandoned without an owner commit receipt.");

                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.GetIndexedContentItem("content_precommit_a") == null && runtime.GetIndexedContentItem("content_precommit_b") != null && runtime.Content.LastGoodGeneration > generationA, "The requeued ContentQuery successor should publish generation B normally.");
            });
        }

        private static void ContentQueryStaleCompletionDoesNotPublishPreparedSnapshot()
        {
            WithIsolatedPersistentRoot(persistentRoot =>
            {
                string gameDir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.Batch5Content.Stale";
                string itemPath = WriteOfficialContentPack(persistentRoot, "Batch5ContentStale", ownerId, "content_stale_a", "Content Stale A");
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                long generationA = runtime.Content.LastGoodGeneration;

                WriteItem(itemPath, "content_stale_b", "Content Stale B");
                runtime.Content.BeforeGenerationCompleteForTest = batch =>
                    runtime.ContentRefreshGenerations.AbandonAndRequeue(batch, "injected ContentQuery stale completion before authority commit");
                bool threw = false;
                try
                {
                    runtime.NotifyWorkshopModListChanged();
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("rejected as stale", StringComparison.Ordinal))
                {
                    threw = true;
                }
                finally
                {
                    runtime.Content.BeforeGenerationCompleteForTest = null;
                }

                Assert(threw, "A stale ContentQuery completion must be rejected by the generation authority edge.");
                Assert(runtime.GetIndexedContentItem("content_stale_a") != null && runtime.GetIndexedContentItem("content_stale_b") == null && runtime.Content.LastGoodGeneration == generationA, "A stale completion must never invoke the prepared ContentQuery publisher.");
                Assert(runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.ContentQuery), "The stale ContentQuery completion must retain explicit successor work.");

                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.GetIndexedContentItem("content_stale_a") == null && runtime.GetIndexedContentItem("content_stale_b") != null && runtime.Content.LastGoodGeneration > generationA, "Only a fresh matching ContentQuery completion may publish the stale generation's successor.");
            });
        }

        private static void ContentQueryPostCommitFailureDoesNotRequeuePublishedGeneration()
        {
            WithIsolatedPersistentRoot(persistentRoot =>
            {
                string gameDir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.Batch5Content.PostCommit";
                string itemPath = WriteOfficialContentPack(persistentRoot, "Batch5ContentPostCommit", ownerId, "content_postcommit_a", "Content PostCommit A");
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                int receiptsBefore = runtime.ContentRefreshGenerations.GetReceipts().Count(receipt => receipt.Domain == ContentRefreshDomains.ContentQuery);

                WriteItem(itemPath, "content_postcommit_b", "Content PostCommit B");
                runtime.Content.PostCommitFaultForTest = _ => throw new InvalidOperationException("injected ContentQuery post-commit sink failure");
                bool threw = false;
                try
                {
                    runtime.NotifyWorkshopModListChanged();
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("injected ContentQuery post-commit", StringComparison.Ordinal))
                {
                    threw = true;
                }
                finally
                {
                    runtime.Content.PostCommitFaultForTest = null;
                }

                ContentRefreshReceipt receipt = runtime.ContentRefreshGenerations.GetReceipts().Last(item => item.Domain == ContentRefreshDomains.ContentQuery);
                Assert(threw && runtime.GetIndexedContentItem("content_postcommit_a") == null && runtime.GetIndexedContentItem("content_postcommit_b") != null, "A post-commit diagnostic failure must leave ContentQuery generation B visible.");
                Assert(!runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.ContentQuery), "A terminal ContentQuery generation must not be requeued after a post-commit diagnostic failure.");
                Assert(receipt.Status == ContentRefreshCompletionStatus.Committed && receipt.CurrentGeneration == runtime.Content.LastGoodGeneration && runtime.ContentRefreshGenerations.GetReceipts().Count(item => item.Domain == ContentRefreshDomains.ContentQuery) == receiptsBefore + 1, "The post-commit failure must retain exactly one committed ContentQuery terminal receipt.");
            });
        }

        private static void ContentQueryOwnerCleanupAndSuccessorGenerationStayConsistent()
        {
            WithIsolatedPersistentRoot(persistentRoot =>
            {
                string gameDir = NewTempGameDir();
                const string folder = "Batch5ContentOwnerCleanup";
                const string ownerId = "DTMAPI.Tests.Batch5Content.OwnerCleanup";
                WriteOfficialContentPack(persistentRoot, folder, ownerId, "content_owner_cleanup", "Content Owner Cleanup");
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                long generationA = runtime.Content.LastGoodGeneration;
                Assert(runtime.Content.CountOwnerResources(ownerId) > 0, "The active ContentQuery owner should have roots before cleanup.");

                WriteOfficialModInfos(persistentRoot, "Local." + folder, enabled: false);
                runtime.NotifyWorkshopModListChanged();

                ContentRefreshReceipt receipt = runtime.ContentRefreshGenerations.GetReceipts().Last(item => item.Domain == ContentRefreshDomains.ContentQuery);
                Assert(runtime.Content.CountOwnerResources(ownerId) == 0 && runtime.GetIndexedContentItem("content_owner_cleanup") == null, "Owner cleanup and its source-driven successor must leave no visible ContentQuery owner roots.");
                Assert(runtime.Content.LastGoodGeneration > generationA && receipt.Status == ContentRefreshCompletionStatus.Committed && receipt.CurrentGeneration == runtime.Content.LastGoodGeneration, "The owner-cleanup successor must advance one authoritative ContentQuery generation whose terminal receipt names the visible snapshot.");
            });
        }

        private static void CustomAnimalsRejectOnlyBadOwnerAndSteadyFramesDoNotBuildCandidates()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                const string ownerA = "DTMAPI.Tests.Batch5Animals.A";
                const string ownerB = "DTMAPI.Tests.Batch5Animals.B";
                string schemaA = WriteCustomAnimalPack(gameDir, "Batch5AnimalsA", ownerA, "species_a", "anim_a");
                string schemaB = WriteCustomAnimalPack(gameDir, "Batch5AnimalsB", ownerB, "species_b", "anim_b");

                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                var service = new CustomAnimalAnimatorBridgeService(runtime);
                Assert(service.RefreshDefinitions("unit-initial", force: false), "Startup dirty generation should build CustomAnimals candidates.");
                Assert(service.RegisteredKeyCount == 2 && service.RegisteredKeySummary.Contains("anim_a", StringComparison.Ordinal) && service.RegisteredKeySummary.Contains("anim_b", StringComparison.Ordinal), "Both valid owners should publish their initial animator definitions.");
                long ownerAGeneration = service.GetOwnerDefinitionGenerationForTest(ownerA);
                long ownerBGeneration = service.GetOwnerDefinitionGenerationForTest(ownerB);
                int buildsAfterInitial = service.DefinitionCandidateBuildCountForTest;

                for (int index = 0; index < 10000; index++)
                    Assert(!service.RefreshDefinitions("steady-frame", force: false), "A clean CustomAnimals frame should perform only the dirty-state check.");
                Assert(service.DefinitionCandidateBuildCountForTest == buildsAfterInitial, "10,000 clean frames must not build a definition candidate or inspect schema files.");

                File.WriteAllText(schemaA, "[{ invalid-json ]", new UTF8Encoding(false));
                WriteCustomAnimalSchema(schemaB, "species_b", "anim_b_v2");
                runtime.NotifyWorkshopModListChanged();
                Assert(service.RefreshDefinitions("unit-owner-isolation", force: false), "Workshop source refresh should expose one dirty CustomAnimals generation.");
                string summary = service.RegisteredKeySummary;
                Assert(summary.Contains("anim_a", StringComparison.Ordinal) && summary.Contains("anim_b_v2", StringComparison.Ordinal) && !summary.Contains("anim_b,", StringComparison.Ordinal), "Bad owner A should retain its last-good key while valid owner B atomically advances to its new key.");
                Assert(service.GetOwnerDefinitionGenerationForTest(ownerA) == ownerAGeneration, "Rejected owner A must retain its prior owner generation.");
                Assert(service.GetOwnerDefinitionGenerationForTest(ownerB) > ownerBGeneration, "Valid owner B must advance independently of owner A rejection.");
                ContentRefreshReceipt ownerAReceipt = runtime.ContentRefreshGenerations.GetReceipts().Last(receipt => receipt.Domain == ContentRefreshDomains.CustomAnimals && receipt.OwnerId == ownerA);
                ContentRefreshReceipt ownerBReceipt = runtime.ContentRefreshGenerations.GetReceipts().Last(receipt => receipt.Domain == ContentRefreshDomains.CustomAnimals && receipt.OwnerId == ownerB);
                Assert(ownerAReceipt.Status == ContentRefreshCompletionStatus.Rejected && ownerAReceipt.LastGoodGeneration == ownerAGeneration, "Bad owner receipt should preserve owner A last-good generation.");
                Assert(ownerBReceipt.Status == ContentRefreshCompletionStatus.Committed && ownerBReceipt.CurrentGeneration > ownerBGeneration, "Good owner receipt should commit independently.");
            });
        }

        private static void OverlongOwnerDirtyRequestRebuildsAudioAndCustomAnimals()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                string ownerId = "DTMAPI.Tests.Batch5.OverlongOwner." + new string('x', 128);
                string root = Path.Combine(gameDir, "Mods", "Batch5OverlongOwner");
                string dtmapiRoot = Path.Combine(root, "Content", "DTMAPI");
                string audioRoot = Path.Combine(root, "Content", "Audio");
                Directory.CreateDirectory(dtmapiRoot);
                Directory.CreateDirectory(audioRoot);
                WriteManifest(Path.Combine(root, "manifest.json"), ownerId);
                string animalSchema = Path.Combine(dtmapiRoot, "custom-animals.json");
                string audioSchema = Path.Combine(dtmapiRoot, "audio-replacements.json");
                WriteCustomAnimalSchema(animalSchema, "species_long", "anim_long_a");
                WriteAudioSchema(audioSchema, "audio_long_a");
                WritePcm16MonoWav(Path.Combine(audioRoot, "test.wav"));

                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                var animals = new CustomAnimalAnimatorBridgeService(runtime);
                var audio = new AudioReplacementService(runtime, (_, __) => new object());
                Assert(animals.RefreshDefinitions("unit-long-owner-initial", force: false), "Startup dirtiness should build the long owner's CustomAnimals generation.");
                audio.RefreshContentPackDefinitions("unit-long-owner-initial", force: false);
                Assert(animals.RegisteredKeySummary.Contains("anim_long_a", StringComparison.Ordinal), "The initial CustomAnimals definition should be active.");
                Assert(audio.GetState(ownerId).LastReplacementId == "audio_long_a", "The initial Audio definition should be active.");
                int animalBuildsBefore = animals.DefinitionCandidateBuildCountForTest;

                WriteCustomAnimalSchema(animalSchema, "species_long", "anim_long_b");
                WriteAudioSchema(audioSchema, "audio_long_b");
                runtime.ContentRefreshGenerations.MarkDirty(ContentRefreshDomains.CustomAnimals, new[] { ownerId }, "unit-long-owner-change");
                runtime.ContentRefreshGenerations.MarkDirty(ContentRefreshDomains.AudioReplacement, new[] { ownerId }, "unit-long-owner-change");

                Assert(animals.RefreshDefinitions("unit-long-owner-change", force: false), "An overlong owner request must conservatively rebuild the actual CustomAnimals owner.");
                audio.RefreshContentPackDefinitions("unit-long-owner-change", force: false);
                Assert(animals.DefinitionCandidateBuildCountForTest == animalBuildsBefore + 1 && animals.RegisteredKeySummary.Contains("anim_long_b", StringComparison.Ordinal), "CustomAnimals must rebuild and publish the changed long-owner definition.");
                AudioReplacementState audioState = audio.GetState(ownerId);
                Assert(audioState.LastReplacementId == "audio_long_b" && audioState.Replacements.All(entry => entry.ReplacementId != "audio_long_a"), "Audio must rebuild and atomically replace the long owner's prior definition.");
            });
        }

        private static void CustomAnimalsAtomicBarrierPublishesReceiptWithVisibleSnapshot()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.Batch5Animals.Barrier";
                string schema = WriteCustomAnimalPack(gameDir, "Batch5AnimalsBarrier", ownerId, "species_barrier", "anim_barrier_a");
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                var service = new CustomAnimalAnimatorBridgeService(runtime);
                Assert(service.RefreshDefinitions("unit-barrier-initial", force: false), "Initial CustomAnimals generation must commit before the barrier test.");
                int receiptsBefore = runtime.ContentRefreshGenerations.GetReceipts().Count(receipt => receipt.Domain == ContentRefreshDomains.CustomAnimals);

                WriteCustomAnimalSchema(schema, "species_barrier", "anim_barrier_b");
                runtime.ContentRefreshGenerations.MarkDirty(ContentRefreshDomains.CustomAnimals, new[] { ownerId }, "unit-atomic-barrier");
                bool barrierObserved = false;
                service.PreCommitFaultForTest = _ =>
                {
                    barrierObserved = true;
                    Assert(service.RegisteredKeySummary.Contains("anim_barrier_a", StringComparison.Ordinal) && !service.RegisteredKeySummary.Contains("anim_barrier_b", StringComparison.Ordinal), "Before the atomic publisher swaps the live snapshot, only the last-good CustomAnimals definition may be visible.");
                    Assert(runtime.ContentRefreshGenerations.GetReceipts().Count(receipt => receipt.Domain == ContentRefreshDomains.CustomAnimals) == receiptsBefore, "The terminal receipt must not publish before its matching live snapshot swap.");
                };
                try
                {
                    Assert(service.RefreshDefinitions("unit-atomic-barrier", force: false), "The matching CustomAnimals generation should cross the atomic commit barrier.");
                }
                finally
                {
                    service.PreCommitFaultForTest = null;
                }

                ContentRefreshReceipt receipt = runtime.ContentRefreshGenerations.GetReceipts().Last(item => item.Domain == ContentRefreshDomains.CustomAnimals && item.OwnerId == ownerId);
                Assert(barrierObserved && service.RegisteredKeySummary.Contains("anim_barrier_b", StringComparison.Ordinal), "Returning from the atomic commit must expose the new CustomAnimals snapshot.");
                Assert(receipt.Status == ContentRefreshCompletionStatus.Committed && receipt.CurrentGeneration == service.GetOwnerDefinitionGenerationForTest(ownerId) && runtime.ContentRefreshGenerations.GetReceipts().Count(item => item.Domain == ContentRefreshDomains.CustomAnimals) == receiptsBefore + 1, "The new live snapshot and exactly one matching terminal receipt must become authoritative at the same commit boundary.");
            });
        }

        private static void CustomAnimalsPreCommitFailureRetainsLastGoodAndRequeues()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.Batch5Animals.PreCommit";
                string schema = WriteCustomAnimalPack(gameDir, "Batch5AnimalsPreCommit", ownerId, "species_precommit", "anim_precommit_a");
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                var service = new CustomAnimalAnimatorBridgeService(runtime);
                Assert(service.RefreshDefinitions("unit-precommit-initial", force: false), "Initial CustomAnimals generation must commit before fault injection.");
                long lastGoodGeneration = service.GetOwnerDefinitionGenerationForTest(ownerId);

                WriteCustomAnimalSchema(schema, "species_precommit", "anim_precommit_b");
                runtime.ContentRefreshGenerations.MarkDirty(ContentRefreshDomains.CustomAnimals, new[] { ownerId }, "unit-precommit-fault");
                service.PreCommitFaultForTest = _ => throw new InvalidOperationException("injected CustomAnimals before visible swap");
                bool threw = false;
                try
                {
                    service.RefreshDefinitions("unit-precommit-fault", force: false);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("injected CustomAnimals", StringComparison.Ordinal))
                {
                    threw = true;
                }
                finally
                {
                    service.PreCommitFaultForTest = null;
                }

                Assert(threw, "The pre-commit fault must cross the real CustomAnimals generation publisher.");
                Assert(service.RegisteredKeySummary.Contains("anim_precommit_a", StringComparison.Ordinal) && !service.RegisteredKeySummary.Contains("anim_precommit_b", StringComparison.Ordinal) && service.GetOwnerDefinitionGenerationForTest(ownerId) == lastGoodGeneration, "A pre-commit failure must preserve the complete last-good CustomAnimals snapshot and generation.");
                Assert(runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.CustomAnimals), "A pre-commit failure must leave a recoverable successor dirty generation instead of committing the failed candidate.");
                ContentRefreshReceipt abandoned = runtime.ContentRefreshGenerations.GetReceipts().Last(receipt => receipt.Domain == ContentRefreshDomains.CustomAnimals);
                Assert(abandoned.Status == ContentRefreshCompletionStatus.Rejected && abandoned.Details.Contains("Unexpected consumer failure", StringComparison.Ordinal), "The failed pre-commit attempt must be explicitly abandoned without publishing an owner commit receipt.");

                Assert(service.RefreshDefinitions("unit-precommit-retry", force: false), "The requeued CustomAnimals successor should remain consumable.");
                Assert(service.RegisteredKeySummary.Contains("anim_precommit_b", StringComparison.Ordinal) && service.GetOwnerDefinitionGenerationForTest(ownerId) > lastGoodGeneration, "The successor should publish the prepared CustomAnimals definition normally.");
            });
        }

        private static void CustomAnimalsStaleCompletionDoesNotPublishPreparedSnapshot()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.Batch5Animals.Stale";
                string schema = WriteCustomAnimalPack(gameDir, "Batch5AnimalsStale", ownerId, "species_stale", "anim_stale_a");
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                var service = new CustomAnimalAnimatorBridgeService(runtime);
                Assert(service.RefreshDefinitions("unit-stale-initial", force: false), "Initial CustomAnimals generation must commit before stale-completion injection.");
                long lastGoodGeneration = service.GetOwnerDefinitionGenerationForTest(ownerId);

                WriteCustomAnimalSchema(schema, "species_stale", "anim_stale_b");
                runtime.ContentRefreshGenerations.MarkDirty(ContentRefreshDomains.CustomAnimals, new[] { ownerId }, "unit-stale-completion");
                service.BeforeGenerationCompleteForTest = batch =>
                    runtime.ContentRefreshGenerations.AbandonAndRequeue(batch, "injected CustomAnimals stale completion before authority commit");
                bool threw = false;
                try
                {
                    service.RefreshDefinitions("unit-stale-completion", force: false);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("rejected as stale", StringComparison.Ordinal))
                {
                    threw = true;
                }
                finally
                {
                    service.BeforeGenerationCompleteForTest = null;
                }

                Assert(threw, "A stale CustomAnimals generation completion must be rejected by the authority edge.");
                Assert(service.RegisteredKeySummary.Contains("anim_stale_a", StringComparison.Ordinal) && !service.RegisteredKeySummary.Contains("anim_stale_b", StringComparison.Ordinal) && service.GetOwnerDefinitionGenerationForTest(ownerId) == lastGoodGeneration, "A stale completion must never invoke the CustomAnimals visible-snapshot publisher.");
                Assert(runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.CustomAnimals), "The injected stale completion must retain explicit successor work.");

                Assert(service.RefreshDefinitions("unit-stale-retry", force: false), "The stale CustomAnimals generation's successor should remain consumable.");
                Assert(service.RegisteredKeySummary.Contains("anim_stale_b", StringComparison.Ordinal) && service.GetOwnerDefinitionGenerationForTest(ownerId) > lastGoodGeneration, "Only a fresh matching completion may publish the stale generation's successor.");
            });
        }

        private static void CustomAnimalsPostCommitFailureDoesNotRequeuePublishedGeneration()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.Batch5Animals.Atomic";
                string schema = WriteCustomAnimalPack(gameDir, "Batch5AnimalsAtomic", ownerId, "species_atomic", "anim_atomic_a");
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                var service = new CustomAnimalAnimatorBridgeService(runtime);
                Assert(service.RefreshDefinitions("unit-atomic-initial", force: false), "Initial CustomAnimals generation must commit.");
                int receiptsBefore = runtime.ContentRefreshGenerations.GetReceipts().Count(receipt => receipt.Domain == ContentRefreshDomains.CustomAnimals);

                WriteCustomAnimalSchema(schema, "species_atomic", "anim_atomic_b");
                runtime.ContentRefreshGenerations.MarkDirty(ContentRefreshDomains.CustomAnimals, new[] { ownerId }, "unit-post-commit-fault");
                service.PostCommitFaultForTest = _ => throw new InvalidOperationException("injected post-commit sink failure");
                bool threw = false;
                try
                {
                    service.RefreshDefinitions("unit-post-commit-fault", force: false);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("injected post-commit", StringComparison.Ordinal))
                {
                    threw = true;
                }
                finally
                {
                    service.PostCommitFaultForTest = null;
                }

                ContentRefreshReceipt receipt = runtime.ContentRefreshGenerations.GetReceipts().Last(item => item.Domain == ContentRefreshDomains.CustomAnimals && item.OwnerId == ownerId);
                Assert(threw && service.RegisteredKeySummary.Contains("anim_atomic_b", StringComparison.Ordinal), "An injected post-commit sink failure must leave the new CustomAnimals snapshot visible.");
                Assert(!runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.CustomAnimals), "A terminal CustomAnimals generation must not be requeued after a post-commit sink failure.");
                Assert(receipt.Status == ContentRefreshCompletionStatus.Committed && runtime.ContentRefreshGenerations.GetReceipts().Count(item => item.Domain == ContentRefreshDomains.CustomAnimals) == receiptsBefore + 1, "The generation must publish exactly one committed terminal receipt.");
            });
        }

        private static void AudioPreCommitFailureRetainsLastGoodAndRequeues()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.Batch5Audio.PreCommit";
                string schema = WriteAudioPack(gameDir, "Batch5AudioPreCommit", ownerId, "audio_precommit_a");
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                var service = new AudioReplacementService(runtime, (_, __) => new object());
                service.RefreshContentPackDefinitions("unit-precommit-initial", force: false);
                long lastGoodGeneration = service.GetOwnerContentPackGenerationForTest(ownerId);

                WriteAudioSchema(schema, "audio_precommit_b");
                runtime.ContentRefreshGenerations.MarkDirty(ContentRefreshDomains.AudioReplacement, new[] { ownerId }, "unit-precommit-fault");
                service.PreCommitFaultForTest = _ => throw new InvalidOperationException("injected before visible swap");
                bool threw = false;
                try
                {
                    service.RefreshContentPackDefinitions("unit-precommit-fault", force: false);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("injected before visible swap", StringComparison.Ordinal))
                {
                    threw = true;
                }
                finally
                {
                    service.PreCommitFaultForTest = null;
                }

                Assert(threw, "The pre-commit fault injection must cross the real Audio generation boundary.");
                Assert(service.GetState(ownerId).LastReplacementId == "audio_precommit_a" && service.GetOwnerContentPackGenerationForTest(ownerId) == lastGoodGeneration, "A failure before the visible swap must preserve the complete last-good Audio owner snapshot.");
                Assert(runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.AudioReplacement), "A pre-commit consumer failure must leave a recoverable successor dirty generation.");
                ContentRefreshReceipt abandoned = runtime.ContentRefreshGenerations.GetReceipts().Last(receipt => receipt.Domain == ContentRefreshDomains.AudioReplacement);
                Assert(abandoned.Status == ContentRefreshCompletionStatus.Rejected && abandoned.Details.Contains("Unexpected consumer failure", StringComparison.Ordinal), "The abandoned pre-commit generation must have one explicit rejected/requeued terminal receipt.");

                service.RefreshContentPackDefinitions("unit-precommit-retry", force: false);
                Assert(service.GetState(ownerId).LastReplacementId == "audio_precommit_b" && service.GetOwnerContentPackGenerationForTest(ownerId) > lastGoodGeneration, "The requeued successor must be able to commit the prepared replacement normally.");
            });
        }

        private static void AudioStaleCompletionDoesNotPublishPreparedSnapshot()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.Batch5Audio.Stale";
                string schema = WriteAudioPack(gameDir, "Batch5AudioStale", ownerId, "audio_stale_a");
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                var service = new AudioReplacementService(runtime, (_, __) => new object());
                service.RefreshContentPackDefinitions("unit-stale-initial", force: false);
                long lastGoodGeneration = service.GetOwnerContentPackGenerationForTest(ownerId);

                WriteAudioSchema(schema, "audio_stale_b");
                runtime.ContentRefreshGenerations.MarkDirty(ContentRefreshDomains.AudioReplacement, new[] { ownerId }, "unit-stale-completion");
                service.BeforeGenerationCompleteForTest = batch =>
                    runtime.ContentRefreshGenerations.AbandonAndRequeue(batch, "injected stale completion before authority commit");
                bool threw = false;
                try
                {
                    service.RefreshContentPackDefinitions("unit-stale-completion", force: false);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("rejected as stale", StringComparison.Ordinal))
                {
                    threw = true;
                }
                finally
                {
                    service.BeforeGenerationCompleteForTest = null;
                }

                Assert(threw, "A stale generation completion must be rejected by the authority edge.");
                Assert(service.GetState(ownerId).LastReplacementId == "audio_stale_a" && service.GetOwnerContentPackGenerationForTest(ownerId) == lastGoodGeneration, "A stale Complete result must never invoke the Audio visible-snapshot publisher.");
                Assert(runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.AudioReplacement), "The injected stale completion must retain its explicit successor work.");

                service.RefreshContentPackDefinitions("unit-stale-retry", force: false);
                Assert(service.GetState(ownerId).LastReplacementId == "audio_stale_b" && service.GetOwnerContentPackGenerationForTest(ownerId) > lastGoodGeneration, "The stale generation's successor should publish only through a fresh matching completion.");
            });
        }

        private static void AudioPostCommitFailureDoesNotRequeuePublishedGeneration()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.Batch5Audio.Atomic";
                string root = Path.Combine(gameDir, "Mods", "Batch5AudioAtomic");
                string dtmapiRoot = Path.Combine(root, "Content", "DTMAPI");
                string audioRoot = Path.Combine(root, "Content", "Audio");
                Directory.CreateDirectory(dtmapiRoot);
                Directory.CreateDirectory(audioRoot);
                WriteManifest(Path.Combine(root, "manifest.json"), ownerId);
                string schema = Path.Combine(dtmapiRoot, "audio-replacements.json");
                WriteAudioSchema(schema, "audio_atomic_a");
                WritePcm16MonoWav(Path.Combine(audioRoot, "test.wav"));

                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                var service = new AudioReplacementService(runtime, (_, __) => new object());
                service.RefreshContentPackDefinitions("unit-atomic-initial", force: false);
                int receiptsBefore = runtime.ContentRefreshGenerations.GetReceipts().Count(receipt => receipt.Domain == ContentRefreshDomains.AudioReplacement);

                WriteAudioSchema(schema, "audio_atomic_b");
                runtime.ContentRefreshGenerations.MarkDirty(ContentRefreshDomains.AudioReplacement, new[] { ownerId }, "unit-post-commit-fault");
                service.PostCommitFaultForTest = _ => throw new InvalidOperationException("injected post-commit sink failure");
                bool threw = false;
                try
                {
                    service.RefreshContentPackDefinitions("unit-post-commit-fault", force: false);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("injected post-commit", StringComparison.Ordinal))
                {
                    threw = true;
                }
                finally
                {
                    service.PostCommitFaultForTest = null;
                }

                ContentRefreshReceipt receipt = runtime.ContentRefreshGenerations.GetReceipts().Last(item => item.Domain == ContentRefreshDomains.AudioReplacement && item.OwnerId == ownerId);
                Assert(threw && service.GetState(ownerId).LastReplacementId == "audio_atomic_b", "An injected post-commit sink failure must leave the new Audio snapshot visible.");
                Assert(!runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.AudioReplacement), "A terminal Audio generation must not be requeued after a post-commit sink failure.");
                Assert(receipt.Status == ContentRefreshCompletionStatus.Committed && runtime.ContentRefreshGenerations.GetReceipts().Count(item => item.Domain == ContentRefreshDomains.AudioReplacement) == receiptsBefore + 1, "The Audio generation must publish exactly one committed terminal receipt.");
            });
        }

        private static void AudioMultiOwnerBatchPublishesOnlyAfterAllOwnersReachATerminalCandidate()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                const string ownerA = "DTMAPI.Tests.Batch5Audio.Multi.A";
                const string ownerB = "DTMAPI.Tests.Batch5Audio.Multi.B";
                string schemaA = WriteAudioPack(gameDir, "Batch5AudioMultiA", ownerA, "audio_multi_a1", suppressNativeWhenReady: false);
                string schemaB = WriteAudioPack(gameDir, "Batch5AudioMultiB", ownerB, "audio_multi_b1", suppressNativeWhenReady: false);
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                var service = new AudioReplacementService(runtime, (_, __) => new object());
                service.RefreshContentPackDefinitions("unit-multi-initial", force: false);
                long ownerAGeneration = service.GetOwnerContentPackGenerationForTest(ownerA);
                long ownerBGeneration = service.GetOwnerContentPackGenerationForTest(ownerB);

                WriteAudioSchema(schemaA, "audio_multi_a2", suppressNativeWhenReady: false);
                File.WriteAllText(schemaB, "[{ invalid-json ]", new UTF8Encoding(false));
                runtime.ContentRefreshGenerations.MarkDirty(ContentRefreshDomains.AudioReplacement, new[] { ownerA, ownerB }, "unit-multi-late-owner-reject");
                service.RefreshContentPackDefinitions("unit-multi-late-owner-reject", force: false);

                Assert(service.GetState(ownerA).LastReplacementId == "audio_multi_a2" && service.GetOwnerContentPackGenerationForTest(ownerA) > ownerAGeneration, "The valid first owner may advance, but only at the batch authority commit after every owner candidate is terminal.");
                Assert(service.GetState(ownerB).LastReplacementId == "audio_multi_b1" && service.GetOwnerContentPackGenerationForTest(ownerB) == ownerBGeneration, "A later invalid owner must retain its entire last-good snapshot while the batch commits other terminal owner outcomes.");
                ContentRefreshReceipt ownerAReceipt = runtime.ContentRefreshGenerations.GetReceipts().Last(receipt => receipt.Domain == ContentRefreshDomains.AudioReplacement && receipt.OwnerId == ownerA);
                ContentRefreshReceipt ownerBReceipt = runtime.ContentRefreshGenerations.GetReceipts().Last(receipt => receipt.Domain == ContentRefreshDomains.AudioReplacement && receipt.OwnerId == ownerB);
                Assert(ownerAReceipt.Status == ContentRefreshCompletionStatus.Committed && ownerAReceipt.CurrentGeneration == service.GetOwnerContentPackGenerationForTest(ownerA), "The first owner's terminal receipt must name the generation that became visible at the atomic batch swap.");
                Assert(ownerBReceipt.Status == ContentRefreshCompletionStatus.Rejected && ownerBReceipt.LastGoodGeneration == ownerBGeneration, "The later owner's terminal receipt must explicitly reject while preserving its authoritative last-good generation.");
                Assert(!runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.AudioReplacement), "A batch with per-owner committed/rejected terminal outcomes must close exactly once without requeueing published state.");
            });
        }

        private static void AudioPublicationFailureDoesNotSkipDemandReconciliation()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.Batch5Audio.DemandFault";
                WriteAudioPack(gameDir, "Batch5AudioDemandFault", ownerId, "audio_demand_fault");
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                var service = new AudioReplacementService(runtime, (_, __) => new object());
                service.PublicationSideEffectFaultForTest = _ => throw new InvalidOperationException("injected Audio log/lifecycle publication failure");
                try
                {
                    service.RefreshContentPackDefinitions("unit-demand-side-effect-fault", force: false);
                }
                finally
                {
                    service.PublicationSideEffectFaultForTest = null;
                }

                ContentRefreshReceipt receipt = runtime.ContentRefreshGenerations.GetReceipts().Last(item => item.Domain == ContentRefreshDomains.AudioReplacement && item.OwnerId == ownerId);
                Assert(service.GetState(ownerId).LastReplacementId == "audio_demand_fault" && receipt.Status == ContentRefreshCompletionStatus.Committed, "A throwable post-commit publication side effect must not undo the committed Audio snapshot or terminal receipt.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AudioReplacement) == 1 && runtime.DemandCoordinator.GetOwnerDemandCount(ownerId) == 1, "Audio demand reconciliation must run as a non-skippable best-effort step even when logging/lifecycle publication throws first.");
                Assert(!runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.AudioReplacement), "A post-commit Audio publication fault must not requeue the already-published generation.");
            });
        }

        private static void AudioReadOnlyHealthQueriesDoNotCreatePhantomOwnerResources()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.Batch5Audio.NoSchema";
                string ownerRoot = Path.Combine(gameDir, "Mods", "Batch5AudioNoSchema");
                Directory.CreateDirectory(ownerRoot);
                WriteManifest(Path.Combine(ownerRoot, "manifest.json"), ownerId);
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                var feature = new AudioReplacementFeature(runtime);

                AudioReplacementState initial = feature.Service.GetState(ownerId);
                BridgeFeatureStatus initialStatus = feature.Service.GetStatus(ownerId);
                Assert(initial.Status == "not-configured" && initialStatus.Status == "not-configured", "A pure Audio GetState/GetStatus query for an unconfigured owner must return a transient not-configured projection.");
                Assert(feature.Service.CountOwnerResources(ownerId) == 0, "Pure Audio health queries must not persist a phantom owner state.");

                feature.SaveLoaded(isNewGame: false);
                feature.PublishHookStatuses();
                AudioReplacementState afterSaveLoaded = feature.Service.GetState(ownerId);
                BridgeFeatureStatus afterHealth = feature.Service.GetStatus(ownerId);
                Assert(afterSaveLoaded.Status == "not-configured" && afterHealth.Status == "not-configured", "SaveLoaded and hook-health publication must preserve the no-schema owner's transient not-configured result.");
                Assert(feature.Service.CountOwnerResources(ownerId) == 0 && feature.Service.GetAudioReplacementLifecycleSummary().Contains("states=0", StringComparison.Ordinal), "A no-schema owner must retain zero Audio owner resources after SaveLoaded and repeated health queries.");
            });
        }

        private static void AudioUpdaterDoesNotTraverseWhenNoEntryIsPending()
        {
            WithIsolatedPersistentRoot(_ =>
            {
                string gameDir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                runtime.Start();
                var service = new AudioReplacementService(runtime);
                service.RefreshContentPackDefinitions("unit-startup", force: false);
                var owner = new ManifestModel
                {
                    Name = "Batch 5 Audio",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.Batch5Audio"
                };
                service.RegisterReplacement(owner, new AudioReplacementOptions
                {
                    ReplacementId = "missing-but-retained",
                    NativeSoundEvent = "PLAY_RESOURCE_PAPER_BOX",
                    AudioPath = Path.Combine(gameDir, "missing.wav"),
                    Enabled = true,
                    SuppressNativeWhenReady = true
                });
                Assert(service.HasEnabledDefinitions && service.PendingEntryCountForTest == 0, "A terminal failed entry may remain diagnosable but must not activate the pending updater.");
                long visitsBefore = service.PendingEntryVisitCountForTest;
                for (int index = 0; index < 10000; index++)
                    service.Update();
                Assert(service.PendingEntryVisitCountForTest == visitsBefore && service.PendingEntryCountForTest == 0, "10,000 frames with zero pending entries must traverse zero Audio entries.");
            });
        }

        private static string WriteCustomAnimalPack(string gameDir, string folder, string ownerId, string speciesId, string animatorKey)
        {
            string root = Path.Combine(gameDir, "Mods", folder);
            string dtmapiRoot = Path.Combine(root, "Content", "DTMAPI");
            Directory.CreateDirectory(dtmapiRoot);
            WriteManifest(Path.Combine(root, "manifest.json"), ownerId);
            string schemaPath = Path.Combine(dtmapiRoot, "custom-animals.json");
            WriteCustomAnimalSchema(schemaPath, speciesId, animatorKey);
            return schemaPath;
        }

        private static void WriteCustomAnimalSchema(string schemaPath, string speciesId, string animatorKey)
        {
            File.WriteAllText(
                schemaPath,
                "[{ \"speciesId\": \"" + speciesId + "\", \"templateSpeciesId\": \"goat\", \"aiTemplate\": \"goat\", \"animatorMode\": \"assetBundle\", \"adultAnimatorKey\": \"" + animatorKey + "\", \"animatorBundle\": \"Content/Bundles/animals\", \"adultAnimatorAsset\": \"controller\" }]",
                new UTF8Encoding(false));
        }

        private static string WriteAudioPack(
            string gameDir,
            string folder,
            string ownerId,
            string replacementId,
            bool suppressNativeWhenReady = true)
        {
            string root = Path.Combine(gameDir, "Mods", folder);
            string dtmapiRoot = Path.Combine(root, "Content", "DTMAPI");
            string audioRoot = Path.Combine(root, "Content", "Audio");
            Directory.CreateDirectory(dtmapiRoot);
            Directory.CreateDirectory(audioRoot);
            WriteManifest(Path.Combine(root, "manifest.json"), ownerId);
            string schemaPath = Path.Combine(dtmapiRoot, "audio-replacements.json");
            WriteAudioSchema(schemaPath, replacementId, suppressNativeWhenReady);
            WritePcm16MonoWav(Path.Combine(audioRoot, "test.wav"));
            return schemaPath;
        }

        private static void WriteAudioSchema(string schemaPath, string replacementId, bool suppressNativeWhenReady = true)
        {
            File.WriteAllText(
                schemaPath,
                "[{ \"id\": \"" + replacementId + "\", \"category\": \"SimpleSfx\", \"nativeSoundEvent\": \"PLAY_RESOURCE_PAPER_BOX\", \"file\": \"Content/Audio/test.wav\", \"suppressNativeWhenReady\": " + suppressNativeWhenReady.ToString().ToLowerInvariant() + " }]",
                new UTF8Encoding(false));
        }

        private static void WritePcm16MonoWav(string path)
        {
            const int sampleRate = 8000;
            const short channels = 1;
            const short bitsPerSample = 16;
            const int sampleCount = 80;
            int dataLength = sampleCount * sizeof(short);
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: false);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataLength);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bitsPerSample / 8);
            writer.Write((short)(channels * bitsPerSample / 8));
            writer.Write(bitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataLength);
            for (int index = 0; index < sampleCount; index++)
                writer.Write((short)(index % 2 == 0 ? 1000 : -1000));
        }

        private static void WriteManifest(string path, string ownerId)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Manifest directory is unavailable."));
            File.WriteAllText(
                path,
                "{ \"Name\": \"Batch 5 Content Test\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + ownerId + "\", \"Type\": \"ContentPack\" }",
                new UTF8Encoding(false));
        }

        private static void WriteItem(string path, string itemId, string title)
        {
            File.WriteAllText(
                path,
                "[{ \"id\": \"" + itemId + "\", \"sub_type\": \"material\", \"title\": { \"text\": \"" + title + "\", \"english\": \"" + title + "\" } }]",
                new UTF8Encoding(false));
        }

        private static string WriteOfficialContentPack(string persistentRoot, string folder, string ownerId, string itemId, string title)
        {
            string packageRoot = Path.Combine(persistentRoot, "MODS", folder);
            string dtmapiRoot = Path.Combine(packageRoot, "Content", "DTMAPI");
            string tableRoot = Path.Combine(packageRoot, "Content", "Tables");
            Directory.CreateDirectory(dtmapiRoot);
            Directory.CreateDirectory(tableRoot);
            WriteManifest(Path.Combine(dtmapiRoot, "manifest.json"), ownerId);
            File.WriteAllText(Path.Combine(packageRoot, "info.json"), "{ \"name\": \"Batch 5 Content Generation Test\" }", new UTF8Encoding(false));
            string itemPath = Path.Combine(tableRoot, "item_tbitem.json");
            WriteItem(itemPath, itemId, title);
            WriteOfficialModInfos(persistentRoot, "Local." + folder, enabled: true);
            return itemPath;
        }

        private static void WriteOfficialModInfos(string persistentRoot, string officialId, bool enabled)
        {
            string saveRoot = Path.Combine(persistentRoot, "SAVE");
            Directory.CreateDirectory(saveRoot);
            File.WriteAllText(
                Path.Combine(saveRoot, "mod_infos.json"),
                "{ \"modInfos\": { \"" + officialId + "\": { \"id\": \"" + officialId + "\", \"enabled\": " + (enabled ? "true" : "false") + ", \"priority\": -1, \"source\": \"Local\", \"title\": \"Batch 5 Content Test\" } } }",
                new UTF8Encoding(false));
        }

        private static string NewTempGameDir()
        {
            string path = Path.Combine(DtmApiTestSession.Current.RootPath, "batch5-content-generation", "game-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(path, "Mods"));
            return path;
        }

        private static void WithIsolatedPersistentRoot(Action<string> action)
        {
            string? previous = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "batch5-content-generation", "persistent-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "SAVE"));
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", root);
                action(root);
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previous);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("Batch5ContentGenerationTests: " + message);
        }

        private sealed class FakeHost : IRuntimeHost, ILegacyDevelopmentModSourceTestHost
        {
            public FakeHost(string gamePath)
            {
                GamePath = gamePath;
                PluginPath = Path.Combine(gamePath, "BepInEx", "plugins");
            }

            public string GamePath { get; }
            public string PluginPath { get; }
            public string HostName => "Batch5ContentGenerationTests";
            public bool IncludeLegacyDevelopmentModSourceForTests => true;
            public void Log(string message) { }
            public void LogWarning(string message) { }
            public void LogError(string message, Exception? exception = null) { }
        }
    }
}
