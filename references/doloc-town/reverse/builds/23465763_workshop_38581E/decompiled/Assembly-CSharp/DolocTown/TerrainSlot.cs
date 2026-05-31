using UnityEngine;

namespace DolocTown;

public class TerrainSlot
{
	public readonly Vector2Int pos;

	private bool isFilled;

	public bool IsFilled
	{
		get
		{
			return isFilled;
		}
		set
		{
			isFilled = value;
		}
	}

	public bool IsEmpty => !isFilled;

	public TerrainContent Content { get; private set; }

	public TerrainSlot(Vector2Int position)
	{
		pos = position;
		Fill(null);
	}

	public void Fill(TerrainContent cnt)
	{
		Content = cnt;
		IsFilled = cnt != null;
	}

	public void Clear()
	{
		if (isFilled)
		{
			Fill(null);
		}
	}
}
