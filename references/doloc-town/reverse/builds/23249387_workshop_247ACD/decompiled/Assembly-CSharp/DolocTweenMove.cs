using DG.Tweening;
using UnityEngine;

public class DolocTweenMove : DolocTweenDynamicWrapper
{
	private Transform transform;

	public Vector3 position { get; set; }

	public DolocTweenMove(Transform transform)
	{
		this.transform = transform;
	}

	public DolocTweenMove(Transform transform, Ease ease, float time)
		: base(ease, time)
	{
		this.transform = transform;
	}

	public void play(Vector3 position)
	{
		this.position = position;
		play();
	}

	public void play(Vector3 position, TweenCallback cb)
	{
		this.position = position;
		play(cb);
	}

	public void forcePlay(Vector3 position)
	{
		this.position = position;
		forcePlay();
	}

	public void forcePlay(Vector3 position, TweenCallback cb)
	{
		this.position = position;
		forcePlay(cb);
	}

	protected override Tween build()
	{
		return transform.DOMove(position, base.time).SetEase(base.ease);
	}
}
