using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public interface IAffector
{
	AffectorType AffectType { get; }

	Vector2Int[] CurrentAffectedPositions { get; }

	IEquipmentHost equipmentHost { get; }

	bool IsCoverPositions(Vector2Int[] positions);

	void InvokeAffect()
	{
		foreach (IAffectorReceiver aroundEquipment in GetAroundEquipments())
		{
			aroundEquipment.Affect(this);
		}
	}

	void InvokeAffectNoRender()
	{
		foreach (IAffectorReceiver aroundEquipment in GetAroundEquipments())
		{
			aroundEquipment.AffectNoRender(this);
		}
	}

	IEnumerable<Equipment> GetAroundEquipments()
	{
		HashSet<Equipment> hashSet = new HashSet<Equipment>();
		Vector2Int[] currentAffectedPositions = CurrentAffectedPositions;
		foreach (Vector2Int cellpos in currentAffectedPositions)
		{
			Equipment equipment = equipmentHost.GetEquipment(cellpos);
			if (!hashSet.Contains(equipment) && equipment is IAffectorReceiver)
			{
				hashSet.Add(equipment);
			}
		}
		return hashSet;
	}
}
