using UnityEngine;

namespace DolocTown.TreeGraph;

public class TreeGraphNode<T>
{
	public readonly string id;

	public readonly Vector2Int pos;

	public readonly string[] parents;

	public readonly T data;

	public TreeGraphNode(string id, Vector2Int pos, string[] parents, T data)
	{
		this.id = id;
		this.pos = pos;
		this.parents = parents;
		this.data = data;
	}
}
