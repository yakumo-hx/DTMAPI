using UnityEngine;

namespace DolocTown.Config.Building;

public struct FulcrumData
{
	public readonly Vector2Int[] Offsets;

	public readonly TerrainLayerName[] RaycastLayerMasks;

	public readonly int Width;

	public readonly int Depth;

	public FulcrumData(Vector2Int[] offsets, TerrainLayerName[] raycastLayerMasks, int depth)
	{
		Offsets = offsets;
		RaycastLayerMasks = raycastLayerMasks;
		Width = offsets.Length;
		Depth = depth;
	}
}
