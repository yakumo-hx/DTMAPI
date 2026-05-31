using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DolocTweenFadeInoutGroup : DolocTweenDynamicWrapper
{
	private List<DolocTweenFadeInout> wrappers;

	public DolocTweenFadeInoutGroup()
	{
		wrappers = new List<DolocTweenFadeInout>();
	}

	public void join(Image image, Ease ease = Ease.Linear, float time = 1f)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(image, ease, time);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(dolocTweenFadeInout);
	}

	public void join(Text text, Ease ease = Ease.Linear, float time = 1f)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(text, ease, time);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(dolocTweenFadeInout);
	}

	public void join(TMP_Text text, Ease ease = Ease.Linear, float time = 1f)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(text, ease, time);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(dolocTweenFadeInout);
	}

	public void add(Image image, Ease ease = Ease.Linear, float time = 1f)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(image, ease, time);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.APPEND;
		wrappers.Add(dolocTweenFadeInout);
	}

	public void add(Text text, Ease ease = Ease.Linear, float time = 1f)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(text, ease, time);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.APPEND;
		wrappers.Add(dolocTweenFadeInout);
	}

	public void add(TMP_Text text, Ease ease = Ease.Linear, float time = 1f)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(text, ease, time);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.APPEND;
		wrappers.Add(dolocTweenFadeInout);
	}

	public void setAlpha(float value)
	{
		foreach (DolocTweenFadeInout wrapper in wrappers)
		{
			wrapper.alpha = value;
		}
	}

	protected override Tween build()
	{
		Sequence sequence = DOTween.Sequence();
		foreach (DolocTweenFadeInout wrapper in wrappers)
		{
			if (wrapper.connectType == DolocTweenConnectType.JOIN)
			{
				sequence.Join(wrapper.animation);
			}
			else if (wrapper.connectType == DolocTweenConnectType.APPEND)
			{
				sequence.Append(wrapper.animation);
			}
		}
		return sequence;
	}

	public void forcePlay(float alpha, Ease ease = Ease.Linear, float time = 1f, TweenCallback callback = null)
	{
		float alpha2 = Mathf.Abs(1f - alpha);
		foreach (DolocTweenFadeInout wrapper in wrappers)
		{
			wrapper.setParams(ease, time);
			wrapper.alphaTarget = alpha;
			wrapper.alpha = alpha2;
		}
		if (callback != null)
		{
			forcePlay(callback);
		}
		else
		{
			forcePlay();
		}
	}

	public void forcePlay(float alpha, float sourceAlpha, Ease ease = Ease.Linear, float time = 1f, TweenCallback callback = null)
	{
		foreach (DolocTweenFadeInout wrapper in wrappers)
		{
			wrapper.setParams(ease, time);
			wrapper.alphaTarget = alpha;
			wrapper.alpha = sourceAlpha;
		}
		if (callback != null)
		{
			forcePlay(callback);
		}
		else
		{
			forcePlay();
		}
	}
}
