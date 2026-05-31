using DG.Tweening;
using UnityEngine;

namespace DolocTown.GameServiceLocator;

public interface IEffectsProvider
{
	void Clear();

	void RaiseInstPS(Vector2 positionWS, InstantParticleEffectsType type);

	void RaiseInstPS(Vector2 positionWS, InstantParticleEffectsType type, string sortingLayer, int sortingOrder);

	InstantGoEffects GetInstantGoEffects(InstantGoEffectsType type);

	void RaiseInstGo(Vector2 positionWS, InstantGoEffectsType type);

	void RaiseInstGo(Vector2 positionWS, InstantGoEffectsType type, Vector2 dir);

	void RaiseInstAnim(Vector2 positionWS, InstAnimEffectType type);

	void RaiseInstAnim(Vector2 positionWS, InstAnimEffectType type, Vector2 dir);

	void RaiseInstAnim(Vector2 positionWS, InstAnimEffectType type, bool flip);

	void RaiseInstAnim(Vector2 positionWS, InstAnimEffectType type, Material mat);

	void RaiseInstAnim(Vector2 positionWS, InstAnimEffectType type, string layer, int sortingOrder = 0);

	InstantAnimationEffects RaiseInstAnimCustom(Vector2 positionWS, InstAnimEffectType type);

	void RaiseInstAnim(Vector2 positionWS, InstAnimEffectType type, Vector2 dir, bool flip, Material mat, string layer, int sortingOrder = 0);

	void RaiseContinuesPS(Vector2 positionWS, ContinuesParticleEffectsType type, float duration);

	ContinuesParticleEffects RaiseContinuesPS(Vector2 positionWS, ContinuesParticleEffectsType type);

	ContinuesGoEffects RaiseContinuesGo(Vector2 positionWS, ContinuesGoEffectsType type, float duration);

	void ClearAllContinusEffects();

	ContinuesAnimationEffects RaiseContinuesAnim(ContinuesAnimEffectType type);

	void RecycleContinuesAnim(ContinuesAnimationEffects effects);

	void RaiseFadeUpSprite(Vector2 positionWS, Sprite icon, Ease ease, float duration, float popDistance, bool flipX);

	void MoveFadeOutSprite(Vector2 from, Vector2 to, Sprite icon, Ease ease, float duration);

	void RaiseFadeDownSprite(Vector2 positionWS, Sprite icon, Ease ease, float duration, float popDistance);

	void RaiseFadeUpSpriteArray(Vector2 positionWS, Sprite[] icon, Ease ease, float duration, float popDistance, bool flipX);

	void RaiseFadeDownSpriteArray(Vector2 positionWS, Sprite[] icon, Ease ease, float duration, float popDistance);

	void RaiseWind(Vector2 ws, Vector2 force, float dur);

	void RaiseLargeWind(Vector2 ws, float speed, float duration);

	void RaiseScreenTwist(Vector2 ws, float dur);

	void RaiseGhostShadow(Vector3 ws, Vector2 scale, Sprite sprite, bool useScale = false, float targetScale = 3f);

	T GetEntityEffect<T>() where T : GameEntity;

	T GetEntityEffect<T>(int limitation) where T : GameEntity;

	void ClearEntityEffects<T>() where T : GameEntity;

	void RecycleEntityEffect<T>(T entity) where T : GameEntity;
}
