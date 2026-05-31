using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public class TerrainObstacle : TerrainContent
{
	private Vector2Int[] layerPositions;

	public TerrainObstacle(Vector2Int[] layerPositions)
		: base(0, Vector2Int.zero, Vector2.zero)
	{
		this.layerPositions = layerPositions;
	}

	protected override Dictionary<TerrainLayerName, Vector2Int[]> CalLayerPositions(Vector2Int anchor)
	{
		return new Dictionary<TerrainLayerName, Vector2Int[]> { 
		{
			TerrainLayerName.ExtraObstacles,
			layerPositions
		} };
	}

	protected override bool ValidateDeserialization()
	{
		return true;
	}
}
