using System;
using DG.Tweening;

public abstract class DolocTweenAnimation
{
	private DolocTween dolocTween;

	public float time { get; set; }

	public Ease ease { get; set; }

	public DolocTweenAnimation()
	{
		dolocTween = new DolocTween(buildAnim);
		ease = Ease.Linear;
	}

	public abstract Tween buildAnim();

	public void play(bool needWait = false)
	{
		dolocTween.play();
	}

	public void play(Action callback, bool needWait = false)
	{
		dolocTween.play(callback, needWait);
	}

	public void setBaseArgs(Ease ease, float time)
	{
		this.ease = ease;
		this.time = time;
	}
}
