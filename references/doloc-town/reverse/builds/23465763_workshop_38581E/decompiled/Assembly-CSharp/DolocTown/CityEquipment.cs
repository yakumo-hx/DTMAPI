using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(EquipmentRenderer))]
public class CityEquipment : InteractableObject
{
	[SerializeField]
	private string equipmentName;

	private CityEquipmentLogic logic;

	protected IEquipmentHost host => DolocAPI.CurrentRoom;

	protected Equipment equipment => logic?.Equipment;

	protected EquipmentRenderer equipmentRenderer { get; private set; }

	protected override object tipCaller
	{
		get
		{
			if (equipment != null)
			{
				return equipment;
			}
			return this;
		}
	}

	protected override bool showTip
	{
		get
		{
			if (equipment is EquipmentWorker equipmentWorker)
			{
				return !equipmentWorker.IsWorking;
			}
			return true;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		equipmentRenderer = GetComponent<EquipmentRenderer>() ?? base.gameObject.AddComponent<EquipmentRenderer>();
		equipmentRenderer.Init();
	}

	protected override void OnLoadData(Room room)
	{
		base.OnLoadData(room);
		if (!base.archiveData.TryGetLogicEntity<CityEquipmentLogic>(this, out logic) || logic.Equipment.Name != equipmentName)
		{
			base.archiveData.UnregisterLogicEntity(this, logic);
			logic = null;
			if (DolocAPI.QueryEquipment(equipmentName, out var proto))
			{
				Equipment equipment = EquipmentManager.CreateDisposeEquipment(room, proto, turn: false);
				logic = new CityEquipmentLogic(base.roomId, base.guid, equipment);
				base.archiveData.RegisterLogicEntity(this, logic);
			}
		}
	}

	protected override void OnRender(Room room)
	{
		base.OnRender(room);
		if (room == null || equipmentRenderer == null)
		{
			base.gameObject.SetActive(value: false);
		}
		else
		{
			logic?.BindRender(room, equipmentRenderer);
		}
	}

	protected override void OnUnRender()
	{
		base.OnUnRender();
		if (base.gameObject.activeSelf)
		{
			logic?.UnBindRender(equipmentRenderer);
		}
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		logic?.Interact();
	}

	protected override void OnDestroy()
	{
		logic?.UnBindRender(equipmentRenderer);
		base.OnDestroy();
	}
}
