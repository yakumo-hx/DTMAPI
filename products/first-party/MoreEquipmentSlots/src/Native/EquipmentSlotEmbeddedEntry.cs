using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.MoreEquipmentSlots
{
    internal static class EquipmentSlotEmbeddedEntry
    {
        internal static int RequireScope(int? eventSlot, bool isNewGame, int currentSlot, long? epoch)
        {
            if (currentSlot < 0 || !epoch.HasValue || (!isNewGame && !eventSlot.HasValue) ||
                (eventSlot.HasValue && eventSlot.Value != currentSlot))
                throw new InvalidDataException("MES embedded entry requires the initialized archive slot and active save epoch.");
            return currentSlot;
        }

        internal static bool TrySelect(bool exists, bool candidate, IProtectedSourceFiles service, int slot,
            string configRoot, out EquipmentSlotLegacySource? source, out string error, EquipmentSlotSaveScope? expectedScope = null)
        {
            source = null; error = "";
            try
            {
                source = EquipmentSlotLegacySource.CaptureForSlot(service, slot, configRoot, out error, expectedScope);
                if (error.Length > 0) return true;
                string[] recovery = RecoveryInputs(configRoot, slot);
                if (source == null && recovery.Length > 0)
                    error = "仍有旧恢复/认领文件，缺失主文件不能证明空装备；原文件和历史恢复责任保留。";
                if (source != null)
                    source.RequireNormalEntry(() => recovery.SequenceEqual(RecoveryInputs(configRoot, slot), StringComparer.Ordinal),
                        retainRecoveryFiles: recovery.Length > 0);
                // Every ordinary entry uses the same session, including unavailable
                // sources. Residual recovery files never reactivate a sidecar writer.
                return true;
            }
            catch (Exception ex)
            {
                source?.Dispose(); source = null; error = ex.Message;
                return true;
            }
        }

        private static string[] RecoveryInputs(string root, int slot)
        {
            const string fileName = "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json";
            string global = Path.Combine(root, fileName);
            string directory = Path.Combine(root, "protected-items", "equipment-slots",
                "slot-" + slot.ToString(System.Globalization.CultureInfo.InvariantCulture));
            string primary = Path.Combine(directory, fileName), previous = primary + ".previous";
            var entries = new List<string>();
            AddEntries(entries, directory, "*", primary, previous);
            AddEntries(entries, EquipmentSlotLegacyMigration.GetPreSchemaGlobalClaimDirectory(global), "*");
            AddEntries(entries, root, fileName + ".*");
            entries.Sort(StringComparer.Ordinal);
            return entries.ToArray();
        }

        private static void AddEntries(List<string> entries, string directory, string pattern, params string[] excluded)
        {
            try
            {
                foreach (string entry in Directory.EnumerateFileSystemEntries(directory, pattern, SearchOption.TopDirectoryOnly))
                    if (!excluded.Contains(entry, StringComparer.OrdinalIgnoreCase)) entries.Add(entry);
            }
            catch (DirectoryNotFoundException) { }
        }
    }
}
