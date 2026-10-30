using System;

namespace DTMAPI.MonsterContent.Hosting
{
    /// <summary>Shared admission for template mappings. Frame coverage is advisory. No image decoder or engine types.</summary>
    public static class SkinMappingAdmission
    {
        /// <summary>A missing or out-of-image rectangle renders no sprite. Extra IDs are unused.</summary>
        public static bool HasPixels(MonsterFrameMapping? frame, int width, int height) => frame != null
            && frame.X >= 0 && frame.Y >= 0 && frame.Width > 0 && frame.Height > 0
            && (long)frame.X + frame.Width <= width && (long)frame.Y + frame.Height <= height;

        public static MonsterDiagnostic? Validate(string owner, MonsterDefinition definition, MonsterPresentationInfo info)
        {
            var basic = MonsterValidation.Validate(owner, definition);
            if (basic != null) return basic;
            return Validate(owner, definition.LocalId, definition.TemplateId, definition.Appearance.Skin, info, "appearance.skin.");
        }
        internal static MonsterDiagnostic? Validate(string owner, string id, string target, MonsterSkin? skin, MonsterPresentationInfo info, string prefix)
        {
            if (skin == null) return null;
            MonsterDiagnostic Error(string code, string field, string message) => new MonsterDiagnostic(code, message, owner, owner + ":" + id, target, "Publish", prefix + field, "Use the current template frame metadata.");
            var basic = PresentationValidation.ValidateSkin(skin, (field, message) => Error("InvalidPresentation", field, message), "");
            if (basic != null) return basic;
            if (!info.CanMapAllFrames) return Error("TemplateFramesUnavailable", "templateFingerprint", info.Limitation);
            return null;
        }
    }
}
