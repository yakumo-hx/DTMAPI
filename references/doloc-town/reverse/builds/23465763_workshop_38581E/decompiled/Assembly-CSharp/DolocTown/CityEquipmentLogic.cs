using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class CityEquipmentLogic : InteractableObjectLogic
{
	[JsonProperty]
	private Equipment equipment;

	public Equipment Equipment => equipment;

	protected EquipmentInfo proto => equipment.proto;

	[JsonConstructor]
	public CityEquipmentLogic(string roomId, string guid, Equipment equipment)
		: base(roomId, guid)
	{
		this.equipment = equipment;
		base.IsValid = equipment?.IsValid ?? false;
	}

	public override void OnAfterLoadArchiveData(bool isNewGame)
	{
		base.OnAfterLoadArchiveData(isNewGame);
		equipment.SetHost(base.host, validatePosition: false);
	}

	protected override void OnUpdatePerSecond(bool isRender)
	{
		base.OnUpdatePerSecond(isRender);
		if (isRender)
		{
			equipment.OriginalUpdate();
		}
		else
		{
			equipment.OriginalUpdateNoRender();
		}
	}

	public void Interact()
	{
		OnInteract();
	}

	protected virtual void OnInteract()
	{
		equipment.OriginalInteract();
	}

	public void BindRender(Room room, EquipmentRenderer renderer)
	{
		shouldRender = true;
		Vector2Int anchor = room.Geometry.CalcCellPosition(renderer.position2d) - new Vector2Int(equipment.proto.CoverSize.x / 2, 0);
		equipment.MoveTerrainContent(anchor, renderer.position2d);
		equipment.Renderer = renderer;
		renderer.Equipment = equipment;
		equipment.OriginalRender();
	}

	public void UnBindRender(EquipmentRenderer renderer)
	{
		shouldRender = false;
		if (equipment.IsRender)
		{
			equipment.OriginalUnRender();
			equipment.Renderer = null;
			renderer.Equipment = null;
		}
	}
}
