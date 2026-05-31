using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemRescuePager : ItemAnimation
{
	public ItemRescuePager(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemRescuePager(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override bool CheckCondition()
	{
		if (!(DolocAPI.CurrentRoom is TemplateRoom))
		{
			return true;
		}
		DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.ItemRescuePagerConditionFailed);
		return false;
	}
}
