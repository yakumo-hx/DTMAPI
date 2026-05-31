using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

public class LightController
{
	private readonly System.Random _random;

	private readonly Dictionary<int, float> sourceIntensityBuffer = new Dictionary<int, float>();

	private readonly Light2D[] lights;

	private SpriteRenderer[] spriteRenderers;

	private Tween _tween;

	private float RandomValue => (float)_random.NextDouble();

	public static LightController FromGameObject(GameObject gameObject)
	{
		return new LightController(gameObject.GetComponentsInChildren<Light2D>(includeInactive: true), gameObject.GetComponentsInChildren<SpriteRenderer>(includeInactive: true));
	}

	public static LightController FromLights(Light2D[] lights)
	{
		List<Light2D> list = new List<Light2D>();
		List<SpriteRenderer> list2 = new List<SpriteRenderer>();
		foreach (Light2D light2D in lights)
		{
			list.AddRange(light2D.GetComponentsInChildren<Light2D>(includeInactive: true));
			list2.AddRange(light2D.GetComponentsInChildren<SpriteRenderer>(includeInactive: true));
		}
		return new LightController(list.ToArray(), list2.ToArray());
	}

	private LightController(Light2D[] lights = null, SpriteRenderer[] renderers = null)
	{
		this.lights = lights ?? Array.Empty<Light2D>();
		spriteRenderers = renderers ?? Array.Empty<SpriteRenderer>();
		Light2D[] array = this.lights;
		foreach (Light2D light2D in array)
		{
			if (!sourceIntensityBuffer.TryAdd(light2D.GetHashCode(), light2D.intensity))
			{
				Debug.LogWarning("初始化LightController时遇到异常: 灯光" + light2D.name + "哈希值重复");
			}
		}
		SpriteRenderer[] array2 = spriteRenderers;
		foreach (SpriteRenderer spriteRenderer in array2)
		{
			if (!sourceIntensityBuffer.TryAdd(spriteRenderer.GetHashCode(), spriteRenderer.color.a))
			{
				Debug.LogWarning("初始化LightController时遇到异常: 渲染器" + spriteRenderer.name + "哈希值重复");
			}
		}
		_random = new System.Random(Guid.NewGuid().GetHashCode());
	}

	public void Exclude(SpriteRenderer spriteRenderer)
	{
		List<SpriteRenderer> list = new List<SpriteRenderer>();
		SpriteRenderer[] array = spriteRenderers;
		foreach (SpriteRenderer spriteRenderer2 in array)
		{
			if (!(spriteRenderer2 == spriteRenderer))
			{
				list.Add(spriteRenderer2);
			}
		}
		spriteRenderers = list.ToArray();
		sourceIntensityBuffer.Remove(spriteRenderer.GetInstanceID());
	}

	private float GetSourceIntensity(object obj)
	{
		return sourceIntensityBuffer.GetValueOrDefault(obj.GetHashCode(), 0f);
	}

	public void SetIntensity(float value)
	{
		Light2D[] array = lights;
		foreach (Light2D light2D in array)
		{
			if (light2D == null)
			{
				break;
			}
			light2D.intensity = value * GetSourceIntensity(light2D);
		}
		SpriteRenderer[] array2 = spriteRenderers;
		foreach (SpriteRenderer spriteRenderer in array2)
		{
			if (!(spriteRenderer == null))
			{
				spriteRenderer.color = spriteRenderer.color.Alpha(value * GetSourceIntensity(spriteRenderer));
				continue;
			}
			break;
		}
	}

	public void FlickerFlat(float duration)
	{
		_tween?.Kill();
		float value = 0f;
		DOTween.To(() => value, delegate(float v)
		{
			value = v;
			SetIntensity(RandomValue);
		}, 1f, duration).OnComplete(delegate
		{
			SetIntensity(1f);
			_tween = null;
		});
	}

	public void FlickerLinearAttenuation(float duration = 1f, Action<float> onFlicker = null)
	{
		_tween?.Kill();
		float value = 1f;
		DOTween.To(() => value, delegate(float v)
		{
			value = v;
			float num = Mathf.Lerp(RandomValue, 1f, v);
			SetIntensity(num);
			onFlicker?.Invoke(num);
		}, 0f, duration).OnComplete(delegate
		{
			SetIntensity(1f);
			_tween = null;
		});
	}

	public void TurnOnLinear(float duration)
	{
		_tween?.Kill();
		float value = 0f;
		DOTween.To(() => value, delegate(float x)
		{
			value = x;
			SetIntensity(x);
		}, 1f, duration).OnComplete(delegate
		{
			_tween = null;
		});
	}

	public void TurnOnFlicker(float duration)
	{
		_tween?.Kill();
		float value = 0f;
		DOTween.To(() => value, delegate(float x)
		{
			value = x;
			SetIntensity(Mathf.Lerp(RandomValue * x, 1f, x));
		}, 1f, duration).OnComplete(delegate
		{
			SetIntensity(1f);
			_tween = null;
		});
	}

	public void TurnOffLinear(float duration)
	{
		_tween?.Kill();
		float value = 1f;
		DOTween.To(() => value, delegate(float x)
		{
			value = x;
			SetIntensity(x);
		}, 0f, duration).OnComplete(delegate
		{
			SetIntensity(0f);
			_tween = null;
		});
	}

	public void Dispose()
	{
		_tween?.Kill();
		_tween = null;
	}
}
