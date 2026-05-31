using System;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Weather;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class SimpleWell : Equipment, IWaterContainer
{
	private readonly EquipmentFuncSimpleWell func;

	[JsonProperty]
	private int currentValue;

	public override bool CanInteractContinues => currentValue < func.Capacity;

	public override bool IsDirty => currentValue > 0;

	[DebugInfo("水井信息")]
	public string WellInfo => $"{currentValue}/{func.Capacity} = {CapacityPercent}";

	private float capacityReciprocal => 1f / (float)func.Capacity;

	private float CapacityPercent => (float)currentValue * capacityReciprocal;

	public Vector3 BarPosition
	{
		get
		{
			Vector3 positionTop = base.PositionTop;
			positionTop.z = -6f;
			return positionTop;
		}
	}

	public int Water => currentValue;

	public SimpleWell(IEquipmentHost host, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, id, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncSimpleWell)proto.Function;
		currentValue = 0;
	}

	[JsonConstructor]
	protected SimpleWell(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, int currentValue)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		func = (EquipmentFuncSimpleWell)proto.Function;
		this.currentValue = currentValue;
	}

	protected override void OnInteract()
	{
		this.PushSceneOperationTip();
		if (base.Host.CurrentWeatherInfo.Id == WeatherType.SCORCH_SUN)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrRunoutWater);
		}
		else
		{
			DrawWater();
		}
	}

	private void DrawWater()
	{
		if (currentValue >= func.Capacity)
		{
			DolocAPI.ShowMessageBoxErr(DolocConfig.StaticTexts.UiOperationErrWellFull);
			return;
		}
		if (!DolocAPI.CostToolEnergy())
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrLackOfEnergy);
			return;
		}
		SendUseEquipmentMessage();
		DolocAPI.agent._Interact(delegate
		{
			DolocAPI.RaiseInstantAnimEffects(base.PositionCenter, InstAnimEffectType.WATER_LITTLE);
			currentValue = Mathf.Min(currentValue + func.Adder, func.Capacity);
			UpdateBarRenderer();
		}, "chop");
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_EQUIPMENT_WELL_DRAW_WATER);
	}

	public int TakeWater(int require)
	{
		if (_TakeWater(require, out var value))
		{
			return value;
		}
		DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrRunoutWater);
		return 0;
	}

	public void Evaporation(int value, bool shouldRender = false)
	{
		if (value > 0)
		{
			currentValue -= value;
			if (currentValue < 0)
			{
				currentValue = 0;
			}
			if (shouldRender && base.IsTouch)
			{
				UpdateBarRenderer();
				ShowEvaporationEffect(playSound: false);
			}
		}
	}

	public void TryTakeWater(int cost, Action callback)
	{
		if (_TryTakeWater(cost))
		{
			callback?.Invoke();
		}
		else
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrRunoutWater);
		}
	}

	private bool _TryTakeWater(int cost)
	{
		if (currentValue < cost)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrRunoutWater);
			return false;
		}
		currentValue -= cost;
		UpdateBarRenderer();
		DolocAPI.RaiseInstantAnimEffects(base.PositionCenter, InstAnimEffectType.WATER_LITTLE);
		return true;
	}

	private bool _TakeWater(int cost, out int value)
	{
		PushTip();
		if (currentValue <= 0)
		{
			value = 0;
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrRunoutWater);
			return false;
		}
		value = Mathf.Min(currentValue, cost);
		currentValue -= value;
		UpdateBarRenderer();
		DolocAPI.RaiseInstantAnimEffects(base.PositionCenter, InstAnimEffectType.WATER_LITTLE);
		return true;
	}

	private void UpdateBarRenderer()
	{
		ProgressBarRendererWater renderComponent = base.Renderer.GetRenderComponent<ProgressBarRendererWater>();
		if (!(renderComponent == null))
		{
			renderComponent.position = BarPosition;
			renderComponent.SetVisible(value: true);
			if (currentValue <= 0)
			{
				renderComponent.Progress = -1f;
			}
			else
			{
				renderComponent.Progress = CapacityPercent;
			}
		}
	}

	protected override void OnUnRender()
	{
		HideTip();
		base.Renderer.RemoveRenderComponent<ProgressBarRendererWater>();
	}

	protected override void OnTouch()
	{
		ShowTip(DolocConfig.StaticTexts.UiOperationWell);
		UpdateBarRenderer();
	}

	protected override void OnDisTouch()
	{
		HideTip();
		if (base.Renderer != null)
		{
			base.Renderer.RemoveRenderComponent<ProgressBarRendererWater>();
		}
	}

	public void DrawMax()
	{
		currentValue = func.Capacity;
	}
}
