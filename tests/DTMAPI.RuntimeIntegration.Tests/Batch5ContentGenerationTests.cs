using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            RunContentQuery();
        }

        internal static void RunContentQuery()
        {
            ContentQuerySkipsInactiveOfficialSourcesAndIsolatesCompatibleInputs();
            ContentQueryBuildsOnceOnConcurrentFirstDemand();
            ContentQueryDeferredInputsCannotReviveClosedOwners();
            ContentQueryRejectRetainsAtomicLastGoodAndStableModSnapshots();
            ContentQueryRejectsAnUnavailableStillActiveOwnerRoot();
            ContentQueryAtomicBarrierPublishesReceiptWithVisibleSnapshot();
            ContentQueryPreCommitFailureRetainsLastGoodAndRequeues();
            ContentQueryStaleCompletionDoesNotPublishPreparedSnapshot();
            ContentQueryPostCommitFailureDoesNotRequeuePublishedGeneration();
            ContentQueryOwnerCleanupAndSuccessorGenerationStayConsistent();
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
            generations.MarkDirty("SecondContentDomain", new[] { "Owner.A" }, "startup");
            Assert(generations.TryGetDirty("SecondContentDomain", out ContentRefreshDirtyBatch failed), "The failing consumer should acquire the original generation.");
            generations.MarkDirty("SecondContentDomain", new[] { "Owner.B" }, "change-during-consumer");

            Assert(generations.AbandonAndRequeue(failed, "injected consumer failure"), "An unexpected consumer failure should close the in-flight generation and requeue its work.");
            Assert(generations.TryGetDirty("SecondContentDomain", out ContentRefreshDirtyBatch retry), "The domain must remain consumable after an unexpected consumer failure.");
            Assert(retry.Generation > failed.Generation && retry.OwnerIds.SequenceEqual(new[] { "Owner.A", "Owner.B" }, StringComparer.OrdinalIgnoreCase), "The successor must have a new generation and preserve original plus in-flight owner dirtiness.");
            Assert(generations.GetReceipts().Last().Status == ContentRefreshCompletionStatus.Rejected, "The abandoned generation should leave a bounded rejection receipt.");
            Assert(generations.Complete(retry, new[] { new ContentRefreshCompletion("all", ContentRefreshCompletionStatus.Committed, 0, 1, 1, "retry committed") }), "The successor should complete normally.");
            Assert(!generations.IsDirty("SecondContentDomain"), "A successful successor must return the domain to clean state.");
        }

        private static void CompletionDiagnosticsRemainBoundedAcrossManyOwners()
        {
            var generations = new ContentRefreshGenerationService();
            generations.MarkDirty("SecondContentDomain", new[] { "all" }, "many-owner-refresh");
            Assert(generations.TryGetDirty("SecondContentDomain", out ContentRefreshDirtyBatch batch), "The many-owner generation should become consumable.");
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

            generations.MarkDirty("SecondContentDomain", new[] { ordinaryOwner }, "ordinary-owner");
            Assert(generations.TryGetDirty("SecondContentDomain", out ContentRefreshDirtyBatch ordinaryBatch), "An ordinary owner should produce a consumable generation.");
            Assert(ordinaryBatch.OwnerIds.Count == 1 && ordinaryBatch.OwnerIds[0] == ordinaryOwner, "An owner that fits the authoritative bound must retain its exact identity in the dirty batch.");
            Assert(generations.Complete(
                ordinaryBatch,
                new[] { new ContentRefreshCompletion(ordinaryOwner, ContentRefreshCompletionStatus.Committed, 0, 1, 1, "ordinary-owner") }),
                "The ordinary-owner generation should complete.");

            generations.MarkDirty("SecondContentDomain", new[] { overlongOwner }, "overlong-owner");
            Assert(generations.TryGetDirty("SecondContentDomain", out ContentRefreshDirtyBatch overlongBatch), "An overlong owner should still produce a consumable generation.");
            Assert(overlongBatch.OwnerIds.Count == 1 && overlongBatch.OwnerIds[0] == "all", "An owner beyond the authoritative bound must conservatively select all owners instead of becoming a display hash.");
            Assert(generations.Complete(
                overlongBatch,
                new[] { new ContentRefreshCompletion(overlongOwner, ContentRefreshCompletionStatus.Committed, 1, 2, 2, "overlong-owner") }),
                "The conservative overlong-owner generation should complete.");
            ContentRefreshReceipt receipt = generations.GetReceipts().Last();
            Assert(receipt.OwnerId.Length <= 128 && receipt.OwnerId != overlongOwner, "Receipt/display owner identities must remain bounded independently of authoritative dirty selection.");
        }

        private static void ContentQueryBuildsOnceOnConcurrentFirstDemand()
        {
            WithIsolatedPersistentRoot(persistentRoot =>
            {
                string gameDir = NewTempGameDir();
                string itemPath = WriteOfficialContentPack(persistentRoot, "Batch5ContentLazy", "DTMAPI.Tests.ContentLazy", "content_lazy", "Lazy Item");
                var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                using (var unreadableUntilQuery = new FileStream(itemPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    runtime.Start();
                    runtime.NotifyWorkshopModListChanged();
                    _ = runtime.CreateSnapshot();
                    Assert(runtime.Content.CandidateBuildCountForTest == 0 && runtime.Content.IndexedItemCount == 0 && runtime.Content.IndexedItemSourceCount == 0,
                        "Startup, source refresh, snapshots and diagnostic counts must not scan an unused query index.");
                    Assert(runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.ContentQuery) &&
                        !runtime.ContentRefreshGenerations.GetReceipts().Any(receipt => receipt.Domain == ContentRefreshDomains.ContentQuery),
                        "Unused query inputs should retain pending dirtiness without inventing a terminal publication receipt.");
                }

                Task<IContentItemInfo?>[] readers = Enumerable.Range(0, 8)
                    .Select(_ => Task.Run(() => runtime.GetIndexedContentItem("content_lazy"))).ToArray();
                Task.WaitAll(readers);
                Assert(readers.All(reader => reader.Result != null) && runtime.Content.CandidateBuildCountForTest == 1,
                    "Concurrent first queries must synchronously see one complete publication without Runtime-thread callbacks or duplicate builds.");
                _ = runtime.Content.FindAssets("json");
                _ = runtime.Content.GetKnownContentTypes();
                _ = runtime.Content.GetAllIndexedItems();
                Assert(runtime.Content.CandidateBuildCountForTest == 1 && !runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.ContentQuery),
                    "Steady queries must reuse the terminal publication.");
                runtime.NotifyRuntimeShutdown("ContentQuery first demand test");
            });
        }

        private static void ContentQueryDeferredInputsCannotReviveClosedOwners()
        {
            foreach (bool shutdown in new[] { false, true })
            {
                WithIsolatedPersistentRoot(persistentRoot =>
                {
                    const string folder = "Batch5ContentDeferredClose";
                    const string ownerId = "DTMAPI.Tests.ContentDeferredClose";
                    string gameDir = NewTempGameDir();
                    WriteOfficialContentPack(persistentRoot, folder, ownerId, "content_deferred_close", "Deferred Close");
                    var runtime = new DtmApiRuntime(new FakeHost(gameDir));
                    runtime.Start();
                    IContentQueryHelper retained = runtime.Content;
                    Assert(runtime.Content.CandidateBuildCountForTest == 0, "Retaining a helper alone must not demand an index.");
                    if (shutdown)
                        runtime.NotifyRuntimeShutdown("deferred content owner close");
                    else
                    {
                        WriteOfficialModInfos(persistentRoot, "Local." + folder, enabled: false);
                        runtime.NotifyWorkshopModListChanged();
                    }
                    Assert(retained.GetIndexedItem("content_deferred_close") == null && !retained.FindAssets("json").Any(asset => asset.SourceModId == ownerId) && runtime.Content.CountOwnerResources(ownerId) == 0,
                        "A stale helper's first query after disable or shutdown must not revive pending owner inputs.");
                    if (!shutdown)
                        runtime.NotifyRuntimeShutdown("deferred content owner disable test");
                });
            }
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
                Directory.Move(ownerRoot + ".temporarily-unavailable", ownerRoot);
                ContentQueryRebuildResult late = service.RebuildCandidate(new[] { activeOwner }, invalidBatch.Generation + 1, "prepared before owner close");
                Assert(late.Success && service.RemoveOwner(manifest.UniqueID) > 0, "The published owner must remain removable while another candidate is prepared.");
                bool staleRejected = false;
                try { late.PublishPreparedSnapshot!(); }
                catch (InvalidOperationException) { staleRejected = true; }
                Assert(staleRejected && service.CountOwnerResources(manifest.UniqueID) == 0 && service.FindAssets("txt").Count == 0,
                    "A late candidate must not republish resources belonging to the owner that was just closed.");
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
                Assert(runtime.GetIndexedContentItem("content_barrier_a") != null, "Initial query must establish generation A before testing its successor.");
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
                Assert(runtime.GetIndexedContentItem("content_precommit_a") != null, "Initial query must establish generation A before testing its successor.");
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
                Assert(runtime.GetIndexedContentItem("content_stale_a") != null, "Initial query must establish generation A before testing its successor.");
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
                Assert(runtime.GetIndexedContentItem("content_postcommit_a") != null, "Initial query must establish generation A before testing its successor.");
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
                Assert(runtime.GetIndexedContentItem("content_owner_cleanup") != null, "Initial query must establish generation A before testing its successor.");
                long generationA = runtime.Content.LastGoodGeneration;
                Assert(runtime.Content.CountOwnerResources(ownerId) > 0, "The active ContentQuery owner should have roots before cleanup.");

                WriteOfficialModInfos(persistentRoot, "Local." + folder, enabled: false);
                runtime.NotifyWorkshopModListChanged();

                ContentRefreshReceipt receipt = runtime.ContentRefreshGenerations.GetReceipts().Last(item => item.Domain == ContentRefreshDomains.ContentQuery);
                Assert(runtime.Content.CountOwnerResources(ownerId) == 0 && runtime.GetIndexedContentItem("content_owner_cleanup") == null, "Owner cleanup and its source-driven successor must leave no visible ContentQuery owner roots.");
                Assert(runtime.Content.LastGoodGeneration > generationA && receipt.Status == ContentRefreshCompletionStatus.Committed && receipt.CurrentGeneration == runtime.Content.LastGoodGeneration, "The owner-cleanup successor must advance one authoritative ContentQuery generation whose terminal receipt names the visible snapshot.");
            });
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
