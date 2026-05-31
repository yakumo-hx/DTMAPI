using UnityEngine;

namespace DolocTown.TreeGraph;

public struct TreeGraphLink
{
	public Vector2Int parent;

	public Vector2Int child;

	public TreeGraphLink(Vector2Int parent, Vector2Int child)
	{
		this.parent = parent;
		this.child = child;
	}
}
