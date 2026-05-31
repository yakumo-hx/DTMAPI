using DG.Tweening;
using DG.Tweening.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DolocTweenFadeInout : DolocTweenDynamicWrapper
{
	private DolocTweenColorHandler colorBase;

	public Color color
	{
		get
		{
			return colorBase.colorGetter();
		}
		set
		{
			colorBase.colorSetter(value);
		}
	}

	public float alpha
	{
		set
		{
			Color pNewValue = colorBase.colorGetter();
			pNewValue.a = value;
			colorBase.colorSetter(pNewValue);
		}
	}

	public float alphaTarget { get; set; }

	public DolocTweenFadeInout(Text text)
	{
		colorBase = new DolocTweenColorHandler(text);
	}

	public DolocTweenFadeInout(Text text, Ease ease, float time)
		: base(ease, time)
	{
		colorBase = new DolocTweenColorHandler(text);
	}

	public DolocTweenFadeInout(Image img)
	{
		colorBase = new DolocTweenColorHandler(img);
	}

	public DolocTweenFadeInout(Image img, Ease ease, float time)
		: base(ease, time)
	{
		colorBase = new DolocTweenColorHandler(img);
	}

	public DolocTweenFadeInout(TMP_Text text)
	{
		colorBase = new DolocTweenColorHandler(text);
	}

	public DolocTweenFadeInout(TMP_Text text, Ease ease, float time)
		: base(ease, time)
	{
		colorBase = new DolocTweenColorHandler(text);
	}

	public DolocTweenFadeInout(DOGetter<Color> getter, DOSetter<Color> setter)
	{
		colorBase = new DolocTweenColorHandler(getter, setter);
	}

	public DolocTweenFadeInout(DOGetter<Color> getter, DOSetter<Color> setter, Ease ease, float time)
		: base(ease, time)
	{
		colorBase = new DolocTweenColorHandler(getter, setter);
	}

	protected override Tween build()
	{
		return DOTween.ToAlpha(colorBase.colorGetter, colorBase.colorSetter, alphaTarget, base.time).SetEase(base.ease);
	}

	public void play(float alpha)
	{
		alphaTarget = alpha;
		play();
	}

	public void play(float alpha, TweenCallback cb)
	{
		alphaTarget = alpha;
		play(cb);
	}

	public void forcePlay(float alpha)
	{
		alphaTarget = alpha;
		forcePlay();
	}

	public void forcePlay(float alpha, TweenCallback cb)
	{
		alphaTarget = alpha;
		forcePlay(cb);
	}
}
