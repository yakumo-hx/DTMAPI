using DG.Tweening;
using DG.Tweening.Core;

public class DolocTweenValue : DolocTweenDynamicWrapper
{
	private float target;

	private DOGetter<float> getter;

	private DOSetter<float> setter;

	public DolocTweenValue()
	{
	}

	public DolocTweenValue(DOGetter<float> getter, DOSetter<float> setter)
	{
		this.getter = getter;
		this.setter = setter;
	}

	public DolocTweenValue(DOGetter<float> getter, DOSetter<float> setter, Ease ease, float time)
		: base(ease, time)
	{
		this.getter = getter;
		this.setter = setter;
	}

	protected override Tween build()
	{
		return DOTween.To(getter, setter, target, base.time).SetEase(base.ease);
	}

	public void play(float value)
	{
		target = value;
		play();
	}

	public void play(float value, TweenCallback cb)
	{
		target = value;
		play(cb);
	}

	public void forcePlay(float value)
	{
		target = value;
		forcePlay();
	}

	public void forcePlay(float value, TweenCallback cb)
	{
		target = value;
		forcePlay(cb);
	}
}
