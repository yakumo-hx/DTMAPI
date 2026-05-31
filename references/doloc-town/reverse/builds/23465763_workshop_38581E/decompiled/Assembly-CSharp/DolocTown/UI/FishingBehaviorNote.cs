using DG.Tweening;
using RedSaw;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class FishingBehaviorNote : DolocUiRecyclableObject
{
	private Tween tween;

	private Image image;

	public Sprite sprite
	{
		set
		{
			image.sprite = value;
		}
	}

	public Color color
	{
		set
		{
			image.color = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		image = GetComponent<Image>();
	}

	public void StartScroll(float scrollWidthPerSecond, ObjectPool<FishingBehaviorNote> pool)
	{
		KillTween();
		float duration = (base.width + base.anchoredPositionX) / scrollWidthPerSecond;
		tween = base.rectTransform.DOAnchorPosX(0f - base.width, duration).SetEase(Ease.Linear).OnComplete(delegate
		{
			KillTween();
			pool.Recycle(this);
		});
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		KillTween();
	}

	private void KillTween()
	{
		if (tween != null && tween.IsActive())
		{
			tween.Kill();
		}
	}
}
