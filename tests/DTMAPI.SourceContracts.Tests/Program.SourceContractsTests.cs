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

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void PreviewVersionMetadataIsConsistent()
        {
            PlatformVersionProjectionTests.RunAll(FindRepositoryRoot());
        }

        private static void PublicApiStatusMetadataMatchesMatrix()
        {
            foreach ((Type Type, string Since) row in new[]
                     {
                         (typeof(IModRegistry), "0.1.0"),
                         (typeof(ITranslationHelper), "0.1.12"),
                         (typeof(IDtmConfigMenuApi), "0.1.0")
                     })
            {
                DtmApiStatusAttribute status = row.Type.GetCustomAttribute<DtmApiStatusAttribute>()
                    ?? throw new InvalidOperationException(row.Type.Name + " must publish explicit stability metadata.");
                Assert(status.Status == DtmApiStatus.StableCandidate && status.Since == row.Since,
                    row.Type.Name + " must mechanically match the public API matrix StableCandidate status without rewriting its historical Since value.");
            }

            DtmApiStatusAttribute keybindDefaults = typeof(IDtmConfigMenuKeybindDefaultsApi).GetCustomAttribute<DtmApiStatusAttribute>()
                ?? throw new InvalidOperationException(nameof(IDtmConfigMenuKeybindDefaultsApi) + " must publish explicit stability metadata.");
            Assert(keybindDefaults.Status == DtmApiStatus.Experimental,
                "The optional config-menu keybind Reset capability must remain Experimental when the primary config-menu surface becomes StableCandidate.");
        }


    }
}
