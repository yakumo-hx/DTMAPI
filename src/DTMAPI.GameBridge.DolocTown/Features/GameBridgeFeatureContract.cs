using System;
using System.Globalization;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class GameBridgeFeatureContract
    {
        public GameBridgeFeatureContract(
            string featureId,
            bool requiresSave,
            bool allowsTitleScreen,
            bool requiresNativeScene,
            bool requiresUi,
            bool environmentResetSensitive,
            bool hasSaveLifetimeState,
            bool hasTitleLifetimeState,
            bool canAutoPauseAfterFailure)
        {
            FeatureId = featureId ?? string.Empty;
            RequiresSave = requiresSave;
            AllowsTitleScreen = allowsTitleScreen;
            RequiresNativeScene = requiresNativeScene;
            RequiresUi = requiresUi;
            EnvironmentResetSensitive = environmentResetSensitive;
            HasSaveLifetimeState = hasSaveLifetimeState;
            HasTitleLifetimeState = hasTitleLifetimeState;
            CanAutoPauseAfterFailure = canAutoPauseAfterFailure;
        }

        public string FeatureId { get; }

        public bool RequiresSave { get; }

        public bool AllowsTitleScreen { get; }

        public bool RequiresNativeScene { get; }

        public bool RequiresUi { get; }

        public bool EnvironmentResetSensitive { get; }

        public bool HasSaveLifetimeState { get; }

        public bool HasTitleLifetimeState { get; }

        public bool CanAutoPauseAfterFailure { get; }

        public string Format()
        {
            return "feature=" + SingleLine(FeatureId) +
                ", requiresSave=" + RequiresSave.ToString(CultureInfo.InvariantCulture) +
                ", allowsTitleScreen=" + AllowsTitleScreen.ToString(CultureInfo.InvariantCulture) +
                ", requiresNativeScene=" + RequiresNativeScene.ToString(CultureInfo.InvariantCulture) +
                ", requiresUi=" + RequiresUi.ToString(CultureInfo.InvariantCulture) +
                ", environmentResetSensitive=" + EnvironmentResetSensitive.ToString(CultureInfo.InvariantCulture) +
                ", saveLifetimeState=" + HasSaveLifetimeState.ToString(CultureInfo.InvariantCulture) +
                ", titleLifetimeState=" + HasTitleLifetimeState.ToString(CultureInfo.InvariantCulture) +
                ", canAutoPauseAfterFailure=" + CanAutoPauseAfterFailure.ToString(CultureInfo.InvariantCulture);
        }

        public string Validate(string actualId)
        {
            if (string.IsNullOrWhiteSpace(FeatureId))
                return "missing feature id";

            if (!FeatureId.Equals(actualId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                return "contract id '" + FeatureId + "' does not match feature id '" + (actualId ?? string.Empty) + "'";

            return string.Empty;
        }

        private static string SingleLine(string value)
        {
            return (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
