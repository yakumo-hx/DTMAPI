using UnityEngine;

namespace DolocTown.UI;

public class DroneEquipmentBar : DolocUiObject
{
	[SerializeField]
	public DroneBar droneBar;

	[SerializeField]
	public MotorBar motorBar;

	public void RenderDroneBar(DroneBarData data)
	{
		droneBar.Render(data);
	}

	public void RenderMotorBar(bool unlock, string place)
	{
		motorBar.Render(unlock, place);
	}
}
