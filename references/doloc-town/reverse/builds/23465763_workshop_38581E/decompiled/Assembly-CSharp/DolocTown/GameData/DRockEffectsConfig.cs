using DG.Tweening;
using DolocTown.GameServiceLocator;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown.GameData;

[CreateAssetMenu(menuName = "多洛可小镇/配置/全局效果配置")]
public class DRockEffectsConfig : ScriptableObject, IEffectsConfigProvider
{
	[SerializeField]
	[ColorUsage(true, true)]
	private Color _builderIndicatorValidColor = Color.green;

	[SerializeField]
	[ColorUsage(true, false)]
	private Color _builderIndicatorValidColorNoHdr = Color.green;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color _builderIndicatorInvalidColor = Color.red;

	[SerializeField]
	[ColorUsage(true, false)]
	private Color _builderIndicatorInvalidColorNoHdr = Color.red;

	[SerializeField]
	private Color _terrainInvalidColor = Color.red;

	[SerializeField]
	private Color _terrainValidColor = Color.green;

	[SerializeField]
	private Color _terrainWarningColor = Color.yellow;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color _destroyIndicatorColor;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color _cellingColor;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color _noOverlayColor;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color _canOverlayColor;

	[SerializeField]
	private Tile _terrainSolidTile;

	[SerializeField]
	private Tile _terrainHollowTile;

	[SerializeField]
	private Tile _cutTile;

	[SerializeField]
	private float _commonUIPopTime = 0.2f;

	[SerializeField]
	private float _dungeonResourceInstPsYRage = 0.5f;

	[SerializeField]
	private float _dungeonResourceDropItemPopYOffset = 1.75f;

	[SerializeField]
	private float _dungeonResourceShineTime = 0.3f;

	[SerializeField]
	private float _dungeonResourceShakeTime = 0.3f;

	[SerializeField]
	private Vector2 _dungeonResourceHitPosXRange = new Vector2(0f, 1f);

	[SerializeField]
	private Vector2 _dungeonResourceHitPosYRange = new Vector2(0.4f, 0.6f);

	[SerializeField]
	private float _dropItemGravityScale = 6f;

	[SerializeField]
	private float _dropItemRaiseYForce = 20f;

	[SerializeField]
	private Vector2 _dropItemRaiseXRange = new Vector2(-4f, 4f);

	[SerializeField]
	private Vector2 _dropItemZRange = new Vector2(-10f, 10f);

	[SerializeField]
	private float _dropSpringRate = 0.5f;

	[SerializeField]
	private Vector2 _dropSpringSpeed = new Vector2(10f, 30f);

	[SerializeField]
	private Color _borderColorEquipment = Color.white;

	[SerializeField]
	private Color _borderColorMissionItem = Color.white;

	[SerializeField]
	private float _vignetteIntensity = 0.33f;

	[SerializeField]
	private float _vignetteSmoothness = 0.8f;

	[SerializeField]
	private Color _vignetteColor = Color.black;

	[SerializeField]
	private float _vignetteShowTime = 0.25f;

	[SerializeField]
	private float _fadeInoutLightness = 0.78f;

	[SerializeField]
	private float _sceneTransitionFadeInTime = 0.3f;

	[SerializeField]
	private float _sceneTransitionFadeOutTime = 0.2f;

	[SerializeField]
	private float _dungeonRoomTransitionTime = 0.4f;

	[SerializeField]
	private Ease _dungeonRoomTransitionEase = Ease.OutQuart;

	[SerializeField]
	private float _cinemaScreenFadeInTime = 0.5f;

	[SerializeField]
	private float _cinemaScreenFadeOutTime = 0.3f;

	[SerializeField]
	private Ease _cinemaScreenFadeInEase = Ease.OutQuart;

	[SerializeField]
	private Ease _cinemaScreenFadeOutEase = Ease.OutQuart;

	[SerializeField]
	private AnimationCurve _sunSpotIntensityCurve;

