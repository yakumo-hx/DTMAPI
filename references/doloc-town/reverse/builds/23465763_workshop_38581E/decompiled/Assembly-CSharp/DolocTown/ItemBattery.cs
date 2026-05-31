using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemBattery : Item
{
	public ItemBattery(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemBattery(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		Use();
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		Use();
	}

	private void Use()
	{
		Drone drone = DolocAPI.CurrentDrone;
		if (drone == null)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrEmptyDrone);
			return;
		}
		if (drone.structure.IsFull)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrDroneFullBattery);
			return;
		}
		CostSelf();
		DolocAPI.agent._Interact(delegate
		{
			ItemFunctionBattery itemFunctionBattery = base.proto.Function as ItemFunctionBattery;
			drone.Charge(itemFunctionBattery?.Power ?? 0);
			DolocAPI.GenerateDropItems(DolocAPI.CurrentRoom, DolocAPI.GlobalParameter.ItemRefOldBattery, DolocAPI.AgentPosition);
		});
	}
}
