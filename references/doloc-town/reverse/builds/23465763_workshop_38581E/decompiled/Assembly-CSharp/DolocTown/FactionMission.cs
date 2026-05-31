using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class FactionMission
{
	public readonly FactionMissionInfo Proto;

	[JsonProperty]
	private bool moneyFinishState;

	public bool IsValid => Proto != null;

	[JsonProperty]
	[DebugInfo("声望任务名称", Color = "ffff00")]
	public string Id => Proto.Id;

	public int FamePoints => Proto.ReputationValue;

	public FactionType MainSeries => Proto.MainSeries;

	public FactionMissionType SubSeries => Proto.SubSeries;

	public bool MoneyFinishState
	{
		get
		{
			if (Proto.RequiredMoney > 0)
			{
				return moneyFinishState;
			}
			return true;
		}
	}

	public bool IsComplete
	{
		get
		{
			FactionItem[] factionItems = FactionItems;
			for (int i = 0; i < factionItems.Length; i++)
			{
				if (!factionItems[i].FinishState)
				{
					return false;
				}
			}
			return MoneyFinishState;
		}
	}

	[JsonProperty("factionItems")]
	public FactionItem[] FactionItems { get; private set; }

	public Reward[] Rewards { get; private set; }

	public FactionMission(FactionMissionInfo proto)
	{
		Proto = proto;
		moneyFinishState = Proto.RequiredMoney <= 0;
		InitFactionItems();
		InitRewards();
	}

	[JsonConstructor]
	private FactionMission(string Id, FactionItem[] factionItems, bool moneyFinishState)
	{
		Proto = DolocConfig.Tables.TbFactionMission.GetOrDefault(Id);
		if (Proto != null)
		{
			if (factionItems == null || factionItems.All((FactionItem x) => !x.FinishState))
			{
				InitFactionItems();
			}
			else
			{
				FactionItems = factionItems;
			}
			this.moneyFinishState = Proto.RequiredMoney <= 0 || moneyFinishState;
			InitRewards();
		}
	}

	private void InitFactionItems()
	{
		List<CountItem> requiredItems = Proto.RequiredItems;
		FactionItems = new FactionItem[requiredItems.Count];
		for (int i = 0; i < FactionItems.Length; i++)
		{
			FactionItems[i] = new FactionItem(requiredItems[i].itemName, requiredItems[i].itemCount);
		}
	}

	private void InitRewards()
	{
		List<RewardProto> extraRewards = Proto.ExtraRewards;
		Rewards = new Reward[extraRewards.Count];
		for (int i = 0; i < Rewards.Length; i++)
		{
			Rewards[i] = DolocAPI.CreateReward(extraRewards[i]);
		}
	}

	public bool CanSubmitFactionMissionItem(string itemName, int count)
	{
		for (int i = 0; i < FactionItems.Length; i++)
		{
			if (FactionItems[i].ItemName == itemName && !FactionItems[i].FinishState)
			{
				return true;
			}
		}
		return false;
	}

	public int MaxSubmitFactionMissionItemCount(string itemName)
	{
		FactionItem[] factionItems = FactionItems;
		foreach (FactionItem factionItem in factionItems)
		{
			if (factionItem.ItemName == itemName)
			{
				return factionItem.Count;
			}
		}
		return 0;
	}

	public void SubmitFactionMissionItem(string itemName)
	{
		for (int i = 0; i < FactionItems.Length; i++)
		{
			if (FactionItems[i].ItemName == itemName)
			{
				FactionItems[i].FinishState = true;
			}
		}
	}

	public void SubmitMoney()
	{
		moneyFinishState = true;
	}

	public void CashMissionReward()
	{
		Reward[] rewards = Rewards;
		for (int i = 0; i < rewards.Length; i++)
		{
			rewards[i].CashRewardOverflowAsEmail();
		}
	}
}
