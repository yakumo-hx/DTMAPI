using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace DTMAPI.Testing
{
    internal sealed class DtmApiTestSession : IDisposable
    {
        private const string Owner = "DTMAPI.TestSession";
        private const int SchemaVersion = 1;
        private const long RetainedSessionByteLimit = 5L * 1024 * 1024 * 1024;
        private const long FailureReceiptByteLimit = 500L * 1024 * 1024;
        private static readonly TimeSpan RetainedSessionAgeLimit = TimeSpan.FromHours(24);
        private static readonly TimeSpan FailureReceiptAgeLimit = TimeSpan.FromDays(7);
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };
        private static DtmApiTestSession? current;

        private readonly string baseRoot;
        private readonly string suite;
        private readonly string sessionId;
        private readonly DateTimeOffset startedAtUtc;
        private readonly bool keepFailedArtifacts;
        private readonly string? previousTemp;
        private readonly string? previousTmp;
        private readonly string? previousSessionRoot;
        private FileStream? lease;
        private string status = "active";
        private Exception? failure;
        private bool disposed;

        private DtmApiTestSession(string suiteName)
        {
            suite = MakeSafeSegment(suiteName);
            string configuredRoot = Environment.GetEnvironmentVariable("DTMAPI_TEST_TEMP_ROOT") ?? string.Empty;
            baseRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.Combine(Path.GetTempPath(), "DTMAPI-test-sessions")
                : configuredRoot);
            Directory.CreateDirectory(baseRoot);
            Scavenge(baseRoot);

            startedAtUtc = DateTimeOffset.UtcNow;
            sessionId = startedAtUtc.ToString("yyyyMMdd-HHmmss") + "-" + Environment.ProcessId + "-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            RootPath = Path.Combine(baseRoot, suite, sessionId);
            Directory.CreateDirectory(RootPath);
            lease = new FileStream(
                Path.Combine(RootPath, "active.lock"),
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                1,
                FileOptions.WriteThrough);

            keepFailedArtifacts = IsTrue(Environment.GetEnvironmentVariable("DTMAPI_KEEP_FAILED_TEST_TEMP"));
            previousTemp = Environment.GetEnvironmentVariable("TEMP");
            previousTmp = Environment.GetEnvironmentVariable("TMP");
            previousSessionRoot = Environment.GetEnvironmentVariable("DTMAPI_TEST_SESSION_ROOT");
            Environment.SetEnvironmentVariable("TEMP", RootPath);
            Environment.SetEnvironmentVariable("TMP", RootPath);
            Environment.SetEnvironmentVariable("DTMAPI_TEST_SESSION_ROOT", RootPath);

            string effectiveTemp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string expectedTemp = RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!string.Equals(effectiveTemp, expectedTemp, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Path.GetTempPath did not adopt the DTMAPI test session root. Expected '" + expectedTemp + "' but found '" + effectiveTemp + "'.");

            WriteSessionReceipt();
            Console.WriteLine("DTMAPI test session: " + RootPath);
        }

        public static DtmApiTestSession Current => current ?? throw new InvalidOperationException("No DTMAPI test session is active.");

        public string RootPath { get; }

        public static DtmApiTestSession Start(string suiteName)
        {
            if (current != null)
                throw new InvalidOperationException("A DTMAPI test session is already active in this process.");

            var session = new DtmApiTestSession(suiteName);
            current = session;
            return session;
        }

        public void MarkSucceeded()
        {
            status = "succeeded";
            failure = null;
        }

        public void MarkFailed(Exception exception)
        {
            status = "failed";
            failure = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            bool succeeded = string.Equals(status, "succeeded", StringComparison.Ordinal);
            bool retainFullTree = !succeeded && keepFailedArtifacts;
            if (string.Equals(status, "active", StringComparison.Ordinal))
                status = "abandoned-in-process";
            status = succeeded ? "completed-success" : retainFullTree ? "retained-failure" : "completed-failure";

            try
            {
                WriteSessionReceipt(DateTimeOffset.UtcNow);
                if (!succeeded)
                    WriteFailureReceipt(retainFullTree);
            }
            finally
            {
                Environment.SetEnvironmentVariable("TEMP", previousTemp);
                Environment.SetEnvironmentVariable("TMP", previousTmp);
                Environment.SetEnvironmentVariable("DTMAPI_TEST_SESSION_ROOT", previousSessionRoot);
                lease?.Dispose();
                lease = null;
                current = null;
            }

            if (retainFullTree)
            {
                Console.Error.WriteLine("DTMAPI retained failed test session for at most 24 hours: " + RootPath);
                return;
            }

            if (DeleteDirectoryWithRetries(RootPath))
            {
                Console.WriteLine("DTMAPI test session cleaned: " + RootPath);
                return;
            }

            status = "cleanup-pending";
            try
            {
                WriteSessionReceipt(DateTimeOffset.UtcNow);
            }
            catch
            {
            }
            Console.Error.WriteLine("DTMAPI test session cleanup is pending; the next test run or cleanup tool will retry: " + RootPath);
        }

        private void WriteSessionReceipt(DateTimeOffset? completedAtUtc = null)
        {
            var receipt = new
            {
                owner = Owner,
                schemaVersion = SchemaVersion,
                suite,
                sessionId,
                processId = Environment.ProcessId,
                processStartUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime().ToString("o"),
                startedAtUtc = startedAtUtc.ToString("o"),
                completedAtUtc = completedAtUtc?.ToString("o"),
                status,
                keepFailedArtifacts,
                rootPath = RootPath,
                failureType = failure?.GetType().FullName,
                failureMessage = Truncate(failure?.Message, 4096)
            };
            WriteJsonAtomically(Path.Combine(RootPath, "session.json"), receipt);
        }

        private void WriteFailureReceipt(bool retainedFullTree)
        {
            string receiptRoot = Path.Combine(baseRoot, "_failure-receipts", suite);
            Directory.CreateDirectory(receiptRoot);
            var receipt = new
            {
                owner = Owner,
                schemaVersion = SchemaVersion,
                suite,
                sessionId,
                failedAtUtc = DateTimeOffset.UtcNow.ToString("o"),
                retainedFullTree,
                retainedRootPath = retainedFullTree ? RootPath : null,
                failureType = failure?.GetType().FullName ?? "UnknownFailure",
                failureMessage = Truncate(failure?.Message, 4096),
                failureStack = Truncate(failure?.ToString(), 16 * 1024)
            };
            WriteJsonAtomically(Path.Combine(receiptRoot, sessionId + ".json"), receipt);
        }

        private static void Scavenge(string root)
        {
            var deferred = new List<DirectoryInfo>();
            foreach (DirectoryInfo suiteDirectory in EnumerateDirectoriesSafely(new DirectoryInfo(root)))
            {
                if (string.Equals(suiteDirectory.Name, "_failure-receipts", StringComparison.OrdinalIgnoreCase) || IsReparsePoint(suiteDirectory))
                    continue;

                foreach (DirectoryInfo sessionDirectory in EnumerateDirectoriesSafely(suiteDirectory))
                {
                    if (IsReparsePoint(sessionDirectory) || !IsOwnedSessionDirectory(sessionDirectory))
                        continue;
                    if (!CanAcquireLease(sessionDirectory))
                        continue;

                    string sessionStatus = ReadReceiptStatus(sessionDirectory);
                    TimeSpan age = DateTimeOffset.UtcNow - sessionDirectory.LastWriteTimeUtc;
                    bool completed = sessionStatus.StartsWith("completed-", StringComparison.Ordinal) || string.Equals(sessionStatus, "cleanup-pending", StringComparison.Ordinal);
                    if (completed || age >= RetainedSessionAgeLimit)
                        DeleteDirectoryWithRetries(sessionDirectory.FullName);
                    else
                        deferred.Add(sessionDirectory);
                }
            }

            long deferredBytes = deferred.Sum(GetDirectoryLengthSafely);
            foreach (DirectoryInfo sessionDirectory in deferred.OrderBy(item => item.LastWriteTimeUtc))
            {
                if (deferredBytes <= RetainedSessionByteLimit)
                    break;
                long length = GetDirectoryLengthSafely(sessionDirectory);
                if (DeleteDirectoryWithRetries(sessionDirectory.FullName))
                    deferredBytes = Math.Max(0, deferredBytes - length);
            }

            ScavengeFailureReceipts(Path.Combine(root, "_failure-receipts"));
        }

        private static void ScavengeFailureReceipts(string root)
        {
            if (!Directory.Exists(root))
                return;

            var retained = new List<FileInfo>();
            foreach (FileInfo receipt in EnumerateFilesSafely(new DirectoryInfo(root)))
            {
                if (DateTimeOffset.UtcNow - receipt.LastWriteTimeUtc >= FailureReceiptAgeLimit)
                {
                    TryDeleteFile(receipt.FullName);
                    continue;
                }
                retained.Add(receipt);
            }

            long retainedBytes = retained.Sum(item => item.Length);
            foreach (FileInfo receipt in retained.OrderBy(item => item.LastWriteTimeUtc))
            {
                if (retainedBytes <= FailureReceiptByteLimit)
                    break;
                if (TryDeleteFile(receipt.FullName))
                    retainedBytes = Math.Max(0, retainedBytes - receipt.Length);
            }
        }

        private static bool IsOwnedSessionDirectory(DirectoryInfo directory)
        {
            string receiptPath = Path.Combine(directory.FullName, "session.json");
            if (!File.Exists(receiptPath))
                return false;
            try
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(receiptPath));
                JsonElement root = document.RootElement;
                return root.TryGetProperty("owner", out JsonElement owner) &&
                    string.Equals(owner.GetString(), Owner, StringComparison.Ordinal) &&
                    root.TryGetProperty("schemaVersion", out JsonElement schema) &&
                    schema.GetInt32() == SchemaVersion;
            }
            catch
            {
                return false;
            }
        }

        private static string ReadReceiptStatus(DirectoryInfo directory)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory.FullName, "session.json")));
                return document.RootElement.TryGetProperty("status", out JsonElement statusElement)
                    ? statusElement.GetString() ?? string.Empty
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool CanAcquireLease(DirectoryInfo directory)
        {
            string leasePath = Path.Combine(directory.FullName, "active.lock");
            if (!File.Exists(leasePath))
                return true;
            try
            {
                using var stream = new FileStream(leasePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static IEnumerable<DirectoryInfo> EnumerateDirectoriesSafely(DirectoryInfo directory)
        {
            try
            {
                return directory.Exists ? directory.EnumerateDirectories().ToArray() : Array.Empty<DirectoryInfo>();
            }
            catch
            {
                return Array.Empty<DirectoryInfo>();
            }
        }

        private static IEnumerable<FileInfo> EnumerateFilesSafely(DirectoryInfo directory)
        {
            try
            {
                return directory.Exists ? directory.EnumerateFiles("*.json", SearchOption.AllDirectories).ToArray() : Array.Empty<FileInfo>();
            }
            catch
            {
                return Array.Empty<FileInfo>();
            }
        }

        private static long GetDirectoryLengthSafely(DirectoryInfo directory)
        {
            try
            {
                return directory.Exists
                    ? directory.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length)
                    : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static bool DeleteDirectoryWithRetries(string path)
        {
            for (int attempt = 0; attempt < 4; attempt++)
            {
                try
                {
                    if (!Directory.Exists(path))
                        return true;
                    Directory.Delete(path, recursive: true);
                    return !Directory.Exists(path);
                }
                catch (UnauthorizedAccessException)
                {
                    NormalizeFileAttributes(path);
                }
                catch (IOException)
                {
                }
                Thread.Sleep(100 * (attempt + 1));
            }
            return !Directory.Exists(path);
        }

        private static void NormalizeFileAttributes(string path)
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);
            }
            catch
            {
            }
        }

        private static bool TryDeleteFile(string path)
        {
            try
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
                return !File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsReparsePoint(FileSystemInfo item) => (item.Attributes & FileAttributes.ReparsePoint) != 0;

        private static bool IsTrue(string? value) =>
            string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

        private static string MakeSafeSegment(string value)
        {
            string safe = new string((value ?? string.Empty)
                .Select(character => char.IsLetterOrDigit(character) || character == '.' || character == '-' || character == '_' ? character : '-')
                .ToArray())
                .Trim('-');
            return safe.Length == 0 ? "tests" : safe;
        }

        private static string? Truncate(string? value, int maxLength) =>
            string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value.Substring(0, maxLength);

        private static void WriteJsonAtomically(string path, object value)
        {
            string tempPath = path + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("JSON receipt has no parent directory."));
            File.WriteAllText(tempPath, JsonSerializer.Serialize(value, JsonOptions));
            File.Move(tempPath, path, overwrite: true);
        }
    }
}
