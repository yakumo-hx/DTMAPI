using System;
using System.Collections.Generic;
namespace DTMAPI.MonsterContent
{
    internal sealed class EncounterLedger
    {
        private readonly HashSet<string> attempted = new HashSet<string>(StringComparer.Ordinal);
        private int rebuildDepth;
        private bool cleared, failed;
        internal long Generation { get; private set; } = 1;
        internal bool IsRebuilding => rebuildDepth > 0;
        internal bool RecoveryPending { get; private set; }
        internal bool CanSpawn => !IsRebuilding && !RecoveryPending;
        internal bool TryBegin(string rule) => CanSpawn && attempted.Add(rule);
        internal bool HasAttempt(string rule) => attempted.Contains(rule);
        internal void BeginRebuild()
        {
            if (rebuildDepth++ == 0) { cleared = false; failed = false; }
        }
        // Called only after the native list was actually emptied. Nested clears belong
        // to one transaction, but every real clear still invalidates native slot claims.
        internal void ConfirmClear()
        {
            if (!IsRebuilding || !cleared) { Generation++; attempted.Clear(); }
            if (IsRebuilding) { cleared = true; RecoveryPending = true; }
            // A standalone clear cannot recover an earlier incomplete regeneration.
        }
        internal void EndRebuild(bool success)
        {
            if (!IsRebuilding) return;
            failed |= !success;
            if (--rebuildDepth == 0 && cleared && !failed) RecoveryPending = false;
        }
    }
}
