using System.Linq;
using DG.Tweening;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class ProjectiveShadowManager
{
	private readonly SpriteRenderer[] _spriteRenderers;

	private readonly float[] _originalAlphas;

	private readonly bool _isEmpty;

	private float _scale;

	private Tween _tween;

	public ProjectiveShadowManager(SpriteRenderer[] shadows)
	{
		_spriteRenderers = shadows;
		SpriteRenderer[] spriteRenderers = _spriteRenderers;
		for (int i = 0; i < spriteRenderers.Length; i++)
		{
			spriteRenderers[i].gameObject.SetActive(value: true);
		}
		_originalAlphas = shadows.Select((SpriteRenderer x) => x.color.a).ToArray();
		_isEmpty = shadows.IsNullOrEmpty();
		_scale = 0f;
	}

	private void SetScale(float scale)
	{
		_scale = scale;
		for (int i = 0; i < _spriteRenderers.Length; i++)
		{
			SpriteRenderer obj = _spriteRenderers[i];
			obj.color = obj.color.Alpha(scale * _originalAlphas[i]);
		}
	}

	private void TransitAlpha(float targetScale, float duration)
	{
		_tween = DOTween.To(() => _scale, SetScale, targetScale, duration).OnComplete(delegate
		{
			_tween = null;
		});
	}

	public void SetTimeInfo(float t, WeatherType weather, bool isInitial)
	{
		if (!_isEmpty)
		{
			AnimationCurve staticProjectiveShadowIntensityCurve = DolocAPI.eftConfig.StaticProjectiveShadowIntensityCurve;
			if (staticProjectiveShadowIntensityCurve == null)
			{
				Debug.LogWarning("StaticProjectiveShadowIntensityCurve is null");
			}
			float num = ((!weather.IsSunnyWeather()) ? 0f : (staticProjectiveShadowIntensityCurve?.Evaluate(t) ?? 0f));
			_tween?.Kill();
			if (isInitial)
			{
				SetScale(num);
			}
			else
			{
				TransitAlpha(num, DolocAPI.eftConfig.StaticProjectiveShadowTransitionDuration);
			}
		}
	}

	public void Dispose()
	{
		_tween?.Kill();
	}
}
