using System;
using System.Collections.Generic;
using DG.Tweening;
using DolocTown.GameData;
using DolocTown.GameServiceLocator;
using UnityEngine;

namespace DolocTown;

public class EffectsManager : IEffectsProvider
{
	private readonly GameEntitySystem<GEMGameEffectsAttribute> _m_effects_system;

	private readonly InstantParticleEffectsManager _m_instPsManager;

	private readonly InstantGoEffectsManager _m_instGoManager;

	private readonly ContinuesParticleEffectsManager _m_continuesPsManager;

	private readonly ContinuesGoEffectsManager _m_continuesGoManager;

	private readonly FadeUpTipManager _m_fadeUpTipManager;

	private readonly Dictionary<InstAnimEffectType, string> _instAnimNameMap = new Dictionary<InstAnimEffectType, string>();

	private RuntimeAnimatorController _instAnimController;

	public EffectsManager(Transform container, int maxCount = 5)
	{
		_m_effects_system = new GameEntitySystem<GEMGameEffectsAttribute>(container);
		InitInstAnimEffectsManager();
		_InitOtherEffects();
		_m_instPsManager = new InstantParticleEffectsManager(container, maxCount);
		_m_instGoManager = new InstantGoEffectsManager(container, maxCount);
		_m_continuesPsManager = new ContinuesParticleEffectsManager(container);
		_m_continuesGoManager = new ContinuesGoEffectsManager(container);
		_m_fadeUpTipManager = new FadeUpTipManager(container);
	}

	public void Clear()
	{
		_m_effects_system.Clear();
		_m_instPsManager.Clear();
		_m_continuesPsManager.Clear();
		_m_continuesGoManager.Clear();
		_m_fadeUpTipManager.Clear();
		_m_instGoManager.Clear();
	}

	public void ClearAllContinusEffects()
	{
		_m_continuesPsManager.Clear();
	}

	void IEffectsProvider.RaiseInstPS(Vector2 positionWS, InstantParticleEffectsType type)
	{
		_m_instPsManager.Raise(positionWS, type);
	}

	void IEffectsProvider.RaiseInstPS(Vector2 positionWS, InstantParticleEffectsType type, string sortingLayer, int sortingOrder)
	{
		_m_instPsManager.Raise(positionWS, type, sortingLayer, sortingOrder);
	}

	private void InitInstAnimEffectsManager()
	{
		_instAnimController = DolocAPI.GetAsset<RuntimeAnimatorController>(DolocGameAssets.GAME_EFFECTS_ANIM);
		AnimationClip[] animationClips = _instAnimController.animationClips;
		foreach (AnimationClip animationClip in animationClips)
		{
			if (Enum.TryParse<InstAnimEffectType>(animationClip.name, ignoreCase: true, out var result))
			{
				_instAnimNameMap.Add(result, animationClip.name);
			}
		}
		_m_effects_system.SetCreatedCallback(delegate(InstantAnimationEffects R)
		{
			R.Recycle = _m_effects_system.Recycle;
			R.animator.runtimeAnimatorController = _instAnimController;
		});
	}

	InstantAnimationEffects IEffectsProvider.RaiseInstAnimCustom(Vector2 positionWS, InstAnimEffectType type)
	{
		if (!_instAnimNameMap.TryGetValue(type, out var value))
		{
			return null;
		}
		InstantAnimationEffects instantAnimationEffects = _m_effects_system.Next<InstantAnimationEffects>();
		if (instantAnimationEffects == null)
		{
			return null;
		}
		instantAnimationEffects.sp.sharedMaterial = LocMaterials.GAME_MAT_2D;
		instantAnimationEffects.sp.sortingLayerName = "GroundFront";
		instantAnimationEffects.sp.sortingOrder = 0;
		instantAnimationEffects.transform.localScale = Vector3.one;
		instantAnimationEffects.transform.localRotation = Quaternion.identity;
		instantAnimationEffects.Play(positionWS, value);
		return instantAnimationEffects;
	}

	void IEffectsProvider.RaiseInstAnim(Vector2 positionWS, InstAnimEffectType type)
	{
		if (_instAnimNameMap.TryGetValue(type, out var value))
		{
			InstantAnimationEffects instantAnimationEffects = _m_effects_system.Next<InstantAnimationEffects>();
			if (!(instantAnimationEffects == null))
			{
				instantAnimationEffects.sp.sharedMaterial = LocMaterials.GAME_MAT_2D;
				instantAnimationEffects.sp.sortingLayerName = "GroundFront";
				instantAnimationEffects.sp.sortingOrder = 0;
				instantAnimationEffects.transform.localScale = Vector3.one;
				instantAnimationEffects.transform.localRotation = Quaternion.identity;
				instantAnimationEffects.Play(positionWS, value);
			}
		}
	}

