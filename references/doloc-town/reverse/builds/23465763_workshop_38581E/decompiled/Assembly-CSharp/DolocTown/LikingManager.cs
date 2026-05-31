using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.NPC;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class LikingManager
{
	[JsonProperty]
	private Dictionary<string, Liking> likingsLut = new Dictionary<string, Liking>();

	private List<string> celebrationOrder = new List<string>();

	private int Ceiling => DolocAPI.GlobalParameter.LikingCeiling;

	public string[] CelebrationOrder => celebrationOrder.ToArray();

	public LikingManager()
	{
		likingsLut.Clear();
		foreach (NpcInfo data in DolocConfig.Tables.TbNpc.DataList)
		{
			likingsLut.Add(data.Id, new Liking());
		}
	}

	[JsonConstructor]
	public LikingManager(Dictionary<string, Liking> likingsLut)
	{
		this.likingsLut = likingsLut;
		foreach (NpcInfo data in DolocConfig.Tables.TbNpc.DataList)
		{
			this.likingsLut.TryAdd(data.Id, new Liking());
		}
	}

	public void ReceiveGift(string npcName, string itemName, bool isBirthday)
	{
		float num = DolocConfig.Tables.TbLikingLevelMap.Get(QueryGiftLevel(npcName, itemName)).Value;
		if (isBirthday)
		{
			num = Mathf.Max(0f, num * DolocAPI.GlobalParameter.LikingRatioOnBirthday);
		}
		AddTargetNpcLikingValue(npcName, num);
		likingsLut[npcName].giftRecord.Add(itemName);
		if (!isBirthday)
		{
			likingsLut[npcName].giftCount++;
		}
		if (likingsLut[npcName].extraGiftCount > 0)
		{
			likingsLut[npcName].extraGiftCount--;
		}
	}

	public int QueryGiftLevel(string npcName, string itemName)
	{
		int value = 0;
		if (DolocConfig.Tables.TbNpcLiking.DataMap.TryGetValue(itemName, out var value2))
		{
			value2.ItemMap.TryGetValue(npcName, out value);
		}
		return value;
	}

	public int QueryNpcLikingLv(string npcName)
	{
		if (!likingsLut.TryGetValue(npcName, out var value))
		{
			return 0;
		}
		return value.LikingLevel;
	}

	public int QueryNpcLikingValue(string npcName)
	{
		if (!likingsLut.TryGetValue(npcName, out var value))
		{
			return 0;
		}
		return (int)value.likingValue;
	}

	public bool QueryNpcLikingInfo(string npcName, out Liking liking)
	{
		return likingsLut.TryGetValue(npcName, out liking);
	}

	public void AddTargetNpcLikingValue(string npcName, float value)
	{
		if (likingsLut.TryGetValue(npcName, out var value2))
		{
			int likingLevel = value2.LikingLevel;
			value2.likingValue = Math.Clamp(value2.likingValue + value, 0f, DolocAPI.GetNpcLikingLvLimit(npcName) * Ceiling);
			if (value2.LikingLevel > likingLevel)
			{
				DolocAPI.BroadcastString(GameEventType.NPC_LIKING_RISE, npcName);
				DolocAPI.archiveHandle.RecordCollection(CollectionType.Npc, npcName);
				DolocAPI.ShowMessageBoxNode(LocSprites.UI_INFOICON_LOVE_24PX, string.Format(DolocConfig.StaticTexts.UiNpcLikingRise, DolocAPI.GetNpcTitle(npcName)));
			}
			else if (value2.LikingLevel < likingLevel)
			{
				DolocAPI.ShowMessageBoxNode(LocSprites.UI_INFOICON_HATE_24PX, string.Format(DolocConfig.StaticTexts.UiNpcLikingDecline, DolocAPI.GetNpcTitle(npcName)));
			}
			DolocAPI.CurrentRoom?.SceneHandle?.RefreshCondition();
		}
	}

	public void AddLikingByGreeting(string npcName)
	{
		QueryNpcLikingInfo(npcName, out var liking);
		if (!liking.haveSaid)
		{
			liking.haveSaid = true;
			AddTargetNpcLikingValue(npcName, DolocAPI.GlobalParameter.LikingFirstGreetingDay);
		}
	}

	public void RefreshNpcCelebrationOrder()
	{
		celebrationOrder = (from key in likingsLut.Keys.Where(delegate(string key)
			{
				NpcInfo orDefault2 = DolocConfig.Tables.TbNpc.GetOrDefault(key);
				return orDefault2 != null && orDefault2.OrderInBirthdayWisher >= 0;
			})
			orderby likingsLut[key].likingValue descending
			select key).ThenByDescending(delegate(string key)
		{
			if (likingsLut[key].likingValue == 0f)
			{
				NpcInfo orDefault = DolocConfig.Tables.TbNpc.GetOrDefault(key);
				if (orDefault.OrderInBirthdayWisher <= 0)
				{
					return UnityEngine.Random.Range(-1000, 0);
				}
				return orDefault.OrderInBirthdayWisher;
			}
			return UnityEngine.Random.Range(-1000, 0);
		}).ToList();
	}

	public string GetCelebrationNpcByRank(int rank, int ignoreAmbienceCount = -1)
	{
		if (celebrationOrder.IsNullOrEmpty())
		{
			RefreshNpcCelebrationOrder();
		}
		if (celebrationOrder.Count < rank + 1)
		{
			return "";
		}
		if (ignoreAmbienceCount < 0)
		{
			return celebrationOrder[rank];
		}
		int num = 0;
		foreach (string item in celebrationOrder)
		{
			if (num >= ignoreAmbienceCount)
			{
				NpcInfo orDefault = DolocConfig.Tables.TbNpc.GetOrDefault(item);
				if (orDefault == null || !orDefault.IsBirthdayAmbience)
				{
					continue;
				}
			}
			if (num >= rank)
			{
				return item;
			}
			num++;
		}
		return "";
	}

	public void DailyRefresh()
	{
		foreach (Npc allNpc in DolocAPI.archiveHandle.cityData.npcManager.AllNpcs)
		{
			QueryNpcLikingInfo(allNpc.NpcName, out var liking);
			liking.haveSaid = false;
			liking.isParticipateActivity = false;
			DolocAPI.AddDialogueNode(allNpc.proto.GiftDialogue);
		}
	}

	public void WeeklyRefresh()
	{
		foreach (Liking value in likingsLut.Values)
		{
			value.giftCount = 0;
		}
	}

	public void AddExtraGiftCountToAllNpcs(int count)
	{
		foreach (Liking value in likingsLut.Values)
		{
			value.extraGiftCount = count;
		}
	}

	public void ClearExtraGiftCount()
	{
		foreach (Liking value in likingsLut.Values)
		{
			value.extraGiftCount = 0;
		}
	}
}
