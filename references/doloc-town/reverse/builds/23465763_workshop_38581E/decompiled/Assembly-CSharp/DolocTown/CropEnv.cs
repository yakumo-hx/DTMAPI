using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DolocTown;

public class CropEnv
{
	private readonly PlantBasin _plantBasin;

	private readonly int _hRange;

	private readonly int _vRangeTop;

	private readonly int _vRangeBottom;

	private Room CurrentRoom => _plantBasin.CurrentRoom;

	public Vector2Int[] AroundPositions => _plantBasin.GetAffectedPositionsExcludeSelf(_hRange, _vRangeTop, _vRangeBottom);

	public IEnumerable<PlantBasin> AroundBasins
	{
		get
		{
			if (CurrentRoom == null)
			{
				yield break;
			}
			Queue<Vector2Int> positionsUnexplored = new Queue<Vector2Int>(AroundPositions);
			HashSet<Vector2Int> positionsExplored = new HashSet<Vector2Int>();
			IEquipmentHost host = CurrentRoom;
			while (positionsUnexplored.Count > 0)
			{
				Vector2Int vector2Int = positionsUnexplored.Dequeue();
				if (!positionsExplored.Add(vector2Int))
				{
					continue;
				}
				PlantBasin equipment = host.GetEquipment<PlantBasin>(vector2Int);
				if (equipment != null)
				{
					Vector2Int[] coveredPositions = equipment.CoveredPositions;
					foreach (Vector2Int item in coveredPositions)
					{
						positionsExplored.Add(item);
					}
					yield return equipment;
				}
			}
		}
	}

	public IEnumerable<PlantBasin> AroundEmptyBasins => AroundBasins.Where((PlantBasin basin) => !basin.HasCrop);

	public IEnumerable<Crop> AroundCrops
	{
		get
		{
			foreach (PlantBasin aroundBasin in AroundBasins)
			{
				if (aroundBasin.HasCrop)
				{
					yield return aroundBasin.Crop;
				}
			}
		}
	}

	public CropEnv(PlantBasin plantBasin, int hRange, int vRangeTop, int vRangeBottom)
	{
		_plantBasin = plantBasin;
		_hRange = hRange;
		_vRangeTop = vRangeTop;
		_vRangeBottom = vRangeBottom;
	}

	public IEnumerable<PlantBasin> ChooseAroundBasins(Func<PlantBasin, bool> condition)
	{
		return AroundBasins.Where((PlantBasin basin) => condition(basin));
	}
}