	void IEffectsProvider.RaiseInstAnim(Vector2 positionWS, InstAnimEffectType type, Vector2 dir)
	{
		if (_instAnimNameMap.TryGetValue(type, out var value))
		{
			InstantAnimationEffects instantAnimationEffects = _m_effects_system.Next<InstantAnimationEffects>();
			if (!(instantAnimationEffects == null))
			{
				instantAnimationEffects.sp.sharedMaterial = LocMaterials.GAME_MAT_2D;
				instantAnimationEffects.sp.sortingLayerName = "GroundFront";
				instantAnimationEffects.sp.sortingOrder = 0;
				instantAnimationEffects.transform.localScale = Vector3.one;
				instantAnimationEffects.transform.localRotation = dir.GetRotation();
				instantAnimationEffects.Play(positionWS, value);
			}
		}
	}

	void IEffectsProvider.RaiseInstAnim(Vector2 positionWS, InstAnimEffectType type, bool flip)
	{
		if (_instAnimNameMap.TryGetValue(type, out var value))
		{
			InstantAnimationEffects instantAnimationEffects = _m_effects_system.Next<InstantAnimationEffects>();
			if (!(instantAnimationEffects == null))
			{
				instantAnimationEffects.sp.sharedMaterial = LocMaterials.GAME_MAT_2D;
				instantAnimationEffects.sp.sortingLayerName = "GroundFront";
				instantAnimationEffects.sp.sortingOrder = 0;
				instantAnimationEffects.transform.localScale = (flip ? new Vector3(-1f, 1f, 1f) : Vector3.one);
				instantAnimationEffects.transform.localRotation = Quaternion.identity;
				instantAnimationEffects.Play(positionWS, value);
			}
		}
	}

	void IEffectsProvider.RaiseInstAnim(Vector2 positionWS, InstAnimEffectType type, Material mat)
	{
		if (!_instAnimNameMap.TryGetValue(type, out var value))
		{
			Debug.LogWarning($"InstAnimEffectType {type} not found");
			return;
		}
		InstantAnimationEffects instantAnimationEffects = _m_effects_system.Next<InstantAnimationEffects>();
		if (!(instantAnimationEffects == null))
		{
			instantAnimationEffects.sp.sharedMaterial = ((mat != null) ? mat : LocMaterials.GAME_MAT_2D);
			instantAnimationEffects.sp.sortingLayerName = "GroundFront";
			instantAnimationEffects.sp.sortingOrder = 0;
			instantAnimationEffects.transform.localScale = Vector3.one;
			instantAnimationEffects.transform.localRotation = Quaternion.identity;
			instantAnimationEffects.Play(positionWS, value);
		}
	}

	void IEffectsProvider.RaiseInstAnim(Vector2 positionWS, InstAnimEffectType type, string layer, int sortingOrder = 0)
	{
		if (_instAnimNameMap.TryGetValue(type, out var value))
		{
			InstantAnimationEffects instantAnimationEffects = _m_effects_system.Next<InstantAnimationEffects>();
			if (!(instantAnimationEffects == null))
			{
				instantAnimationEffects.sp.sharedMaterial = LocMaterials.GAME_MAT_2D;
				instantAnimationEffects.sp.sortingLayerName = layer;
				instantAnimationEffects.sp.sortingOrder = sortingOrder;
				instantAnimationEffects.transform.localScale = Vector3.one;
				instantAnimationEffects.transform.localRotation = Quaternion.identity;
				instantAnimationEffects.Play(positionWS, value);
			}
		}
	}

	void IEffectsProvider.RaiseInstAnim(Vector2 positionWS, InstAnimEffectType type, Vector2 dir, bool flip, Material mat, string layerName, int sortingOrder = 0)
	{
		if (_instAnimNameMap.TryGetValue(type, out var value))
		{
			InstantAnimationEffects instantAnimationEffects = _m_effects_system.Next<InstantAnimationEffects>();
			if (!(instantAnimationEffects == null))
			{
				instantAnimationEffects.sp.sharedMaterial = ((mat != null) ? mat : LocMaterials.GAME_MAT_2D);
				instantAnimationEffects.sp.sortingLayerName = layerName;
				instantAnimationEffects.sp.sortingOrder = sortingOrder;
				instantAnimationEffects.transform.localScale = (flip ? new Vector3(-1f, 1f, 1f) : Vector3.one);
				instantAnimationEffects.transform.localRotation = dir.GetRotation();
				instantAnimationEffects.Play(positionWS, value);
			}
		}
	}

	InstantGoEffects IEffectsProvider.GetInstantGoEffects(InstantGoEffectsType type)
	{
		return _m_instGoManager.GetInstantGoEffects(type);
	}

	void IEffectsProvider.RaiseInstGo(Vector2 positionWS, InstantGoEffectsType type)
	{
		_m_instGoManager.Raise(positionWS, type);
	}

	void IEffectsProvider.RaiseInstGo(Vector2 positionWS, InstantGoEffectsType type, Vector2 dir)
	{
		_m_instGoManager.Raise(positionWS, type, dir);
	}

