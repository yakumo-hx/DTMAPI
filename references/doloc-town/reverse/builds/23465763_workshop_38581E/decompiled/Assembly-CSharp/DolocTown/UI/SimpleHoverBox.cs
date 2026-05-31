using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class SimpleHoverBox : HoverBoxBase
{
	[SerializeField]
	private TextMeshProUGUI txtContent;

	[SerializeField]
	private float duration = 2f;

	[SerializeField]
	private Image background;

	private Sequence _sequence;

	private bool autoFade;

	private Color defaultColor;

	protected override void __Init()
	{
		base.__Init();
		defaultColor = background.color;
	}

	private void Show()
	{
		base.gameObject.SetActive(value: true);
		_sequence?.Kill();
		if (autoFade)
		{
			_sequence = DOTween.Sequence();
			_sequence.AppendInterval(duration);
			_sequence.OnComplete(Hide);
		}
	}

	public override void Hide()
	{
		_sequence?.Kill();
		base.gameObject.SetActive(value: false);
		SetStyle(HoverBoxStyle.Default);
	}

	public void RenderAndShow(string text, bool autoFade = false, HoverBoxStyle style = HoverBoxStyle.Default, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
	{
		SetStyle(style);
		SetText(txtContent, DolocAPI.GetParsedKeystrokeText(text));
		txtContent.alignment = alignment;
		this.autoFade = autoFade;
		Show();
	}

	private void SetStyle(HoverBoxStyle style)
	{
		if (style == HoverBoxStyle.Light)
		{
			background.color = DolocUiColor.BACKCOLOR_LIGHT;
			txtContent.color = DolocUiColor.BACKCOLOR_LEVEL3;
		}
		else
		{
			background.color = defaultColor;
			txtContent.color = DolocUiColor.TEXTCOLOR_STD;
		}
	}
}
