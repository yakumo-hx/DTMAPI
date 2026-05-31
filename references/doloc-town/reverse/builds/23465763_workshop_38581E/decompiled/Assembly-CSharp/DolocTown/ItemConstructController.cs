using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Room;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemConstructController : Item, IActiveItem
{
	public ItemConstructController(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemConstructController(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	private void OnUse()
	{
		RoomConstructInfo roomConstructInfo = DolocAPI.CurrentRoom.RoomConstructInfo;
		if (roomConstructInfo.AllowBuildBuilding || roomConstructInfo.AllowBuildEquipment || roomConstructInfo.AllowBuildPlatform)
		{
			GlobalBuilderState.HandleStartUp();
		}
		else
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.FarmbuilderErrBuildSystemNotSupport);
		}
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		OnUse();
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		OnUse();
	}

	public override bool IsSame(Item other)
	{
		return false;
	}
}
