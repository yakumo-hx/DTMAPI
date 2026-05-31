using System.Linq;
using DG.Tweening;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

public class SunSpotManager
{
	private readonly Light2D[] _sunspots;

	private readonly bool isEmpty;

	private readonly float[] _originalIntensities;

	private float _intensityScale;

	private Tween _tween;

	public SunSpotManager(Light2D[] sunspots)
	{
		_sunspots = sunspots;
		isEmpty = sunspots.IsNullOrEmpty();
		if (!isEmpty)
		{
			Light2D[] sunspots2 = _sunspots;
			for (int i = 0; i < sunspots2.Length; i++)
			{
				sunspots2[i].gameObject.SetActive(value: true);
			}
			_originalIntensities = _sunspots.Select((Light2D x) => x.intensity).ToArray();
		}
	}

	private void _SetIntensity(float value)
	{
		_intensityScale = value;
		_tween?.Kill();
		if (!isEmpty)
		{
			for (int i = 0; i < _sunspots.Length; i++)
			{
				_sunspots[i].intensity = _intensityScale * _originalIntensities[i];
			}
		}
	}

	private void _TransitIntensity(float value, float duration)
	{
		_tween?.Kill();
		_tween = DOTween.To(() => _intensityScale, delegate(float v)
		{
			_intensityScale = v;
		}, value, duration).OnUpdate(delegate
		{
			for (int i = 0; i < _sunspots.Length; i++)
			{
				_sunspots[i].intensity = _intensityScale * _originalIntensities[i];
			}
		}).OnComplete(delegate
		{
			_tween = null;
		});
	}

	public void SetTimeInfo(float t, WeatherType weather, bool isInitial)
	{
		if (!isEmpty)
		{
			AnimationCurve sunSpotIntensityCurve = DolocAPI.eftConfig.SunSpotIntensityCurve;
			if (sunSpotIntensityCurve == null)
			{
				Debug.LogWarning("SunSpotIntensityCurve is null");
			}
			float value = ((!weather.IsSunnyWeather()) ? 0f : (sunSpotIntensityCurve?.Evaluate(t) ?? 0f));
			if (isInitial)
			{
				_SetIntensity(value);
			}
			else
			{
				_TransitIntensity(value, DolocAPI.eftConfig.SunSpotTransitionDuration);
			}
		}
	}

	public void Dispose()
	{
		_tween?.Kill();
	}
}
