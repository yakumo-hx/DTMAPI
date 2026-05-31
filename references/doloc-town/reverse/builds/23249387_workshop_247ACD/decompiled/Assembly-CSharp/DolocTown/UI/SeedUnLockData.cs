using System.Linq;
using DolocTown.Config.Plant;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct SeedUnLockData : IUIData
{
	public bool notEmpty { get; }

	public Sprite icon { get; }

	public bool isUnLock { get; }

	public string title { get; }

	public string description { get; }

	public string costInfo { get; }

	public bool enough { get; }

	public bool locked { get; }

	public SeedUnLockData(SeedUnlockInfo proto)
	{
		this = default(SeedUnLockData);
		notEmpty = false;
		Item item = DolocAPI.GenerateItem(proto?.Id ?? "");
		if (proto != null && item != null)
		{
			notEmpty = true;
			icon = item.uiSprite;
			isUnLock = DolocAPI.archiveHandle.IsSeedNodeUnlocked(proto.Id);
			title = item.title;
			description = item.description;
			string[] titles = proto.Costs.Select((CountItem x) => DolocAPI.GetItemTitle(x.itemName)).ToArray();
			int[] counts = proto.Costs.Select((CountItem x) => x.itemCount).ToArray();
			costInfo = DolocUtils.GenerateCountItemInfo(titles, counts, "\n");
			enough = !isUnLock && DolocAPI.CanAfford(proto.Costs, checkBox: true);
			locked = false;
		}
	}
}
