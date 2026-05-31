using DG.Tweening;
using UnityEngine;

public class DolocTweenShake : DolocTweenDynamicWrapper
{
	private Transform transform;

	private Vector2 sourcePosition;

	public float strength { get; set; }

	public DolocTweenShake(Transform transform, float strength)
	{
		this.transform = transform;
		sourcePosition = transform.localPosition;
		this.strength = strength;
	}

	public DolocTweenShake(Transform transform, float strength, Ease ease, float time)
		: base(ease, time)
	{
		this.transform = transform;
		sourcePosition = transform.localPosition;
		this.strength = strength;
	}

	protected override Tween build()
	{
		return transform.DOShakePosition(base.time, strength);
	}

	public void play(float s)
	{
		strength = s;
		play();
	}

	public void play(float s, TweenCallback cb)
	{
		strength = s;
		play(cb);
	}

	public void forcePlay(float s)
	{
		strength = s;
		forcePlay();
	}

	public void forcePlay(float s, TweenCallback cb)
	{
		strength = s;
		forcePlay(cb);
	}
}
