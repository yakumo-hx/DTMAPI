using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DTMAPI.MultiPlatformInstaller;

internal enum TransactionInspectionKind
{
    Clean,
    RecoverableInstallReceipt,
    RecoverableUninstallReceipt,
    RecoverableGenesis,
    SterileNoReceipt,
    UnsafeNoReceipt,
    InvalidReceipt,
    ForeignEngineReceipt,
    OrphanState
}

internal sealed class TransactionInspection
{
    public string Root { get; init; } = string.Empty;
    public TransactionInspectionKind Kind { get; init; }
    public TransactionReceiptV1? Receipt { get; init; }
    public string Detail { get; init; } = string.Empty;
}

internal sealed class UninstallExecutionResult
{
    public UninstallStateV1 State { get; init; } = new();
    public string BackupRoot { get; init; } = string.Empty;
    public string StateReceiptWarning { get; init; } = string.Empty;
}

internal sealed class RuntimeTransaction
{
    internal const string CanonicalPrefix = ".dtmapi-runtime-install-";
    internal const string PreviewPrefix = ".dtmapi-multiplatform-install-";
    internal const string LegacyStatePrefix = ".runtime-install-transaction-";
    private const string GenesisPrefix = ".dtmapi-multiplatform-genesis-";
    private const string InstallEngine = "DTMAPI.MultiPlatform/install";
    private const string BepInExEngine = "DTMAPI.MultiPlatform/bepinex";
    private const string UninstallEngine = "DTMAPI.MultiPlatform/uninstall";
    private static readonly Regex StampPattern = new(
        "^[0-9]{8}-[0-9]{6}-[0-9]{3}-[0-9a-fA-F]{8}$",
        RegexOptions.CultureInvariant);
    private static readonly Regex SterileReceiptResiduePattern = new(
        "^transaction\\.json\\.(?:tmp|bak)-[0-9a-fA-F]{32}$",
        RegexOptions.CultureInvariant);
    private readonly string _gameDir;

    public RuntimeTransaction(string gameDir)
    {
        _gameDir = FileSystemSafety.NormalizeRoot(gameDir);
    }

