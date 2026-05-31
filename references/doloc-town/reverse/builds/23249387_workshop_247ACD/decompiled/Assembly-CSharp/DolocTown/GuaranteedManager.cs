using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Resource;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class GuaranteedManager
{
	[JsonProperty]
	private Dictionary<GuaranteedType, GuaranteedData> guaranteedDatas;

	[JsonConstructor]
	public GuaranteedManager(Dictionary<GuaranteedType, GuaranteedData> guaranteedDatas = null)
	{
		this.guaranteedDatas = new Dictionary<GuaranteedType, GuaranteedData>();
		foreach (GuaranteedType value2 in Enum.GetValues(typeof(GuaranteedType)))
		{
			if (guaranteedDatas != null && guaranteedDatas.TryGetValue(value2, out var value))
			{
				this.guaranteedDatas[value2] = value;
			}
			else
			{
				this.guaranteedDatas[value2] = new GuaranteedData(value2);
			}
		}
	}

	public void UnlockGuaranteedItem(GuaranteedType type, string spawnId)
	{
		GetGuaranteedData(type).UnlockId(spawnId);
	}

	private bool CheckGuaranteedItemUnlock(GuaranteedType type, string spawnId)
	{
		return GetGuaranteedData(type).CheckGuaranteedItemUnlock(spawnId);
	}

	private bool CheckWithinGlobalLimit(GuaranteedType type, string spawnId)
	{
		return GetGuaranteedData(type).CheckWithinGlobalLimit(spawnId);
	}

	private void TryAddGlobalCount(GuaranteedType type, string spawnId, int count, out int validCount)
	{
		GetGuaranteedData(type).TryAddGlobalCount(spawnId, count, out validCount);
	}

	public GuaranteedData GetGuaranteedData(GuaranteedType type)
	{
		return guaranteedDatas[type];
	}

	private HashSet<string> GetGuaranteedIds(GuaranteedType type, HashSet<string> spawnedIds, HashSet<string> allIds)
	{
		HashSet<string> hashSet = new HashSet<string>();
		GuaranteedData guaranteedData = GetGuaranteedData(type);
		foreach (string allId in allIds)
		{
			if (spawnedIds.Contains(allId))
			{
				guaranteedData.ResetGuaranteedId(allId);
				guaranteedData.TickActiveCount(allId, force: false);
			}
			else if (guaranteedData.TickGuaranteedId(allId))
			{
				hashSet.Add(allId);
			}
		}
		return hashSet;
	}

	public void SpawnResourceDropItems(DungeonResource resource, bool isRender, string overrideSpawnLut)
	{
		if (resource?.Host == null)
		{
			return;
		}
		IDropItemHost dropItemHost = (IDropItemHost)resource.Host;
		Vector3 position = resource.Position;
		ItemSpawnEntry dropSpawnEntry = resource.currentLevelData.DropSpawnEntry;
		int num = dropSpawnEntry.CountRange.RandomCount;
		ItemSpawnInfo itemSpawnInfo = DolocConfig.Tables.TbItemSpawn.GetOrDefault(overrideSpawnLut ?? "") ?? dropSpawnEntry.SpawnLut_Ref;
		switch (resource.ResourceType)
		{
		case DungeonResourceType.TREE:
		case DungeonResourceType.TREE_TRUNK:
			num = DolocAPI.AbilitySystem.collectionAbility.GetCollectionWoodsCount(num);
			break;
		case DungeonResourceType.ORE:
		case DungeonResourceType.SAND:
			num = DolocAPI.AbilitySystem.collectionAbility.GetCollectionStoneCount(num);
			break;
		case DungeonResourceType.WEEDS:
		case DungeonResourceType.WEEDS_SMALL:
			num = DolocAPI.AbilitySystem.collectionAbility.GetCollectionWeedsCount(num);
			break;
		case DungeonResourceType.MECHANICAL_REMAINS:
			num = DolocAPI.AbilitySystem.collectionAbility.GetCollectionGarbageCount(num);
			break;
		}
		CountItem[] array = itemSpawnInfo.SpawnItems(num, (ItemSpawnData spawnData) => IsValidResourceId(spawnData.SpawnId));
		for (int i = 0; i < array.Length; i++)
		{
			CountItem countItem = array[i];
			TryAddGlobalCount(GuaranteedType.Resource, countItem.itemName, countItem.itemCount, out var validCount);
			array[i].itemCount = validCount;
		}
		if (isRender)
		{
			position.y += DolocAPI.eftConfig.dungeonResourceDropItemPopYOffset;
			float num2 = (float)resource.Proto.Width / 3f * 1.5f - 0.1f;
			CountItem[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				CountItem countItem2 = array2[j];
				for (int k = 0; k < countItem2.itemCount; k++)
				{
					dropItemHost.CreateDropItem(countItem2.itemName, position, shouldSendMsg: true, UnityEngine.Random.Range(0f - num2, num2));
				}
			}
		}
		else
		{
			dropItemHost.CreateDropItemsNoRender(array, position, shouldSendMessage: true);
		}
		foreach (string guaranteedId in GetGuaranteedIds(GuaranteedType.Resource, array.Select((CountItem x) => x.itemName).ToHashSet(), itemSpawnInfo.SpawnDatas.Select((ItemSpawnData x) => x.ItemName).Where(IsValidResourceId).ToHashSet()))
		{
			TryAddGlobalCount(GuaranteedType.Resource, guaranteedId, 1, out var validCount2);
			if (validCount2 > 0)
			{
				Debug.Log(("触发资源采集保底：" + guaranteedId).Colored(DolocUiColor.EYECATCHCOLOR_PURPLE));
				if (isRender)
				{
					position.y += DolocAPI.eftConfig.dungeonResourceDropItemPopYOffset;
					float num3 = (float)resource.Proto.Width / 3f * 1.5f - 0.1f;
					dropItemHost.CreateDropItem(guaranteedId, position, shouldSendMsg: true, UnityEngine.Random.Range(0f - num3, num3));
				}
				else
				{
					dropItemHost.CreateDropItemNoRender(guaranteedId, position, shouldSendMsg: true);
				}
			}
		}
	}

	private bool IsValidResourceId(string id)
	{
		if (CheckWithinGlobalLimit(GuaranteedType.Resource, id))
		{
			return CheckGuaranteedItemUnlock(GuaranteedType.Resource, id);
		}
		return false;
	}

	public void SpawnMonsterDropItems(Monster monster)
	{
		if (monster?.Host == null)
		{
			return;
		}
		IDropItemHost dropItemHost = (IDropItemHost)monster.Host;
		ItemSpawnEntry dropSpawnEntry = monster.proto.DropSpawnEntry;
		Vector3 position = monster.Controller.position;
		if (dropSpawnEntry?.SpawnLut_Ref == null)
		{
			return;
		}
		CountItem[] array = dropSpawnEntry.SpawnLut_Ref.SpawnItems(dropSpawnEntry.CountRange.MinCount, dropSpawnEntry.CountRange.MaxCount, (ItemSpawnData spawnData) => IsValidMonsterDropId(spawnData.SpawnId));
		for (int i = 0; i < array.Length; i++)
		{
			CountItem countItem = array[i];
			TryAddGlobalCount(GuaranteedType.Monster, countItem.itemName, countItem.itemCount, out var validCount);
			array[i].itemCount = validCount;
		}
		dropItemHost.CreateDropItemAnimated(array, position, shouldSendMsg: true);
		foreach (string guaranteedId in GetGuaranteedIds(GuaranteedType.Monster, array?.Select((CountItem x) => x.itemName).ToHashSet(), dropSpawnEntry.SpawnLut_Ref.SpawnDatas.Select((ItemSpawnData x) => x.ItemName).Where(IsValidMonsterDropId).ToHashSet()))
		{
			TryAddGlobalCount(GuaranteedType.Monster, guaranteedId, 1, out var validCount2);
			if (validCount2 > 0)
			{
				Debug.Log(("触发怪物掉落保底：" + guaranteedId).Colored(DolocUiColor.EYECATCHCOLOR_PURPLE));
				dropItemHost.CreateDropItemAnimated(new CountItem(guaranteedId, 1), position, shouldSendMsg: true);
			}
		}
	}

	private bool IsValidMonsterDropId(string id)
	{
		if (CheckWithinGlobalLimit(GuaranteedType.Monster, id))
		{
			return CheckGuaranteedItemUnlock(GuaranteedType.Monster, id);
		}
		return false;
	}
}
