using System.Collections.Generic;
using DG.Tweening;

public class UIAnimFadeInoutArray : UIAnimFadeInout
{
	private List<UIAnimSingleFadeInout> list;

	public UIAnimFadeInoutArray()
	{
		list = new List<UIAnimSingleFadeInout>();
	}

	public override Tween buildAnim()
	{
		Sequence sequence = DOTween.Sequence();
		foreach (UIAnimSingleFadeInout item in list)
		{
			item.alpha = base.alpha;
			sequence.Join(item.buildAnim());
		}
		return sequence;
	}

	public override void setAlphaImmediately(float value)
	{
		foreach (UIAnimSingleFadeInout item in list)
		{
			item.setAlphaImmediately(value);
		}
	}

	public override void setArgs(float time, Ease ease)
	{
		foreach (UIAnimSingleFadeInout item in list)
		{
			item.setArgs(time, ease);
		}
	}

	public void add(UIAnimSingleFadeInout fadeInout)
	{
		if (!list.Contains(fadeInout))
		{
			list.Add(fadeInout);
		}
	}

	public void play(float alpha)
	{
		foreach (UIAnimSingleFadeInout item in list)
		{
			item.play(alpha);
		}
	}
}
