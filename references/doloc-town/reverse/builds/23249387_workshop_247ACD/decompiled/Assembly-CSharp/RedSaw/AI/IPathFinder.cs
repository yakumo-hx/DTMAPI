using RedSaw.GameMap;
using UnityEngine;

namespace RedSaw.AI;

public interface IPathFinder
{
	public class EmptyPathFinder : IPathFinder
	{
		public Vector2Int[] FindPath(IGameMap map, Vector2Int f, Vector2Int t)
		{
			return new Vector2Int[1] { t };
		}
	}

	static readonly IPathFinder Empty;

	Vector2Int[] FindPath(IGameMap map, Vector2Int f, Vector2Int to);

	static IPathFinder()
	{
		Empty = new EmptyPathFinder();
	}
}
