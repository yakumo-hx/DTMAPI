using System.Linq;
using System.Text;
using DolocTown.Config;
using DolocTown.Config.Archives;
using DolocTown.Config.UI;
using DolocTown.GameData;
using Sirenix.Utilities;
using UnityEngine;

namespace DolocTown.UI;

public struct AnimalDetailData : IUIData
{
	public bool notEmpty { get; }

	public bool isVisited { get; }

	public bool display { get; }

	public Sprite icon { get; }

	public string title { get; }

	public string countText { get; }

	public string[] itemNames { get; }

	public Sprite[] itemIcons { get; }

	public bool[] obtainedProduct { get; }

	public string documentContent { get; }

	public AnimalDetailData(string animalName)
	{
		this = default(AnimalDetailData);
		if (!DolocAPI.QueryAnimalDocument(animalName, out var document))
		{
			return;
		}
		notEmpty = true;
		display = document.Display;
		if (!DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Creature, animalName, out var record))
		{
			return;
		}
		isVisited = record.isUnlock;
		icon = ((DolocAPI.GetEventTriggerCount(GameEventType.ANIMAL_GROW_UP, animalName) > 0) ? document.AdultAsset.Asset : document.InfancyAsset.Asset);
		title = document.Id_Ref.Title;
		TemplateRoomOutdoor mainFarm = DolocAPI.archiveHandle.farmData.MainFarm;
		int haveCount = mainFarm.DM_animal.GetAnimalCount(animalName);
		mainFarm.DM_building.Buildings.ForEach(delegate(Building build)
		{
			haveCount += build.room.DM_animal.GetAnimalCount(animalName);
		});
		itemNames = DolocAPI.GetAnimalAllProduce(animalName).ToArray();
		itemIcons = itemNames.Select(DolocAPI.GetItemSprite).ToArray();
		obtainedProduct = itemNames.Select((string itemName) => DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Item, itemName, out var record3) && record3.isUnlock).ToArray();
		int eventTriggerCount = DolocAPI.GetEventTriggerCount(GameEventType.ANIMAL_GROW_UP, animalName);
		int eventTriggerCount2 = DolocAPI.GetEventTriggerCount(GameEventType.ANIMAL_BIRTH, animalName);
		CollectionRecord record2;
		int num = itemNames.Count((string item) => DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Item, item, out record2) && record2.isUnlock);
		countText = DolocConfig.StaticTexts.CollectionPanelAnimalPossess.Format(haveCount) + "   " + DolocConfig.StaticTexts.CollectionPanelAnimalBreed.Format(eventTriggerCount2);
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		StringBuilder stringBuilder = new StringBuilder();
		DocumentNodeInfo[] documentInfos = document.DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo in documentInfos)
		{
			switch (documentNodeInfo.DocumentType)
			{
			case DocumentType.RAISING:
				if (num2 == 0 && documentNodeInfo.Value > eventTriggerCount)
				{
					num2 = documentNodeInfo.Value;
				}
				break;
			case DocumentType.BREEDING:
				if (num3 == 0 && documentNodeInfo.Value > eventTriggerCount2)
				{
					num3 = documentNodeInfo.Value;
				}
				break;
			case DocumentType.OBTAIN:
				if (num4 == 0 && documentNodeInfo.Value > num)
				{
					num4 = documentNodeInfo.Value;
				}
				break;
			}
			if (record.documents.Contains(documentNodeInfo.Id))
			{
				stringBuilder.Append(documentNodeInfo.DescriptionAppend);
				stringBuilder.Append("\n\n\n");
			}
		}
		if (num2 > 0)
		{
			stringBuilder.Append(string.Format(DolocConfig.StaticTexts.CollectionPanelAnimalContentLockBringUp, title, $"{eventTriggerCount}/{num2}").Colored(DolocUiColor.SLIENTCOLOR_RED));
			stringBuilder.Append("\n\n");
		}
		if (num3 > 0)
		{
			stringBuilder.Append(string.Format(DolocConfig.StaticTexts.CollectionPanelAnimalContentLockBreed, title, $"{eventTriggerCount2}/{num3}").Colored(DolocUiColor.SLIENTCOLOR_RED));
			stringBuilder.Append("\n\n");
		}
		if (num4 > 0)
		{
			stringBuilder.Append(string.Format(DolocConfig.StaticTexts.CollectionPanelAnimalContentLockProduct, $"{num}/{num4}").Colored(DolocUiColor.SLIENTCOLOR_RED));
			stringBuilder.Append("\n\n");
		}
		documentContent = stringBuilder.ToString();
	}
}
