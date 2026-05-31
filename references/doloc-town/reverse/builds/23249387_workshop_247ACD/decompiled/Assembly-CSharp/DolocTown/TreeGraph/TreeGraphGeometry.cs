using UnityEngine;

namespace DolocTown.TreeGraph;

public class TreeGraphGeometry
{
	private Vector2 rendererNodeSize;

	private Vector2 rendererPadding;

	private Vector2 rendererSpacing;

	private Vector2 rendererEntryPoint;

	public TreeGraphGeometry(Vector2 panelSize, Vector2 nodeSize, Vector2 padding, Vector2Int countRange)
	{
		rendererNodeSize = nodeSize;
		rendererPadding = padding;
		Vector2 vector = panelSize - padding * 2f;
		Vector2 vector2 = nodeSize * countRange;
		Vector2 vector3 = vector - vector2;
		rendererSpacing = new Vector2((countRange.x == 1) ? vector3.x : (vector3.x / (float)(countRange.x - 1)), (countRange.y == 1) ? vector3.y : (vector3.y / (float)(countRange.y - 1)));
		rendererEntryPoint = padding;
	}

	public Vector2 CalcNodePos(Vector2Int pos)
	{
		return pos * (rendererSpacing + rendererNodeSize) + rendererEntryPoint;
	}
}
