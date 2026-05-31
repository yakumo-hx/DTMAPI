using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Automate;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class AutomateParamFilling : AutomateParam
{
	[JsonProperty]
	[DebugInfo("燃料参数")]
	private FillingParams fuelParams;

	[JsonProperty]
	[DebugInfo("小动物饲料参数")]
	private FillingParams animalFeedParams;

	[JsonProperty]
	[DebugInfo("鱼饲料参数")]
	private FillingParams fishFeedParams;

	public float LowFuelThreshold
	{
		get
		{
			return fuelParams.threshold;
		}
		set
		{
			fuelParams.threshold = Mathf.Clamp01(value);
		}
	}

	public bool FilterFuelContainerSkin
	{
		get
		{
			return fuelParams.filterContainerSkin;
		}
		set
		{
			fuelParams.filterContainerSkin = value;
		}
	}

	public HashSet<int> FuelContainerSkinSet => fuelParams.containerSkinSet;

	public float LowAnimalFeederThreshold
	{
		get
		{
			return animalFeedParams.threshold;
		}
		set
		{
			animalFeedParams.threshold = Mathf.Clamp01(value);
		}
	}

	public bool FilterAnimalFeedsContainerSkin
	{
		get
		{
			return animalFeedParams.filterContainerSkin;
		}
		set
		{
			animalFeedParams.filterContainerSkin = value;
		}
	}

	public HashSet<int> AnimalFeedsContainerSkinSet => animalFeedParams.containerSkinSet;

	public float LowFishFeederThreshold
	{
		get
		{
			return fishFeedParams.threshold;
		}
		set
		{
			fishFeedParams.threshold = Mathf.Clamp01(value);
		}
	}

	public bool FilterFishFeedsContainerSkin
	{
		get
		{
			return fishFeedParams.filterContainerSkin;
		}
		set
		{
			fishFeedParams.filterContainerSkin = value;
		}
	}

	public HashSet<int> FishFeedsContainerSkinIdx => fishFeedParams.containerSkinSet;

	public override void LoadDefault()
	{
		AutomateBotFunctionFilling automateBotFunctionFilling = (AutomateBotFunctionFilling)base.Bot.proto.Function;
		fuelParams = new FillingParams(automateBotFunctionFilling.DefaultFuelType, automateBotFunctionFilling.FuelThreshold);
		animalFeedParams = new FillingParams(automateBotFunctionFilling.DefaultAnimalFeedsType, automateBotFunctionFilling.AnimalFeedsThreshold);
		fishFeedParams = new FillingParams(automateBotFunctionFilling.DefaultFishFeedsType, automateBotFunctionFilling.FishFeedsThreshold);
	}

	public AutomateParamFilling(AutomateBot bot)
		: base(bot)
	{
	}

	[JsonConstructor]
	private AutomateParamFilling(FillingParams fuelParams, FillingParams animalFeedParams, FillingParams fishFeedParams)
	{
		this.fuelParams = fuelParams;
		this.animalFeedParams = animalFeedParams;
		this.fishFeedParams = fishFeedParams;
	}

	public override void AfterLoadAutomateBot(AutomateBot bot)
	{
		AutomateBotFunctionFilling automateBotFunctionFilling = (AutomateBotFunctionFilling)bot.proto.Function;
		if (fuelParams == null)
		{
			fuelParams = new FillingParams(automateBotFunctionFilling.DefaultFuelType, automateBotFunctionFilling.FuelThreshold);
		}
		if (animalFeedParams == null)
		{
			animalFeedParams = new FillingParams(automateBotFunctionFilling.DefaultAnimalFeedsType, automateBotFunctionFilling.AnimalFeedsThreshold);
		}
		if (fishFeedParams == null)
		{
			fishFeedParams = new FillingParams(automateBotFunctionFilling.DefaultFishFeedsType, automateBotFunctionFilling.FishFeedsThreshold);
		}
	}

	public bool IsFuel(Item item)
	{
		if (item != null && item.proto.ElectricEnergy > 0)
		{
			return item.subType.Id == fuelParams.itemSubType;
		}
		return false;
	}

	public bool IsFuelContainer(Case container)
	{
		if (container != null && container.labels.Contains("material"))
		{
			if (fuelParams.filterContainerSkin)
			{
				return fuelParams.containerSkinSet.Contains(container.skinIndex);
			}
			return true;
		}
		return false;
	}

	public bool IsAnimalFeeds(Item item)
	{
		int energy;
		if (item != null && item.subType.Id == animalFeedParams.itemSubType)
		{
			return DolocConfig.Tables.TbFeed.IsFeederFeeds(item.name, out energy);
		}
		return false;
	}

	public bool IsAnimalFeedsContainer(Case container)
	{
		if (container != null && container.labels.Contains("husbandry"))
		{
			if (animalFeedParams.filterContainerSkin)
			{
				return animalFeedParams.containerSkinSet.Contains(container.skinIndex);
			}
			return true;
		}
		return false;
	}

	public bool IsFishFeeds(Item item)
	{
		int energy;
		if (item != null && item.subType.Id == fishFeedParams.itemSubType)
		{
			return DolocConfig.Tables.TbFishFeed.IsFishFeeds(item.name, out energy);
		}
		return false;
	}

	public bool IsFishFeedsContainer(Case container)
	{
		if (container != null && container.labels.Contains("husbandry"))
		{
			if (fishFeedParams.filterContainerSkin)
			{
				return fishFeedParams.containerSkinSet.Contains(container.skinIndex);
			}
			return true;
		}
		return false;
	}
}
