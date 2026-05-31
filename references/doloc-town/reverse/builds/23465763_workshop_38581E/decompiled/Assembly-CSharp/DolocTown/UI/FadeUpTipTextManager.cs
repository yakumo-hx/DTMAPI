using DG.Tweening;
using UnityEngine;

namespace DolocTown.UI;

public class FadeUpTipTextManager
{
	public void RaiseFadeUpText(string content, Color color, Vector2 pos, Ease ease, float duration, float popDistance)
	{
		DolocText text = DolocAPI.uiSystem.GetFromPoolInScene<DolocText>();
		text.text = content;
		text.color = color;
		text.position = pos;
		popDistance /= DolocAPI.screenManager.scaleFactor;
		Sequence sequence = DOTween.Sequence();
		sequence.Join(text.transform.DOMoveY(pos.y + popDistance, duration).SetEase(ease));
		sequence.Join(DOTween.ToAlpha(() => text.color, delegate(Color c)
		{
			text.color = c;
		}, 0f, duration));
		sequence.OnComplete(delegate
		{
			DolocAPI.uiSystem.RecycleToPoolInScene(text);
		});
	}
}
