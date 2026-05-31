using DolocTown.Config.Resource;
using DolocTown.GameData;
using DolocTown.NodeCanvas;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class VegetationLuminous : Vegetation, ILuminous
{
	private SimpleLampController _lampController;

	private GameObject _idleEffect;

	private bool _isTurnOn;

	private VegetationFuncLuminous Func => (VegetationFuncLuminous)proto.Function;

	public override Sprite CurrentSprite => Func.SpriteAsset.Asset;

	public VegetationLuminous(VegetationInfo proto, Vector2Int anchor, Vector3 position, Vector2Int[] cvPositions)
		: base(proto, anchor, position, cvPositions)
	{
	}

	[JsonConstructor]
	public VegetationLuminous(int index, Vector2Int anchor, Vector3 position, string vegetationName)
		: base(index, anchor, position, vegetationName)
	{
	}

	public override void OnRender()
	{
		_lampController = new SimpleLampController(base.Renderer.Sr, Func.Lamp_Ref);
		if (!(Func.IdleEffect.Asset == null))
		{
			_idleEffect = Object.Instantiate(Func.IdleEffect.Asset, base.Renderer.transform);
			_idleEffect.transform.localPosition = Func.EffectsOffset;
		}
	}

	public override void OnUnRender()
	{
		_isTurnOn = false;
		_lampController.Dispose();
		if (_idleEffect != null)
		{
			Object.Destroy(_idleEffect.gameObject);
			_idleEffect = null;
		}
	}

	public override void UpdatePerTu()
	{
		OnLightParamChanged();
	}

	public void OnLightParamChanged(bool isInitial = false)
	{
		SwitchScheduleParams param = new SwitchScheduleParams(DolocAPI.archiveHandle.DateNow, DolocAPI.archiveHandle.CurrentWeatherType);
		SwitchScheduleGraph asset = Func.SwitchScheduleAsseet.Asset;
		bool flag = ((Func.SwitchScheduleAsseet.Asset == null) ? DolocAPI.archiveHandle.ShouldLightUp : asset.IsTrue(param));
		if (flag != _isTurnOn)
		{
			_isTurnOn = flag;
			if (flag)
			{
				TurnOn(isInitial);
			}
			else
			{
				TurnOff(isInitial);
			}
		}
	}

	public void TurnOn(bool isInitial)
	{
		if (isInitial)
		{
			_lampController.ToggleLight(value: true);
		}
		else
		{
			_lampController.ToggleLight(value: true, 10f);
		}
		if (_idleEffect != null)
		{
			_idleEffect.SetActive(value: true);
		}
	}

	public void TurnOff(bool isInitial)
	{
		if (_lampController != null)
		{
			if (isInitial)
			{
				_lampController.ToggleLight(value: false);
			}
			else
			{
				_lampController.ToggleLight(value: false, 7f);
			}
		}
		if (_idleEffect != null)
		{
			_idleEffect.SetActive(value: false);
		}
	}

	public override void OnTouch()
	{
		TouchEffect();
	}

	public override void OnWindBlow(Vector2 pos)
	{
		base.OnWindBlow(pos);
		RaiseInstantPSEffects(Func.TouchEffect, base.Renderer.position2d + Func.EffectsOffset);
	}

	public override void OnBomb(float damage, bool ctr, Vector2 pos)
	{
		RaiseInstantPSEffects(Func.TouchEffect, base.Renderer.position2d + Func.EffectsOffset);
		base.OnBomb(damage, ctr, pos);
	}

	public override void OnMonsterTouch(Vector2 pos)
	{
		TouchEffect();
	}

	private void TouchEffect()
	{
		base.Renderer.SwingOnTouch(light: true);
		RaiseInstantPSEffects(Func.TouchEffect, base.Renderer.position2d + Func.EffectsOffset);
	}
}
