using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

[RequireComponent(typeof(Light2D))]
public class AgentLight : MonoBehaviour
{
	private Light2D _light2D;

	private float _originalIntensity;

	private Tween _tween;

	public void Init()
	{
		_light2D = GetComponent<Light2D>();
		_originalIntensity = _light2D.intensity;
		base.gameObject.SetActive(value: false);
	}

	public void Show()
	{
		base.gameObject.SetActive(value: true);
		_tween?.Kill();
		_tween = DOTween.To(() => _light2D.intensity, delegate(float x)
		{
			_light2D.intensity = x;
		}, _originalIntensity, 1f);
	}

	public void Hide()
	{
		_tween?.Kill();
		_tween = DOTween.To(() => _light2D.intensity, delegate(float x)
		{
			_light2D.intensity = x;
		}, 0f, 1f);
		_tween.OnComplete(delegate
		{
			base.gameObject.SetActive(value: false);
			_tween = null;
		});
	}

	public void Fade(float endValue, float duration)
	{
		base.gameObject.SetActive(value: true);
		_tween?.Kill();
		_tween = DOTween.To(() => _light2D.intensity, delegate(float x)
		{
			_light2D.intensity = x;
		}, endValue, duration);
	}
}
