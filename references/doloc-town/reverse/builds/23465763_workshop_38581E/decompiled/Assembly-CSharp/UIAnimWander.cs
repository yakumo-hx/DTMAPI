using DG.Tweening;
using UnityEngine;

public class UIAnimWander : DolocTweenAnimation
{
	private Transform transform;

	private Vector2 showPos;

	private Vector2 hidePos;

	private Vector2 __to;

	public UIAnimWander(Transform transform, Ease ease, float time)
	{
		this.transform = transform;
		setBaseArgs(ease, time);
	}

	public override Tween buildAnim()
	{
		return transform.DOLocalMove(__to, base.time).SetEase(base.ease);
	}

	public void repos(Vector2 offset)
	{
		showPos = transform.localPosition;
		hidePos = showPos + offset;
	}

	public void show()
	{
		__to = showPos;
		play();
	}

	public void hide()
	{
		__to = hidePos;
		play();
	}
}
