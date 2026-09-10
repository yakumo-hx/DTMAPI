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
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static int CountCoreOwnerRootsForTest(DtmApiRuntime runtime, string ownerId)
        {
            MethodInfo count = typeof(DtmApiRuntime).GetMethod("CountCoreOwnerRoots", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("DtmApiRuntime.CountCoreOwnerRoots should expose authoritative Core owner-root counting.");
            return (int)(count.Invoke(runtime, new object[] { ownerId }) ?? 0);
        }
    }
}