	[SerializeField]
	private float _sunSpotTransitionDuration = 8f;

	[SerializeField]
	private AnimationCurve _staticProjectiveShadowIntensityCurve;

	[SerializeField]
	private float _staticProjectiveShadowTransitionDuration = 8f;

	public Color invalidIndicatorColor => _builderIndicatorInvalidColor;

	public Color invalidIndicatorColorNoHdr => _builderIndicatorInvalidColorNoHdr;

	public Color validIndicatorColor => _builderIndicatorValidColor;

	public Color validIndicatorColorNoHdr => _builderIndicatorValidColorNoHdr;

	public Color terrainInvalidColor => _terrainInvalidColor;

	public Color terrainValidColor => _terrainValidColor;

	public Color terrainWarningColor => _terrainWarningColor;

	public Color cellingColor => _cellingColor;

	public Color noOverlayColor => _noOverlayColor;

	public Color canOverlayColor => _canOverlayColor;

	public Tile terrainSolidTile => _terrainSolidTile;

	public Tile terrainHollowTile => _terrainHollowTile;

	public Tile cutTile => _cutTile;

	public Color destroyIndicatorColor => _destroyIndicatorColor;

	public float commonUIPopTime => _commonUIPopTime;

	public float dungeonResourceInstPsYRate => _dungeonResourceInstPsYRage;

	public float dungeonResourceDropItemPopYOffset => _dungeonResourceDropItemPopYOffset;

	public float dungeonResourceShineTime => _dungeonResourceShineTime;

	public float dungeonResourceShakeTime => _dungeonResourceShakeTime;

	public float dungeonResourceHitPosRangeX => Random.Range(_dungeonResourceHitPosXRange.x, _dungeonResourceHitPosXRange.y);

	public float dungeonResourceHitPosRangeY => Random.Range(_dungeonResourceHitPosYRange.x, _dungeonResourceHitPosYRange.y);

	public float vignetteIntensity => _vignetteIntensity;

	public float vignetteSmoothness => _vignetteSmoothness;

	public Color vignetteColor => _vignetteColor;

	public float vignetteShowTime => _vignetteShowTime;

	public float fadeInoutLightness => _fadeInoutLightness;

	public float dropItemGravityScale => _dropItemGravityScale;

	public float dropItemRaiseYForce => _dropItemRaiseYForce;

	public float dropItemRaiseXRange => Random.Range(_dropItemRaiseXRange.x, _dropItemRaiseXRange.y);

	public float dropItemZRange => Random.Range(_dropItemZRange.x, _dropItemZRange.y);

	public float dropSpringRate => _dropSpringRate;

	public Vector2 dropSpringSpeed => _dropSpringSpeed;

	public Color BorderColorEquipment => _borderColorEquipment;

	public Color BorderColorMissionItem => _borderColorMissionItem;

	public float sceneTransitionFadeInTime => _sceneTransitionFadeInTime;

	public float sceneTransitionFadeOutTime => _sceneTransitionFadeOutTime;

	public float dungeonRoomTransitionTime => _dungeonRoomTransitionTime;

	public Ease dungeonRoomTransitionEase => _dungeonRoomTransitionEase;

	public float cinemaScreenFadeInTime => _cinemaScreenFadeInTime;

	public float cinemaScreenFadeOutTime => _cinemaScreenFadeOutTime;

	public Ease cinemaScreenFadeOutEase => _cinemaScreenFadeOutEase;

	public Ease cinemaScreenFadeInEase => _cinemaScreenFadeInEase;

	public AnimationCurve SunSpotIntensityCurve => _sunSpotIntensityCurve;

	public float SunSpotTransitionDuration => _sunSpotTransitionDuration;

	public AnimationCurve StaticProjectiveShadowIntensityCurve => _staticProjectiveShadowIntensityCurve;

	public float StaticProjectiveShadowTransitionDuration => _staticProjectiveShadowTransitionDuration;
}
