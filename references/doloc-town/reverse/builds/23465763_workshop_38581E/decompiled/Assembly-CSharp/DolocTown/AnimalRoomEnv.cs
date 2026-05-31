using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Equipment;
using RedSaw;
using Sirenix.Utilities;
using UnityEngine;

namespace DolocTown;

public class AnimalRoomEnv
{
	public readonly Room room;

	private Vector2Int[] allPositions;

	private HashSet<Vector2Int> allPositionsSet;

	public IEnumerable<Vector2Int> AllPositions => allPositions;

	public bool IsEmpty => allPositions.Length == 0;

	public AnimalRoomEnv(Room room, Vector2Int[] positions)
	{
		this.room = room ?? throw new ArgumentNullException("room");
		if (positions == null)
		{
			positions = Array.Empty<Vector2Int>();
		}
		allPositions = positions;
		allPositionsSet = new HashSet<Vector2Int>(positions);
	}

	public bool GetRandomEmptyPositionForEquipment(Vector2Int coverSize, out Vector2Int anchor)
	{
		anchor = default(Vector2Int);
		EquipmentInfo chickenNestProto = AnimalUtils.ChickenNestProto;
		if (chickenNestProto == null)
		{
			return false;
		}
		IEquipmentHost equipmentHost = room;
		Vector2Int[] array = allPositions.Shuffle();
		foreach (Vector2Int vector2Int in array)
		{
			anchor = vector2Int;
			Vector2Int[] cvPositions = chickenNestProto.CoveredPositions(anchor);
			IEnumerable<Vector2Int> gdPositions = chickenNestProto.GroundPositions(anchor);
			if (equipmentHost.IsEquipmentBuildable(cvPositions, gdPositions))
			{
				return true;
			}
		}
		return false;
	}

	public Vector2Int GetAroundPosition(Vector2Int currentCell, int maxDistance = 4)
	{
		if (allPositions.Length == 0)
		{
			return currentCell;
		}
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 1; i <= maxDistance; i++)
		{
			list.AddRange(from ringPosition in Grid2D.Ring(currentCell, Vector2Int.one * i)
				where allPositionsSet.Contains(ringPosition)
				select ringPosition);
		}
		return list.Choice();
	}

	public bool ContainsPosition(Vector2Int position)
	{
		return allPositionsSet.Contains(position);
	}

	public Vector2Int GetRandomPosition(Vector2Int currentCell)
	{
		if (allPositions.Length != 0)
		{
			return allPositions[UnityEngine.Random.Range(0, allPositions.Length)];
		}
		return currentCell;
	}

	public IEnumerable<IAnimalHoneyComb> GetHoneyCombs()
	{
		HashSet<IAnimalHoneyComb> hashSet = new HashSet<IAnimalHoneyComb>();
		IEquipmentHost equipmentHost = room;
		Vector2Int[] array = allPositions;
		foreach (Vector2Int cellpos in array)
		{
			if (equipmentHost.GetEquipment(cellpos) is IAnimalHoneyComb item)
			{
				hashSet.Add(item);
			}
		}
		return hashSet;
	}

	public IEnumerable<IFeeder> GetFeeders()
	{
		HashSet<IFeeder> hashSet = new HashSet<IFeeder>();
		IEquipmentHost equipmentHost = room;
		Vector2Int[] array = allPositions;
		foreach (Vector2Int cellpos in array)
		{
			Equipment equipment = equipmentHost.GetEquipment(cellpos);
			if (equipment is IFeeder item)
			{
				hashSet.Add(item);
			}
			else if (equipment is PlantBasinGrass plantBasinGrass)
			{
				hashSet.AddRange(plantBasinGrass.Feeders);
			}
		}
		if (room.IsInHouse)
		{
			return hashSet;
		}
		array = allPositions;
		foreach (Vector2Int pos in array)
		{
			if (room.DM_terrain.GetContent<DungeonResource>(pos, TerrainLayerName.ResourceOther) is IFeeder item2)
			{
				hashSet.Add(item2);
			}
		}
		return hashSet;
	}

	public IEnumerable<IAnimalToilet> GetToilets()
	{
		HashSet<IAnimalToilet> hashSet = new HashSet<IAnimalToilet>();
		IEquipmentHost equipmentHost = room;
		Vector2Int[] array = allPositions;
		foreach (Vector2Int cellpos in array)
		{
			if (equipmentHost.GetEquipment(cellpos) is IAnimalToilet item)
			{
				hashSet.Add(item);
			}
		}
		return hashSet;
	}

	public IEnumerable<IAnimalLivestockNursery> GetLivestockNurseries()
	{
		HashSet<IAnimalLivestockNursery> hashSet = new HashSet<IAnimalLivestockNursery>();
		IEquipmentHost equipmentHost = room;
		Vector2Int[] array = allPositions;
		foreach (Vector2Int cellpos in array)
		{
			if (equipmentHost.GetEquipment(cellpos) is IAnimalLivestockNursery item)
			{
				hashSet.Add(item);
			}
		}
		return hashSet;
	}

	public IEnumerable<IAnimalLintRoller> GetLintRollers()
	{
		HashSet<IAnimalLintRoller> hashSet = new HashSet<IAnimalLintRoller>();
		IEquipmentHost equipmentHost = room;
		Vector2Int[] array = allPositions;
		foreach (Vector2Int cellpos in array)
		{
			if (equipmentHost.GetEquipment(cellpos) is IAnimalLintRoller item)
			{
				hashSet.Add(item);
			}
		}
		return hashSet;
	}

	public IEnumerable<IAnimalMilkingMachine> GetMilkingMachines()
	{
		HashSet<IAnimalMilkingMachine> hashSet = new HashSet<IAnimalMilkingMachine>();
		IEquipmentHost equipmentHost = room;
		Vector2Int[] array = allPositions;
		foreach (Vector2Int cellpos in array)
		{
			if (equipmentHost.GetEquipment(cellpos) is IAnimalMilkingMachine { IsAvailable: not false } animalMilkingMachine)
			{
				hashSet.Add(animalMilkingMachine);
			}
		}
		return hashSet;
	}

	public IEnumerable<ChickenNest> GetChickenNests()
	{
		HashSet<ChickenNest> hashSet = new HashSet<ChickenNest>();
		IEquipmentHost equipmentHost = room;
		Vector2Int[] array = allPositions;
		foreach (Vector2Int cellpos in array)
		{
			if (equipmentHost.GetEquipment(cellpos) is ChickenNest item)
			{
				hashSet.Add(item);
			}
		}
		return hashSet;
	}

	public void Refresh(Vector2Int currentCell, int width)
	{
		allPositions = AnimalUtils.FindAvailablePositions(room, currentCell, width, 5000, "AnimalRoomEnv.Refresh");
		allPositionsSet = new HashSet<Vector2Int>(allPositions);
	}
}
