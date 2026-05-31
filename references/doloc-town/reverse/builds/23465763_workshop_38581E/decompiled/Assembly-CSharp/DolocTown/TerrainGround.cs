using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public class TerrainGround : TerrainContent
{
	private Vector2Int[] groundPositions;

	private Vector2Int[] extraObstaclePositions;

	public TerrainGround(Vector2Int[] groundPositions, Vector2Int[] extraObstaclePositions)
		: base(0, Vector2Int.zero, Vector2.zero)
	{
		this.groundPositions = groundPositions;
		this.extraObstaclePositions = extraObstaclePositions;
	}

	protected override Dictionary<TerrainLayerName, Vector2Int[]> CalLayerPositions(Vector2Int anchor)
	{
		return new Dictionary<TerrainLayerName, Vector2Int[]>
		{
			{
				TerrainLayerName.Ground,
				groundPositions
			},
			{
				TerrainLayerName.ExtraObstacles,
				extraObstaclePositions
			}
		};
	}

	protected override bool ValidateDeserialization()
	{
		return true;
	}
}
