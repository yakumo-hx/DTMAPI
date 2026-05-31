using DolocTown.Config;
using DolocTown.Config.Fishing;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class ItemFishFry : Item
{
	private const string DefaultFish = "fish";

	[DebugInfo("成长阈值")]
	private int growthDuration;

	[JsonProperty]
	[DebugInfo("鱼ID", AllowEdit = true)]
	[DebugOnValueChanged("OnNameChanged")]
	public string fishName { get; private set; } = "fish";


	private TbFarmFish FarmFish => DolocConfig.Tables.TbFarmFish;

	public override string title
	{
		get
		{
			if (!FarmFish.DataMap.TryGetValue(fishName, out var _))
			{
				return base.title;
			}
			return DolocUtils.Format(DolocConfig.StaticTexts.ItemFishFryTitleFormat, DolocAPI.GetItemTitle(fishName));
		}
	}

	[JsonProperty]
	[DebugInfo("成长值", AllowEdit = true)]
	public int growth { get; private set; }

	[DebugInfo("成长进度")]
	[DebugProgressBar(null)]
	private float GrowthProgress
	{
		get
		{
			if (growthDuration <= 0)
			{
				return 0f;
			}
			return Mathf.Clamp01((float)growth / (float)growthDuration);
		}
	}

	public bool IsFullyGrown => growth >= growthDuration;

	private void OnNameChanged(string fishName)
	{
		SetFishName(fishName);
	}

	public ItemFishFry(ItemInfo proto, int count)
		: base(proto, count)
	{
		SetFishName("fish");
	}

	[JsonConstructor]
	protected ItemFishFry(string itemName, int itemCount, string fishName, int growth)
		: base(itemName, itemCount)
	{
		this.fishName = fishName ?? "fish";
		this.growth = Mathf.Max(0, growth);
		ResolveStatus();
	}

	public Item ToFishItem()
	{
		return DolocAPI.GenerateItem(fishName);
	}

	public void SetFishName(string fishName)
	{
		if (!fishName.IsNullOrEmpty())
		{
			if (!FarmFish.DataMap.TryGetValue(fishName, out var value))
			{
				fishName = "fish";
				value = FarmFish.DataMap[fishName];
			}
			this.fishName = fishName;
			growthDuration = value.GrowDuration;
			growth = 0;
		}
	}

	public void ResolveStatus()
	{
		if (fishName.IsNullOrEmpty() || !FarmFish.DataMap.ContainsKey(fishName))
		{
			fishName = "fish";
		}
		FarmFishInfo farmFishInfo = FarmFish.DataMap[fishName];
		growthDuration = farmFishInfo.GrowDuration;
	}

	public bool Grow()
	{
		return ++growth >= growthDuration;
	}

	public override bool IsSame(Item other)
	{
		if (other is ItemFishFry itemFishFry)
		{
			if (itemFishFry.fishName == fishName)
			{
				return itemFishFry.growth == growth;
			}
			return false;
		}
		return false;
	}

	public override Item Clone(int count)
	{
		return new ItemFishFry(base.proto, count)
		{
			fishName = fishName,
			growth = growth,
			growthDuration = growthDuration
		};
	}
}
