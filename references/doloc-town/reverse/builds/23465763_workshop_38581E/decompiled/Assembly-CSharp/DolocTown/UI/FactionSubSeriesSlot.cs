using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class FactionSubSeriesSlot : DolocNavigationButton
{
	[SerializeField]
	private Text text;

	[SerializeField]
	private Image textBackground;

	[SerializeField]
	private Image labelPadding;

	[SerializeField]
	private Color normalColor;

	[SerializeField]
	private Color highLightedColor;

	public float labelWidth
	{
		set
		{
			textBackground.rectTransform.sizeDelta = new Vector2(value + 8f, textBackground.rectTransform.sizeDelta.y);
		}
	}

	public float perfectLabelWidth => text.preferredWidth;

	public string title
	{
		set
		{
			text.text = value;
			RefreshWidth();
		}
	}

	private void RefreshWidth()
	{
		float preferredWidth = text.preferredWidth;
		textBackground.rectTransform.sizeDelta = new Vector2(preferredWidth, textBackground.rectTransform.sizeDelta.y);
	}

	protected override void __Init()
	{
		base.__Init();
		base.backgroundColor = normalColor;
		OnHighLighted(value: false);
	}

	protected override void OnHighLighted(bool value)
	{
		base.OnHighLighted(value);
		Color color2 = (base.backgroundColor = (value ? highLightedColor : normalColor));
		textBackground.color = color2;
		labelPadding.gameObject.SetActive(value);
		labelPadding.color = color2;
	}
}
