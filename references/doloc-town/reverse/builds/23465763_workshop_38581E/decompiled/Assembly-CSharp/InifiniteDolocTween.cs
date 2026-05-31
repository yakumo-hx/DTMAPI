using DG.Tweening;

public class InifiniteDolocTween
{
	private Tween animation;

	public InifiniteDolocTween(Tween anim)
	{
		animation = anim;
		animation.SetLoops(-1);
		animation.Pause();
	}

	public void start()
	{
		animation.Play();
	}

	public void stop()
	{
		animation.Pause();
	}

	public void clear()
	{
		animation.Kill();
	}
}
