using DG.Tweening;
using UnityEngine;

namespace DolocTown.UI;

public class MessageBox : MessageBoxBase
{
	protected Vector2 showAnchoredPos;

	protected Vector2 hideAnchoredPos => new Vector2(showAnchoredPos.x, (0f - base.height) * 1.5f);

	protected override void __Init()
	{
		base.__Init();
		showAnchoredPos = base.rectTransform.anchoredPosition;
		SetVisible(value: false);
	}

	protected override Tween createHideTween()
	{
		Sequence sequence = DOTween.Sequence();
		sequence.Join(base.rectTransform.DOAnchorPos(hideAnchoredPos, hideTime).SetEase(ease));
		sequence.Join(DOTween.To(() => base.alpha, delegate(float x)
		{
			base.alpha = x;
		}, 0f, hideTime));
		return sequence;
	}

	protected override Tween createShowTween()
	{
		Sequence sequence = DOTween.Sequence();
		sequence.Join(base.rectTransform.DOAnchorPos(showAnchoredPos, showTime).SetEase(ease));
		sequence.Join(DOTween.To(() => base.alpha, delegate(float x)
		{
			base.alpha = x;
		}, 1f, showTime));
		return sequence;
	}

	protected override void _reset()
	{
		base.transform.localPosition = hideAnchoredPos;
		base.alpha = 0f;
	}
}
