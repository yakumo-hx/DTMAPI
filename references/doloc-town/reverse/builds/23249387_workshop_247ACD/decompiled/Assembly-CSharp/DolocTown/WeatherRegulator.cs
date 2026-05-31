using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class WeatherRegulator : Equipment
{
	private readonly EquipmentFuncWeatherRegulator func;

	private readonly GameEntitySlot<ProgressBarRenderer> progressBarRenderer = new GameEntitySlot<ProgressBarRenderer>();

	public Vector2 PositionSwitch
	{
		get
		{
			Vector2 vector = proto.WorldSpriteSize * new Vector2(0.5f, 1f) + new Vector2(0.25f, 0.25f);
			return (Vector2)base.PositionBottom + vector;
		}
	}

	public WeatherRegulator(IEquipmentHost host, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, id, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncWeatherRegulator)proto.Function;
	}

	[JsonConstructor]
	protected WeatherRegulator(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			func = (EquipmentFuncWeatherRegulator)proto.Function;
		}
	}

	private void UpdateSwitchIcon()
	{
		SingleImage uiComponent = base.Renderer.GetUiComponent<SingleImage>();
		uiComponent.FollowWorldPosition(PositionSwitch);
		uiComponent.Color = Color.white;
		uiComponent.SetSprite(DolocAPI.archiveHandle.timeData.IsWeatherRegulatorCooling ? LocSprites.UI_TIP_EMPTYSOCK : LocSprites.UI_TIP_FILLEDSOCK);
	}

	protected override void OnUnRender()
	{
		base.OnUnRender();
		progressBarRenderer.Release();
		base.Renderer.RemoveUiComponent<SingleImage>();
	}

	protected override void Update()
	{
		if (base.IsTouch)
		{
			ProgressBarRenderer progressBarRenderer = this.progressBarRenderer.Fetch();
			if (progressBarRenderer != null && progressBarRenderer.isVisible)
			{
				progressBarRenderer.Progress = DolocAPI.archiveHandle.GetWeatherRegulatorCooldownProgress();
			}
		}
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		this.ShowSceneOperationTip(PositionTip, DolocConfig.StaticTexts.UiOperationInteract, DolocAPI.UserInput.GlobalInteractActionName);
		progressBarRenderer.Do(delegate(ProgressBarRenderer entity)
		{
			entity.position2d = base.PositionTop;
			entity.Progress = DolocAPI.archiveHandle.GetWeatherRegulatorCooldownProgress();
			entity.Show();
		});
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
		progressBarRenderer.Do(delegate(ProgressBarRenderer entity)
		{
			entity.Hide();
		});
		if (base.Renderer != null)
		{
			base.Renderer.RemoveUiComponent<SingleImage>();
		}
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (DolocAPI.archiveHandle.timeData.IsWeatherRegulatorCooling)
		{
			this.PushSceneOperationTip();
			int weatherRegulatorCd = DolocAPI.archiveHandle.timeData.WeatherRegulatorCd;
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(DolocConfig.StaticTexts.UiOperationErrCooling, DolocAPI.GetFormatTimeLengthByTU(weatherRegulatorCd)));
			return;
		}
		float weatherRegulatorLaunchPower = DolocAPI.GlobalParameter.WeatherRegulatorLaunchPower;
		if (!base.CurrentRoom.RootRoom.DM_electric.TryCostPower(weatherRegulatorLaunchPower))
		{
			this.PushSceneOperationTip();
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrLowPower);
			return;
		}
		InstantGoEffects instantGoEffects = DolocAPI.GetInstantGoEffects(InstantGoEffectsType.RADIO_SIGNAL);
		if (instantGoEffects != null)
		{
			((RadioSignal)instantGoEffects).RadioSignalRaise(base.PositionTop, instantGoEffects.RecycleSelf);
		}
		DolocAPI.Delay(1.5f, delegate
		{
			UniTask.WaitUntil(() => DolocAPI.userInput.CurrentState is NormalGameState).ContinueWith(delegate
			{
				DolocAPI.cameraController.ShakeScreen(1f);
				DolocAPI.ShowMessageBoxSmall(DolocConfig.StaticTexts.UiOperationWeatherHasChanged);
				DolocAPI.archiveHandle.UseWeatherRegulator();
			}).Forget();
		});
		progressBarRenderer.Do(delegate(ProgressBarRenderer entity)
		{
			entity.Progress = 0f;
		});
		this.PushSceneOperationTipToHide();
	}

	[Obsolete("弃用")]
	private void ChangeWeather()
	{
		WeatherType currentWeather = DolocAPI.archiveHandle.CurrentWeatherType;
		WeatherType type = new List<WeatherType>(from WeatherType t in Enum.GetValues(typeof(WeatherType))
			where t != currentWeather && !t.IsMalignantWeather() && t != WeatherType.NONE
			select t).Choice();
		DolocAPI.archiveHandle.SetWeather(type, shouldRender: true);
		DolocAPI.archiveHandle.PatchWeather(type);
	}
}
