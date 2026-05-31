using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

[RequireComponent(typeof(Light2D))]
public class Sun : DolocObject, IEnvLight
{
	[SerializeField]
	private Light2D light2D;

	public bool isCutomLightIntensity => false;

	public bool isEnvLight => false;

	public float intensity
	{
		get
		{
			return light2D.intensity;
		}
		set
		{
			light2D.intensity = value;
		}
	}

	public Color color
	{
		get
		{
			return light2D.color;
		}
		set
		{
			light2D.color = value;
		}
	}

	public void SetDayProcess(float process)
	{
	}
}
