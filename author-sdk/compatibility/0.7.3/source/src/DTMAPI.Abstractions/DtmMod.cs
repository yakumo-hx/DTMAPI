namespace DTMAPI.Abstractions
{
    public abstract class DtmMod
    {
        public IManifest Manifest { get; internal set; } = EmptyManifest.Instance;
        public IMonitor Monitor { get; internal set; } = NullMonitor.Instance;

        public void AttachContext(IManifest manifest, IMonitor monitor)
        {
            Manifest = manifest;
            Monitor = monitor;
        }

        public abstract void Entry(IDtmHelper helper);
    }
}
