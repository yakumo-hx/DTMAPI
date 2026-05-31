using DG.Tweening;
using UnityEngine;

public class DolocTweenScale : DolocTweenDynamicWrapper
{
	private Transform transform;

	public Vector3 endValue { get; set; }

	public DolocTweenScale(Transform transform)
	{
		this.transform = transform;
		base.ease = Ease.Linear;
		base.time = 1f;
	}

	public DolocTweenScale(Transform transform, Ease ease, float time)
	{
		this.transform = transform;
		base.ease = ease;
		base.time = time;
	}

	protected override Tween build()
	{
		return transform.DOScale(endValue, base.time).SetEase(base.ease);
	}

	public void forcePlay(Vector3 endValue)
	{
		this.endValue = endValue;
		forcePlay();
	}

	public void forcePlay(Vector3 endValue, TweenCallback cb)
	{
		this.endValue = endValue;
		forcePlay(cb);
	}
}
