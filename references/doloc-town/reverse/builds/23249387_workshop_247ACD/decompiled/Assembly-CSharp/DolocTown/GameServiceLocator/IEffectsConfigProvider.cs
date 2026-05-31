using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown.GameServiceLocator;

public interface IEffectsConfigProvider
{
	Color invalidIndicatorColor { get; }

	Color invalidIndicatorColorNoHdr { get; }

	Color destroyIndicatorColor { get; }

	Color validIndicatorColor { get; }

	Color validIndicatorColorNoHdr { get; }

	Color terrainInvalidColor { get; }

	Color terrainValidColor { get; }

	Color terrainWarningColor { get; }

	Color cellingColor { get; }

	Color noOverlayColor { get; }

	Color canOverlayColor { get; }

	Tile terrainSolidTile { get; }

	Tile terrainHollowTile { get; }

	Tile cutTile { get; }

	float commonUIPopTime { get; }

	float dungeonResourceInstPsYRate { get; }

	float dungeonResourceDropItemPopYOffset { get; }

	float dungeonResourceHitPosRangeX { get; }

	float dungeonResourceHitPosRangeY { get; }

	float dungeonResourceShineTime { get; }

	float dungeonResourceShakeTime { get; }

	float vignetteIntensity { get; }

	float vignetteSmoothness { get; }

	Color vignetteColor { get; }

	float vignetteShowTime { get; }

	float fadeInoutLightness { get; }

	float sceneTransitionFadeInTime { get; }

	float sceneTransitionFadeOutTime { get; }

	float dungeonRoomTransitionTime { get; }

	Ease dungeonRoomTransitionEase { get; }

	float dropItemGravityScale { get; }

	float dropItemRaiseYForce { get; }

	float dropItemRaiseXRange { get; }

	float dropItemZRange { get; }

	Vector2 dropSpringSpeed { get; }

	float dropSpringRate { get; }

	Color BorderColorEquipment { get; }

	Color BorderColorMissionItem { get; }

	float cinemaScreenFadeInTime { get; }

	float cinemaScreenFadeOutTime { get; }

	Ease cinemaScreenFadeOutEase { get; }

	Ease cinemaScreenFadeInEase { get; }

	AnimationCurve StaticProjectiveShadowIntensityCurve { get; }

	float StaticProjectiveShadowTransitionDuration { get; }

	AnimationCurve SunSpotIntensityCurve { get; }

	float SunSpotTransitionDuration { get; }
}
