using DolocTown.Config;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class AnimalStation : Equipment
{
	public AnimalStation(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	public AnimalStation(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		ShowTip(DolocConfig.StaticTexts.UiOperationView);
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		HideTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (TryGetBuilding(out var building))
		{
			DolocAPI.EnterUI((AnimalPanelUiState state) => state.HandleStartUpArgs(building.room));
		}
	}

	private bool TryGetBuilding(out Building building)
	{
		building = null;
		if (base.CurrentRoom is TemplateRoomInHouse { Building: not null } templateRoomInHouse && templateRoomInHouse.Building.proto.AnimalSpace > 0)
		{
			building = templateRoomInHouse.Building;
			return true;
		}
		Building content = base.CurrentRoom.DM_terrain.GetContent<Building>(base.Anchor);
		if (content == null || content.proto.AnimalSpace <= 0)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationNoAnimalBuilding);
			return false;
		}
		building = content;
		return true;
	}
}
