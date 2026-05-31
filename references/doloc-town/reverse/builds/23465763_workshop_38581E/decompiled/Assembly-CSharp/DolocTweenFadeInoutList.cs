using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DolocTweenFadeInoutList : DolocTweenDynamicWrapper
{
	public List<DolocTweenFadeInout> wrappers;

	public float alpha
	{
		set
		{
			foreach (DolocTweenFadeInout wrapper in wrappers)
			{
				wrapper.alpha = value;
			}
		}
	}

	public DolocTweenFadeInoutList(Ease ease = Ease.Linear, float time = 1f)
	{
		base.ease = ease;
		base.time = time;
		wrappers = new List<DolocTweenFadeInout>();
	}

	public void join(Text text)
	{
		wrappers.Add(new DolocTweenFadeInout(text, Ease.Linear, base.time));
	}

	public void join(TMP_Text text)
	{
		wrappers.Add(new DolocTweenFadeInout(text, Ease.Linear, base.time));
	}

	public void join(Image image)
	{
		wrappers.Add(new DolocTweenFadeInout(image, Ease.Linear, base.time));
	}

	public void join(DOGetter<Color> getter, DOSetter<Color> setter)
	{
		wrappers.Add(new DolocTweenFadeInout(getter, setter, base.ease, base.time));
	}

	protected override Tween build()
	{
		Sequence sequence = DOTween.Sequence();
		foreach (DolocTweenFadeInout wrapper in wrappers)
		{
			sequence.Join(wrapper.animation);
		}
		return sequence;
	}

	public void forcePlay(float alpha)
	{
		foreach (DolocTweenFadeInout wrapper in wrappers)
		{
			wrapper.alphaTarget = alpha;
		}
		forcePlay();
	}
}
