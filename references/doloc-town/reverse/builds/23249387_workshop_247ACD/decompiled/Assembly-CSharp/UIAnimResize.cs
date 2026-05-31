using DG.Tweening;
using UnityEngine;

public class UIAnimResize : DolocTweenAnimation
{
	private Vector2 size;

	private RectTransform transform;

	public UIAnimResize(RectTransform transform)
	{
		this.transform = transform;
	}

	public UIAnimResize(RectTransform transform, Ease ease, float time)
	{
		base.ease = ease;
		base.time = time;
		this.transform = transform;
	}

	public override Tween buildAnim()
	{
		return DOTween.To(() => transform.sizeDelta, delegate(Vector2 v)
		{
			transform.sizeDelta = v;
		}, size, base.time).SetEase(base.ease);
	}

	public void play(Vector2 size)
	{
		this.size = size;
		play();
	}
}
