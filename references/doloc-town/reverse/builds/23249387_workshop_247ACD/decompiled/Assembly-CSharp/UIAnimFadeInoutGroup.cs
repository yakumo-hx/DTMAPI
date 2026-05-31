using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIAnimFadeInoutGroup : UIAnimFadeInout
{
	protected Action<Color>[] setters;

	private Color currentColor = new Color(1f, 1f, 1f, 1f);

	public Color CurrentColor
	{
		get
		{
			return currentColor;
		}
		set
		{
			currentColor = value;
			Action<Color>[] array = setters;
			for (int i = 0; i < array.Length; i++)
			{
				array[i](currentColor);
			}
		}
	}

	public float Alpha
	{
		get
		{
			return currentColor.a;
		}
		set
		{
			Color color = DolocUtils.setAlpha(currentColor, value);
			CurrentColor = color;
		}
	}

	public UIAnimFadeInoutGroup(Transform transform, Color c)
	{
		currentColor = c;
		initialize(transform);
	}

	protected virtual void initialize(Transform transform)
	{
		List<Action<Color>> list = new List<Action<Color>>();
		Text[] componentsInChildren = transform.GetComponentsInChildren<Text>();
		foreach (Text text in componentsInChildren)
		{
			list.Add(delegate(Color c)
			{
				text.color = c;
			});
		}
		Image[] componentsInChildren2 = transform.GetComponentsInChildren<Image>();
		foreach (Image image in componentsInChildren2)
		{
			list.Add(delegate(Color c)
			{
				image.color = c;
			});
		}
		setters = list.ToArray();
	}

	public override Tween buildAnim()
	{
		return DOTween.ToAlpha(() => CurrentColor, delegate(Color c)
		{
			CurrentColor = c;
		}, base.alpha, base.time).SetEase(base.ease);
	}

	public Tween buildAnim(float alpha, float time, Ease ease)
	{
		base.alpha = alpha;
		base.time = time;
		base.ease = ease;
		return buildAnim();
	}

	public override void setAlphaImmediately(float value)
	{
		Alpha = value;
	}
}