    public IReadOnlyList<TransactionInspection> InspectAll()
    {
        List<TransactionInspection> result = new();
        HashSet<string> canonicalStamps = new(StringComparer.OrdinalIgnoreCase);

        foreach (string root in Directory.EnumerateFileSystemEntries(_gameDir, "*", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(root);
            if (name.StartsWith(CanonicalPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string stamp = name[CanonicalPrefix.Length..];
                canonicalStamps.Add(stamp);
                result.Add(Directory.Exists(root)
                    ? InspectTransactionRoot(root, stamp, isPreviewPrefix: false)
                    : InvalidEntry(root, "Canonical transaction prefix is occupied by a non-directory entry."));
            }
            else if (name.StartsWith(PreviewPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string stamp = name[PreviewPrefix.Length..];
                result.Add(Directory.Exists(root)
                    ? InspectTransactionRoot(root, stamp, isPreviewPrefix: true)
                    : InvalidEntry(root, "Preview transaction prefix is occupied by a non-directory entry."));
            }
            else if (name.StartsWith(GenesisPrefix, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(Directory.Exists(root)
                    ? InspectGenesis(root)
                    : InvalidEntry(root, "Transaction genesis prefix is occupied by a non-directory entry."));
            }
        }

        string stateParent = FileSystemSafety.CombineUnder(_gameDir, "DTMAPI");
        if (!Directory.Exists(stateParent))
            return result;

        try
        {
            FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, stateParent);
            foreach (string stateRoot in Directory.EnumerateFileSystemEntries(stateParent, LegacyStatePrefix + "*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(stateRoot);
                string stamp = name[LegacyStatePrefix.Length..];
                if (canonicalStamps.Contains(stamp))
                    continue;
                result.Add(Directory.Exists(stateRoot)
                    ? InspectOrphanState(stateRoot, stamp)
                    : InvalidEntry(stateRoot, "Legacy transaction-state prefix is occupied by a non-directory entry."));
            }
        }
        catch (Exception ex)
        {
            result.Add(new TransactionInspection
            {
                Root = stateParent,
                Kind = TransactionInspectionKind.InvalidReceipt,
                Detail = "Legacy transaction-state roots could not be inspected safely: " + ex.Message
            });
        }

        return result.OrderBy(item => item.Root, PathComparer()).ToList();
    }

    public void PrepareForInstall() => RecoverForMutation();

    public void AssertSafeForUninstall() => RecoverForMutation();

    private void RecoverForMutation()
    {
        IReadOnlyList<TransactionInspection> inspections = InspectAll();
        TransactionInspection? blocker = inspections.FirstOrDefault(inspection => inspection.Kind is not (
            TransactionInspectionKind.Clean or
            TransactionInspectionKind.SterileNoReceipt or
            TransactionInspectionKind.RecoverableGenesis or
            TransactionInspectionKind.RecoverableInstallReceipt or
            TransactionInspectionKind.RecoverableUninstallReceipt));
        if (blocker is not null)
            throw Blocked(blocker);

        List<TransactionInspection> actionable = inspections.Where(inspection => inspection.Kind is
            TransactionInspectionKind.RecoverableGenesis or
            TransactionInspectionKind.RecoverableInstallReceipt or
            TransactionInspectionKind.RecoverableUninstallReceipt).ToList();
        if (actionable.Count > 1)
            throw new InstallerException(InstallerCodes.TransactionBlocked, 3,
                "检测到多个需要恢复的安装事务；无法证明处理顺序，操作在修改文件前停止。",
                "Multiple recoverable installer transactions exist and their safe order cannot be proven. The action stopped before mutation.",
                string.Join(" | ", actionable.Select(item => item.Root + ":" + item.Kind)));

        foreach (TransactionInspection inspection in inspections)
        {
            switch (inspection.Kind)
            {
                case TransactionInspectionKind.SterileNoReceipt:
                    RevalidateAndDeleteSterile(inspection.Root);
                    break;
                case TransactionInspectionKind.RecoverableGenesis:
                    RevalidateAndDeleteGenesis(inspection);
                    break;
                case TransactionInspectionKind.RecoverableInstallReceipt:
                    if (inspection.Receipt is null)
                        throw Blocked(inspection);
                    RecoverInstall(inspection.Root, inspection.Receipt);
                    break;
                case TransactionInspectionKind.RecoverableUninstallReceipt:
                    if (inspection.Receipt is null)
                        throw Blocked(inspection);
                    RecoverUninstall(inspection.Root, inspection.Receipt);
                    break;
                case TransactionInspectionKind.Clean:
                    break;
                default:
                    throw Blocked(inspection);
            }
        }
    }

    private static TransactionInspection InvalidEntry(string path, string detail) => new()
    {
        Root = path,
        Kind = TransactionInspectionKind.InvalidReceipt,
        Detail = detail
    };

    public void Execute(IReadOnlyList<InstallFile> files, InstallStateV1? installState, string operationLabel)
    {
        if (files.Count == 0)
            return;

        string engine = operationLabel switch
        {
            "install" or "runtime" => InstallEngine,
            "bepinex" => BepInExEngine,
            _ => throw new InvalidDataException("Unsupported transaction operation label: " + operationLabel)
        };
        string transactionId = CreateStamp();
        string transactionRoot = FileSystemSafety.CombineUnder(_gameDir, CanonicalPrefix + transactionId);
        string receiptPath = Path.Combine(transactionRoot, "transaction.json");
        List<InstallFile> ordered = files.OrderBy(GetInstallOrder)
            .ThenBy(file => file.TargetRelativePath, StringComparer.Ordinal)
            .ToList();
        TransactionReceiptV1 receipt = new()
        {
            TransactionId = transactionId,
            CreatedAtUtc = DateTime.UtcNow.ToString("O"),
            GameDir = _gameDir,
            Engine = engine,
            Phase = "Prepared"
        };
        byte[]? installStateBytes = installState is null
            ? null
            : Encoding.UTF8.GetBytes(JsonStore.Serialize(installState, InstallerJsonContext.Default.InstallStateV1));
        string installStateSha256 = installStateBytes is null ? string.Empty : FileSystemSafety.Sha256Bytes(installStateBytes);
        int operationCount = ordered.Count + (installStateBytes is null ? 0 : 1);

        for (int index = 0; index < operationCount; index++)
        {
            string targetRelative = index < ordered.Count
                ? FileSystemSafety.NormalizeRelativePath(ordered[index].TargetRelativePath)
                : "DTMAPI/install-state.json";
            string target = FileSystemSafety.CombineUnder(_gameDir, targetRelative);
            if (Directory.Exists(target))
                throw new InstallerException(InstallerCodes.LocalAccess, 3,
                    "目标文件位置被同名目录占用，安装已停止。",
                    "A target file path is occupied by a directory; installation stopped.", target);
            receipt.Operations.Add(new TransactionOperation
            {
                Ordinal = index,
                TargetRelativePath = targetRelative,
                CandidateRelativePath = "candidate/" + index.ToString("D4") + ".bin",
                BackupRelativePath = "backup/" + index.ToString("D4") + ".bin",
                CandidateSha256 = index < ordered.Count ? ordered[index].Sha256 : installStateSha256,
                CandidateLength = index < ordered.Count ? ordered[index].Length : installStateBytes!.LongLength,
                HadOriginal = File.Exists(target),
                State = "Pending"
            });
        }

        try
        {
            CreateVisibleTransaction(transactionRoot, receipt);
        }
        catch (Exception ex)
        {
            throw new InstallerException(InstallerCodes.TransactionGenesis, 3,
                "无法建立第一份安装恢复凭据；游戏文件尚未修改。",
                "The first recovery receipt could not be established; game files were not changed.", ex.Message, ex);
        }

        try
        {
            string candidateRoot = Path.Combine(transactionRoot, "candidate");
            string backupRoot = Path.Combine(transactionRoot, "backup");
            FileSystemSafety.EnsureDirectoryUnder(transactionRoot, candidateRoot);
            FileSystemSafety.EnsureDirectoryUnder(transactionRoot, backupRoot);

            for (int index = 0; index < ordered.Count; index++)
            {
                TransactionOperation operation = receipt.Operations[index];
                string destination = FileSystemSafety.CombineUnder(transactionRoot, operation.CandidateRelativePath);
                FileSystemSafety.CopyStableFile(ordered[index].SourcePath, destination);
                if (!FileSystemSafety.Matches(destination, operation.CandidateLength, operation.CandidateSha256))
                    throw new InvalidDataException("Candidate copy failed integrity validation: " + destination);
            }

            if (installStateBytes is not null)
            {
                TransactionOperation stateOperation = receipt.Operations[^1];
                string stateCandidate = FileSystemSafety.CombineUnder(transactionRoot, stateOperation.CandidateRelativePath);
                File.WriteAllBytes(stateCandidate, installStateBytes);
                InstallStateV1? stateReadBack = JsonStore.Read(stateCandidate, InstallerJsonContext.Default.InstallStateV1);
                if (stateReadBack is null || stateReadBack.SchemaVersion != 1 ||
                    !string.Equals(FileSystemSafety.NormalizeRoot(stateReadBack.GameDir), _gameDir, FileSystemSafety.PathComparison) ||
                    !FileSystemSafety.Matches(stateCandidate, stateOperation.CandidateLength, stateOperation.CandidateSha256))
                    throw new InvalidDataException("Candidate install-state.json failed read-back validation.");
            }
            receipt.Phase = "CandidateReady";
            WriteReceipt(receiptPath, receipt);

            foreach (TransactionOperation operation in receipt.Operations)
            {
                string target = FileSystemSafety.CombineUnder(_gameDir, operation.TargetRelativePath);
                string candidate = FileSystemSafety.CombineUnder(transactionRoot, operation.CandidateRelativePath);
                string backup = FileSystemSafety.CombineUnder(transactionRoot, operation.BackupRelativePath);
                string targetParent = Path.GetDirectoryName(target)!;
                FileSystemSafety.EnsureDirectoryUnder(_gameDir, targetParent);

                operation.State = "MovingOriginal";
                receipt.Phase = "Committing";
                WriteReceipt(receiptPath, receipt);
                if (File.Exists(target))
                {
                    FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, target);
                    FileSystemSafety.EnsureDirectoryUnder(transactionRoot, Path.GetDirectoryName(backup)!);
                    File.Move(target, backup);
                }
                operation.State = "OriginalMoved";
                WriteReceipt(receiptPath, receipt);

                File.Move(candidate, target);
                if (!FileSystemSafety.Matches(target, operation.CandidateLength, operation.CandidateSha256))
                    throw new InvalidDataException("Published target failed integrity validation: " + target);
                operation.State = "CandidatePublished";
                WriteReceipt(receiptPath, receipt);
            }

            receipt.Phase = "Committed";
            WriteReceipt(receiptPath, receipt);
            FinalizeCommittedInstall(transactionRoot, receipt);
        }
        catch (Exception ex)
        {
            try
            {
                if (string.Equals(receipt.Phase, "Committed", StringComparison.Ordinal))
                    FinalizeCommittedInstall(transactionRoot, receipt);
                else
                    RollbackInstall(transactionRoot, receipt);
            }
            catch (Exception recoveryError)
            {
                throw new InstallerException(InstallerCodes.TransactionBlocked, 3,
                    "安装失败，并且自动恢复未能完整完成。请保留事务目录并收集日志。",
                    "Installation failed and automatic recovery could not complete. Preserve the transaction directory and collect logs.",
                    ex.Message + " | Recovery: " + recoveryError.Message, ex);
            }

            throw new InstallerException(InstallerCodes.LocalAccess, 3,
                "安装过程中出现本地文件访问失败，已回滚本次 Runtime 修改。通常这不是防火墙问题。",
                "A local file operation failed during installation. This Runtime mutation was rolled back; this is usually not a firewall problem.",
                ex.Message, ex);
        }
    }

    public UninstallExecutionResult ExecuteUninstall(IEnumerable<string> rawAllowlist)
    {
        string stamp = CreateStamp();
        string finalBackupRoot = FileSystemSafety.CombineUnder(_gameDir, "DTMAPI/backups/uninstall-" + stamp);
        string statePath = FileSystemSafety.CombineUnder(_gameDir, "DTMAPI/uninstall-state-" + stamp + ".json");
        List<string> allowlist = rawAllowlist
            .Select(FileSystemSafety.NormalizeRelativePath)
            .Distinct(PathComparer())
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToList();
        TransactionReceiptV1 receipt = new()
        {
            TransactionId = stamp,
            CreatedAtUtc = DateTime.UtcNow.ToString("O"),
            GameDir = _gameDir,
            Engine = UninstallEngine,
            Phase = "Prepared"
        };
        UninstallStateV1 state = new()
        {
            RemovedAtUtc = DateTime.UtcNow.ToString("O"),
            GameDir = _gameDir,
            BackupRoot = finalBackupRoot
        };

        foreach (string relative in allowlist)
        {
            string target = FileSystemSafety.CombineUnder(_gameDir, relative);
            if (Directory.Exists(target))
                throw new InstallerException(InstallerCodes.LocalAccess, 3,
                    "卸载目标被同名目录占用，操作已停止。",
                    "An uninstall target is occupied by a directory; the action stopped.", target);
            if (!File.Exists(target))
            {
                state.Preserved.Add("missing: " + relative);
                continue;
            }

            FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, target);
            FileInfo info = new(target);
            int ordinal = receipt.Operations.Count;
            receipt.Operations.Add(new TransactionOperation
            {
                Ordinal = ordinal,
                TargetRelativePath = relative,
                CandidateRelativePath = "candidate/" + ordinal.ToString("D4") + ".bin",
                BackupRelativePath = "backup/" + ordinal.ToString("D4") + ".bin",
                CandidateSha256 = FileSystemSafety.Sha256(target),
                CandidateLength = info.Length,
                HadOriginal = true,
                State = "Pending"
            });
            state.Removed.Add(relative);
        }

        if (receipt.Operations.Count == 0)
        {
            state.NoOp = true;
            FileSystemSafety.EnsureDirectoryUnder(_gameDir, FileSystemSafety.CombineUnder(_gameDir, "DTMAPI"));
            WriteUninstallState(statePath, state);
            return new UninstallExecutionResult { State = state, BackupRoot = finalBackupRoot };
        }

        // Validate the long-lived destination before any live Runtime file is
        // moved. A pre-existing junction/symlink must fail with zero removals.
        FileSystemSafety.EnsureDirectoryUnder(_gameDir,
            FileSystemSafety.CombineUnder(_gameDir, "DTMAPI/backups"));
        string transactionRoot = FileSystemSafety.CombineUnder(_gameDir, CanonicalPrefix + stamp);
        string receiptPath = Path.Combine(transactionRoot, "transaction.json");
        try
        {
            CreateVisibleTransaction(transactionRoot, receipt);
        }
        catch (Exception ex)
        {
            throw new InstallerException(InstallerCodes.TransactionGenesis, 3,
                "无法建立第一份卸载恢复凭据；游戏文件尚未修改。",
                "The first uninstall recovery receipt could not be established; game files were not changed.", ex.Message, ex);
        }

        try
        {
            FileSystemSafety.EnsureDirectoryUnder(transactionRoot, Path.Combine(transactionRoot, "backup"));
            foreach (TransactionOperation operation in receipt.Operations)
            {
                string target = FileSystemSafety.CombineUnder(_gameDir, operation.TargetRelativePath);
                string backup = FileSystemSafety.CombineUnder(transactionRoot, operation.BackupRelativePath);
                operation.State = "MovingOriginal";
                receipt.Phase = "Committing";
                WriteReceipt(receiptPath, receipt);

                FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, target);
                FileSystemSafety.EnsureDirectoryUnder(transactionRoot, Path.GetDirectoryName(backup)!);
                File.Move(target, backup);
                if (!FileSystemSafety.Matches(backup, operation.CandidateLength, operation.CandidateSha256))
                    throw new InvalidDataException("Uninstall backup failed integrity validation: " + backup);
                operation.State = "OriginalMoved";
                WriteReceipt(receiptPath, receipt);
            }

            WriteUninstallState(Path.Combine(transactionRoot, "uninstall-state.json"), state);
            receipt.Phase = "Committed";
            WriteReceipt(receiptPath, receipt);
            return FinalizeCommittedUninstall(transactionRoot, receipt, state);
        }
        catch (Exception ex)
        {
            try
            {
                if (string.Equals(receipt.Phase, "Committed", StringComparison.Ordinal))
                    return FinalizeCommittedUninstall(transactionRoot, receipt, state);
                RollbackUninstall(transactionRoot, receipt);
            }
            catch (Exception recoveryError)
            {
                throw new InstallerException(InstallerCodes.TransactionBlocked, 3,
                    "卸载失败，并且自动恢复未能完整完成。请保留事务目录。",
                    "Uninstall failed and automatic recovery could not complete. Preserve the transaction directory.",
                    ex.Message + " | Recovery: " + recoveryError.Message, ex);
            }

            throw new InstallerException(InstallerCodes.LocalAccess, 3,
                "卸载遇到本地文件访问失败，已恢复本次移动。通常这不是防火墙问题。",
                "Uninstall hit a local file-access failure and restored this action's moves; this is usually not a firewall problem.",
                ex.Message, ex);
        }

        throw new InvalidOperationException("Unreachable uninstall transaction state.");
    }

    private TransactionInspection InspectTransactionRoot(string root, string stamp, bool isPreviewPrefix)
    {
        try
        {
            FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, root);
            if (!StampPattern.IsMatch(stamp))
                throw new InvalidDataException("The transaction directory stamp is not canonical.");

            string matchingState = FileSystemSafety.CombineUnder(_gameDir, "DTMAPI/" + LegacyStatePrefix + stamp);
            if (FileSystemSafety.EntryExistsIncludingLink(matchingState))
            {
                return new TransactionInspection
                {
                    Root = root,
                    Kind = TransactionInspectionKind.ForeignEngineReceipt,
                    Detail = "A matching accepted PowerShell state root exists; this engine will not adopt the pair."
                };
            }

            string receiptPath = Path.Combine(root, "transaction.json");
            if (!File.Exists(receiptPath))
            {
                return new TransactionInspection
                {
                    Root = root,
                    Kind = IsSterile(root, stamp) ? TransactionInspectionKind.SterileNoReceipt : TransactionInspectionKind.UnsafeNoReceipt,
                    Detail = "No final transaction.json receipt exists."
                };
            }

            FileSystemSafety.AssertNoLinksOnExistingPath(root, receiptPath);
            TransactionReceiptV1? receipt = JsonStore.Read(receiptPath, InstallerJsonContext.Default.TransactionReceiptV1);
            if (receipt is null || !IsOwnEngine(receipt.Engine))
            {
                return new TransactionInspection
                {
                    Root = root,
                    Kind = TransactionInspectionKind.ForeignEngineReceipt,
                    Receipt = receipt,
                    Detail = isPreviewPrefix
                        ? "The preview-prefix transaction does not carry this host's exact engine identity."
                        : "The canonical transaction belongs to the accepted PowerShell engine or another engine."
                };
            }

            ValidateReceipt(root, receipt, stamp);
            bool uninstall = string.Equals(receipt.Engine, UninstallEngine, StringComparison.Ordinal);
            return new TransactionInspection
            {
                Root = root,
                Kind = uninstall
                    ? TransactionInspectionKind.RecoverableUninstallReceipt
                    : TransactionInspectionKind.RecoverableInstallReceipt,
                Receipt = receipt,
                Detail = uninstall
                    ? "A valid multi-platform uninstall transaction requires recovery/finalization."
                    : "A valid multi-platform install transaction requires recovery/finalization."
            };
        }
        catch (Exception ex)
        {
            return new TransactionInspection
            {
                Root = root,
                Kind = TransactionInspectionKind.InvalidReceipt,
                Detail = ex.Message
            };
        }
    }

