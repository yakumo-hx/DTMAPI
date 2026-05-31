using System;
using DG.Tweening;
using UnityEngine;

public class UIAnimMovingFadeInout : DolocTweenAnimation
{
	private Transform transform;

	private UIAnimFadeInout fadeInout;

	private float alpha2;

	private Action setAlpha;

	public Vector3 to { get; set; }

	public float alpha { get; set; }

	public UIAnimMovingFadeInout(Transform transform, UIAnimFadeInout fadeInout)
	{
		this.transform = transform;
		this.fadeInout = fadeInout;
		setAlpha = setFadeInoutAlpha;
	}

	public void setArgs(Ease alphaEase, Ease movingEase, float time)
	{
		base.ease = movingEase;
		base.time = time;
		fadeInout.setArgs(time, alphaEase);
	}

	public void setFadeInoutAlpha()
	{
		fadeInout.alpha = alpha;
		if (fadeInout is UIAnimFadeInoutGroupEx)
		{
			((UIAnimFadeInoutGroupEx)fadeInout).alphaText = alpha;
		}
	}

	public void setFadeInoutAlphaEx()
	{
		fadeInout.alpha = alpha;
		if (fadeInout is UIAnimFadeInoutGroupEx)
		{
			((UIAnimFadeInoutGroupEx)fadeInout).alphaText = alpha2;
		}
	}

	public override Tween buildAnim()
	{
		Sequence sequence = DOTween.Sequence();
		setAlpha();
		sequence.Join(fadeInout.buildAnim());
		sequence.Join(transform.DOLocalMove(to, base.time).SetEase(base.ease));
		return sequence;
	}

	public void play(Vector3 to, float alpha)
	{
		this.to = to;
		this.alpha = alpha;
		setAlpha = setFadeInoutAlpha;
		play();
	}

	public void play(Vector3 to, float alpha, float alpha2)
	{
		this.to = to;
		this.alpha = alpha;
		this.alpha2 = alpha2;
		setAlpha = setFadeInoutAlphaEx;
		play();
	}

	public void play(Vector3 to, float alpha, float alpha2, Action callback)
	{
		this.to = to;
		this.alpha = alpha;
		this.alpha2 = alpha2;
		setAlpha = setFadeInoutAlphaEx;
		play(callback);
	}

	public void play(Vector3 to, float alpha, Action callback)
	{
		this.to = to;
		this.alpha = alpha;
		play(callback);
	}

	public T fetchAlphaHandler<T>() where T : UIAnimFadeInout
	{
		return (T)fadeInout;
	}

	public void setAlphaImmediately(float value)
	{
		fadeInout.setAlphaImmediately(value);
	}
}
