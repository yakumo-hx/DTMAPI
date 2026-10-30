using System;

namespace DTMAPI.Abstractions
{
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.3", Notes = "Synchronous, owner-bound package bytes. Returned arrays belong to the caller; no decoded native objects or shared cache.")]
    public interface IModResourceHelper
    {
        OwnerDataResult<byte[]> ReadBytes(string key);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.3", Notes = "One Host registration per CodeMod owner. Prepare validates data without effects; Core retains the returned activation before calling Activate.")]
    public interface IContentHostService
    {
        void Register(string contractVersion, Func<IContentPack, IContentPackActivation> prepare);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.3", Notes = "A selected Pack and its Host jointly guard all operations. Files and Resources are rooted in PackData. Domain JSON and image decoding belong to the Host.")]
    public interface IContentPack
    {
        IManifest Manifest { get; }
        string HostId { get; }
        string Schema { get; }
        IOwnerFileHelper Files { get; }
        IModResourceHelper Resources { get; }
        ITranslationHelper Translation { get; }
        IManifest? GetRequiredDependency(string providerId);
        TApi? GetApi<TApi>(string providerId) where TApi : class;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.3", Notes = "Activate runs once on the runtime thread. Dispose must also handle partial activation and is retried after failure; release responsibility must only clear after successful cleanup.")]
    public interface IContentPackActivation : IDisposable
    {
        void Activate();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.3")]
    public static class ContentHostServiceExtensions
    {
        public static IModResourceHelper GetModResources(this IDtmHelper helper) => helper.GetRequiredService<IModResourceHelper>("0.7.3", "0.7.3");
        public static IContentHostService GetContentHost(this IDtmHelper helper) => helper.GetRequiredService<IContentHostService>("0.7.3", "0.7.3");
    }
}
