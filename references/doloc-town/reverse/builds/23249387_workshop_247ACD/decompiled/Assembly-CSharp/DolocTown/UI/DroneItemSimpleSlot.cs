using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DroneItemSimpleSlot : DolocNavigationButton
{
	[SerializeField]
	private Image emptyHint;

	[SerializeField]
	private Color normalBgColor;

	[SerializeField]
	private Color highLightBgColor;

	public void Render(Sprite icon, Sprite type = null)
	{
		bool flag = icon == null;
		iconImg.sprite = icon;
		if (type != null)
		{
			emptyHint.sprite = type;
		}
		iconImg.gameObject.SetActive(!flag);
		emptyHint.gameObject.SetActive(flag);
	}

	protected override void OnHighLighted(bool value)
	{
		base.backgroundColor = (value ? highLightBgColor : normalBgColor);
	}
}
