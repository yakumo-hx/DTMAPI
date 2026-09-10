using System;

namespace DTMAPI.Abstractions
{
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2", Notes = "Candidate optional platform services; only exact supported contract types resolve. Does not extend IDtmHelper's required members.")]
    public interface IDtmHelperServices
    {
        TService? GetService<TService>() where TService : class;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public static class DtmHelperServiceExtensions
    {
        public static TService? GetOptionalService<TService>(this IDtmHelper helper) where TService : class
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            return (helper as IDtmHelperServices)?.GetService<TService>();
        }

        public static TService GetRequiredService<TService>(this IDtmHelper helper,
            string minimumRuntimeVersion, string minimumApiTarget) where TService : class
        {
            if (string.IsNullOrWhiteSpace(minimumRuntimeVersion)) throw new ArgumentException("A minimum Runtime version is required.", nameof(minimumRuntimeVersion));
            if (string.IsNullOrWhiteSpace(minimumApiTarget)) throw new ArgumentException("A minimum API target is required.", nameof(minimumApiTarget));
            return helper.GetOptionalService<TService>() ?? throw new NotSupportedException(
                "Platform service " + typeof(TService).FullName + " is unavailable. Requires Runtime >= " +
                minimumRuntimeVersion + " and Author SDK API target >= " + minimumApiTarget + ".");
        }
    }
}
