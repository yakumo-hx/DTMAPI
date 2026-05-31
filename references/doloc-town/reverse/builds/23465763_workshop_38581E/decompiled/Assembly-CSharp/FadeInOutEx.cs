using System;
using DG.Tweening;
using UnityEngine;

public class FadeInOutEx
{
	private Material mat;

	private Tweener animation;

	private float value
	{
		get
		{
			return mat.GetFloat("_Lightness");
		}
		set
		{
			mat.SetFloat("_Lightness", value);
		}
	}

	public FadeInOutEx(Material mat)
	{
		this.mat = mat;
	}

	private void Play(float time, Ease ease, float endvalue, Action callback)
	{
		animation?.Kill();
		animation = DOTween.To(() => value, delegate(float v)
		{
			value = v;
		}, endvalue, time).SetEase(ease).OnComplete(delegate
		{
			callback?.Invoke();
		});
	}

	public void FadeIn(float time, Action callback = null)
	{
		Play(time, Ease.InOutSine, 0f, callback);
	}

	public void FadeOut(float time, Action callback = null)
	{
		Play(time, Ease.InOutSine, 1f, callback);
	}
}
