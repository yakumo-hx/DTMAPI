using UnityEngine;

namespace DolocTown.UI;

public struct ItemSimpleData : IUIData
{
	public bool notEmpty { get; }

	public Sprite sprite { get; }

	public string count { get; }

	public bool obtained { get; }

	public ItemSimpleData(Sprite sprite, int count)
	{
		this = default(ItemSimpleData);
		if (!(sprite == null))
		{
			notEmpty = true;
			this.sprite = sprite;
			this.count = count.ToString();
			obtained = true;
		}
	}
}
