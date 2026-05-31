using UnityEngine;

namespace DolocTown;

public class SceneLightFluorescentLamp : SceneLight
{
	[SerializeField]
	private Sprite _emissionSprite;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color _emissionColor = Color.white;

	[SerializeField]
	private bool _disableFlicker;

	private SpriteRenderer _spriteRenderer;

	private LightController _controller;

	public override void Init()
	{
		base.Init();
		_spriteRenderer = GetComponent<SpriteRenderer>();
		_controller = LightController.FromGameObject(base.gameObject);
		_controller.Exclude(_spriteRenderer);
		TurnOff(isInitial: true);
	}

	protected override void TurnOff(bool isInitial)
	{
		_spriteRenderer.ToggleLightOff();
		_controller.SetIntensity(0f);
	}

	protected override void TurnOn(bool isInitial)
	{
		_spriteRenderer.ToggleLightOn(_emissionSprite, _emissionColor);
		_controller.SetIntensity(1f);
	}

	public override void OnUpdatePerTu()
	{
		if (!_disableFlicker && Random.value < 0.3f)
		{
			_controller.FlickerLinearAttenuation();
		}
	}

	protected override void OnDispose()
	{
		_controller.Dispose();
	}
}
