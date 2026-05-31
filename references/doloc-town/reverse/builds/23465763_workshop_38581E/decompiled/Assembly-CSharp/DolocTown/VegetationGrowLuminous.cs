using DolocTown.Config;
using DolocTown.Config.Resource;
using DolocTown.GameData;
using DolocTown.NodeCanvas;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class VegetationGrowLuminous : VegetationGrow, ILuminous
{
	private bool _isTurnOn;

	private SimpleLampController _lampController;

	private VegetationFuncGrowLuminous GrowLuminousFunc => (VegetationFuncGrowLuminous)proto.Function;

	public override bool OnlyTouch => base.currentLevel < base.maxLevel;

	public VegetationGrowLuminous(VegetationInfo proto, Vector2Int anchor, Vector3 position, Vector2Int[] cvPositions)
		: base(proto, anchor, position, cvPositions)
	{
	}

	[JsonConstructor]
	public VegetationGrowLuminous(int index, Vector2Int anchor, Vector3 position, string vegetationName, int currentLevel, int currentGrowth, int randomSeed)
		: base(index, anchor, position, vegetationName, currentLevel, currentGrowth, randomSeed)
	{
	}

	public override void OnRender()
	{
		_lampController = new SimpleLampController(base.Renderer.Sr, GrowLuminousFunc.Lamp_Ref);
		if (generateDrop)
		{
			_isTurnOn = true;
			_lampController.ToggleLight(value: true);
		}
	}

	public override void OnUnRender()
	{
		_isTurnOn = false;
		_lampController.Dispose();
	}

	public override void UpdatePerTu()
	{
		base.UpdatePerTu();
		OnLightParamChanged();
	}

	public override void OnTouch()
	{
		if (base.currentLevel >= base.maxLevel)
		{
			base.Renderer.ShowOutline = true;
			ShowTip(DolocConfig.StaticTexts.UiOperationHarvest, DolocAPI.UserInput.GlobalInteractActionName);
		}
	}

	public override void OnDisTouch()
	{
		base.Renderer.ShowOutline = false;
		HideTip();
	}

	public override void OnInteract()
	{
		if (base.currentLevel >= base.maxLevel)
		{
			DolocAPI.agent._Interact(delegate
			{
				ReGrow();
				GenerateDropItems();
				SendGatherMessage();
				PushTipToDisappear();
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_HARVEST);
			});
		}
	}

	protected override void SetGrowthLevel(int growthLevel, bool useTween)
	{
		base.SetGrowthLevel(growthLevel, useTween);
		if (base.isRender)
		{
			if (growthLevel == base.maxLevel)
			{
				_lampController.ToggleLight(value: true, 10f);
			}
			else
			{
				_lampController.ToggleLight(value: false, 2f);
			}
		}
	}

	public override void OnBomb(float damage, bool ctr, Vector2 pos)
	{
		PushTipToDisappear();
		base.OnBomb(damage, ctr, pos);
	}

	public void OnLightParamChanged(bool isInitial = false)
	{
		SwitchScheduleParams param = new SwitchScheduleParams(DolocAPI.archiveHandle.DateNow, DolocAPI.archiveHandle.CurrentWeatherType);
		SwitchScheduleGraph asset = GrowLuminousFunc.SwitchScheduleAsseet.Asset;
		bool flag = ((GrowLuminousFunc.SwitchScheduleAsseet.Asset == null) ? DolocAPI.archiveHandle.ShouldLightUp : asset.IsTrue(param));
		if (flag != _isTurnOn)
		{
			_isTurnOn = flag;
			if (flag && generateDrop)
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
	}

	public void TurnOff(bool isInitial)
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
}
