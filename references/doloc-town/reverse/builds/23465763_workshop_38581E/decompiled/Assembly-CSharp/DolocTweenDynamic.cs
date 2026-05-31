using System;
using DG.Tweening;

public class DolocTweenDynamic : IDolocTween
{
	private Tween animation;

	private Func<Tween> generator;

	public bool isPlaying
	{
		get
		{
			if (animation != null)
			{
				return animation.IsPlaying();
			}
			return false;
		}
	}

	public DolocTweenDynamic(Func<Tween> generator)
	{
		this.generator = generator;
		animation = null;
	}

	public void Kill()
	{
		animation?.Kill();
	}

	public void play()
	{
		if (!isPlaying)
		{
			animation = generator();
			animation.OnComplete(delegate
			{
				animation = null;
			});
		}
	}

	public void play(TweenCallback cb)
	{
		if (!isPlaying)
		{
			animation = generator();
			animation.OnComplete(delegate
			{
				cb?.Invoke();
				animation = null;
			});
		}
	}

	public void forcePlay()
	{
		if (isPlaying)
		{
			animation.Kill();
		}
		animation = generator();
		animation.OnComplete(delegate
		{
			animation = null;
		});
	}

	public void forcePlay(TweenCallback cb)
	{
		if (isPlaying)
		{
			animation.Kill();
		}
		animation = generator();
		animation.OnComplete(delegate
		{
			cb?.Invoke();
			animation = null;
		});
	}

	public void join(Tween t)
	{
	}

	public void add(Tween t)
	{
	}

	public void stop()
	{
		if (isPlaying)
		{
			animation.Kill();
			animation = null;
		}
	}
}
