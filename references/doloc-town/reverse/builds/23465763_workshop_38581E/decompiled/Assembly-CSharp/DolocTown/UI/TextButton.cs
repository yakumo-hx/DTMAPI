using TMPro;
using UnityEngine;

namespace DolocTown.UI;

public class TextButton : DolocNavigationButton
{
	[SerializeField]
	protected TMP_Text textCmp;

	protected CanvasGroup canvasGroup;

	public override float alpha
	{
		get
		{
			return canvasGroup.alpha;
		}
		set
		{
			canvasGroup.alpha = value;
		}
	}

	public TextAlignmentOptions alignment
	{
		set
		{
			textCmp.alignment = value;
		}
	}

	public string text
	{
		get
		{
			return textCmp.text;
		}
		set
		{
			textCmp.text = value;
		}
	}

	public float preferredWidth => textCmp.preferredWidth;

	public Color textColor
	{
		set
		{
			textCmp.color = value;
			textCmp.text = textCmp.text.ChangeSpriteColor(value);
		}
	}

	public TextAlignmentOptions TextAlignment
	{
		set
		{
			textCmp.alignment = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		if (textCmp == null)
		{
			textCmp = GetComponentInChildren<TMP_Text>();
		}
		if (!TryGetComponent<CanvasGroup>(out canvasGroup))
		{
			canvasGroup = base.gameObject.AddComponent<CanvasGroup>();
		}
	}

	protected override void OnGrayed(bool value)
	{
		alpha = (value ? 0.5f : 1f);
	}
}