    private TransactionInspection InspectGenesis(string root)
    {
        try
        {
            FileSystemSafety.AssertTreeHasNoLinks(root);
            string receiptPath = Path.Combine(root, "transaction.json");
            if (!File.Exists(receiptPath) || Directory.GetFileSystemEntries(root).Length != 1)
                throw new InvalidDataException("Genesis residue is not an exact one-receipt tree.");
            TransactionReceiptV1? receipt = JsonStore.Read(receiptPath, InstallerJsonContext.Default.TransactionReceiptV1);
            if (receipt is null || !IsOwnEngine(receipt.Engine) || receipt.Phase != "Prepared" ||
                receipt.Operations.Count == 0 || receipt.Operations.Any(operation => operation.State != "Pending") ||
                !StampPattern.IsMatch(receipt.TransactionId))
                throw new InvalidDataException("Genesis receipt identity is invalid.");
            string expectedRoot = FileSystemSafety.CombineUnder(_gameDir, CanonicalPrefix + receipt.TransactionId);
            if (Directory.Exists(expectedRoot))
                throw new InvalidDataException("Genesis residue conflicts with a visible canonical transaction.");
            ValidateReceipt(expectedRoot, receipt, receipt.TransactionId);
            return new TransactionInspection
            {
                Root = root,
                Kind = TransactionInspectionKind.RecoverableGenesis,
                Receipt = receipt,
                Detail = "A pre-publication receipt exists; no live mutation began."
            };
        }
        catch (Exception ex)
        {
            return new TransactionInspection { Root = root, Kind = TransactionInspectionKind.InvalidReceipt, Detail = ex.Message };
        }
    }

