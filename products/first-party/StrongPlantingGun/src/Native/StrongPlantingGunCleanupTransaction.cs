using System;
using System.Collections.Generic;

namespace DTMAPI.StrongPlantingGun
{
    internal static class StrongPlantingGunCleanupTransaction
    {
        internal static List<Exception> Run(
            Action restoreCapacity,
            Action exactOwnerUnpatch)
        {
            if (restoreCapacity == null)
            {
                throw new ArgumentNullException(
                    nameof(restoreCapacity));
            }
            if (exactOwnerUnpatch == null)
            {
                throw new ArgumentNullException(
                    nameof(exactOwnerUnpatch));
            }

            var failures = new List<Exception>();
            Try(restoreCapacity, failures);
            // Exact-owner cleanup is deliberately independent: capacity
            // restore failure must never skip Harmony cleanup.
            Try(exactOwnerUnpatch, failures);
            return failures;
        }

        private static void Try(
            Action action,
            ICollection<Exception> failures)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }
    }
}
