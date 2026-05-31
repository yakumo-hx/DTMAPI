using System.Collections.Generic;
using UnityEngine;

namespace DolocTown.TreeGraph;

public class TreeGraphPositionBuffer
{
	private Dictionary<Vector2Int, Vector2> posBufferByPos;

	private Dictionary<string, Vector2> posBufferById;

	public TreeGraphPositionBuffer()
	{
		posBufferById = new Dictionary<string, Vector2>();
		posBufferByPos = new Dictionary<Vector2Int, Vector2>();
	}

	public void RecordPosition(string id, Vector2Int posInt, Vector2 pos)
	{
		if (posBufferById.ContainsKey(id))
		{
			posBufferById[id] = pos;
		}
		else
		{
			posBufferById.Add(id, pos);
		}
		if (posBufferByPos.ContainsKey(posInt))
		{
			posBufferByPos[posInt] = pos;
		}
		else
		{
			posBufferByPos.Add(posInt, pos);
		}
	}

	public Vector2 GetPosition(string id)
	{
		if (posBufferById.ContainsKey(id))
		{
			return posBufferById[id];
		}
		return default(Vector2);
	}

	public bool QueryPosition(string id, out Vector2 position)
	{
		return posBufferById.TryGetValue(id, out position);
	}

	public Vector2 GetPosition(Vector2Int posInt)
	{
		if (posBufferByPos.ContainsKey(posInt))
		{
			return posBufferByPos[posInt];
		}
		return default(Vector2);
	}
}
