using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal interface IGameBridgeFeature
    {
        string Id { get; }

        void RegisterApis(IManifest manifest);

        void PublishHookStatuses();

        void InstallHooks(HarmonyReflectionPatcher patcher);

        void Update();

        void SaveLoaded(bool isNewGame);

        void ReturnedToTitle();

        void EnvironmentReset(string reason);
    }
}
