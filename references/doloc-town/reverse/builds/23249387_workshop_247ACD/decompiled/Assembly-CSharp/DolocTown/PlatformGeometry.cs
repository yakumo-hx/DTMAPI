using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class PlatformGeometry
{
	[JsonProperty]
	[JsonConverter(typeof(VectorConverter))]
	private Vector3Int platform;

	[JsonProperty]
	[JsonConverter(typeof(VectorConverter))]
	public Vector2Int columnHeight;

	public Vector3Int PlatformInfo => platform;

	public int Left => platform.x;

	public int Right => platform.y;

	public int Height => platform.z;

	public Vector2Int LeftTop => new Vector2Int(platform.x, platform.z);

	public Vector2Int RightTop => new Vector2Int(platform.y, platform.z);

	public Vector2Int LeftBottom => new Vector2Int(platform.x, platform.z - columnHeight.x);

	public Vector2Int Anchor => LeftBottom;

	public Vector2Int CenterTop => new Vector2Int(platform.x + Width / 2, platform.z);

	public IEnumerable<Vector2Int> SurfacePositions
	{
		get
		{
			int height = platform.z + 1;
			for (int i = platform.x; i <= platform.y; i++)
			{
				yield return new Vector2Int(i, height);
			}
		}
	}

	public IEnumerable<Vector2Int> PlatformPositions
	{
		get
		{
			int num = platform.x + 1;
			for (int i = num; i < platform.y; i++)
			{
				yield return new Vector2Int(i, platform.z);
			}
		}
	}

	public IEnumerable<Vector2Int> PlatformPositionsIncludeBorder
	{
		get
		{
			for (int i = platform.x; i <= platform.y; i++)
			{
				yield return new Vector2Int(i, platform.z);
			}
		}
	}

	public IEnumerable<Vector2Int> ColumnPositions
	{
		get
		{
			int height = platform.z - 1;
			int bottom2 = height - columnHeight.x;
			for (int j = height; j > bottom2; j--)
			{
				yield return new Vector2Int(platform.x, j);
			}
			bottom2 = height - columnHeight.y;
			for (int j = height; j > bottom2; j--)
			{
				yield return new Vector2Int(platform.y, j);
			}
		}
	}

	public IEnumerable<Vector2Int> AllPositions
	{
		get
		{
			yield return LeftTop;
			yield return RightTop;
			foreach (Vector2Int columnPosition in ColumnPositions)
			{
				yield return columnPosition;
			}
			foreach (Vector2Int platformPosition in PlatformPositions)
			{
				yield return platformPosition;
			}
		}
	}

	public int Width => platform.y - platform.x + 1;

	public int TileCount => columnHeight.x + columnHeight.y + Width;

	public Vector2Int CoveredSize => new Vector2Int(Width, Mathf.Max(columnHeight.x, columnHeight.y) + 1);

	public static PlatformGeometry Calculate(Vector2Int LB, Vector2Int RB, Vector2Int RT)
	{
		if (LB.x > RB.x)
		{
			Vector2Int vector2Int = LB;
			Vector2Int vector2Int2 = RB;
			RB = vector2Int;
			LB = vector2Int2;
			RT.x = RB.x;
		}
		Vector3Int vector3Int = new Vector3Int(LB.x, RB.x, RT.y);
		Vector2Int vector2Int3 = new Vector2Int(RT.y - LB.y, RT.y - RB.y);
		return new PlatformGeometry(vector3Int, vector2Int3);
	}

	[JsonConstructor]
	public PlatformGeometry(Vector3Int platform, Vector2Int columnHeight)
	{
		this.platform = platform;
		this.columnHeight = columnHeight;
	}

	public void Shift(Vector2Int offset)
	{
		platform.x += offset.x;
		platform.y += offset.x;
		platform.z += offset.y;
	}

	public bool IsSame(PlatformGeometry other, bool ignoreColumn = true)
	{
		if (!ignoreColumn)
		{
			if (other.platform == platform)
			{
				return other.columnHeight == columnHeight;
			}
			return false;
		}
		return other.platform == platform;
	}

	public Vector2Int[] GetFillPositions()
	{
		if (Width == 0)
		{
			return Array.Empty<Vector2Int>();
		}
		Vector2Int[] array = new Vector2Int[Width];
		for (int i = 0; i < Width; i++)
		{
			array[i] = new Vector2Int(i + platform.x, platform.z);
		}
		return array;
	}

	public IEnumerable<Vector2Int> GetColumnPositions()
	{
		yield return LeftTop;
		yield return RightTop;
		foreach (Vector2Int columnPosition in ColumnPositions)
		{
			yield return columnPosition;
		}
	}

	public void HandleColumn(Action<Vector2Int> handle)
	{
		foreach (Vector2Int columnPosition in ColumnPositions)
		{
			handle(columnPosition);
		}
	}

	public void HandlePlatform(Action<Vector2Int> handle)
	{
		foreach (Vector2Int platformPosition in PlatformPositions)
		{
			handle(platformPosition);
		}
	}

	public void HandleAll(Action<Vector2Int> handle)
	{
		foreach (Vector2Int allPosition in AllPositions)
		{
			handle(allPosition);
		}
	}

	public bool CheckSurface(Func<Vector2Int, bool> check)
	{
		foreach (Vector2Int surfacePosition in SurfacePositions)
		{
			if (!check(surfacePosition))
			{
				return false;
			}
		}
		return true;
	}

	public bool IsConnected(PlatformGeometry other)
	{
		if (Left != other.Right + 1)
		{
			return Right == other.Left - 1;
		}
		return true;
	}

	public bool IsNotConnected(PlatformGeometry other)
	{
		if (Left != other.Right + 1)
		{
			return Right != other.Left - 1;
		}
		return false;
	}

	public bool IsColumn(Vector2Int pos)
	{
		if (pos.x == Left)
		{
			if (pos.y < Height)
			{
				return pos.y >= Height - columnHeight.x;
			}
			return false;
		}
		if (pos.x == Right)
		{
			if (pos.y < Height)
			{
				return pos.y >= Height - columnHeight.y;
			}
			return false;
		}
		return false;
	}

	public IEnumerable<Vector2Int> GetColumnPositionsFromPos(Vector2Int pos)
	{
		int bottom2;
		if (pos.x == Left)
		{
			bottom2 = Height - columnHeight.x;
			if (pos.y < bottom2 || pos.y >= Height)
			{
				yield break;
			}
			for (int y2 = pos.y; y2 >= bottom2; y2--)
			{
				yield return new Vector2Int(pos.x, y2);
			}
		}
		if (pos.x != Right)
		{
			yield break;
		}
		bottom2 = Height - columnHeight.y;
		if (pos.y >= bottom2 && pos.y < Height)
		{
			for (int y2 = pos.y; y2 >= bottom2; y2--)
			{
				yield return new Vector2Int(pos.x, y2);
			}
		}
	}

	public int CutColumn(Vector2Int pos)
	{
		if (pos.x == Left)
		{
			int num = Height - columnHeight.x;
			if (pos.y < num || pos.y >= Height)
			{
				return 0;
			}
			int num2 = pos.y - num + 1;
			columnHeight.x -= num2;
			return num2;
		}
		if (pos.x == Right)
		{
			int num3 = Height - columnHeight.y;
			if (pos.y < num3 || pos.y >= Height)
			{
				return 0;
			}
			int num4 = pos.y - num3 + 1;
			columnHeight.y -= num4;
			return num4;
		}
		return 0;
	}
}
