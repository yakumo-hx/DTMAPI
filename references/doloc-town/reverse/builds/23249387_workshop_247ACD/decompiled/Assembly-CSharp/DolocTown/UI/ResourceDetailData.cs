using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DolocTown.Config;
using DolocTown.Config.Archives;
using DolocTown.Config.Item;
using DolocTown.Config.Resource;
using DolocTown.Config.UI;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct ResourceDetailData : IUIData
{
	public bool notEmpty { get; }

	public bool isVisited { get; }

	public bool display { get; }

	public bool isLeftRightLayout { get; }

	public Sprite icon { get; }

	public string title { get; }

	public int collectCount { get; }

	public string habitat { get; }

	public string growthPeriod { get; }

	public string[] itemNames { get; }

	public Sprite[] dropItems { get; }

	public bool[] obtained { get; }

	public string documentContent { get; }

	public ResourceDetailData(string resourceId)
	{
		this = default(ResourceDetailData);
		if (!DolocAPI.QueryResourceDocument(resourceId, out var document))
		{
			return;
		}
		notEmpty = true;
		display = document.Display;
		isLeftRightLayout = document.LeftRightLayout;
		if (!DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Resource, resourceId, out var record))
		{
			return;
		}
		isVisited = record.isUnlock;
		icon = document.SceneSpriteAsset.Asset;
		title = document.Title;
		habitat = string.Format(DolocConfig.StaticTexts.CollectionPanelMonsterHabitat, document.Habitat.Colored(DolocUiColor.TEXTCOLOR_STD));
		growthPeriod = string.Empty;
		if (document.IsPlant)
		{
			int[] array = Array.Empty<int>();
			switch (document.ResourceType)
			{
			case ResourceDocumentType.Resource:
				array = DolocConfig.Tables.TbResource.GetOrDefault(resourceId).SpawnMonths;
				break;
			case ResourceDocumentType.Vegetation:
				array = DolocConfig.Tables.TbVegetation.GetOrDefault(resourceId).GrowingMonths;
				break;
			}
			string text = ((array.Length != 0) ? string.Format(DolocUtils.Format(DolocConfig.StaticTexts.CollectionPanelResourceGrowthPeriod, array.Select((int month) => DolocAPI.HandleTimeString(DolocConfig.StaticTexts.UiTextMonths, month)).HandleJoinString().Colored(DolocUiColor.TEXTCOLOR_STD))) : string.Format(DolocConfig.StaticTexts.CollectionPanelResourceGrowthPeriod, DolocConfig.StaticTexts.CollectionPanelResourceYearRoundGrowth.Colored(DolocUiColor.TEXTCOLOR_STD)));
			growthPeriod = text;
		}
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		Dictionary<string, float> dictionary2 = new Dictionary<string, float>();
		switch (document.ResourceType)
		{
		case ResourceDocumentType.Resource:
		{
			ResourceInfo orDefault = DolocConfig.Tables.TbResource.GetOrDefault(resourceId);
			for (int num = orDefault.LevelDatas.Length - 1; num >= 0; num--)
			{
				foreach (ItemSpawnData spawnData in orDefault.LevelDatas[num].DropSpawnEntry.SpawnLut_Ref.SpawnDatas)
				{
					GlobalGuaranteedInfo globalGuaranteedInfo2 = DolocConfig.Tables.TbGlobalGuaranteed.Get(GuaranteedType.Resource, spawnData.ItemName);
					if (globalGuaranteedInfo2 != null && !globalGuaranteedInfo2.Viewable)
					{
						continue;
					}
					if (num == orDefault.LevelDatas.Length - 1)
					{
						dictionary.Add(spawnData.ItemName, spawnData.SpawnWeight);
					}
					else if (!dictionary.ContainsKey(spawnData.ItemName))
					{
						if (dictionary2.ContainsKey(spawnData.ItemName))
						{
							dictionary2[spawnData.ItemName] = Mathf.Max(spawnData.SpawnWeight, dictionary2[spawnData.ItemName]);
						}
						else
						{
							dictionary2.Add(spawnData.ItemName, spawnData.SpawnWeight);
						}
					}
				}
			}
			break;
		}
		case ResourceDocumentType.Vegetation:
			foreach (ItemSpawnData spawnData2 in DolocConfig.Tables.TbVegetation.GetOrDefault(resourceId).DropSpawnEntry.SpawnLut_Ref.SpawnDatas)
			{
				GlobalGuaranteedInfo globalGuaranteedInfo = DolocConfig.Tables.TbGlobalGuaranteed.Get(GuaranteedType.Resource, spawnData2.ItemName);
				if (globalGuaranteedInfo == null || globalGuaranteedInfo.Viewable)
				{
					dictionary.Add(spawnData2.ItemName, spawnData2.SpawnWeight);
				}
			}
			break;
		}
		List<KeyValuePair<string, float>> list = dictionary.OrderByDescending((KeyValuePair<string, float> x) => x.Value).ToList();
		list.AddRange(dictionary2.OrderByDescending((KeyValuePair<string, float> x) => x.Value));
		itemNames = list.Select((KeyValuePair<string, float> x) => x.Key).ToArray();
		dropItems = itemNames.Select(DolocAPI.GetItemSprite).ToArray();
		obtained = itemNames.Select((string itemName) => DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Item, itemName, out var record2) && record2.isUnlock).ToArray();
		collectCount = document.ResourceType switch
		{
			ResourceDocumentType.Resource => DolocAPI.GetEventTriggerCount(GameEventType.FELL_DUNGEON_RESOURCE, resourceId), 
			ResourceDocumentType.Vegetation => DolocAPI.GetEventTriggerCount(GameEventType.GATHERING_VEGETATION_FRUIT, resourceId), 
			_ => 0, 
		};
		int num2 = 0;
		StringBuilder stringBuilder = new StringBuilder();
		CustomizedDocumentNodeInfo[] documentInfos = document.DocumentInfos;
		foreach (CustomizedDocumentNodeInfo customizedDocumentNodeInfo in documentInfos)
		{
			if (customizedDocumentNodeInfo.DocumentType == DocumentType.COLLECT && num2 == 0 && customizedDocumentNodeInfo.Value > collectCount)
			{
				num2 = customizedDocumentNodeInfo.Value;
			}
			if (record.documents.Contains(customizedDocumentNodeInfo.Id))
			{
				stringBuilder.Append(customizedDocumentNodeInfo.DescriptionAppend);
				stringBuilder.Append("\n\n\n");
			}
		}
		if (num2 > 0)
		{
			stringBuilder.Append(string.Format(DolocConfig.StaticTexts.CollectionPanelResourceContentLockTip, title, $"{collectCount}/{num2}").Colored(DolocUiColor.SLIENTCOLOR_RED));
			stringBuilder.Append("\n\n");
		}
		documentContent = stringBuilder.ToString();
	}
}
