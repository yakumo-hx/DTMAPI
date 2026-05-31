using UnityEngine;

namespace RedSaw.AI;

public class JpsDirection
{
	public Vector2Int pos;

	public Vector2Int direction;

	public int cost;

	public JpsDirection(Vector2Int p, Vector2Int d, int c)
	{
		pos = p;
		direction = d;
		cost = c;
	}
}
