using System.Collections.Generic;
using UnityEngine;

namespace RedSaw.AI;

public class JpsNode
{
	public Vector2Int pos;

	public List<Vector2Int> parents;

	public int cost;

	public JpsNode(Vector2Int parent, Vector2Int pos, int cost)
	{
		this.pos = pos;
		this.cost = cost;
		parents = new List<Vector2Int>();
		parents.Add(parent);
	}
}
