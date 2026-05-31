using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class SprinklerManual : Affector
{
	protected readonly EquipmentFuncSprinklerManual _manualSprinkler;

	[DebugInfo("是否正在工作")]
	private bool isWorking;

	[DebugInfo("工作计数器")]
	private Counter workingCounter = new Counter();

	public override AffectorType AffectType => AffectorType.Sprinkler;

	public SprinklerManual(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
		_manualSprinkler = (EquipmentFuncSprinklerManual)proto.Function;
	}

	[JsonConstructor]
	protected SprinklerManual(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		_manualSprinkler = (EquipmentFuncSprinklerManual)proto.Function;
	}

	public bool TryTakeWaterFromPositions(int value, IEnumerable<Vector2Int> positions)
	{
		List<(IWaterContainer, int)> list = new List<(IWaterContainer, int)>();
		IWaterContainer[] equipmentsOfAnyType = base.Host.GetEquipmentsOfAnyType<IWaterContainer>(positions);
		foreach (IWaterContainer waterContainer in equipmentsOfAnyType)
		{
			if (waterContainer.Water > value)
			{
				list.Add((waterContainer, value));
				value = 0;
				break;
			}
			list.Add((waterContainer, waterContainer.Water));
			value -= waterContainer.Water;
		}
		if (value > 0)
		{
			return false;
		}
		foreach (var (waterContainer2, require) in list)
		{
			waterContainer2.TakeWater(require);
		}
		return true;
	}

	public bool TryTakeAgentWater(int value)
	{
		Item[] array = DolocAPI.archiveHandle.InventorySystem.inventory.ReadAll();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is IWaterContainer waterContainer && waterContainer.Water >= value)
			{
				waterContainer.TakeWater(value);
				return true;
			}
		}
		return false;
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		this.ShowSceneOperationTip(PositionTip, DolocUtils.Format(DolocConfig.StaticTexts.UiOperationStartSomething, proto.Title), DolocAPI.UserInput.GlobalInteractActionName);
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
	}

	protected override void OnInteract()
	{
		int cost = _manualSprinkler.SprinklerCost;
		this.PushSceneOperationTip();
		DolocAPI.agent._Interact(delegate
		{
			if (this.TryCostWater(cost))
			{
				isWorking = true;
				workingCounter.SetInterval(10);
				ShineArea(Color.white);
				RaiseSprinklerEffects();
				DoAffector();
			}
			else
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrRunoutWaterAround);
			}
		});
	}

	protected override void Update()
	{
		base.Update();
		if (isWorking && workingCounter.Tick())
		{
			isWorking = false;
		}
	}

	protected override void UpdateNoRender()
	{
		base.UpdateNoRender();
		if (isWorking && workingCounter.Tick())
		{
			isWorking = false;
		}
	}

	protected void RaiseSprinklerEffects()
	{
		DolocAPI.RaiseContinuesPS((Vector2)base.Position + _manualSprinkler.EffectOffset, ContinuesParticleEffectsType.SPRINKLER, Random.Range(4, 6));
	}
}
