using Sirenix.OdinInspector;
using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(fileName = "WeatherRenderInfo", menuName = "多洛可小镇/天气系统/天气渲染信息")]
public class WeatherRenderInfoSO : SerializedScriptableObject
{
	[SerializeField]
	public float sunIntensity;

	[SerializeField]
	public float bloomThreshold = 1f;

	[SerializeField]
	public float bloomIntensity;

	[SerializeField]
	public float vignetteIntensity;

	[SerializeField]
	public float vignetteSmoothness;

	[SerializeField]
	public Color shadowColor;

	[SerializeField]
	public Color highlightColor;

	[SerializeField]
	public Gradient sunColorGradient;

	[SerializeField]
	public AnimationCurve saturation;
}
