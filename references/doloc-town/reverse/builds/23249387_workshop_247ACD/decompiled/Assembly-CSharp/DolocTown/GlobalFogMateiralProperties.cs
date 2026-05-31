using UnityEngine;

namespace DolocTown;

public class GlobalFogMateiralProperties : MonoBehaviour
{
	[SerializeField]
	private float fogMoveSpeed = 0.1f;

	[SerializeField]
	private Color fogColor1 = new Color(0.5f, 0.5f, 0.5f, 1f);

	[SerializeField]
	private Color fogColor2 = new Color(0.5f, 0.5f, 0.5f, 1f);

	[SerializeField]
	private float fogBorderSize = 0.1f;

	[SerializeField]
	private float fogNoiseScale = 0.1f;

	[SerializeField]
	private float fogIntensity = 0.5f;

	[SerializeField]
	private float fogOverallIntensity = 0.5f;

	private static readonly int CloudMoveSpeed = Shader.PropertyToID("_CloudMoveSpeed");

	private static readonly int CloudColor = Shader.PropertyToID("_CloudColor");

	private static readonly int CloudColor2 = Shader.PropertyToID("_CloudColor2");

	private static readonly int CloudBorderSize = Shader.PropertyToID("_CloudBorderSize");

	private static readonly int CloudNoiseScale = Shader.PropertyToID("_CloudNoiseScale");

	private static readonly int CloudIntensity = Shader.PropertyToID("_CloudIntensity");

	private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

	public float Intensity => fogOverallIntensity;

	public void ApplyMaterialProperties(Material material, bool setIntensity = false)
	{
		material.SetFloat(CloudMoveSpeed, fogMoveSpeed);
		material.SetColor(CloudColor, fogColor1);
		material.SetColor(CloudColor2, fogColor2);
		material.SetFloat(CloudBorderSize, fogBorderSize);
		material.SetFloat(CloudNoiseScale, fogNoiseScale);
		material.SetFloat(CloudIntensity, fogIntensity);
		if (setIntensity)
		{
			material.SetFloat(IntensityId, fogOverallIntensity);
		}
	}
}
