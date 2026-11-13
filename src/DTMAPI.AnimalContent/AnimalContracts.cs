using System;
using DTMAPI.AudioContent;

[assembly: DTMAPI.Abstractions.DtmApiStatus(DTMAPI.Abstractions.DtmApiStatus.Experimental)]

namespace DTMAPI.AnimalContent
{
    public static class AnimalModule { public const string ProviderId = "DTMAPI.Animals"; }
    public enum AnimalStage { Child, Adult }
    public sealed class AnimalVoiceBinding
    {
        public string SpeciesId { get; set; } = "";
        public AnimalStage Stage { get; set; }
        public string NativeEvent { get; set; } = "";
        public IAudioSound Sound { get; set; } = null!;
    }
    public interface IAnimalContentApi
    {
        IAnimalRegistration Register(AnimalDefinitionBatch definitions);
        IDisposable RegisterVoices(AnimalVoiceBinding[] voices);
    }
    public enum AnimalRegistrationState { WaitingNative, Ready, Released }
    public sealed class AnimalRegistrationSnapshot
    {
        public AnimalRegistrationSnapshot(AnimalRegistrationState state, string diagnostic = "") { State = state; Diagnostic = diagnostic; }
        public AnimalRegistrationState State { get; }
        public string Diagnostic { get; }
    }
    public interface IAnimalRegistration : IDisposable { AnimalRegistrationSnapshot GetSnapshot(); }
    public sealed class AnimalDefinitionBatch
    {
        public AnimalDefinition[] Definitions { get; set; } = Array.Empty<AnimalDefinition>();
    }
    /// <summary>为原生物种补充绘制与 AI 返回状态；行为读取实际动物的原生配置。</summary>
    public sealed class AnimalDefinition
    {
        public string SpeciesId { get; set; } = "";
        /// <summary>仅用于 assetBundle 加载失败时选择原生动画机。</summary>
        public string TemplateSpeciesId { get; set; } = "";
        public string AnimatorMode { get; set; } = "";
        /// <summary>仅用于 assetBundle 模式的成年动画机地址。</summary>
        public string AdultAnimatorKey { get; set; } = "";
        /// <summary>仅用于 assetBundle 模式的幼体动画机地址。</summary>
        public string ChildAnimatorKey { get; set; } = "";
        public string AnimatorBundle { get; set; } = "";
        public string AdultAnimatorAsset { get; set; } = "";
        public string ChildAnimatorAsset { get; set; } = "";
        public string FrameManifest { get; set; } = "";
        public string TemplateSpritePrefix { get; set; } = "";
        public string CustomSpritePrefix { get; set; } = "";
    }
}
