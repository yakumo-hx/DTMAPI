using UnityEngine;

namespace DolocTown.UI;

public struct FactionItemData : IUIData
{
	public Sprite sprite;

	public bool isFinished;

	public string count;

	public bool notEmpty { get; }

	public FactionItemData(FactionItem item)
	{
		this = default(FactionItemData);
		if (item != null)
		{
			notEmpty = true;
			sprite = DolocAPI.GetItemSprite(item.ItemName);
			isFinished = item.FinishState;
			count = item.Count.ToString();
		}
	}

	public FactionItemData(Sprite sprite, int count, bool isFinished)
	{
		this = default(FactionItemData);
		if (!(sprite == null) && count > 0)
		{
			notEmpty = true;
			this.sprite = sprite;
			this.isFinished = isFinished;
			this.count = count.ToString();
		}
	}
}
