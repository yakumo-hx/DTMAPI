using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class CraftSlot<TData> : DolocNavigationButton where TData : ICraftData
{
	[SerializeField]
	protected Text title;

	[SerializeField]
	protected GameObject craftIcon;

	public Color txtColor
	{
		set
		{
			title.color = value;
		}
	}

	public virtual void Render(TData data)
	{
		base.iconSprite = data.outputItemSprite;
		title.text = data.recipeTitle;
		craftIcon.SetActive(data.isCostEnough);
	}
}
