using System;
using DTMAPI.AudioContent;

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
    /// <summary>Maps existing native species to AI and rendering. Native Content owns species and save identities.</summary>
    public sealed class AnimalDefinition
    {
        public string SpeciesId { get; set; } = "";
        public string TemplateSpeciesId { get; set; } = "";
        public string AiTemplate { get; set; } = "";
        public string AnimatorMode { get; set; } = "";
        public string AdultAnimatorKey { get; set; } = "";
        public string ChildAnimatorKey { get; set; } = "";
        public string AnimatorBundle { get; set; } = "";
        public string AdultAnimatorAsset { get; set; } = "";
        public string ChildAnimatorAsset { get; set; } = "";
        public string FrameManifest { get; set; } = "";
        public string TemplateSpritePrefix { get; set; } = "";
        public string CustomSpritePrefix { get; set; } = "";
    }
}
