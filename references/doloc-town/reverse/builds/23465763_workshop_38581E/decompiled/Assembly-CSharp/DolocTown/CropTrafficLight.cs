using System.Linq;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class CropTrafficLight : Equipment
{
	private readonly EquipmentFuncCropTrafficLight func;

	private Building bindedBuilding;

	private Counter counter;

	public CropTrafficLight(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		counter = DolocAPI.GlobalParameter.NewTuCounter;
		func = (EquipmentFuncCropTrafficLight)proto.Function;
	}

	[JsonConstructor]
	public CropTrafficLight(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			counter = DolocAPI.GlobalParameter.NewTuCounter;
			func = (EquipmentFuncCropTrafficLight)proto.Function;
		}
	}

	protected override void OnRender()
	{
		base.OnRender();
		if (base.CurrentRoom is TemplateRoomInHouse templateRoomInHouse)
		{
			bindedBuilding = templateRoomInHouse.Building;
		}
		else
		{
			bindedBuilding = base.CurrentRoom.DM_terrain.GetContent<Building>(base.Anchor);
		}
		base.Renderer.Sr.RenderAsCropTrafficLight(func.MaskSprite.Asset);
		UpdateTrafficLights();
	}

	protected override void OnUnRender()
	{
		base.OnUnRender();
		base.Renderer.Sr.RenderAsNormal();
		bindedBuilding = null;
	}

	protected override void Update()
	{
		base.Update();
		if (counter.Tick())
		{
			UpdateTrafficLights();
		}
	}

	private void RenderTrafficLights(bool isLeftLightOn, bool isRightLightOn)
	{
		base.Renderer.Sr.UpdateCropTrafficLight(isLeftLightOn, isRightLightOn);
	}

	private void UpdateTrafficLights()
	{
		if (bindedBuilding?.room == null)
		{
			RenderTrafficLights(isLeftLightOn: false, isRightLightOn: false);
			return;
		}
		TemplateRoomInHouse room = bindedBuilding.room;
		bool isLeftLightOn = ((IEquipmentHost)room).GetEquipments<PlantBasin>().Any((PlantBasin x) => x.IsCropMature);
		bool isRightLightOn = ((IEquipmentHost)room).GetEquipments<PlantBasin>().Any((PlantBasin x) => x.NeedWaterOrClear);
		RenderTrafficLights(isLeftLightOn, isRightLightOn);
	}
}
