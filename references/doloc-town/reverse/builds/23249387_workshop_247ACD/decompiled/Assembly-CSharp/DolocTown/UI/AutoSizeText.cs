using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class AutoSizeText : DolocUiObject
{
	[SerializeField]
	private Image titleBg;

	[SerializeField]
	private Text content;

	[SerializeField]
	private int padding = 12;

	[SerializeField]
	private int minWidth;

	public string text
	{
		get
		{
			return content.text;
		}
		set
		{
			SetText(value);
		}
	}

	public Color textColor
	{
		set
		{
			content.color = value;
		}
	}

	public Color backgroundColor
	{
		set
		{
			titleBg.color = value;
		}
	}

	private void SetText(string text)
	{
		if (text.IsNullOrEmpty())
		{
			SetVisible(value: false);
		}
		SetVisible(value: true);
		content.text = text;
		if (titleBg != null)
		{
			float x = Mathf.Max(minWidth, content.preferredWidth + (float)(padding * 2));
			float y = titleBg.rectTransform.sizeDelta.y;
			titleBg.rectTransform.sizeDelta = new Vector2(x, y);
		}
	}
}
