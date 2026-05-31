using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(fileName = "WaterTemplate", menuName = "多洛可小镇/水体模板")]
public class WaterParamSO : ScriptableObject
{
	[SerializeField]
	public WaterType WaterType;

	[SerializeField]
	public bool shouldWave = true;

	[SerializeField]
	public int springCount = 51;

	[SerializeField]
	public float springConst = 0.23f;

	[SerializeField]
	public float damping = 0.03f;

	[SerializeField]
	public float spread = 0.06f;

	[SerializeField]
	public float simplifyThreshold = 0.5f;

	[SerializeField]
	public int waterModerate = 5;

	[SerializeField]
	public WaveParam waveParamDefault;

	[SerializeField]
	public WaveParam waveParamOnEnter;

	[SerializeField]
	public WaveParam waveParamOnStay;

	[SerializeField]
	public WaveParam waveParamOnRainDrop;

	[SerializeField]
	public WaveParam waveOnBomb;

	[SerializeField]
	public WaveParam waveOnAttack;

	[SerializeField]
	public float waterScale = 0.25f;

	[SerializeField]
	public float waterDensity = 0.1f;

	[SerializeField]
	public Color waterBorderColor = Color.white;

	[SerializeField]
	public float distortionSpeed = 0.14f;

	[SerializeField]
	public float distortionIntensity = 0.41f;

	[SerializeField]
	public Color waterColor = Color.white;

	[SerializeField]
	public Color waterDeepColor = Color.white;

	[SerializeField]
	public float waterDepth = 1.21f;

	[SerializeField]
	public float fogSpeed = 0.1f;

	[SerializeField]
	public float fogIntensity = 1.61f;

	[SerializeField]
	public float fogDepth = 0.61f;

	private bool IsDeepWater => WaterType != WaterType.SHALLOW;
}
