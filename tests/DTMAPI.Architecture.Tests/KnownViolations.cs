using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace DTMAPI.Architecture.Tests
{
    // known-violations.txt 每行是"检查名|内容"，# 开头的是注释。这份清单只能删，不能加。
    internal static class KnownViolations
    {
        private const string FileName = "known-violations.txt";

        internal static void AssertSame(string check, ISet<string> found)
        {
            HashSet<string> known = Read(check);
            List<string> added = found.Where(item => !known.Contains(item))
                .OrderBy(item => item, StringComparer.Ordinal).ToList();
            List<string> resolved = known.Where(item => !found.Contains(item))
                .OrderBy(item => item, StringComparer.Ordinal).ToList();
            if (added.Count == 0 && resolved.Count == 0)
                return;

            var message = new StringBuilder();
            if (added.Count > 0)
            {
                message.AppendLine("新出现的问题（要改代码，不要加进清单）：");
                foreach (string item in added)
                    message.AppendLine("  " + item);
            }
            if (resolved.Count > 0)
            {
                message.AppendLine("已经修好的问题（请从 " + FileName + " 删掉这几行）：");
                foreach (string item in resolved)
                    message.AppendLine("  " + check + "|" + item);
            }
            Assert.Fail(message.ToString());
        }

        private static HashSet<string> Read(string check)
        {
            var items = new HashSet<string>(StringComparer.Ordinal);
            foreach (string rawLine in File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, FileName)))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                int separator = line.IndexOf('|');
                if (separator > 0 && string.Equals(line.Substring(0, separator), check, StringComparison.Ordinal))
                    items.Add(line.Substring(separator + 1));
            }
            return items;
        }
    }
}
