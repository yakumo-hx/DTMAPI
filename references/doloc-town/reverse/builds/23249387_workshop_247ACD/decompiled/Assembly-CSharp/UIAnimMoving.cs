using System;
using DG.Tweening;
using UnityEngine;

public class UIAnimMoving : DolocTweenAnimation
{
	internal Vector3 position;

	public Transform transform { get; set; }

	public UIAnimMoving(Transform transform, Ease ease, float time)
	{
		this.transform = transform;
		base.ease = ease;
		base.time = time;
	}

	public UIAnimMoving(Transform transform)
	{
		this.transform = transform;
	}

	public void setArgs(Ease ease, float time)
	{
		base.ease = ease;
		base.time = time;
	}

	public override Tween buildAnim()
	{
		return transform.DOLocalMove(position, base.time).SetEase(base.ease);
	}

	public void play(Vector3 endPosition, Action callback, bool needWait = false)
	{
		position = endPosition;
		play(callback);
	}

	public void play(Vector2 endPosition, bool needWait = false)
	{
		position = endPosition;
		play();
	}
}
