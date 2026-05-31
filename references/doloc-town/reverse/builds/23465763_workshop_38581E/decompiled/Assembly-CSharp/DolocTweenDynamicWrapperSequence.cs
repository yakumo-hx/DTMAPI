using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DolocTweenDynamicWrapperSequence : DolocTweenDynamicWrapper
{
	private Dictionary<string, DolocTweenDynamicWrapper> wrappers;

	public DolocTweenDynamicWrapperSequence()
	{
		wrappers = new Dictionary<string, DolocTweenDynamicWrapper>();
	}

	public DolocTweenDynamicWrapper getWrapper(string name)
	{
		wrappers.TryGetValue(name, out var value);
		return value;
	}

	public void join(string name, DolocTweenDynamicWrapper wrapper)
	{
		wrapper.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(name, wrapper);
	}

	public void joinLocalMove(string name, Transform transform)
	{
		DolocTweenLocalMove dolocTweenLocalMove = new DolocTweenLocalMove(transform);
		dolocTweenLocalMove.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(name, dolocTweenLocalMove);
	}

	public void joinLocalMove(string name, Transform transform, Ease ease, float time)
	{
		DolocTweenLocalMove dolocTweenLocalMove = new DolocTweenLocalMove(transform, ease, time);
		dolocTweenLocalMove.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(name, dolocTweenLocalMove);
	}

	public void joinFadeInout(string name, Image image)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(image);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(name, dolocTweenFadeInout);
	}

	public void joinFadeInout(string name, Image image, Ease ease, float time)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(image, ease, time);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(name, dolocTweenFadeInout);
	}

	public void joinFadeInout(string name, Text text)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(text);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(name, dolocTweenFadeInout);
	}

	public void joinFadeInout(string name, Text text, Ease ease, float time)
	{
		DolocTweenFadeInout dolocTweenFadeInout = new DolocTweenFadeInout(text, ease, time);
		dolocTweenFadeInout.connectType = DolocTweenConnectType.JOIN;
		wrappers.Add(name, dolocTweenFadeInout);
	}

	public void setParams(string name, Ease ease, float time)
	{
		if (wrappers.ContainsKey(name))
		{
			wrappers[name].setParams(ease, time);
		}
	}

	public void setParamsLocalMove(string name, Vector3 position)
	{
		if (wrappers.ContainsKey(name))
		{
			DolocTweenDynamicWrapper dolocTweenDynamicWrapper = wrappers[name];
			if (dolocTweenDynamicWrapper is DolocTweenLocalMove)
			{
				((DolocTweenLocalMove)dolocTweenDynamicWrapper).position = position;
			}
		}
	}

	public void setParamsLocalMove(string name, Vector3 position, Ease ease, float time)
	{
		if (wrappers.ContainsKey(name))
		{
			DolocTweenDynamicWrapper dolocTweenDynamicWrapper = wrappers[name];
			if (dolocTweenDynamicWrapper is DolocTweenLocalMove)
			{
				dolocTweenDynamicWrapper.setParams(ease, time);
				((DolocTweenLocalMove)dolocTweenDynamicWrapper).position = position;
			}
		}
	}

	public void setParamsFadeInout(string name, float alpha)
	{
		if (wrappers.ContainsKey(name))
		{
			DolocTweenDynamicWrapper dolocTweenDynamicWrapper = wrappers[name];
			if (dolocTweenDynamicWrapper is DolocTweenFadeInout)
			{
				((DolocTweenFadeInout)dolocTweenDynamicWrapper).alphaTarget = alpha;
			}
		}
	}

	public void setParamsFadeInout(string name, float alpha, Ease ease, float time)
	{
		if (wrappers.ContainsKey(name))
		{
			DolocTweenDynamicWrapper dolocTweenDynamicWrapper = wrappers[name];
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
		foreach (DolocTweenDynamicWrapper value in wrappers.Values)
		{
			if (value.connectType == DolocTweenConnectType.JOIN)
			{
				sequence.Join(value.animation);
			}
			else
			{
				sequence.Append(value.animation);
			}
		}
		return sequence;
	}
}
