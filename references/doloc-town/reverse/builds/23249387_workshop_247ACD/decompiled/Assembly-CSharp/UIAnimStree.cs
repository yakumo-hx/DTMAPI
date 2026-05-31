using DG.Tweening;
using UnityEngine;

public class UIAnimStree
{
	private InifiniteDolocTween animation;

	public UIAnimStree(Transform transform, Vector2 from, Vector2 to, float time, Ease ease)
	{
		transform.localPosition = from;
		Sequence sequence = DOTween.Sequence();
		sequence.Append(transform.DOLocalMove(to, time).SetEase(ease));
		sequence.Append(transform.DOLocalMove(from, time).SetEase(ease));
		animation = new InifiniteDolocTween(sequence);
	}

	public void start()
	{
		animation.start();
	}

	public void stop()
	{
		animation.stop();
	}
}
