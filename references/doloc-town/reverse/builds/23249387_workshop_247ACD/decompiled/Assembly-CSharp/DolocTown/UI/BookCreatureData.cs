using DolocTown.Config;
using DolocTown.Config.Fishing;
using DolocTown.Config.Item;
using DolocTown.Config.UI;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct BookCreatureData : IUIData
{
	public bool notEmpty { get; }

	public bool isAnimal { get; }

	public Sprite icon { get; }

	public string name { get; }

	public bool isVisited { get; }

	public bool isNew { get; }

	public BookCreatureData(string creatureId)
	{
		this = default(BookCreatureData);
		if (DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Creature, creatureId, out var record))
		{
			isVisited = record.isUnlock;
			isNew = !record.isRead && isVisited;
		}
		FishDocumentInfo document2;
		ItemInfo proto;
		if (DolocAPI.QueryAnimalDocument(creatureId, out var document))
		{
			notEmpty = true;
			isAnimal = true;
			icon = ((DolocAPI.GetEventTriggerCount(GameEventType.ANIMAL_GROW_UP, creatureId) > 0) ? document.UiAdultSpriteAsset.Asset : document.UiSpriteAsset.Asset);
			name = (isVisited ? document.Id_Ref.Title : DolocConfig.StaticTexts.UiOperationTalkUnknown);
		}
		else if (DolocAPI.QueryFishDocument(creatureId, out document2) && DolocAPI.QueryItemProto(document2.Id, out proto))
		{
			notEmpty = true;
			isAnimal = false;
			icon = proto.UiSpriteAsset.Asset;
			name = (isVisited ? proto.Title : DolocConfig.StaticTexts.UiOperationTalkUnknown);
		}
	}
}
