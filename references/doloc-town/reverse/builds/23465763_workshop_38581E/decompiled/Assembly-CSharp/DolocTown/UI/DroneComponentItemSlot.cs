using DolocTown.Config;
using DolocTown.Config.Drone;

namespace DolocTown.UI;

public class DroneComponentItemSlot : DroneItemSlot
{
	public void SetType(ComponentType type)
	{
		txtEmpty.text = string.Format(DolocConfig.StaticTexts.DroneComponentTitle, DolocConfig.Tables.TbDroneSlot.GetOrDefault(type).Title);
	}
}
