using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace DTMAPI.Zoom
{
    internal static class ZoomHookTransaction
    {
        internal static void Install(
            Action attach,
            Action patch,
            Func<bool> exactOwnerPresent,
            Action unpatch,
            Action detach)
        {
            if (attach == null ||
                patch == null ||
                exactOwnerPresent == null ||
                unpatch == null ||
                detach == null)
            {
                throw new ArgumentNullException(
                    "Zoom Hook transaction delegates are required.");
            }

            attach();
            try
            {
                patch();
                if (!exactOwnerPresent())
                {
                    throw new InvalidOperationException(
                        "Zoom could not prove all three exact ProductNative camera patches.");
                }
            }
            catch (Exception installFailure)
            {
                var failures =
                    new List<Exception> { installFailure };
                try
                {
                    unpatch();
                }
                catch (Exception cleanupFailure)
                {
                    failures.Add(cleanupFailure);
                }
                finally
                {
                    detach();
                }
                if (exactOwnerPresent())
                {
                    failures.Add(
                        new InvalidOperationException(
                            "Zoom rollback retained its exact owner."));
                }
                if (failures.Count > 1)
                {
                    throw new AggregateException(
                        "Zoom atomic Hook installation failed and exact-owner rollback was incomplete.",
                        failures);
                }
                ExceptionDispatchInfo
                    .Capture(installFailure)
                    .Throw();
                throw new InvalidOperationException(
                    "Unreachable Zoom Hook transaction state.");
            }
        }
    }
}
