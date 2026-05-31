using System.Linq;
using System.Text;
using DolocTown.Config;
using DolocTown.Config.Archives;
using DolocTown.Config.Monster;
using DolocTown.Config.UI;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct MonsterDetailData : IUIData
{
	public bool notEmpty { get; }

	public bool isVisited { get; }

	public bool display { get; }

	public Sprite icon { get; }

	public string title { get; }

	public string killText { get; }

	public string habitat { get; }

	public string[] itemNames { get; }

	public Sprite[] dropItems { get; }

	public bool[] obtained { get; }

	public string documentContent { get; }

	public MonsterDetailData(string monsterId)
	{
		this = default(MonsterDetailData);
		if (!DolocAPI.QueryMonsterDocument(monsterId, out var document))
		{
			return;
		}
		notEmpty = true;
		display = document.Display;
		if (!DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Monster, monsterId, out var record))
		{
			return;
		}
		isVisited = record.isUnlock;
		icon = document.SpriteAsset.Asset;
		title = document.Title;
		habitat = string.Format(DolocConfig.StaticTexts.CollectionPanelMonsterHabitat, document.Habitat.Colored(DolocUiColor.TEXTCOLOR_STD));
		itemNames = document.SpecialDrop;
		dropItems = itemNames.Select(DolocAPI.GetItemSprite).ToArray();
		obtained = itemNames.Select((string itemName) => DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Item, itemName, out var record2) && record2.isUnlock).ToArray();
		int eventTriggerCount = DolocAPI.GetEventTriggerCount(GameEventType.SLAIN_MONSTER, monsterId);
		switch (document.MonsterType)
		{
		case MonsterType.ORGANISM:
			killText = string.Format(DolocConfig.StaticTexts.CollectionPanelMonsterOrganism, eventTriggerCount);
			break;
		case MonsterType.MACHINERY:
		case MonsterType.BIONICS:
			killText = string.Format(DolocConfig.StaticTexts.CollectionPanelMonsterMachinery, eventTriggerCount);
			break;
		case MonsterType.BOSS:
			killText = string.Format(DolocConfig.StaticTexts.CollectionPanelMonsterBoss, eventTriggerCount);
			break;
		}
		int num = 0;
		StringBuilder stringBuilder = new StringBuilder();
		DocumentNodeInfo[] documentInfos = document.DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo in documentInfos)
		{
			if (documentNodeInfo.DocumentType == DocumentType.KILL && num == 0 && documentNodeInfo.Value > eventTriggerCount)
			{
				num = documentNodeInfo.Value;
			}
			if (record.documents.Contains(documentNodeInfo.Id))
			{
				stringBuilder.Append(documentNodeInfo.DescriptionAppend);
				stringBuilder.Append("\n\n\n");
			}
		}
		if (num > 0)
		{
			stringBuilder.Append(string.Format(DolocConfig.StaticTexts.CollectionPanelMonsterContentLockTip, title, $"{eventTriggerCount}/{num}").Colored(DolocUiColor.SLIENTCOLOR_RED));
			stringBuilder.Append("\n\n");
		}
		documentContent = stringBuilder.ToString();
	}
}
