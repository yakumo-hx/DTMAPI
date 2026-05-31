using DG.Tweening;
using UnityEngine;

namespace DolocTown.UI;

public class MessageBoxLarge : MessageBoxBase
{
	protected Vector2 hideAnchoredPos;

	protected Vector2 showAnchoredPos;

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
		return sequence;
	}

	protected override Tween createShowTween()
	{
		Sequence sequence = DOTween.Sequence();
		sequence.Join(base.rectTransform.DOAnchorPos(showAnchoredPos, showTime).SetEase(ease));
		return sequence;
	}

	protected override void _reset()
	{
		hideAnchoredPos = new Vector2(showAnchoredPos.x * base.screenScaleX, base.height * 1.5f);
		base.rectTransform.anchoredPosition = hideAnchoredPos;
	}

	public void test()
	{
		Show(LocSprites.UI_INFOICON_STAR, "测试消息");
	}
}
