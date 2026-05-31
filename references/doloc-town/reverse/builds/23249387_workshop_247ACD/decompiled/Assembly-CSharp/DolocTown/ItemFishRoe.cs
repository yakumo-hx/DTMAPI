using DolocTown.Config;
using DolocTown.Config.Fishing;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class ItemFishRoe : Item
{
	private int incubateDuration;

	[JsonProperty]
	[DebugInfo("鱼ID", AllowEdit = true)]
	[DebugOnValueChanged("ResolveStatus")]
	public string fishName { get; private set; } = "fish";


	[JsonProperty]
	[DebugInfo("孵化值", AllowEdit = true)]
	public int incubation { get; private set; }

	[DebugInfo("孵化进度")]
	[DebugProgressBar(null)]
	private float IncubateProgress
	{
		get
		{
			if (incubateDuration <= 0)
			{
				return 0f;
			}
			return Mathf.Clamp01((float)incubation / (float)incubateDuration);
		}
	}

	public int IncubateGap => Mathf.Max(0, incubateDuration - incubation);

	public bool IsFullyIncubated => incubation >= incubateDuration;

	public ItemFishRoe(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemFishRoe(string itemName, int itemCount, string fishName, int growth)
		: base(itemName, itemCount)
	{
		this.fishName = fishName ?? "fish";
		incubation = Mathf.Max(0, growth);
		ResolveStatus();
	}

	public void SetFishName(string fishName)
	{
		if (!fishName.IsNullOrEmpty())
		{
			if (!DolocConfig.Tables.TbFarmFish.DataMap.TryGetValue(fishName, out var value))
			{
				fishName = "fish";
				value = DolocConfig.Tables.TbFarmFish.DataMap[fishName];
			}
			this.fishName = fishName;
			incubateDuration = value.IncubateDuration;
			incubation = 0;
		}
	}

	public void ResolveStatus()
	{
		if (fishName.IsNullOrEmpty() || !DolocConfig.Tables.TbFarmFish.DataMap.ContainsKey(fishName))
		{
			fishName = "fish";
		}
		FarmFishInfo farmFishInfo = DolocConfig.Tables.TbFarmFish.DataMap[fishName];
		incubateDuration = farmFishInfo.IncubateDuration;
	}

	public bool Incubate()
	{
		return ++incubation >= incubateDuration;
	}

	public ItemFishFry ToFry()
	{
		Item item = DolocAPI.GenerateItem(DolocAPI.GlobalParameter.ItemRefFishFry);
		if (item == null)
		{
			return null;
		}
		ItemFishFry obj = (ItemFishFry)item;
		obj.SetFishName(fishName);
		return obj;
	}

	public override bool IsSame(Item other)
	{
		if (other is ItemFishRoe itemFishRoe)
		{
			if (itemFishRoe.fishName == fishName)
			{
				return itemFishRoe.incubation == incubation;
			}
			return false;
		}
		return false;
	}

	public override Item Clone(int count)
	{
		return new ItemFishRoe(base.proto, count)
		{
			fishName = fishName,
			incubation = incubation,
			incubateDuration = incubateDuration
		};
	}
}
