using System;
using DG.Tweening;

public class DolocTween
{
	private Tween anim;

	private bool isPlaying;

	private Func<Tween> animBuilder;

	public DolocTween(Func<Tween> animBuilder)
	{
		this.animBuilder = animBuilder;
		anim = null;
		isPlaying = false;
	}

	public void play(bool needWait = false)
	{
		__play(delegate
		{
			anim = null;
			isPlaying = false;
		}, needWait);
	}

	public void play(Action callback, bool needWait = false)
	{
		isPlaying = true;
		__play(delegate
		{
			anim = null;
			isPlaying = false;
			callback();
		}, needWait);
	}

	private void __play(Action callback, bool needWait)
	{
		if (!needWait || !isPlaying)
		{
			isPlaying = true;
			if (anim != null)
			{
				anim.Kill();
			}
			anim = animBuilder();
			anim.OnComplete(delegate
			{
				callback();
			});
		}
	}
}
