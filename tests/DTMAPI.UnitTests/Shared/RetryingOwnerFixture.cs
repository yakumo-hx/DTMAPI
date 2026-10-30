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

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private sealed class RetryingOwnerCleanupParticipant : IModOwnerCleanupParticipant
        {
            private readonly bool failFirstCleanup;
            private readonly List<string>? callOrder;

            public RetryingOwnerCleanupParticipant(string participantId, bool failFirstCleanup, List<string>? callOrder = null)
            {
                ParticipantId = participantId;
                this.failFirstCleanup = failFirstCleanup;
                this.callOrder = callOrder;
                RemainingResources = 1;
            }

            public string ParticipantId { get; }

            public int CallCount { get; private set; }

            public int RemainingResources { get; private set; }

            public ModOwnerCleanupParticipantResult RemoveOwner(string ownerId, ModOwnerCleanupReason reason)
            {
                CallCount++;
                callOrder?.Add(ParticipantId);
                if (failFirstCleanup && CallCount == 1)
                    return new ModOwnerCleanupParticipantResult(0, RemainingResources, "Injected first-attempt retained root.");

                int removed = RemainingResources;
                RemainingResources = 0;
                return new ModOwnerCleanupParticipantResult(removed, RemainingResources, "Removed retryable owner root.");
            }
        }

        private static string InvokeOwnerDeactivation(DtmApiRuntime runtime, string ownerId, ModOwnerCleanupReason reason, bool shutdown)
        {
            MethodInfo cleanup = typeof(DtmApiRuntime).GetMethod("DeactivateOwner", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("DeactivateOwner should exist.");
            return (string)(cleanup.Invoke(runtime, new object[] { ownerId, reason, shutdown, string.Empty }) ?? string.Empty);
        }

        private static IModRegistry GetModRegistry(DtmApiRuntime runtime)
        {
            PropertyInfo? registryProperty = typeof(DtmApiRuntime).GetProperty("ModRegistry", BindingFlags.Instance | BindingFlags.NonPublic);
            return (IModRegistry)(registryProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime mod registry should exist."));
        }
    }
}
