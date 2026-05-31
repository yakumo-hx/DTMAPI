using System;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class RecipeSubItemSlot : DolocNavigationButton
{
	[SerializeField]
	private Text textNum;

	[SerializeField]
	private Color normalBgColor;

	[SerializeField]
	private Color highLightBgColor;

	[SerializeField]
	private GameObject border;

	public Action<RectTransform> resetScrollRectCallback;

	private ItemRecipeSlot parentSlot;

	protected override void __Init()
	{
		base.__Init();
		border.SetActive(value: false);
	}

	public void Render(ItemSimpleData data)
	{
		if (!data.notEmpty)
		{
			base.iconColor = DolocColor.empty;
			textNum.text = string.Empty;
		}
		else
		{
			base.iconSprite = data.sprite;
			base.iconColor = (data.obtained ? Color.white : Color.black);
			textNum.text = data.count;
		}
	}

	public void Render(Sprite sprite, string count)
	{
		base.iconSprite = sprite;
		textNum.text = count;
	}

	protected override void OnHighLighted(bool value)
	{
		base.backgroundColor = (value ? highLightBgColor : normalBgColor);
	}

	protected override void OnSelect()
	{
		base.OnSelect();
		border.SetActive(value: true);
		resetScrollRectCallback?.Invoke(base.rectTransform);
	}

	protected override void OnDeselect()
	{
		base.OnDeselect();
		border.SetActive(value: false);
	}
}
