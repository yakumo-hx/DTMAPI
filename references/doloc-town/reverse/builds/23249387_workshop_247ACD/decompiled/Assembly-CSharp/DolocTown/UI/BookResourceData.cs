using DolocTown.Config;
using DolocTown.Config.UI;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct BookResourceData : IUIData
{
	public bool notEmpty { get; }

	public Sprite icon { get; }

	public string name { get; }

	public bool isVisited { get; }

	public bool isNew { get; }

	public BookResourceData(string resourceId)
	{
		this = default(BookResourceData);
		if (DolocAPI.QueryResourceDocument(resourceId, out var document))
		{
			notEmpty = true;
			icon = document.UiSpriteAsset.Asset;
			if (DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Resource, resourceId, out var record))
			{
				isVisited = record.isUnlock;
				isNew = !record.isRead && isVisited;
			}
			name = (isVisited ? document.Title : DolocConfig.StaticTexts.UiOperationTalkUnknown);
		}
	}
}
