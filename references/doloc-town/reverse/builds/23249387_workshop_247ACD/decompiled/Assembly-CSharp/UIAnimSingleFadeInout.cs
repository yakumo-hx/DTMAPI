using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.UI;

public class UIAnimSingleFadeInout : UIAnimFadeInout
{
	private DOGetter<Color> getter;

	private DOSetter<Color> setter;

	public UIAnimSingleFadeInout(SpriteRenderer sp)
	{
		getter = () => sp.color;
		setter = delegate(Color c)
		{
			sp.color = c;
		};
	}

	public UIAnimSingleFadeInout(Image image)
	{
		getter = () => image.color;
		setter = delegate(Color c)
		{
			image.color = c;
		};
	}

	public UIAnimSingleFadeInout(Text text)
	{
		getter = () => text.color;
		setter = delegate(Color c)
		{
			text.color = c;
		};
	}

	public void play(float alpha)
	{
		base.alpha = alpha;
		play();
	}

	public override Tween buildAnim()
	{
		return DOTween.ToAlpha(getter, setter, base.alpha, base.time).SetEase(base.ease);
	}

	public override void setAlphaImmediately(float value)
	{
		setter(DolocUtils.setAlpha(getter(), value));
	}
}
