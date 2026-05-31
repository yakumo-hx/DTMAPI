using DG.Tweening;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class ProjectiveShadow : GameEntity, ITimeAffectable
{
	[SerializeField]
	private AnimationCurve _shadowScaleCurve;

	[SerializeField]
	private float _shadowTransitionDuration = 20f;

	private Tween _tween;

	private float _currentTargetIntensity;

	public void InitTimeAffectable()
	{
	}

	public void SetShadowIntensity(float alpha, bool isInitial)
	{
		SetShadowIntensity(alpha, _shadowTransitionDuration, isInitial);
	}

	public void SetShadowColor(Color c, bool transit = false, float duration = 3f)
	{
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (component == null)
		{
			return;
		}
		if (transit)
		{
			_tween?.Kill();
			_tween = component.DOColor(c.Alpha(_currentTargetIntensity), duration).OnComplete(delegate
			{
				_tween = null;
			});
			return;
		}
		int num;
		if (_tween != null)
		{
			num = (_tween.IsPlaying() ? 1 : 0);
			if (num != 0)
			{
				_tween.Pause();
			}
		}
		else
		{
			num = 0;
		}
		component.SetColorWithoutChangingAlpha(c);
		if (num != 0)
		{
			_tween.Play();
		}
	}

	private void SetShadowIntensity(float value, float transitionTime, bool isInitial)
	{
		_currentTargetIntensity = value;
		_tween?.Kill();
		_tween = null;
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (component == null)
		{
			return;
		}
		if (isInitial)
		{
			component.SetAlpha(value);
			return;
		}
		_tween = component.DOFade(value, transitionTime).OnComplete(delegate
		{
			_tween = null;
		});
	}

	public void SetTimeInfo(float timeProcess, WeatherType weather, bool isInitial = false)
	{
		if (!weather.IsSunnyWeather())
		{
			SetShadowIntensity(0f, isInitial);
			return;
		}
		float alpha = _shadowScaleCurve.Evaluate(timeProcess);
		SetShadowIntensity(alpha, isInitial);
	}

	public void StopTween()
	{
		_tween?.Kill();
		_tween = null;
	}

	private void OnDestroy()
	{
		StopTween();
	}
}
