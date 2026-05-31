using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Animal;
using DolocTown.Config.Archives;
using DolocTown.Config.EnvOptimizer;
using DolocTown.Config.Fishing;
using DolocTown.Config.Monster;
using DolocTown.Config.NPC;
using DolocTown.Config.Resource;
using DolocTown.Config.UI;
using DolocTown.GameData;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class CollectionManager
{
	[JsonProperty]
	public readonly Dictionary<string, CollectionRecord> collections = new Dictionary<string, CollectionRecord>();

	[JsonProperty]
	public readonly Dictionary<string, CollectionRecord> creatureCollections = new Dictionary<string, CollectionRecord>();

	[JsonProperty]
	public readonly Dictionary<string, CollectionRecord> monsterCollections = new Dictionary<string, CollectionRecord>();

	[JsonProperty]
	public readonly Dictionary<string, CollectionRecord> resourceCollections = new Dictionary<string, CollectionRecord>();

	[JsonProperty]
	public readonly Dictionary<string, CollectionRecord> npcCollections = new Dictionary<string, CollectionRecord>();

	[JsonProperty]
	public HashSet<string> unlockCollectionFunc { get; private set; }

	public CollectionManager()
	{
		unlockCollectionFunc = new HashSet<string>();
	}

	[JsonConstructor]
	private CollectionManager(HashSet<string> unlockCollectionFunc, Dictionary<string, CollectionRecord> collections, Dictionary<string, CollectionRecord> creatureCollections, Dictionary<string, CollectionRecord> monsterCollections, Dictionary<string, CollectionRecord> resourceCollections, Dictionary<string, CollectionRecord> npcCollections)
	{
		this.unlockCollectionFunc = unlockCollectionFunc ?? new HashSet<string>();
		this.collections = collections ?? new Dictionary<string, CollectionRecord>();
		this.creatureCollections = creatureCollections ?? new Dictionary<string, CollectionRecord>();
		this.monsterCollections = monsterCollections ?? new Dictionary<string, CollectionRecord>();
		this.resourceCollections = resourceCollections ?? new Dictionary<string, CollectionRecord>();
		this.npcCollections = npcCollections ?? new Dictionary<string, CollectionRecord>();
	}

	public void AfterLoadData()
	{
		string title;
		foreach (AnimalDocumentInfo data in DolocConfig.Tables.TbAnimalDocument.DataList)
		{
			RefreshAnimalRecord(data.Id, out title);
		}
		foreach (FishDocumentInfo data2 in DolocConfig.Tables.TbFishDocument.DataList)
		{
			RefreshFishRecord(data2.Id, out title);
		}
		foreach (MonsterDocumentInfo data3 in DolocConfig.Tables.TbMonsterDocument.DataList)
		{
			RefreshMonsterRecord(data3.Id, out title);
		}
		foreach (NpcDocumentInfo data4 in DolocConfig.Tables.TbNpcDocument.DataList)
		{
			RefreshNpcRecord(data4.Id, out title);
		}
		foreach (ResourceDocumentInfo data5 in DolocConfig.Tables.TbResourceDocument.DataList)
		{
			RefreshResourceRecord(data5.Id, out title);
		}
	}

	public bool CheckCollectionFuncUnlocked(CompendiumLabel label)
	{
		return unlockCollectionFunc.Contains(DolocConfig.Tables.TbCompendiumMenu.GetById(label).Name);
	}

	public void UnlockCollectionFunc(CompendiumLabel label)
	{
		unlockCollectionFunc.Add(DolocConfig.Tables.TbCompendiumMenu.GetById(label).Name);
	}

	public bool ReadCollectionRecord(CompendiumLabel label, string id)
	{
		if (!GetRecord(label, id, out var record))
		{
			return false;
		}
		if (record.isRead || !record.isUnlock)
		{
			return false;
		}
		record.isRead = true;
		return true;
	}

	public void RecordCollectionInfo(CollectionType type, string id, bool popTip)
	{
		if ((type & CollectionType.Item) != 0)
		{
			collections.TryAdd(id, new CollectionRecord());
			collections[id].isUnlock = true;
		}
		if ((type & CollectionType.Animal) != 0)
		{
			creatureCollections.TryAdd(id, new CollectionRecord());
			creatureCollections[id].isUnlock = true;
			string title;
			int num = RefreshAnimalRecord(id, out title);
			for (int i = 0; i < num; i++)
			{
				DolocAPI.PerformEnvOptimizerBehaviour(EnvOptimizerBehaviourType.UNLOCK_ANIMAL);
			}
			if (num > 0 && popTip && CheckCollectionFuncUnlocked(CompendiumLabel.Creature))
			{
				DolocAPI.ShowMessageBoxNodeComplete(DolocUtils.Format(DolocConfig.StaticTexts.UiNpcFileUpdation, title));
			}
		}
		if ((type & CollectionType.Product) != 0)
		{
			string title2;
			int num2 = RefreshAnimalProductRecord(id, out title2);
			for (int j = 0; j < num2; j++)
			{
				DolocAPI.PerformEnvOptimizerBehaviour(EnvOptimizerBehaviourType.UNLOCK_ANIMAL);
			}
			if (num2 > 0 && popTip && CheckCollectionFuncUnlocked(CompendiumLabel.Creature))
			{
				DolocAPI.ShowMessageBoxNodeComplete(DolocUtils.Format(DolocConfig.StaticTexts.UiNpcFileUpdation, title2));
			}
		}
		if ((type & CollectionType.Fish) != 0)
		{
			creatureCollections.TryAdd(id, new CollectionRecord());
			creatureCollections[id].isUnlock = true;
			string title3;
			int num3 = RefreshFishRecord(id, out title3);
			for (int k = 0; k < num3; k++)
			{
				DolocAPI.PerformEnvOptimizerBehaviour(EnvOptimizerBehaviourType.UNLOCK_FISH);
			}
			if (num3 > 0 && popTip && CheckCollectionFuncUnlocked(CompendiumLabel.Creature))
			{
				DolocAPI.ShowMessageBoxNodeComplete(DolocUtils.Format(DolocConfig.StaticTexts.UiNpcFileUpdation, title3));
			}
		}
		if ((type & CollectionType.Monster) != 0)
		{
			monsterCollections.TryAdd(id, new CollectionRecord());
			monsterCollections[id].isUnlock = true;
			string title4;
			int num4 = RefreshMonsterRecord(id, out title4);
			for (int l = 0; l < num4; l++)
			{
				DolocAPI.PerformEnvOptimizerBehaviour(EnvOptimizerBehaviourType.UNLOCK_MONSTER);
			}
			if (num4 > 0 && popTip && CheckCollectionFuncUnlocked(CompendiumLabel.Monster))
			{
				DolocAPI.ShowMessageBoxNodeComplete(DolocUtils.Format(DolocConfig.StaticTexts.UiNpcFileUpdation, title4));
			}
		}
		if ((type & CollectionType.Resource) != 0)
		{
			resourceCollections.TryAdd(id, new CollectionRecord());
			resourceCollections[id].isUnlock = true;
			string title5;
			List<CustomizedDocumentNodeInfo> list = RefreshResourceRecord(id, out title5);
			foreach (CustomizedDocumentNodeInfo item in list)
			{
				DolocAPI.PerformEnvOptimizerBehaviour(EnvOptimizerBehaviourType.UNLOCK_RESOURCE, item);
			}
			if (list.Count > 0 && popTip && CheckCollectionFuncUnlocked(CompendiumLabel.Resource))
			{
				DolocAPI.ShowMessageBoxNodeComplete(DolocUtils.Format(DolocConfig.StaticTexts.UiNpcFileUpdation, title5));
			}
		}
		if ((type & CollectionType.Npc) != 0)
		{
			npcCollections.TryAdd(id, new CollectionRecord());
			bool isUnlock = npcCollections[id].isUnlock;
			npcCollections[id].isUnlock = true;
			if (RefreshNpcRecord(id, out var title6) > 0 && popTip && CheckCollectionFuncUnlocked(CompendiumLabel.Npc))
			{
				DolocAPI.ShowMessageBoxNodeComplete(isUnlock ? DolocUtils.Format(DolocConfig.StaticTexts.UiNpcFileUpdation, title6) : DolocUtils.Format(DolocConfig.StaticTexts.UiNpcVisit, title6));
			}
		}
	}

	public void UnlockNpcPartDocument(string npcName, string documentId)
	{
		npcCollections.TryAdd(npcName, new CollectionRecord());
		CollectionRecord collectionRecord = npcCollections[npcName];
		collectionRecord.isUnlock = true;
		if (!collectionRecord.documents.Contains(documentId))
		{
			collectionRecord.RefreshRecord(documentId);
			if (CheckCollectionFuncUnlocked(CompendiumLabel.Npc))
			{
				DolocAPI.ShowMessageBoxNodeComplete(DolocUtils.Format(DolocConfig.StaticTexts.UiNpcFileUpdation, DolocAPI.GetNpcTitle(npcName)));
			}
		}
	}

	public void UnlockMonsterPartDocument(string name, string documentId)
	{
		if (!DolocAPI.QueryMonsterDocument(name, out var document))
		{
			return;
		}
		monsterCollections.TryAdd(name, new CollectionRecord());
		CollectionRecord collectionRecord = monsterCollections[name];
		collectionRecord.isUnlock = true;
		if (!collectionRecord.documents.Contains(documentId))
		{
			collectionRecord.RefreshRecord(documentId);
			if (CheckCollectionFuncUnlocked(CompendiumLabel.Monster))
			{
				DolocAPI.ShowMessageBoxNodeComplete(DolocUtils.Format(DolocConfig.StaticTexts.UiNpcFileUpdation, document.Title));
			}
		}
	}

	public int GetMonsterDocumentCount()
	{
		int num = 0;
		foreach (MonsterDocumentInfo data in DolocConfig.Tables.TbMonsterDocument.DataList)
		{
			if (monsterCollections.TryGetValue(data.Id, out var record))
			{
				num += data.DocumentInfos.Count((DocumentNodeInfo info) => record.documents.Contains(info.Id));
			}
		}
		return num;
	}

	public int GetAnimalDocumentCount()
	{
		int num = 0;
		foreach (AnimalDocumentInfo data in DolocConfig.Tables.TbAnimalDocument.DataList)
		{
			if (creatureCollections.TryGetValue(data.Id, out var record))
			{
				num += data.DocumentInfos.Count((DocumentNodeInfo info) => record.documents.Contains(info.Id));
			}
		}
		return num;
	}

	public int GetFishDocumentCount()
	{
		int num = 0;
		foreach (FishDocumentInfo data in DolocConfig.Tables.TbFishDocument.DataList)
		{
			if (creatureCollections.TryGetValue(data.Id, out var record))
			{
				num += data.DocumentInfos.Count((DocumentNodeInfo info) => record.documents.Contains(info.Id));
			}
		}
		return num;
	}

	public int GetResourceDocumentCount()
	{
		int num = 0;
		foreach (ResourceDocumentInfo data in DolocConfig.Tables.TbResourceDocument.DataList)
		{
			if (resourceCollections.TryGetValue(data.Id, out var record))
			{
				num += data.DocumentInfos.Count((CustomizedDocumentNodeInfo info) => record.documents.Contains(info.Id));
			}
		}
		return num;
	}

	public List<CustomizedDocumentNodeInfo> GetResourceDocumentList()
	{
		List<CustomizedDocumentNodeInfo> list = new List<CustomizedDocumentNodeInfo>();
		foreach (ResourceDocumentInfo data in DolocConfig.Tables.TbResourceDocument.DataList)
		{
			if (resourceCollections.TryGetValue(data.Id, out var record))
			{
				list.AddRange(data.DocumentInfos.Where((CustomizedDocumentNodeInfo info) => record.documents.Contains(info.Id)));
			}
		}
		return list;
	}

	public bool GetRecord(CompendiumLabel label, string id, out CollectionRecord record)
	{
		return label switch
		{
			CompendiumLabel.Item => collections.TryGetValue(id, out record), 
			CompendiumLabel.Creature => creatureCollections.TryGetValue(id, out record), 
			CompendiumLabel.Monster => monsterCollections.TryGetValue(id, out record), 
			CompendiumLabel.Resource => resourceCollections.TryGetValue(id, out record), 
			CompendiumLabel.Npc => npcCollections.TryGetValue(id, out record), 
			_ => collections.TryGetValue(id, out record), 
		};
	}

	public int RefreshAnimalRecord(string name, out string title)
	{
		title = string.Empty;
		if (!creatureCollections.TryGetValue(name, out var value) || !value.isUnlock)
		{
			return 0;
		}
		if (!DolocAPI.QueryAnimalDocument(name, out var document))
		{
			return 0;
		}
		int num = 0;
		title = document.Id_Ref.Title;
		DocumentNodeInfo[] documentInfos = document.DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo in documentInfos)
		{
			if (value.documents.Contains(documentNodeInfo.Id))
			{
				continue;
			}
			switch (documentNodeInfo.DocumentType)
			{
			case DocumentType.DEFAULT:
				num++;
				value.RefreshRecord(documentNodeInfo.Id);
				break;
			case DocumentType.RAISING:
				if (DolocAPI.GetEventTriggerCount(GameEventType.ANIMAL_GROW_UP, name) >= documentNodeInfo.Value)
				{
					num++;
					value.RefreshRecord(documentNodeInfo.Id);
				}
				break;
			case DocumentType.BREEDING:
				if (DolocAPI.GetEventTriggerCount(GameEventType.ANIMAL_BIRTH, name) >= documentNodeInfo.Value)
				{
					num++;
					value.RefreshRecord(documentNodeInfo.Id);
				}
				break;
			case DocumentType.OBTAIN:
			{
				if (DolocAPI.GetAnimalAllProduce(name).Count((string itemName) => DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Item, itemName, out var record) && record.isUnlock) >= documentNodeInfo.Value)
				{
					num++;
					value.RefreshRecord(documentNodeInfo.Id);
				}
				break;
			}
			}
		}
		return num;
	}

	public int RefreshAnimalProductRecord(string name, out string title)
	{
		int num = 0;
		title = string.Empty;
		foreach (AnimalDocumentInfo data in DolocConfig.Tables.TbAnimalDocument.DataList)
		{
			List<string> source = DolocAPI.GetAnimalAllProduce(data.Id).ToList();
			DocumentNodeInfo[] documentInfos = data.DocumentInfos;
			foreach (DocumentNodeInfo documentNodeInfo in documentInfos)
			{
				if (creatureCollections.TryGetValue(data.Id, out var value) && value.isUnlock && !value.documents.Contains(documentNodeInfo.Id) && documentNodeInfo.DocumentType == DocumentType.OBTAIN && source.Count((string item) => DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Item, item, out var record) && record.isUnlock) >= documentNodeInfo.Value)
				{
					num++;
					title = data.Id_Ref.Title;
					value.RefreshRecord(documentNodeInfo.Id);
				}
			}
		}
		return num;
	}

	public int RefreshFishRecord(string name, out string title)
	{
		title = string.Empty;
		if (!creatureCollections.TryGetValue(name, out var value) || !value.isUnlock)
		{
			return 0;
		}
		if (!DolocAPI.QueryFishDocument(name, out var document))
		{
			return 0;
		}
		int num = 0;
		title = DolocAPI.GetItemTitle(name);
		DocumentNodeInfo[] documentInfos = document.DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo in documentInfos)
		{
			if (!value.documents.Contains(documentNodeInfo.Id))
			{
				switch (documentNodeInfo.DocumentType)
				{
				case DocumentType.DEFAULT:
					num++;
					value.RefreshRecord(documentNodeInfo.Id);
					break;
				}
			}
		}
		return num;
	}

	public int RefreshMonsterRecord(string name, out string title)
	{
		title = string.Empty;
		if (!monsterCollections.TryGetValue(name, out var value) || !value.isUnlock)
		{
			return 0;
		}
		if (!DolocAPI.QueryMonsterDocument(name, out var document))
		{
			return 0;
		}
		int num = 0;
		title = document.Title;
		DocumentNodeInfo[] documentInfos = document.DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo in documentInfos)
		{
			if (value.documents.Contains(documentNodeInfo.Id))
			{
				continue;
			}
			switch (documentNodeInfo.DocumentType)
			{
			case DocumentType.DEFAULT:
				num++;
				value.RefreshRecord(documentNodeInfo.Id);
				break;
			case DocumentType.KILL:
				if (DolocAPI.GetEventTriggerCount(GameEventType.SLAIN_MONSTER, name) >= documentNodeInfo.Value)
				{
					num++;
					value.RefreshRecord(documentNodeInfo.Id);
				}
				break;
			}
		}
		return num;
	}

	public int RefreshNpcRecord(string name, out string title)
	{
		title = string.Empty;
		if (!npcCollections.TryGetValue(name, out var value) || !value.isUnlock)
		{
			return 0;
		}
		if (!DolocConfig.Tables.TbNpcDocument.DataMap.TryGetValue(name, out var value2))
		{
			return 0;
		}
		int num = 0;
		title = DolocAPI.GetNpcTitle(name);
		DocumentNodeInfo[] documentInfos = value2.DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo in documentInfos)
		{
			if (value.documents.Contains(documentNodeInfo.Id))
			{
				continue;
			}
			switch (documentNodeInfo.DocumentType)
			{
			case DocumentType.DEFAULT:
				num++;
				value.RefreshRecord(documentNodeInfo.Id);
				break;
			case DocumentType.FAVORABILITY:
				if (DolocAPI.QueryNpcLikingLv(name) >= documentNodeInfo.Value)
				{
					num++;
					value.RefreshRecord(documentNodeInfo.Id);
				}
				break;
			}
		}
		return num;
	}

	public List<CustomizedDocumentNodeInfo> RefreshResourceRecord(string name, out string title)
	{
		title = string.Empty;
		List<CustomizedDocumentNodeInfo> list = new List<CustomizedDocumentNodeInfo>();
		if (!resourceCollections.TryGetValue(name, out var value) || !value.isUnlock)
		{
			return list;
		}
		if (!DolocAPI.QueryResourceDocument(name, out var document))
		{
			return list;
		}
		title = document.Title;
		CustomizedDocumentNodeInfo[] documentInfos = document.DocumentInfos;
		foreach (CustomizedDocumentNodeInfo customizedDocumentNodeInfo in documentInfos)
		{
			if (value.documents.Contains(customizedDocumentNodeInfo.Id))
			{
				continue;
			}
			switch (customizedDocumentNodeInfo.DocumentType)
			{
			case DocumentType.DEFAULT:
				list.Add(customizedDocumentNodeInfo);
				value.RefreshRecord(customizedDocumentNodeInfo.Id);
				break;
			case DocumentType.COLLECT:
				if (document.ResourceType switch
				{
					ResourceDocumentType.Resource => DolocAPI.GetEventTriggerCount(GameEventType.FELL_DUNGEON_RESOURCE, name), 
					ResourceDocumentType.Vegetation => DolocAPI.GetEventTriggerCount(GameEventType.GATHERING_VEGETATION_FRUIT, name), 
					_ => 0, 
				} >= customizedDocumentNodeInfo.Value)
				{
					list.Add(customizedDocumentNodeInfo);
					value.RefreshRecord(customizedDocumentNodeInfo.Id);
				}
				break;
			}
		}
		return list;
	}
}
