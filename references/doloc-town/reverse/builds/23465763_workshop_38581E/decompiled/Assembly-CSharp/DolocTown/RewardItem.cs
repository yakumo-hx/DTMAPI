using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Mission;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class RewardItem : Reward
{
	[JsonProperty]
	public readonly string itemName;

	[JsonProperty]
	public readonly int itemCount;

	public override Sprite RewardIcon => DolocAPI.GetItemSprite(itemName);

	public override int Count => itemCount;

	public override string RewardInfo => DolocUtils.Format(DolocConfig.StaticTexts.RewardInfoItem, DolocAPI.GetItemTitle(itemName), itemCount);

	public override string RewardBriefInfo => DolocUtils.Format(DolocConfig.StaticTexts.UiItemTip, DolocAPI.GetItemTitle(itemName), itemCount);

	[JsonConstructor]
	public RewardItem(string itemName, int itemCount, bool overflowAsEmail = false)
		: base(RewardType.ITEM)
	{
		this.itemName = itemName;
		this.itemCount = itemCount;
	}

	public override bool CheckValid()
	{
		ItemInfo proto;
		if (itemCount > 0 && !string.IsNullOrEmpty(itemName))
		{
			return DolocAPI.QueryItemProto(itemName, out proto);
		}
		return false;
	}

	public override bool CashReward()
	{
		if (itemCount <= 0)
		{
			return false;
		}
		ItemFactory.GenerateItem(itemName, itemCount, out var item);
		if (item.disposable)
		{
			DolocAPI.PlaceInBackpackOrGenerateDropItem(item);
			return true;
		}
		if (DolocAPI.TryPlaceInBackpack(itemName, itemCount))
		{
			return true;
		}
		DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiErrBackpackIsFull);
		return false;
	}

	public override bool CashRewardOverflowAsEmail(string emailTemplate = "send_item_template")
	{
		if (itemCount <= 0)
		{
			return false;
		}
		DolocAPI.TryPlaceInBackpack(itemName, itemCount, sendEmailOnOverflow: true);
		return true;
	}

	public override bool TryCombine(Reward other, out Reward reward)
	{
		if (!(other is RewardItem rewardItem) || rewardItem.itemName != itemName)
		{
			reward = null;
			return false;
		}
		reward = new RewardItem(itemName, itemCount + rewardItem.itemCount);
		return true;
	}
}
