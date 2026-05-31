using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DolocTweenDynamicWrapperList : DolocTweenDynamicWrapper
{
	private List<DolocTweenDynamicWrapper> wrappers;

	public DolocTweenDynamicWrapperList()
	{
		wrappers = new List<DolocTweenDynamicWrapper>();
	}

	public DolocTweenDynamicWrapper getWrapper(int idx)
	{
		if (wrappers.Count > idx)
		{
			return wrappers[idx];
		}
		return null;
	}

	public void join(DolocTweenDynamicWrapper wrapper)
	{
		wrapper.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(wrapper);
	}

	public void add(DolocTweenDynamicWrapper wrapper)
	{
		wrapper.connectType = DolocTweenConnectType.APPEND;
		wrappers.Add(wrapper);
	}

	public void joinLocalMove(Transform transform)
	{
		DolocTweenLocalMove dolocTweenLocalMove = new DolocTweenLocalMove(transform);
		dolocTweenLocalMove.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(dolocTweenLocalMove);
	}

	public void joinLocalMove(Transform transform, Ease ease, float time)
	{
		DolocTweenLocalMove dolocTweenLocalMove = new DolocTweenLocalMove(transform, ease, time);
		dolocTweenLocalMove.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(dolocTweenLocalMove);
	}

	public void joinFadeInout(Image image)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(image);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(dolocTweenFadeInout);
	}

	public void joinFadeInout(Image image, Ease ease, float time)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(image, ease, time);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(dolocTweenFadeInout);
	}

	public void joinFadeInout(Text text)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(text);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(dolocTweenFadeInout);
	}

	public void joinFadeInout(Text text, Ease ease, float time)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(text, ease, time);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(dolocTweenFadeInout);
	}

	public void setParams(int idx, Ease ease, float time)
	{
		if (wrappers.Count > idx)
		{
			wrappers[idx].setParams(ease, time);
		}
	}

	public void setParamsLocalMove(int idx, Vector3 position)
	{
		if (wrappers.Count > idx)
		{
			DolocTweenDynamicWrapper dolocTweenDynamicWrapper = wrappers[idx];
			if (dolocTweenDynamicWrapper is DolocTweenLocalMove)
			{
				((DolocTweenLocalMove)dolocTweenDynamicWrapper).position = position;
			}
		}
	}

	public void setParamsFadeInout(int idx, float alpha)
	{
		if (wrappers.Count > idx)
		{
			DolocTweenDynamicWrapper dolocTweenDynamicWrapper = wrappers[idx];
			if (dolocTweenDynamicWrapper is DolocTweenFadeInout)
			{
				((DolocTweenFadeInout)dolocTweenDynamicWrapper).alphaTarget = alpha;
			}
		}
	}

	public void setParamsFadeInout(int idx, float alpha, Ease ease, float time)
	{
		if (wrappers.Count > idx)
		{
			DolocTweenDynamicWrapper dolocTweenDynamicWrapper = wrappers[idx];
			if (dolocTweenDynamicWrapper is DolocTweenFadeInout)
			{
				dolocTweenDynamicWrapper.setParams(ease, time);
				((DolocTweenFadeInout)dolocTweenDynamicWrapper).alphaTarget = alpha;
			}
		}
	}

	protected override Tween build()
	{
		Sequence sequence = DOTween.Sequence();
		foreach (DolocTweenDynamicWrapper wrapper in wrappers)
		{
			if (wrapper.connectType == DolocTweenConnectType.JOIN)
			{
				sequence.Join(wrapper.animation);
			}
			else
			{
				sequence.Append(wrapper.animation);
			}
		}
		return sequence;
	}

	public void iterate(Action<DolocTweenDynamicWrapper> handle)
	{
		wrappers.ForEach(handle);
	}

	public void _toAlpha(float alpha)
	{
		foreach (DolocTweenFadeInout wrapper in wrappers)
		{
			wrapper.alphaTarget = alpha;
		}
		forcePlay();
	}

	public void _toAlpha(float alpha, Ease ease, float time)
	{
		foreach (DolocTweenDynamicWrapper wrapper in wrappers)
		{
			wrapper.setParams(ease, time);
			((DolocTweenFadeInout)wrapper).alphaTarget = alpha;
		}
		forcePlay();
	}

	public void _toAlpha(float alpha, TweenCallback callback)
	{
		foreach (DolocTweenFadeInout wrapper in wrappers)
		{
			wrapper.alphaTarget = alpha;
		}
		forcePlay(callback);
	}

	public void _toAlpha(float alpha, Ease ease, float time, TweenCallback callback)
	{
		foreach (DolocTweenDynamicWrapper wrapper in wrappers)
		{
			wrapper.setParams(ease, time);
			((DolocTweenFadeInout)wrapper).alphaTarget = alpha;
		}
		forcePlay(callback);
	}
}
