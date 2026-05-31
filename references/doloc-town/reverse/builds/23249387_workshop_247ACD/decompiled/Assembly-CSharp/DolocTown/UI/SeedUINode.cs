using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class SeedUINode : DolocNavigationButton
{
	[SerializeField]
	private GameObject iconComplete;

	[SerializeField]
	private GameObject iconLock;

	[SerializeField]
	private Text title;

	[SerializeField]
	private Text cost;

	[SerializeField]
	private Text info;

	[SerializeField]
	private GameObject mask;

	[SerializeField]
	private Color normalTextColor = DolocUiColor.TEXTCOLOR_STD;

	[SerializeField]
	private Color selectTextColor = DolocUiColor.SLIENTCOLOR_GREEN;

	[SerializeField]
	private Color normalBgColor = DolocUiColor.BACKCOLOR_LEVEL3;

	[SerializeField]
	private Color selectBgColor = DolocUiColor.SLIENTCOLOR_PURPLE;

	public bool Selected
	{
		set
		{
			base.backgroundColor = (value ? selectBgColor : normalBgColor);
			title.color = (value ? selectTextColor : normalTextColor);
		}
	}

	public RectTransform hoveredRect => cost.rectTransform;

	public void Render(SeedUnLockData data)
	{
		base.iconSprite = data.icon;
		iconComplete.SetActive(data.isUnLock);
		iconLock.SetActive(data.locked);
		title.text = data.title;
		cost.text = data.costInfo;
		info.text = data.description;
		mask.SetActive(!data.enough);
	}
}