	ContinuesAnimationEffects IEffectsProvider.RaiseContinuesAnim(ContinuesAnimEffectType type)
	{
		ContinuesAnimationEffects continuesAnimationEffects = _m_effects_system.Next<ContinuesAnimationEffects>();
		if (continuesAnimationEffects == null)
		{
			return null;
		}
		continuesAnimationEffects.Play(type.ToString().ToLower());
		return continuesAnimationEffects;
	}

	void IEffectsProvider.RecycleContinuesAnim(ContinuesAnimationEffects effects)
	{
		_m_effects_system.Recycle(effects);
	}

	void IEffectsProvider.RaiseFadeUpSprite(Vector2 positionWS, Sprite icon, Ease ease, float duration, float popDistance, bool flipX)
	{
		_m_fadeUpTipManager.RaiseUp(positionWS, icon, ease, duration, popDistance, flipX);
	}

	void IEffectsProvider.MoveFadeOutSprite(Vector2 from, Vector2 to, Sprite icon, Ease ease, float duration)
	{
		_m_fadeUpTipManager.MoveTo(from, to, icon, ease, duration);
	}

	public void RaiseFadeDownSprite(Vector2 positionWS, Sprite icon, Ease ease, float duration, float popDistance)
	{
		_m_fadeUpTipManager.RaiseDown(positionWS, icon, ease, duration, popDistance);
	}

	void IEffectsProvider.RaiseFadeUpSpriteArray(Vector2 positionWS, Sprite[] icons, Ease ease, float duration, float popDistance, bool flipX)
	{
		_m_fadeUpTipManager.RaiseSpriteArray(positionWS, icons, ease, duration, popDistance, flipX).Forget();
	}

	public void RaiseFadeDownSpriteArray(Vector2 positionWS, Sprite[] icon, Ease ease, float duration, float popDistance)
	{
	}

	void IEffectsProvider.RaiseContinuesPS(Vector2 positionWS, ContinuesParticleEffectsType type, float duration)
	{
		_m_continuesPsManager.Raise(positionWS, type, duration);
	}

	ContinuesParticleEffects IEffectsProvider.RaiseContinuesPS(Vector2 positionWS, ContinuesParticleEffectsType type)
	{
		return _m_continuesPsManager.Raise(positionWS, type);
	}

	ContinuesGoEffects IEffectsProvider.RaiseContinuesGo(Vector2 positionWS, ContinuesGoEffectsType type, float duration)
	{
		return _m_continuesGoManager.Raise(positionWS, type, duration);
	}

	private void _InitOtherEffects()
	{
		_m_effects_system.SetCreatedCallback(delegate(Wind R)
		{
			R.Recycle = _m_effects_system.Recycle;
		});
		_m_effects_system.SetCreatedCallback(delegate(WindLarge R)
		{
			R.Recycle = _m_effects_system.Recycle;
		});
		_m_effects_system.SetCreatedCallback(delegate(TwistEffects R)
		{
			R.Recycle = _m_effects_system.Recycle;
		});
		_m_effects_system.SetCreatedCallback(delegate(GhostShadow R)
		{
			R.Recycle = _m_effects_system.Recycle;
		});
	}

	public void RaiseWind(Vector2 ws, Vector2 force, float dur)
	{
		Wind wind = _m_effects_system.Next<Wind>();
		if (!(wind == null))
		{
			wind.Raise(ws, force, dur);
		}
	}

	public void RaiseLargeWind(Vector2 position, float speed, float duration)
	{
		WindLarge windLarge = _m_effects_system.Next<WindLarge>();
		if (!(windLarge == null))
		{
			windLarge.Raise(position, speed, duration);
		}
	}

	public void RaiseScreenTwist(Vector2 ws, float dur)
	{
		TwistEffects twistEffects = _m_effects_system.Next<TwistEffects>();
		if (!(twistEffects == null))
		{
			twistEffects.Raise(ws, dur);
		}
	}

	public void RaiseGhostShadow(Vector3 ws, Vector2 scale, Sprite sprite, bool useScale = false, float targetScale = 3f)
	{
		GhostShadow ghostShadow = _m_effects_system.Next<GhostShadow>();
		if (!(ghostShadow == null))
		{
			ghostShadow.Invoke(ws, scale, sprite, useScale, targetScale);
		}
	}

	public T GetEntityEffect<T>() where T : GameEntity
	{
		return _m_effects_system.Next<T>();
	}

	public T GetEntityEffect<T>(int limit) where T : GameEntity
	{
		return _m_effects_system.NextLimit<T>(limit);
	}

	public void ClearEntityEffects<T>() where T : GameEntity
	{
		_m_effects_system.Clear<T>();
	}

	public void RecycleEntityEffect<T>(T effects) where T : GameEntity
	{
		_m_effects_system.Recycle(effects);
	}
}
