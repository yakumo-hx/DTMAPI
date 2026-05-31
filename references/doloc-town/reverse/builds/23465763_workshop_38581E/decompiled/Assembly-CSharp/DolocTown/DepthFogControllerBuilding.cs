using System;
using DG.Tweening;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;
using UnityEngine.Rendering;

namespace DolocTown;

public class DepthFogControllerBuilding : GameEntity
{
	[SerializeField]
	private DepthFogConfigSO _defultConfigSo;

	[SerializeField]
	private DepthFogConfigSO[] _depthConfigs;

	[SerializeField]
	private SpriteRenderer skyRenderer;

	[SerializeField]
	private SpriteRenderer remoteRenderer;

	[SerializeField]
	private Shader shader;

	private Tween currentTween;

	private bool _isEnabled;

	private bool _isInRoom;

	private bool _shouldShowBackground;

	protected override void __Init()
	{
		base.__Init();
		if (skyRenderer == null || remoteRenderer == null)
		{
			Debug.LogError("天空层或远景层未设置，请检查配置");
			return;
		}
		Texture2D texture2D = RedSaw.TextureUtils.CreatePureColorTexture(Color.white, 624, 270);
		Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 8f);
		skyRenderer.sprite = sprite;
		remoteRenderer.sprite = sprite;
		skyRenderer.color = _defultConfigSo.ColorSky;
		remoteRenderer.color = _defultConfigSo.ColorRemote;
		if (shader == null)
		{
			Debug.LogError("DepthFogControllerBuilding.Init: 未设置室内雾效着色器");
			return;
		}
		Material sharedMaterial = new Material(shader);
		skyRenderer.sharedMaterial = sharedMaterial;
		remoteRenderer.sharedMaterial = sharedMaterial;
		SetEnabled(value: false);
	}

	private static void SetMask(SpriteRenderer sr, bool value)
	{
		CompareFunction compareFunction = (value ? CompareFunction.Equal : CompareFunction.Disabled);
		sr.sharedMaterial.SetFloat("_StencilComp", (float)compareFunction);
	}

	public void SetInRoom(bool isInRoom, bool shouldShowBackground)
	{
		_isInRoom = isInRoom;
		_shouldShowBackground = shouldShowBackground;
		if (!isInRoom)
		{
			SetVisible(value: false);
			return;
		}
		SetMask(skyRenderer, shouldShowBackground);
		SetMask(remoteRenderer, shouldShowBackground);
		SetEnabled(shouldShowBackground);
	}

	public void SetEnabled(bool value)
	{
		_isEnabled = value;
		SetVisible(_isEnabled && _isInRoom && _shouldShowBackground);
	}

	private DepthFogConfigSO GetConfig(int lv)
	{
		if (_depthConfigs.IsNullOrEmpty() || lv < 0 || lv >= _depthConfigs.Length)
		{
			return null;
		}
		return _depthConfigs[lv];
	}

	public void SetDensity(int lv, bool transit, float duration = 5f)
	{
		DepthFogConfigSO config = GetConfig(lv);
		if (config == null)
		{
			__SetColor(_defultConfigSo.ColorSky, _defultConfigSo.ColorRemote, transit, duration);
		}
		else
		{
			__SetColor(config.ColorSky, config.ColorRemote, transit, duration);
		}
	}

	public void ClearDensity(bool shouldTransit, float duration = 5f)
	{
		__SetColor(_defultConfigSo.ColorSky, _defultConfigSo.ColorRemote, shouldTransit, duration);
	}

	private Tween __layer_transition(Color color, SpriteRenderer renderer, float duration)
	{
		if (color.a > 0f)
		{
			return renderer.DOColor(color, duration);
		}
		renderer.color = DolocUtils.setAlpha(color, 0f);
		return renderer.DOFade(color.a, duration);
	}

	public void __SetColor(Color skyColor, Color remoteColor, bool transit, float duration, Action callback = null)
	{
		currentTween?.Kill();
		if (transit)
		{
			Sequence sequence = DOTween.Sequence();
			sequence.Join(__layer_transition(skyColor, skyRenderer, duration));
			sequence.Join(__layer_transition(remoteColor, remoteRenderer, duration));
			sequence.OnComplete(delegate
			{
				callback?.Invoke();
				currentTween = null;
			});
			currentTween = sequence;
		}
		else
		{
			skyRenderer.color = skyColor;
			remoteRenderer.color = remoteColor;
			callback?.Invoke();
		}
	}
}
