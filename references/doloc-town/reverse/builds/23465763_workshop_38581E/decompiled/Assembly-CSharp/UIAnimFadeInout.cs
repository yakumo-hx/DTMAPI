using DG.Tweening;

public abstract class UIAnimFadeInout : DolocTweenAnimation
{
	public float alpha { get; set; }

	public virtual void setArgs(float time, Ease ease)
	{
		base.time = time;
		base.ease = ease;
	}

	public abstract void setAlphaImmediately(float value);
}
