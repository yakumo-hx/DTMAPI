using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class AnimalSlot : DolocNavigationButton
{
	[SerializeField]
	protected Text title;

	[SerializeField]
	protected Text stateInfo;

	[SerializeField]
	protected Text space;

	public Color txtColor
	{
		set
		{
			title.color = value;
		}
	}

	public void Render(AnimalFullInfoData data)
	{
		SetText(title, data.visible ? data.title : "???");
		SetText(stateInfo, data.visible ? data.stateBaseInfo : " ");
		SetText(space, data.spaceInfo);
		SetSprite(iconImg, data.icon);
		iconImg.color = (data.visible ? Color.white : new Color(0f, 0f, 0f, 0.8f));
	}
}
