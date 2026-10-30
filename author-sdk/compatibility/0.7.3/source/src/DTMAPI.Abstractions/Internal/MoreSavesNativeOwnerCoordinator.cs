using System;

namespace DTMAPI.Abstractions
{
    /// <summary>
    /// Internal one-writer guard for the native GameManager.archiveFileCount state holder.
    /// It is deliberately not a public mod API.
    /// </summary>
    internal static class MoreSavesNativeOwnerCoordinator
    {
        internal const string CompatibilityOwner = "DTMAPI.GameBridge.DolocTown.Compatibility.SaveSlots";
        internal const string ProductOwner = "DTMAPI.More" + "SavesMod";
        private static readonly object Sync = new object();
        private static string activeOwner = string.Empty;

        internal static bool TryAcquire(string owner, out string failure)
        {
            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException("A native owner identity is required.", nameof(owner));
            lock (Sync)
            {
                if (activeOwner.Length == 0 || activeOwner.Equals(owner, StringComparison.Ordinal))
                {
                    activeOwner = owner;
                    failure = string.Empty;
                    return true;
                }

                failure = "GameManager.archiveFileCount is already owned by " + activeOwner + ".";
                return false;
            }
        }

        internal static bool ReleaseAfterNativeRestore(string owner, bool nativeRestoreSucceeded)
        {
            lock (Sync)
            {
                if (!activeOwner.Equals(owner ?? string.Empty, StringComparison.Ordinal))
                    return false;
                if (!nativeRestoreSucceeded)
                    return false;
                activeOwner = string.Empty;
                return true;
            }
        }

        internal static bool IsOwnedBy(string owner)
        {
            lock (Sync)
                return activeOwner.Equals(owner ?? string.Empty, StringComparison.Ordinal);
        }

        internal static string ActiveOwner
        {
            get
            {
                lock (Sync)
                    return activeOwner;
            }
        }

        internal static void ResetForTests()
        {
            lock (Sync)
                activeOwner = string.Empty;
        }
    }
}
