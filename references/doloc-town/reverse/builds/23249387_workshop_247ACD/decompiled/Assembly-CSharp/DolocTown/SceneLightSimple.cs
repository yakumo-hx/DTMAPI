using UnityEngine;

namespace DolocTown;

public class SceneLightSimple : SceneLight
{
	private enum TurnOnMode
	{
		Constant,
		Linear,
		Flicker
	}

	private LightController _lightController;

	[SerializeField]
	private TurnOnMode _turnOnMode;

	[SerializeField]
	private float _turnOnDuration = 1f;

	private bool NeedDuration => _turnOnMode != TurnOnMode.Constant;

	private void TurnOnConstant()
	{
		_lightController.SetIntensity(1f);
	}

	private void TurnOffConstant()
	{
		_lightController.SetIntensity(0f);
	}

	public override void Init()
	{
		base.Init();
		_lightController = LightController.FromGameObject(base.gameObject);
		TurnOff(isInitial: true);
	}

	protected override void TurnOff(bool isInitial)
	{
		if (isInitial)
		{
			TurnOffConstant();
			return;
		}
		switch (_turnOnMode)
		{
		case TurnOnMode.Constant:
			TurnOffConstant();
			break;
		case TurnOnMode.Linear:
		case TurnOnMode.Flicker:
			if (_lightController == null)
			{
				_lightController = LightController.FromGameObject(base.gameObject);
			}
			_lightController.TurnOffLinear(_turnOnDuration);
			break;
		default:
			TurnOffConstant();
			break;
		}
	}

	protected override void TurnOn(bool isInitial)
	{
		if (isInitial)
		{
			TurnOnConstant();
			return;
		}
		switch (_turnOnMode)
		{
		case TurnOnMode.Constant:
			TurnOnConstant();
			break;
		case TurnOnMode.Linear:
			_lightController.TurnOnLinear(_turnOnDuration);
			break;
		case TurnOnMode.Flicker:
			_lightController.TurnOnFlicker(_turnOnDuration);
			break;
		default:
			TurnOnConstant();
			break;
		}
	}
}
