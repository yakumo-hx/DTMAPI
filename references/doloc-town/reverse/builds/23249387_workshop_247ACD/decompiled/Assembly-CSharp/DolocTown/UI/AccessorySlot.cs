using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class AccessorySlot : DolocNavigationButton
{
	[SerializeField]
	private Image emptyHint;

	[SerializeField]
	private Color normalBgColor;

	[SerializeField]
	private Color highLightBgColor;

	public void Render(Sprite icon)
	{
		bool flag = icon == null;
		iconImg.sprite = icon;
		iconImg.gameObject.SetActive(!flag);
		emptyHint.gameObject.SetActive(flag);
	}

	public void ShowEquipmentItemViewer(Item item, string defaultText)
	{
		if (item == null)
		{
			this.HoverTextSmall(defaultText.Colored(DolocUiColor.SLIENTCOLOR_BLUE));
			return;
		}
		UIAlignmentType targetAnchor = UIAlignmentType.LeftTop;
		UIAlignmentType hoverPivot = UIAlignmentType.LeftBottom;
		if (DolocAPI.screenManager.screenSize.y - base.positionY < 2f * base.size.y)
		{
			targetAnchor = UIAlignmentType.LeftBottom;
			hoverPivot = UIAlignmentType.LeftTop;
		}
		this.HoverItemViewer(new ItemData(item), targetAnchor, hoverPivot);
	}

	protected override void OnHighLighted(bool value)
	{
		base.backgroundColor = (value ? highLightBgColor : normalBgColor);
	}
}
