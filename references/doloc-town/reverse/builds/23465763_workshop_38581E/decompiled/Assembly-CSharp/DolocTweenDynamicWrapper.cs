using DG.Tweening;

public abstract class DolocTweenDynamicWrapper
{
	public DolocTweenDynamic tween;

	public Ease ease { get; set; }

	public float time { get; set; }

	public DolocTweenConnectType connectType { get; set; }

	public Tween animation => build();

	public DolocTweenDynamicWrapper()
	{
		tween = new DolocTweenDynamic(build);
		setParams(Ease.Linear, 1f);
	}

	public DolocTweenDynamicWrapper(Ease ease, float time)
	{
		tween = new DolocTweenDynamic(build);
		setParams(ease, time);
	}

	public void Kill()
	{
		tween.Kill();
	}

	protected abstract Tween build();

	public void setParams(Ease ease, float time)
	{
		this.ease = ease;
		this.time = time;
	}

	public void play()
	{
		tween.play();
	}

	public void play(TweenCallback cb)
	{
		tween.play(cb);
	}

	public void forcePlay()
	{
		tween.forcePlay();
	}

	public void forcePlay(TweenCallback cb)
	{
		tween.forcePlay(cb);
	}

	public void stop()
	{
		tween.stop();
	}
}
