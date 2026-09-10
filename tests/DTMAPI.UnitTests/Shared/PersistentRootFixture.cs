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

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static string? UseTempPersistentRoot()
        {
            string? previousRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            string root = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "SAVE"));
            Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", root);
            return previousRoot;
        }

        private static void RestorePersistentRoot(string? previousRoot)
        {
            Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousRoot);
        }
    }
}
