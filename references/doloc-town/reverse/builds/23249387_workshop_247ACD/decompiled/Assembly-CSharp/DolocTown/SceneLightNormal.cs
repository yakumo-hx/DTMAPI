using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

public class SceneLightNormal : SceneLight
{
	[SerializeField]
	private Sprite _emissionSprite;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color _emissionColor = Color.white;

	[SerializeField]
	private float _emissionIntensity = 1f;

	[SerializeField]
	private Light2D[] _lights;

	[SerializeField]
	private SpriteRenderer[] _volumes;

	private Light2D[] _lights_auto_collected => GetComponentsInChildren<Light2D>(includeInactive: true);

	private SpriteRenderer[] _volumes_auto_collected => GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

	public override void Init()
	{
		base.Init();
		TurnOff(isInitial: true);
	}

	protected override void TurnOff(bool isInitial)
	{
		GetComponent<SpriteRenderer>().ToggleLightOff();
		Light2D[] lights_auto_collected = _lights_auto_collected;
		foreach (Light2D light2D in lights_auto_collected)
		{
			if (!(light2D == null))
			{
				light2D.gameObject.SetActive(value: false);
			}
		}
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		SpriteRenderer[] volumes_auto_collected = _volumes_auto_collected;
		foreach (SpriteRenderer spriteRenderer in volumes_auto_collected)
		{
			if (!(spriteRenderer == null) && !(spriteRenderer == component))
			{
				spriteRenderer.gameObject.SetActive(value: false);
			}
		}
	}

	protected override void TurnOn(bool isInitial)
	{
		GetComponent<SpriteRenderer>().ToggleLightOn(_emissionSprite, _emissionColor, _emissionIntensity);
		Light2D[] lights_auto_collected = _lights_auto_collected;
		foreach (Light2D light2D in lights_auto_collected)
		{
			if (!(light2D == null))
			{
				light2D.gameObject.SetActive(value: true);
			}
		}
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		SpriteRenderer[] volumes_auto_collected = _volumes_auto_collected;
		foreach (SpriteRenderer spriteRenderer in volumes_auto_collected)
		{
			if (!(spriteRenderer == null) && !(spriteRenderer == component))
			{
				spriteRenderer.gameObject.SetActive(value: true);
			}
		}
	}
}
