using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemSleepingBag : ItemAnimation
{
	public ItemSleepingBag(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemSleepingBag(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override bool CheckCondition()
	{
		if (((IMonsterHost)DolocAPI.CurrentRoom).MonsterCount > 0)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.ItemSleepingBagConditionFailedMonster);
			return false;
		}
		if (DolocAPI.CurrentWater != null && DolocAPI.CurrentWater.isTouched)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.ItemSleepingBagConditionFailedWater);
			return false;
		}
		return true;
	}
}
