using DG.Tweening;
using UnityEngine;

public class UIAnimScaleToX : DolocTweenAnimation
{
	private Transform transform;

	private float scaleTo;

	public UIAnimScaleToX(Transform transform)
	{
		this.transform = transform;
	}

	public UIAnimScaleToX(Transform transform, Ease ease, float time)
	{
		this.transform = transform;
		base.ease = ease;
		base.time = time;
	}

	public void setArgs(Ease ease, float time)
	{
		base.ease = ease;
		base.time = time;
	}

	public override Tween buildAnim()
	{
		return transform.DOScaleX(scaleTo, base.time).SetEase(base.ease);
	}

	public void play(float to)
	{
		scaleTo = to;
		play();
	}
}
