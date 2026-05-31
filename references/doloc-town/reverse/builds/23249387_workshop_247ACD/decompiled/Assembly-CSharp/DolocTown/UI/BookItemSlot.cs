using UnityEngine;

namespace DolocTown.UI;

public class BookItemSlot : DolocNavigationButton
{
	[SerializeField]
	private GameObject redPoint;

	[SerializeField]
	private Color normalBgColor;

	[SerializeField]
	private Color highLightBgColor;

	[SerializeField]
	private GameObject border;

	[HideInInspector]
	public bool forceHighLighted;

	protected override void __Init()
	{
		base.__Init();
		border.SetActive(value: false);
	}

	public void Render(BookItemData data)
	{
		base.iconSprite = data.icon;
		redPoint.SetActive(data.isNew);
		base.iconColor = (data.obtained ? Color.white : Color.black);
	}

	protected override void OnHighLighted(bool value)
	{
		base.backgroundColor = (value ? highLightBgColor : normalBgColor);
	}

	protected override void OnSelect()
	{
		base.OnSelect();
		border.SetActive(value: true);
		base.highLighted = true;
		DolocAPI.HideHoverBox();
	}

	protected override void OnDeselect()
	{
		base.OnDeselect();
		border.SetActive(value: false);
		if (!forceHighLighted)
		{
			base.highLighted = false;
		}
	}
}
