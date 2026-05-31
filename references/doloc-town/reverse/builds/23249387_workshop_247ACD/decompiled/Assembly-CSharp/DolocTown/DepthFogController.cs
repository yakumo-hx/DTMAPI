using System;
using DG.Tweening;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class DepthFogController : DolocObject
{
	[SerializeField]
	private DepthFogConfigSO _defultConfigSo;

	[SerializeField]
	private DepthFogConfigSO[] _configSos;

	private SpriteRenderer[] fogRenderers;

	private bool _isEnabled;

	private bool _isInRoom;

	private bool _shouldShowBackground;

	private Tween _currentTween;

	protected override void __Init()
	{
		base.__Init();
		fogRenderers = base.transform.GetComponentsInChildren<SpriteRenderer>();
		ClearDensity(shouldTransit: false);
		SetVisible(value: false);
	}

	public void SetInRoom(bool value)
	{
		_isInRoom = value;
		SetEnabled(!_isInRoom);
	}

	private void LoadConfig(DepthFogConfigSO config)
	{
		if (config == null)
		{
			return;
		}
		for (int i = 0; i < fogRenderers.Length; i++)
		{
			if (!(base.transform.GetChild(i) == null))
			{
				SpriteRenderer spriteRenderer = fogRenderers[i];
				if (!(spriteRenderer == null))
				{
					spriteRenderer.color = config.GetColor(i);
				}
			}
		}
	}

	private Color[] GetColors(int lv)
	{
		Color[] array = new Color[base.transform.childCount];
		if (_configSos.IsNullOrEmpty() || lv < 0 || lv >= _configSos.Length)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = Color.clear;
			}
			return array;
		}
		DepthFogConfigSO depthFogConfigSO = _configSos[lv];
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = depthFogConfigSO.GetColor(j);
		}
		return array;
	}

	public void SetDensity(int lv, bool transit, float duration = 5f)
	{
		__SetColors(GetColors(lv), transit, duration);
	}

	public void ClearDensity(bool shouldTransit, float duration = 5f)
	{
		_currentTween?.Kill();
		if (!shouldTransit)
		{
			for (int i = 0; i < fogRenderers.Length; i++)
			{
				fogRenderers[i].color = _defultConfigSo.GetColor(i);
			}
			return;
		}
		Sequence sequence = DOTween.Sequence();
		for (int j = 0; j < fogRenderers.Length; j++)
		{
			fogRenderers[j].DOColor(_defultConfigSo.GetColor(j), duration);
		}
		sequence.OnComplete(delegate
		{
			_currentTween = null;
		});
		_currentTween = sequence;
	}

	private void __SetColors(Color[] colors, bool transit, float duration, Action callback = null)
	{
		_currentTween?.Kill();
		if (transit)
		{
			Sequence sequence = DOTween.Sequence();
			for (int i = 0; i < base.transform.childCount; i++)
			{
				SpriteRenderer component = base.transform.GetChild(i).GetComponent<SpriteRenderer>();
				if (!(component == null))
				{
					if (colors[i].a > 0f)
					{
						sequence.Join(component.DOColor(colors[i], duration));
						continue;
					}
					component.color = DolocUtils.setAlpha(colors[i], 0f);
					sequence.Join(component.DOFade(colors[i].a, duration));
				}
			}
			sequence.OnComplete(delegate
			{
				_currentTween = null;
				callback?.Invoke();
			});
			_currentTween = sequence;
			return;
		}
		for (int j = 0; j < base.transform.childCount; j++)
		{
			SpriteRenderer component2 = base.transform.GetChild(j).GetComponent<SpriteRenderer>();
			if (!(component2 == null))
			{
				component2.color = colors[j];
				callback?.Invoke();
			}
		}
	}

	public void SetEnabled(bool value)
	{
		_isEnabled = value;
		SetVisible(_isEnabled && !_isInRoom);
	}
}
