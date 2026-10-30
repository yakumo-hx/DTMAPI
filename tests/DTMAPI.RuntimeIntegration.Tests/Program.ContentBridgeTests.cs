#pragma warning disable CS0618 // Frozen compatibility contracts are intentionally exercised.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void AuthorSessionReloadRequiresRestartWithoutReadingContent()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string gameDir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                MethodInfo reload = typeof(DolocTownGameBridge).GetMethod("ReloadSelectedAuthorContent", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("Author-session reload classifier should exist.");
                string root = Path.Combine(gameDir, "author-reload");
                Directory.CreateDirectory(root);
                DiscoveredMod content = CreateAuthorReloadCandidate(root, "DTMAPI.Tests.AuthorContent", "ContentPack", string.Empty);
                // The active manifest is already owned by Runtime. Restart-only replies need no disk revalidation.
                using var manifest = new FileStream(content.ManifestPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                using var metadata = new FileStream(Path.Combine(root, "info.json"), FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                AuthorSessionOperationResult result = InvokeAuthorReloadClassifier(bridge, reload, content);
                Assert(result.Status == "restart-required" && result.Code == "content-restart-required", "Content requires restart even while its on-disk manifest and metadata are being edited.");
                AuthorSessionOperationResult code = InvokeAuthorReloadClassifier(bridge, reload,
                    CreateAuthorReloadCandidate(root, "DTMAPI.Tests.AuthorCode", "CodeMod", "mod.dll"));
                Assert(code.Status == "restart-required" && code.Code == "codemod-restart-required", "CodeMod assemblies remain process-lifetime and restart-only.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static DiscoveredMod CreateAuthorReloadCandidate(string root, string uniqueId, string type, string entryDll)
        {
            var manifest = new ManifestModel
            {
                Name = uniqueId,
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = uniqueId,
                Type = type,
                EntryDll = entryDll,
                MinimumDTMApiVersion = "0.7.5"
            };
            string packagedManifest = Path.Combine(root, "Content", "DTMAPI", "manifest.json");
            string manifestPath = File.Exists(packagedManifest) ? packagedManifest : Path.Combine(root, "manifest.json");
            return new DiscoveredMod(manifest, root, manifestPath, "LocalDevelopment", null, true, false, string.Empty, false, "unit-test");
        }

        private static AuthorSessionOperationResult InvokeAuthorReloadClassifier(
            DolocTownGameBridge bridge,
            MethodInfo reload,
            DiscoveredMod candidate)
        {
            return (AuthorSessionOperationResult)(reload.Invoke(bridge, new object[] { candidate })
                ?? throw new InvalidOperationException("Author-session reload classifier returned null."));
        }

    }
}
