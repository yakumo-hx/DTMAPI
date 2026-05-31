using DolocTown.Config;
using DolocTown.Config.UI;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct BookMonsterData : IUIData
{
	public bool notEmpty { get; }

	public Sprite icon { get; }

	public string name { get; }

	public bool isVisited { get; }

	public bool isNew { get; }

	public bool display { get; }

	public BookMonsterData(string monsterId)
	{
		this = default(BookMonsterData);
		if (DolocAPI.QueryMonsterDocument(monsterId, out var document))
		{
			notEmpty = true;
			icon = document.UiSpriteAsset.Asset;
			if (DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Monster, monsterId, out var record))
			{
				isVisited = record.isUnlock;
				isNew = !record.isRead && isVisited;
			}
			name = (isVisited ? document.Title : DolocConfig.StaticTexts.UiOperationTalkUnknown);
			display = document.DefaultUnlock || isVisited;
		}
	}
}
