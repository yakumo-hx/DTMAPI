using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DTMAPI.Internal.Authoring
{
    // Deliberately separate from Runtime minimum/game/SDK coordinate comparison.
    // Numeric components are strings: SemVer imposes no Int32/Int64 size ceiling.
    internal sealed class PackageSemanticVersion : IComparable<PackageSemanticVersion>
    {
        private static readonly Regex Syntax = new Regex(
            @"\A(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?\z",
            RegexOptions.CultureInvariant);
        private readonly string[] core;
        private readonly string[] prerelease;
        private PackageSemanticVersion(string text, string[] core, string[] prerelease)
        { Text = text; this.core = core; this.prerelease = prerelease; }
        public string Text { get; }
        public bool IsPrerelease => prerelease.Length != 0;

        public static PackageSemanticVersion Parse(string text)
        {
            if (text == null || text.Length > 4096) throw new InvalidDataException("dependency-version-invalid: expected complete SemVer 2.");
            Match match = Syntax.Match(text);
            if (!match.Success) throw new InvalidDataException("dependency-version-invalid: " + text);
            string[] pre = match.Groups[4].Success ? match.Groups[4].Value.Split('.') : Array.Empty<string>();
            if (pre.Any(part => Numeric(part) && part.Length > 1 && part[0] == '0'))
                throw new InvalidDataException("dependency-version-invalid: numeric prerelease has a leading zero: " + text);
            return new PackageSemanticVersion(text, new[] { match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value }, pre);
        }

        public int CompareTo(PackageSemanticVersion? other)
        {
            if (other == null) return 1;
            for (int i = 0; i < 3; i++)
            { int result = CompareNumeric(core[i], other.core[i]); if (result != 0) return result; }
            if (!IsPrerelease || !other.IsPrerelease) return IsPrerelease == other.IsPrerelease ? 0 : IsPrerelease ? -1 : 1;
            for (int i = 0; i < Math.Min(prerelease.Length, other.prerelease.Length); i++)
            {
                string left = prerelease[i], right = other.prerelease[i];
                bool ln = Numeric(left), rn = Numeric(right);
                int result = ln && rn ? CompareNumeric(left, right) : ln != rn ? ln ? -1 : 1 : string.CompareOrdinal(left, right);
                if (result != 0) return result;
            }
            return prerelease.Length.CompareTo(other.prerelease.Length);
        }

        private static bool Numeric(string value) => value.All(c => c >= '0' && c <= '9');
        private static int CompareNumeric(string left, string right) => left.Length == right.Length
            ? string.CompareOrdinal(left, right) : left.Length.CompareTo(right.Length);
    }

    internal sealed class PackageVersionRange
    {
        public PackageVersionRange(string minimumInclusive, string? maximumExclusive, bool includePrerelease)
        {
            Minimum = PackageSemanticVersion.Parse(minimumInclusive);
            Maximum = maximumExclusive == null ? null : PackageSemanticVersion.Parse(maximumExclusive);
            if (Maximum != null && Minimum.CompareTo(Maximum) >= 0)
                throw new InvalidDataException("dependency-range-invalid: maximumExclusive must exceed minimumInclusive.");
            IncludePrerelease = includePrerelease;
        }
        public PackageSemanticVersion Minimum { get; }
        public PackageSemanticVersion? Maximum { get; }
        public bool IncludePrerelease { get; }
        public bool Contains(PackageSemanticVersion version) => (!version.IsPrerelease || IncludePrerelease)
            && version.CompareTo(Minimum) >= 0 && (Maximum == null || version.CompareTo(Maximum) < 0);
    }
}
