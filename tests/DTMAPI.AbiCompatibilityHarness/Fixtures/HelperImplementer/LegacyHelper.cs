using DTMAPI.Abstractions;

namespace Frozen
{
    public sealed class LegacyHelper : IDtmHelper
    {
        public int Writes { get; private set; }
        public IManifest ModManifest => null!;
        public IMonitor Monitor => null!;
        public IEventsHelper Events => null!;
        public IConfigHelper Config => null!;
        public IModRegistry ModRegistry => null!;
        public IWorkshopHelper Workshop => null!;
        public IUiHelper UI => null!;
        public IDiagnosticsHelper Diagnostics => null!;
        public IContentQueryHelper Content => null!;
        public IInputHelper Input => null!;
        public ITranslationHelper Translation => null!;
        public T ReadConfig<T>() where T : new() => new T();
        public void WriteConfig<T>(T value) { Writes++; }
    }
}
