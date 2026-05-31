using System;
using DG.Tweening;
using UnityEngine;

namespace RedSaw.UI;

public class RsMoveComponent
{
	private Transform transform;

	private Ease ease;

	private float time;

	private Tween animation;

	private Action callback;

	public RsMoveComponent(Transform transform, Ease ease = Ease.Linear, float time = 1f)
	{
		this.transform = transform;
		this.ease = ease;
		this.time = time;
	}

	private Tween build(Vector2 pos)
	{
		return transform.DOLocalMove(pos, time).SetEase(ease).OnComplete(_callback);
	}

	private void _callback()
	{
		if (callback != null)
		{
			callback();
			callback = null;
		}
	}

	public void moveTo(Vector2 position)
	{
		if (animation != null)
		{
			animation.Kill();
		}
		animation = build(position);
	}
}