    private TransactionInspection InspectOrphanState(string stateRoot, string stamp)
    {
        try
        {
            FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, stateRoot);
            if (!StampPattern.IsMatch(stamp))
                throw new InvalidDataException("The legacy state-root stamp is not canonical.");
            return new TransactionInspection
            {
                Root = stateRoot,
                Kind = TransactionInspectionKind.OrphanState,
                Detail = "An accepted PowerShell transaction state root has no matching canonical transaction root."
            };
        }
        catch (Exception ex)
        {
            return new TransactionInspection { Root = stateRoot, Kind = TransactionInspectionKind.InvalidReceipt, Detail = ex.Message };
        }
    }

    private void RecoverInstall(string root, TransactionReceiptV1 receipt)
    {
        ValidateReceipt(root, receipt, receipt.TransactionId);
        if (string.Equals(receipt.Phase, "Committed", StringComparison.Ordinal))
            FinalizeCommittedInstall(root, receipt);
        else
            RollbackInstall(root, receipt);
    }

    private void FinalizeCommittedInstall(string root, TransactionReceiptV1 receipt)
    {
        ValidateReceipt(root, receipt, receipt.TransactionId);
        if (string.Equals(receipt.Engine, UninstallEngine, StringComparison.Ordinal))
            throw new InvalidDataException("An uninstall receipt cannot be finalized as an install.");
        foreach (TransactionOperation operation in receipt.Operations)
        {
            string target = FileSystemSafety.CombineUnder(_gameDir, operation.TargetRelativePath);
            FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, target);
            if (!FileSystemSafety.Matches(target, operation.CandidateLength, operation.CandidateSha256))
                throw new InvalidDataException("Committed transaction has an unexpected live target: " + target);
        }
        DeleteTransactionTree(root);
    }

    private void RollbackInstall(string root, TransactionReceiptV1 receipt)
    {
        ValidateReceipt(root, receipt, receipt.TransactionId);
        if (string.Equals(receipt.Engine, UninstallEngine, StringComparison.Ordinal))
            throw new InvalidDataException("An uninstall receipt cannot use install rollback.");
        foreach (TransactionOperation operation in receipt.Operations.OrderByDescending(item => item.Ordinal))
        {
            string target = FileSystemSafety.CombineUnder(_gameDir, operation.TargetRelativePath);
            string candidate = FileSystemSafety.CombineUnder(root, operation.CandidateRelativePath);
            string backup = FileSystemSafety.CombineUnder(root, operation.BackupRelativePath);

            if (File.Exists(backup))
            {
                FileSystemSafety.AssertNoLinksOnExistingPath(root, backup);
                if (File.Exists(target))
                {
                    FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, target);
                    if (!FileSystemSafety.Matches(target, operation.CandidateLength, operation.CandidateSha256))
                        throw new InvalidDataException("Rollback found an ambiguous live target and preserved both copies: " + target);
                    File.Delete(target);
                }
                FileSystemSafety.EnsureDirectoryUnder(_gameDir, Path.GetDirectoryName(target)!);
                File.Move(backup, target);
            }
            else if (operation.HadOriginal)
            {
                if (operation.State is not ("Pending" or "MovingOriginal") || !File.Exists(target))
                    throw new InvalidDataException("Rollback cannot prove the original file still exists: " + target);
                FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, target);
            }
            else if (File.Exists(target))
            {
                FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, target);
                if (!FileSystemSafety.Matches(target, operation.CandidateLength, operation.CandidateSha256))
                    throw new InvalidDataException("Rollback refused to delete an unexpected live file: " + target);
                File.Delete(target);
            }

            FileSystemSafety.TryDeleteFile(candidate);
        }
        DeleteTransactionTree(root);
    }

    private void RecoverUninstall(string root, TransactionReceiptV1 receipt)
    {
        ValidateReceipt(root, receipt, receipt.TransactionId);
        if (!string.Equals(receipt.Engine, UninstallEngine, StringComparison.Ordinal))
            throw new InvalidDataException("The receipt is not an uninstall transaction.");
        if (string.Equals(receipt.Phase, "Committed", StringComparison.Ordinal))
        {
            UninstallStateV1 state = ReadOrReconstructUninstallState(root, receipt);
            _ = FinalizeCommittedUninstall(root, receipt, state);
        }
        else
        {
            RollbackUninstall(root, receipt);
        }
    }

    private void RollbackUninstall(string root, TransactionReceiptV1 receipt)
    {
        ValidateReceipt(root, receipt, receipt.TransactionId);
        foreach (TransactionOperation operation in receipt.Operations.OrderByDescending(item => item.Ordinal))
        {
            string target = FileSystemSafety.CombineUnder(_gameDir, operation.TargetRelativePath);
            string backup = FileSystemSafety.CombineUnder(root, operation.BackupRelativePath);
            if (File.Exists(backup))
            {
                FileSystemSafety.AssertNoLinksOnExistingPath(root, backup);
                if (!FileSystemSafety.Matches(backup, operation.CandidateLength, operation.CandidateSha256))
                    throw new InvalidDataException("Uninstall rollback backup identity is invalid: " + backup);
                if (File.Exists(target))
                {
                    FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, target);
                    if (!FileSystemSafety.Matches(target, operation.CandidateLength, operation.CandidateSha256))
                        throw new InvalidDataException("Uninstall rollback found a different recreated live target: " + target);
                    File.Delete(backup);
                }
                else
                {
                    FileSystemSafety.EnsureDirectoryUnder(_gameDir, Path.GetDirectoryName(target)!);
                    File.Move(backup, target);
                    if (!FileSystemSafety.Matches(target, operation.CandidateLength, operation.CandidateSha256))
                        throw new InvalidDataException("Restored uninstall target failed integrity validation: " + target);
                }
            }
            else
            {
                if (!File.Exists(target) || !FileSystemSafety.Matches(target, operation.CandidateLength, operation.CandidateSha256))
                    throw new InvalidDataException("Uninstall rollback cannot locate the original file or its backup: " + target);
            }
        }
        DeleteTransactionTree(root);
    }

    private UninstallExecutionResult FinalizeCommittedUninstall(
        string transactionRoot,
        TransactionReceiptV1 receipt,
        UninstallStateV1 state)
    {
        ValidateReceipt(transactionRoot, receipt, receipt.TransactionId);
        if (!string.Equals(receipt.Engine, UninstallEngine, StringComparison.Ordinal) || receipt.Phase != "Committed")
            throw new InvalidDataException("Only a committed uninstall receipt may be finalized.");

        foreach (TransactionOperation operation in receipt.Operations)
        {
            string target = FileSystemSafety.CombineUnder(_gameDir, operation.TargetRelativePath);
            string backup = FileSystemSafety.CombineUnder(transactionRoot, operation.BackupRelativePath);
            if (File.Exists(target))
                throw new InvalidDataException("Committed uninstall has a recreated live target: " + target);
            FileSystemSafety.AssertNoLinksOnExistingPath(transactionRoot, backup);
            if (!FileSystemSafety.Matches(backup, operation.CandidateLength, operation.CandidateSha256))
                throw new InvalidDataException("Committed uninstall backup identity is invalid: " + backup);
        }

        string backupsParent = FileSystemSafety.CombineUnder(_gameDir, "DTMAPI/backups");
        FileSystemSafety.EnsureDirectoryUnder(_gameDir, backupsParent);
        string finalRoot = FileSystemSafety.CombineUnder(backupsParent, "uninstall-" + receipt.TransactionId);
        if (Directory.Exists(finalRoot) || File.Exists(finalRoot))
            throw new InvalidDataException("The final uninstall backup path already exists: " + finalRoot);
        FileSystemSafety.AssertTreeHasNoLinks(transactionRoot);
        Directory.Move(transactionRoot, finalRoot);

        state.BackupRoot = finalRoot;
        state.NoOp = false;
        string statePath = FileSystemSafety.CombineUnder(_gameDir, "DTMAPI/uninstall-state-" + receipt.TransactionId + ".json");
        string warning = string.Empty;
        try
        {
            WriteUninstallState(statePath, state);
        }
        catch (Exception ex)
        {
            warning = "The top-level uninstall state receipt could not be published; the committed receipt remains in " + finalRoot + ": " + ex.Message;
        }
        return new UninstallExecutionResult { State = state, BackupRoot = finalRoot, StateReceiptWarning = warning };
    }

    private UninstallStateV1 ReadOrReconstructUninstallState(string root, TransactionReceiptV1 receipt)
    {
        string statePath = Path.Combine(root, "uninstall-state.json");
        try
        {
            if (File.Exists(statePath))
            {
                FileSystemSafety.AssertNoLinksOnExistingPath(root, statePath);
                UninstallStateV1? state = JsonStore.Read(statePath, InstallerJsonContext.Default.UninstallStateV1);
                if (state is not null && state.SchemaVersion == 1 &&
                    string.Equals(FileSystemSafety.NormalizeRoot(state.GameDir), _gameDir, FileSystemSafety.PathComparison))
                    return state;
            }
        }
        catch
        {
            // The committed transaction receipt is sufficient to reconstruct the
            // informational uninstall state without touching live targets.
        }

        return new UninstallStateV1
        {
            RemovedAtUtc = receipt.CreatedAtUtc,
            GameDir = _gameDir,
            BackupRoot = FileSystemSafety.CombineUnder(_gameDir, "DTMAPI/backups/uninstall-" + receipt.TransactionId),
            Removed = receipt.Operations.OrderBy(item => item.Ordinal).Select(item => item.TargetRelativePath).ToList()
        };
    }

    private void CreateVisibleTransaction(string transactionRoot, TransactionReceiptV1 receipt)
    {
        if (Directory.Exists(transactionRoot) || File.Exists(transactionRoot))
            throw new IOException("Transaction root collision: " + transactionRoot);
        string genesisRoot = FileSystemSafety.CombineUnder(_gameDir, GenesisPrefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(genesisRoot);
        try
        {
            FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, genesisRoot);
            WriteReceipt(Path.Combine(genesisRoot, "transaction.json"), receipt);
            FileSystemSafety.AssertTreeHasNoLinks(genesisRoot);
            Directory.Move(genesisRoot, transactionRoot);
            FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, transactionRoot);
        }
        catch
        {
            try
            {
                if (Directory.Exists(genesisRoot))
                {
                    FileSystemSafety.AssertTreeHasNoLinks(genesisRoot);
                    Directory.Delete(genesisRoot, true);
                }
            }
            catch
            {
                // InspectAll classifies any ambiguous genesis residue on the next run.
            }
            throw;
        }
    }

    private void ValidateReceipt(string root, TransactionReceiptV1? receipt, string expectedStamp)
    {
        if (receipt is null || receipt.SchemaVersion != 1 || !StampPattern.IsMatch(expectedStamp) ||
            !string.Equals(receipt.TransactionId, expectedStamp, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(FileSystemSafety.NormalizeRoot(receipt.GameDir), _gameDir, FileSystemSafety.PathComparison) ||
            !IsOwnEngine(receipt.Engine) || receipt.Operations.Count == 0 ||
            receipt.Phase is not ("Prepared" or "CandidateReady" or "Committing" or "Committed"))
            throw new InvalidDataException("The transaction receipt identity is invalid.");

        HashSet<int> ordinals = new();
        foreach (TransactionOperation operation in receipt.Operations)
        {
            if (!ordinals.Add(operation.Ordinal) || operation.Ordinal < 0 ||
                operation.CandidateLength < 0 || !IsSha256(operation.CandidateSha256) ||
                operation.State is not ("Pending" or "MovingOriginal" or "OriginalMoved" or "CandidatePublished"))
                throw new InvalidDataException("The transaction operation set is invalid.");
            _ = FileSystemSafety.CombineUnder(_gameDir, FileSystemSafety.NormalizeRelativePath(operation.TargetRelativePath));
            _ = FileSystemSafety.CombineUnder(root, FileSystemSafety.NormalizeRelativePath(operation.CandidateRelativePath));
            _ = FileSystemSafety.CombineUnder(root, FileSystemSafety.NormalizeRelativePath(operation.BackupRelativePath));
        }
        if (!ordinals.SetEquals(Enumerable.Range(0, receipt.Operations.Count)))
            throw new InvalidDataException("Transaction operation ordinals are not contiguous.");
    }

    private static int GetInstallOrder(InstallFile file)
    {
        return file.Kind switch
        {
            "runtime-assembly" => 10,
            "runtime-asset" => 11,
            "optional-framework-component" => 20,
            "installer-tool" => 30,
            "diagnostic-tool" => 31,
            "version-authority" => 32,
            "release-manifest" => 40,
            "bepinex" => 5,
            _ => 35
        };
    }

    private static void WriteReceipt(string path, TransactionReceiptV1 receipt)
    {
        JsonStore.WriteAtomic(path, receipt, InstallerJsonContext.Default.TransactionReceiptV1,
            value => ReceiptEquals(value, receipt));
    }

    private static bool ReceiptEquals(TransactionReceiptV1? actual, TransactionReceiptV1 expected)
    {
        if (actual is null || actual.SchemaVersion != expected.SchemaVersion ||
            actual.TransactionId != expected.TransactionId || actual.CreatedAtUtc != expected.CreatedAtUtc ||
            actual.GameDir != expected.GameDir || actual.Engine != expected.Engine || actual.Phase != expected.Phase ||
            actual.Operations.Count != expected.Operations.Count)
            return false;
        for (int index = 0; index < expected.Operations.Count; index++)
        {
            TransactionOperation left = actual.Operations[index];
            TransactionOperation right = expected.Operations[index];
            if (left.Ordinal != right.Ordinal || left.TargetRelativePath != right.TargetRelativePath ||
                left.CandidateRelativePath != right.CandidateRelativePath || left.BackupRelativePath != right.BackupRelativePath ||
                left.CandidateSha256 != right.CandidateSha256 || left.CandidateLength != right.CandidateLength ||
                left.HadOriginal != right.HadOriginal || left.State != right.State)
                return false;
        }
        return true;
    }

    private static void WriteUninstallState(string path, UninstallStateV1 state)
    {
        JsonStore.WriteAtomic(path, state, InstallerJsonContext.Default.UninstallStateV1,
            value => value is not null && value.SchemaVersion == 1 && value.GameDir == state.GameDir &&
                     value.BackupRoot == state.BackupRoot && value.NoOp == state.NoOp &&
                     value.Removed.SequenceEqual(state.Removed, StringComparer.Ordinal));
    }

    private bool IsSterile(string root, string stamp)
    {
        if (!StampPattern.IsMatch(stamp))
            return false;
        string matchingState = FileSystemSafety.CombineUnder(_gameDir, "DTMAPI/" + LegacyStatePrefix + stamp);
        if (FileSystemSafety.EntryExistsIncludingLink(matchingState))
            return false;
        FileSystemSafety.AssertNoLinksOnExistingPath(_gameDir, root);
        foreach (string entry in Directory.GetFileSystemEntries(root))
        {
            FileSystemSafety.AssertNoLinksOnExistingPath(root, entry);
            if (Directory.Exists(entry) || !SterileReceiptResiduePattern.IsMatch(Path.GetFileName(entry)))
                return false;
        }
        return true;
    }

    private void RevalidateAndDeleteSterile(string root)
    {
        string name = Path.GetFileName(root);
        string stamp = name.StartsWith(CanonicalPrefix, StringComparison.OrdinalIgnoreCase)
            ? name[CanonicalPrefix.Length..]
            : name.StartsWith(PreviewPrefix, StringComparison.OrdinalIgnoreCase)
                ? name[PreviewPrefix.Length..]
                : string.Empty;
        if (!IsSterile(root, stamp))
            throw new InvalidDataException("The transaction shell changed before cleanup: " + root);
        Directory.Delete(root, recursive: false);
    }

    private static void RevalidateAndDeleteGenesis(TransactionInspection inspection)
    {
        if (inspection.Receipt is null || inspection.Receipt.Phase != "Prepared" ||
            inspection.Receipt.Operations.Any(operation => operation.State != "Pending"))
            throw new InvalidDataException("Genesis residue changed before cleanup: " + inspection.Root);
        FileSystemSafety.AssertTreeHasNoLinks(inspection.Root);
        if (Directory.GetFileSystemEntries(inspection.Root).Length != 1 ||
            !File.Exists(Path.Combine(inspection.Root, "transaction.json")))
            throw new InvalidDataException("Genesis residue changed before cleanup: " + inspection.Root);
        Directory.Delete(inspection.Root, recursive: true);
    }

    private static void DeleteTransactionTree(string root)
    {
        FileSystemSafety.AssertTreeHasNoLinks(root);
        Directory.Delete(root, recursive: true);
    }

    private static bool IsOwnEngine(string engine) =>
        string.Equals(engine, InstallEngine, StringComparison.Ordinal) ||
        string.Equals(engine, BepInExEngine, StringComparison.Ordinal) ||
        string.Equals(engine, UninstallEngine, StringComparison.Ordinal);

    private static bool IsSha256(string value) =>
        value.Length == 64 && value.All(character => Uri.IsHexDigit(character));

    private static string CreateStamp() =>
        DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N")[..8];

    private static StringComparer PathComparer() => FileSystemSafety.PathComparison == StringComparison.OrdinalIgnoreCase
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private static InstallerException Blocked(TransactionInspection inspection)
    {
        return new InstallerException(InstallerCodes.TransactionBlocked, 3,
            "检测到无法安全接管的安装事务；为避免破坏旧 Runtime，操作已停止。",
            "An installer transaction cannot be safely adopted. The action stopped to protect the previous Runtime.",
            inspection.Root + ": " + inspection.Kind + ": " + inspection.Detail);
    }
}
