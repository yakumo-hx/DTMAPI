using DG.Tweening;
using UnityEngine;

namespace DolocTown.Rendering;

public class MaterialSetter01
{
	private Material mat;

	private string propertyName;

	private float _value;

	private Tween currentTween;

	public float value
	{
		get
		{
			return _value;
		}
		set
		{
			mat.SetFloat(propertyName, value);
			_value = value;
		}
	}

	public MaterialSetter01(Material mat, string propertyName)
	{
		this.mat = mat;
		this.propertyName = propertyName;
	}

	public void Play(float to, float duration = 0.5f, Ease ease = Ease.Linear, TweenCallback callback = null)
	{
		currentTween?.Kill();
		currentTween = DOTween.To(() => value, delegate(float v)
		{
			value = v;
		}, to, duration).SetEase(ease).OnComplete(delegate
		{
			currentTween = null;
			callback?.Invoke();
		});
	}

	public void PlayBack(float inDuration = 0.3f, float outDuration = 0.2f, Ease inEase = Ease.Linear, Ease outEase = Ease.Linear, TweenCallback callback = null)
	{
		currentTween?.Kill();
		value = 1f;
		currentTween = DOTween.Sequence().Append(DOTween.To(() => value, delegate(float v)
		{
			value = v;
		}, 0f, inDuration).SetEase(inEase)).Append(DOTween.To(() => value, delegate(float v)
		{
			value = v;
		}, 1f, outDuration).SetEase(outEase))
			.OnComplete(delegate
			{
				currentTween = null;
				callback?.Invoke();
			});
	}

	public Tween PlayWait(float to, float duration = 0.5f, Ease ease = Ease.Linear, TweenCallback callback = null)
	{
		return DOTween.To(() => value, delegate(float v)
		{
			value = v;
		}, to, duration).SetEase(ease).OnComplete(delegate
		{
			callback?.Invoke();
		});
	}
}
