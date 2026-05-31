using DG.Tweening;

public class DolocTweenStatic : IDolocTween
{
	private Tween animation;

	public bool isPlaying => animation.IsPlaying();

	public Tween currentAnimation => animation;

	public DolocTweenStatic(Tween tween)
	{
		animation = tween;
		animation.SetAutoKill(autoKillOnCompletion: false);
		animation.Pause();
	}

	public void play()
	{
		if (!isPlaying)
		{
			animation.Restart();
		}
	}

	public void play(TweenCallback callback)
	{
		if (!isPlaying)
		{
			animation.OnStepComplete(callback);
			animation.Restart();
		}
	}

	public void forcePlay()
	{
		animation.Restart();
	}

	public void forcePlay(TweenCallback callback)
	{
		animation.Pause();
		animation.OnComplete(callback);
		animation.Restart();
	}

	public void add(Tween t)
	{
		toSequence();
		((Sequence)animation).Append(t);
	}

	public void join(Tween t)
	{
		toSequence();
		((Sequence)animation).Join(t);
	}

	private void toSequence()
	{
		if (!(animation is Sequence))
		{
			Sequence sequence = DOTween.Sequence();
			sequence.SetAutoKill(autoKillOnCompletion: false);
			sequence.Join(animation);
			sequence.Pause();
			animation = sequence;
		}
	}

	public void stop()
	{
		animation.Pause();
	}
}
