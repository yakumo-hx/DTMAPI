namespace DolocTown.UI;

public class ContainerSocketUI : InventoryPanel
{
	protected override int GetLineCapacity(int total)
	{
		return DolocAPI.GlobalParameter.FishTankMaxCountOfSlots;
	}
}
