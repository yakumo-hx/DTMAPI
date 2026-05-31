using DolocTown.Config.Equipment;

namespace DolocTown;

public interface ILamp
{
	LampController LampController { get; }

	LampInfo LampInfo => LampController.lampInfo;

	bool ShouldLight { get; }

	void RefreshLightIntensity()
	{
		LampController.RefreshLightIntensity();
	}
}
