using UnityEngine;

namespace DolocTown;

public class StreetLampBulb : MonoBehaviour
{
	[SerializeField]
	private GameObject _lightAnchor;

	private LightController _controller;

	public Vector2 EffectsPosition
	{
		get
		{
			if (_lightAnchor == null)
			{
				return base.transform.position;
			}
			return _lightAnchor.transform.position;
		}
	}

	public void Init()
	{
		_controller = LightController.FromGameObject(base.gameObject);
		base.gameObject.SetActive(value: true);
		_controller.SetIntensity(0f);
	}

	public void FlickerLinear(float duration = 1f)
	{
		_controller.FlickerLinearAttenuation(duration);
	}

	public void TurnOffLinear(float duration = 1f)
	{
		_controller.TurnOffLinear(duration);
	}

	public void TurnOnFlicker(float duration = 1f)
	{
		_controller.TurnOnFlicker(duration);
	}

	public void TurnOnLinear(float duration = 1f)
	{
		_controller.TurnOnLinear(duration);
	}

	public void Dispose()
	{
		_controller.Dispose();
	}
}
