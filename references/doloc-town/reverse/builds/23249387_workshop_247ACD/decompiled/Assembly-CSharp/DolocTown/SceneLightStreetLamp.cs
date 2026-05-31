using DolocTown.Config.Weather;
using RedSaw;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

public class SceneLightStreetLamp : SceneLight
{
	[SerializeField]
	private Sprite _emissionSprite;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color _emissionColor = Color.white;

	[SerializeField]
	[Range(0f, 1f)]
	private float _mosquitoesProbability = 0.5f;

	[SerializeField]
	[Range(0f, 1f)]
	private float _flickerProbability = 0.3f;

	[SerializeField]
	[Range(0f, 1f)]
	private float _electricSparksProbability = 0.5f;

	[SerializeField]
	[Range(0f, 1f)]
	private float _dustProbability = 0.5f;

	[SerializeField]
	private Light2D _supLight;

	private StreetLampBulb[] _bulbs;

	private SpriteRenderer _spriteRenderer;

	public override void Init()
	{
		base.Init();
		base.gameObject.SetActive(value: true);
		_bulbs = GetComponentsInChildren<StreetLampBulb>(includeInactive: true);
		_spriteRenderer = GetComponent<SpriteRenderer>();
		StreetLampBulb[] bulbs = _bulbs;
		for (int i = 0; i < bulbs.Length; i++)
		{
			bulbs[i].Init();
		}
		_supLight.gameObject.SetActive(value: false);
	}

	protected override void TurnOff(bool isInitial)
	{
		_spriteRenderer.ToggleLightOff();
		if (_supLight != null)
		{
			_supLight.gameObject.SetActive(value: false);
		}
		if (!_bulbs.IsNullOrEmpty())
		{
			StreetLampBulb[] bulbs = _bulbs;
			for (int i = 0; i < bulbs.Length; i++)
			{
				bulbs[i].TurnOffLinear();
			}
		}
	}

	protected override void TurnOn(bool isInitial)
	{
		_spriteRenderer.ToggleLightOn(_emissionSprite, _emissionColor);
		if (_supLight != null)
		{
			_supLight.gameObject.SetActive(value: true);
		}
		if (_bulbs.IsNullOrEmpty())
		{
			return;
		}
		StreetLampBulb[] bulbs = _bulbs;
		foreach (StreetLampBulb streetLampBulb in bulbs)
		{
			if (RandomUtils.Dice(0.5f))
			{
				streetLampBulb.TurnOnLinear();
			}
			else
			{
				streetLampBulb.TurnOnFlicker();
			}
		}
	}

	protected override void OnDispose()
	{
		base.OnDispose();
		if (!_bulbs.IsNullOrEmpty())
		{
			StreetLampBulb[] bulbs = _bulbs;
			for (int i = 0; i < bulbs.Length; i++)
			{
				bulbs[i].Dispose();
			}
		}
	}

	public override void OnUpdatePerTu()
	{
		if (_bulbs.IsNullOrEmpty())
		{
			return;
		}
		bool flag = false;
		if (base.WeatherType == WeatherType.SUNNY && RandomUtils.Dice(_mosquitoesProbability))
		{
			flag = true;
			StreetLampBulb streetLampBulb = _bulbs.Choice();
			DolocAPI.effectProvider.RaiseInstPS(streetLampBulb.EffectsPosition, InstantParticleEffectsType.MOSQUITOES);
		}
		if (!flag && RandomUtils.Dice(_dustProbability))
		{
			StreetLampBulb streetLampBulb2 = _bulbs.Choice();
			DolocAPI.effectProvider.RaiseInstPS(streetLampBulb2.EffectsPosition, InstantParticleEffectsType.AIRDUST);
		}
		if (RandomUtils.Dice(_flickerProbability))
		{
			StreetLampBulb streetLampBulb3 = _bulbs.Choice();
			streetLampBulb3.FlickerLinear();
			if (RandomUtils.Dice(_electricSparksProbability))
			{
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_OBJECT_LIGHTBULB_BUZZING, base.gameObject);
				DolocAPI.effectProvider.RaiseInstPS(streetLampBulb3.transform.position, InstantParticleEffectsType.ELECTRIC_SPARKS);
			}
		}
	}

	private void OnEnable()
	{
		DolocAPI.Sound.RegisterGameObject(base.gameObject);
	}

	private void OnDisable()
	{
		DolocAPI.Sound.UnregisterGameObject(base.gameObject);
	}
}
