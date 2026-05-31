using System;
using RedSaw.GameMap;
using UnityEngine;

namespace RedSaw.AI;

public class AStarGroundColumn : AStarBase, IPathFinder
{
	private IGameMap _map;

	private int vRange;

	private int hRange;

	private int jumpTolerance;

	private int touchTolerance;

	public AStarGroundColumn(int vRange, int hRange, int jumpTolerance, int touchTolerance)
	{
		this.vRange = vRange;
		this.hRange = hRange;
		this.jumpTolerance = jumpTolerance;
		this.touchTolerance = touchTolerance;
	}

	public Vector2Int[] FindPath(IGameMap map, Vector2Int f, Vector2Int to)
	{
		_map = map;
		return PathFinderUtils.SimplifyGroundPath(map, FindPath(f, to, fromDir: false));
	}

	protected override Vector2Int[] GetNeighbours(Vector2Int pos)
	{
		return _map.GetJumpPoints(pos, hRange, vRange, jumpTolerance, touchTolerance);
	}

	public Vector2Int[] GetNeighboursExposed(Vector2Int pos)
	{
		return GetNeighbours(pos);
	}

	protected override Vector2Int[] GetNeighbourDirs()
	{
		return Array.Empty<Vector2Int>();
	}

	protected override bool IsObstacle(Vector2Int pos)
	{
		return !_map.IsGround(pos);
	}
}
