using DG.Tweening;
using UnityEngine;

namespace DolocTown.UI;

public class FadeUpTipSpriteManager
{
	public void RaiseFadeUpSprite(Vector2 pos, Sprite sprite, Ease ease, float duration, float popDistance, bool inScene)
	{
		if (!(sprite == null))
		{
			popDistance /= DolocAPI.screenManager.scaleFactor;
			DolocImage image = DolocAPI.uiSystem.GetFromPool<DolocImage>(inScene);
			image.sprite = sprite;
			image.position = pos;
			image.alpha = 1f;
			Sequence sequence = DOTween.Sequence();
			sequence.Join(image.transform.DOMoveY(pos.y + popDistance, duration).SetEase(ease));
			sequence.Join(DOTween.ToAlpha(() => image.color, delegate(Color c)
			{
				image.color = c;
			}, 0f, duration));
			sequence.OnComplete(delegate
			{
				DolocAPI.uiSystem.RecycleToPool(inScene, image);
			});
		}
	}

	public void RaiseFadeDownSprite(Vector2 pos, Sprite sprite, Ease ease, float duration, float popDistance, bool inScene)
	{
		if (!(sprite == null))
		{
			popDistance /= DolocAPI.screenManager.scaleFactor;
			DolocImage image = DolocAPI.uiSystem.GetFromPool<DolocImage>(inScene);
			image.sprite = sprite;
			image.position = new Vector2(pos.x, pos.y + popDistance);
			image.alpha = 0f;
			Sequence sequence = DOTween.Sequence();
			sequence.Join(image.transform.DOMoveY(pos.y, duration).SetEase(ease));
			sequence.Join(DOTween.ToAlpha(() => image.color, delegate(Color c)
			{
				image.color = c;
			}, 1f, duration));
			sequence.OnComplete(delegate
			{
				DolocAPI.uiSystem.RecycleToPool(inScene, image);
			});
		}
	}
}
