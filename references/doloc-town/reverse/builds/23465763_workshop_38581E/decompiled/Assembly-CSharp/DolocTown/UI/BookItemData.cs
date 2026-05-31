using DolocTown.Config.Item;
using DolocTown.Config.UI;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct BookItemData : IUIData
{
	public bool notEmpty { get; }

	public Sprite icon { get; }

	public bool isNew { get; }

	public bool obtained { get; }

	public BookItemData(ItemInfo itemProto)
	{
		this = default(BookItemData);
		if (itemProto != null)
		{
			notEmpty = true;
			icon = itemProto.UiSpriteAsset.Asset;
			if (DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Item, itemProto.Id, out var record))
			{
				isNew = !record.isRead && record.isUnlock;
				obtained = record.isUnlock;
			}
		}
	}
}
