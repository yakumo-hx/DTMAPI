using System.Collections.Generic;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class Fence : Equipment, IAnimalFence
{
	public IEnumerable<Vector2Int> FencePositions => base.CoveredPositions;

	public Fence(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	public Fence(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
	}

	public override void OnCreated()
	{
		base.OnCreated();
		base.CurrentRoom.animalSystem.OnFenceChanged(base.CurrentRoom);
	}

	protected override void OnRemove()
	{
		base.OnRemove();
		base.CurrentRoom.animalSystem.OnFenceChanged(base.CurrentRoom);
	}
}
