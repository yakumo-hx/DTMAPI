using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TimerTip : DolocUiEntity
{
	[SerializeField]
	private Image background;

	[SerializeField]
	private Text text;

	private float originAlpha;

	private Tween _tween;

	public string Text
	{
		get
		{
			return text.text;
		}
		set
		{
			text.text = value ?? string.Empty;
		}
	}

	public Color Color
	{
		get
		{
			return text.color;
		}
		set
		{
			text.color = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		originAlpha = background.color.a;
		base.gameObject.SetActive(value: false);
	}

	public void Show()
	{
		_tween?.Kill();
		background.color = background.color.Alpha(0f);
		text.color = text.color.Alpha(0f);
		SetVisible(value: true);
		Sequence sequence = DOTween.Sequence();
		sequence.Append(background.DOFade(originAlpha, 0.5f));
		sequence.Join(text.DOFade(1f, 0.5f));
		sequence.OnComplete(delegate
		{
			_tween = null;
		});
		_tween = sequence;
	}

	public void Hide()
	{
		_tween?.Kill();
		Sequence sequence = DOTween.Sequence();
		sequence.Append(background.DOFade(0f, 0.5f));
		sequence.Join(text.DOFade(0f, 0.5f));
		sequence.OnComplete(delegate
		{
			SetVisible(value: false);
			_tween = null;
		});
		_tween = sequence;
	}
}
