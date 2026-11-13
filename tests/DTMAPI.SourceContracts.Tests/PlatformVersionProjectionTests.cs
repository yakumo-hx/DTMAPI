using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using DTMAPI.Abstractions;
using DTMAPI.BepInExBootstrap;
using DTMAPI.Core.Runtime;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static class PlatformVersionProjectionTests
    {
        public static void RunAll(string repo)
        {
            RuntimeSourceProjectsVersionAuthority(repo);
            ControlledProjectionsRejectDrift();
        }

        private static void RuntimeSourceProjectsVersionAuthority(string repo)
        {
            XDocument authority = XDocument.Load(Path.Combine(repo, "build/dtmapi-runtime-version.props"));
            VersionDimensions expected = ReadAuthority(authority);
            Equal(DtmApiRuntime.ApiVersion, expected.Release, "Core API version");
            Equal(DtmApiRuntime.BinaryVersion, expected.File, "Core binary version");
            foreach (Assembly assembly in new[]
                     {
                         typeof(IDtmHelper).Assembly, typeof(DtmApiRuntime).Assembly,
                         typeof(BootstrapPlugin).Assembly, typeof(DolocTownGameBridge).Assembly,
                         typeof(ConfigMenuRegistry).Assembly
                     })
            {
                ValidateProjection(expected, new VersionDimensions(
                    assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
                    assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version,
                    assembly.GetName().Version!.ToString()), assembly.GetName().Name!);
            }

        }

        private static void ControlledProjectionsRejectDrift()
        {
            XDocument authority = XDocument.Parse("<Project><PropertyGroup>" +
                "<DtmApiVersionAuthoritySchema>1</DtmApiVersionAuthoritySchema>" +
                "<DtmApiReleaseVersion>2.3.4</DtmApiReleaseVersion>" +
                "<DtmApiBinaryFileVersion>2.3.4.0</DtmApiBinaryFileVersion>" +
                "<DtmApiAssemblyCompatibilityVersion>1.0.0.0</DtmApiAssemblyCompatibilityVersion>" +
                "</PropertyGroup></Project>");
            VersionDimensions expected = ReadAuthority(authority);
            ValidateProjection(expected, new VersionDimensions("2.3.4", "2.3.4.0", "1.0.0.0"), "Fixture");
            Reject(() => ValidateProjection(expected, new VersionDimensions("2.3.3", "2.3.4.0", "1.0.0.0"), "Fixture"), "Fixture release");
            Reject(() => ValidateProjection(expected, new VersionDimensions("2.3.4", "2.3.3.0", "1.0.0.0"), "Fixture"), "Fixture file");
            Reject(() => ValidateProjection(expected, new VersionDimensions("2.3.4", "2.3.4.0", "2.3.4.0"), "Fixture"), "Fixture assembly");
            authority.Descendants("DtmApiVersionAuthoritySchema").Single().Value = "99";
            Reject(() => ReadAuthority(authority), "Authority schema");

        }

        private static VersionDimensions ReadAuthority(XDocument document)
        {
            Equal(document.Descendants("DtmApiVersionAuthoritySchema").Single().Value, "1", "Authority schema");
            string Value(string name) => document.Descendants(name).Single().Value;
            return new VersionDimensions(Value("DtmApiReleaseVersion"), Value("DtmApiBinaryFileVersion"), Value("DtmApiAssemblyCompatibilityVersion"));
        }

        private static void ValidateProjection(VersionDimensions expected, VersionDimensions actual, string label)
        {
            Equal(actual.Release, expected.Release, label + " release");
            Equal(actual.File, expected.File, label + " file");
            Equal(actual.Assembly, expected.Assembly, label + " assembly");
        }

        private sealed record VersionDimensions(string Release, string File, string Assembly);
        private static void Equal(string actual, string expected, string label) => Require(actual == expected, label + ": expected " + expected + ", got " + actual);
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void Reject(Action action, string expected)
        {
            try { action(); }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith(expected, StringComparison.Ordinal)) { return; }
            throw new InvalidOperationException("Negative fixture did not reject " + expected);
        }
    }
}
