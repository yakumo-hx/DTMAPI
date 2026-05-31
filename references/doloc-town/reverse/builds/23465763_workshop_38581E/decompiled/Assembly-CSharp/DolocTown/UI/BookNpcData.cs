using DolocTown.Config;
using DolocTown.Config.UI;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct BookNpcData : IUIData
{
	public bool notEmpty { get; }

	public Sprite icon { get; }

	public string name { get; }

	public bool isVisited { get; }

	public bool isNew { get; }

	public BookNpcData(string npcName)
	{
		this = default(BookNpcData);
		if (DolocConfig.Tables.TbNpcDocument.DataMap.TryGetValue(npcName, out var value))
		{
			notEmpty = true;
			icon = value.UiSpriteAsset.Asset;
			if (DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Npc, npcName, out var record))
			{
				isVisited = record.isUnlock;
				isNew = !record.isRead && isVisited;
			}
			name = DolocAPI.GetNpcTitle(npcName);
		}
	}
}
