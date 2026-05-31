using DG.Tweening;
using UnityEngine;

public class UIAnimScale : DolocTweenAnimation
{
	public Transform transform;

	public float scaleTo { get; set; }

	public float scaleFrom { get; set; }

	public UIAnimScale(Transform transform)
	{
		this.transform = transform;
		scaleFrom = this.transform.localScale.x;
	}

	public UIAnimScale(Transform transform, Ease ease, float time)
	{
		this.transform = transform;
		scaleFrom = this.transform.localScale.x;
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
		Sequence sequence = DOTween.Sequence();
		sequence.Append(transform.DOScale(scaleTo, base.time).SetEase(base.ease));
		sequence.Append(transform.DOScale(scaleFrom, base.time).SetEase(base.ease));
		return sequence;
	}

	public void play(float scale)
	{
		scaleTo = scale;
		play();
	}
}
