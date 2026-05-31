using System.Collections.Generic;
using System.Linq;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class AnimalMapForBuilding
{
	private readonly Room farm;

	private HashSet<Vector2Int> walkablePositions;

	public IEnumerable<Vector2Int> AllPositions => walkablePositions;

	public AnimalMapForBuilding(Room farm)
	{
		this.farm = farm;
	}

	public void Refresh()
	{
		List<Vector2Int> list = new List<Vector2Int>();
		foreach (Building building in farm.DM_building.Buildings)
		{
			list.AddRange(GetAllWalkablePositions(building));
		}
		walkablePositions = new HashSet<Vector2Int>(list);
	}

	public bool CheckWalkableByBuilding(Vector2Int pos)
	{
		return walkablePositions.Contains(pos);
	}

	private static List<Vector2Int> GetAllWalkablePositions(Building building)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		list.AddRange(building.proto.FrontFloorPositions.Offset(building.Anchor));
		list.AddRange(from p in building.proto.FrontCeilingPositions.Offset(building.Anchor)
			select new Vector2Int(p.x, p.y + 1));
		return list;
	}
}
