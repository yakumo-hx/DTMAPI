using UnityEngine;
using UnityEngine.Tilemaps;

namespace RedSaw;

public class TilemapAutoPainter : MonoBehaviour
{
	private enum TilePosition
	{
		None,
		Empty,
		Core,
		Top,
		Left,
		Right,
		Bottom,
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight,
		InnerBottomLeft,
		InnerBottomRight,
		InnerTopLeft,
		InnerTopRight
	}

	private int warningTimes;

	[HideInInspector]
	public Vector2 positionHandleEnd;

	[HideInInspector]
	public Vector2 positionHandleStart;

	[SerializeField]
	private Tilemap tilemap;

	private Vector2Int _selectionStart;

	private Vector2Int _selectionEnd;

	[SerializeField]
	private TileBase coreTile;

	[SerializeField]
	private TileBase topTile;

	[SerializeField]
	private TileBase leftTile;

	[SerializeField]
	private TileBase rightTile;

	[SerializeField]
	private TileBase bottomTile;

	[SerializeField]
	private TileBase topLeftTile;

	[SerializeField]
	private TileBase topRightTile;

	[SerializeField]
	private TileBase bottomLeftTile;

	[SerializeField]
	private TileBase bottomRightTile;

	[SerializeField]
	private TileBase innerBottomLeftTile;

	[SerializeField]
	private TileBase innerBottomRightTile;

	[SerializeField]
	private TileBase innerTopLeftTile;

	[SerializeField]
	private TileBase innerTopRightTile;

	public void OnPositionHandleEndChanged(Vector2 newValue)
	{
		if (tilemap == null)
		{
			if (++warningTimes % 30 == 0)
			{
				Debug.LogWarning("TilemapAutoPainter:没有设置Tilemap");
			}
		}
		else
		{
			_selectionEnd = Lattice(newValue, tilemap.cellSize);
		}
	}

	public void OnPositionHandleStartChanged(Vector2 newValue)
	{
		if (tilemap == null)
		{
			if (++warningTimes % 30 == 0)
			{
				Debug.LogWarning("TilemapAutoPainter:没有设置Tilemap");
			}
		}
		else
		{
			_selectionStart = Lattice(newValue, tilemap.cellSize);
		}
	}

	public void OnFocus()
	{
		Recalculate();
	}

	private void Recalculate()
	{
		if (tilemap == null)
		{
			Debug.LogWarning("TilemapAutoPainter:对象\"" + base.gameObject.name + "\"没有设置Tilemap");
			return;
		}
		_selectionStart = Lattice(positionHandleStart, tilemap.cellSize);
		_selectionEnd = Lattice(positionHandleEnd, tilemap.cellSize);
	}

	private Vector2Int Lattice(Vector2 position, Vector2 gridSize)
	{
		return new Vector2Int(Mathf.FloorToInt(position.x / gridSize.x), Mathf.FloorToInt(position.y / gridSize.y));
	}

	private bool IsAllTileSet()
	{
		if (coreTile != null && topTile != null && leftTile != null && rightTile != null && bottomTile != null && topLeftTile != null && topRightTile != null && bottomLeftTile != null)
		{
			return bottomRightTile != null;
		}
		return false;
	}

	private bool IsAllInnerTileSet()
	{
		if (innerBottomLeftTile != null && innerBottomRightTile != null && innerTopLeftTile != null)
		{
			return innerTopRightTile != null;
		}
		return false;
	}

	private void LoadTileFromCurrentSelection()
	{
		int num = Mathf.Min(_selectionEnd.x, _selectionStart.x);
		int num2 = Mathf.Min(_selectionEnd.y, _selectionStart.y);
		new Vector3Int(num, num2, 0);
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				Vector3Int position = new Vector3Int(i + num, j + num2);
				TileBase tile = tilemap.GetTile(position);
				switch (CalcPosition(new Vector2Int(i, j)))
				{
				case TilePosition.Bottom:
					bottomTile = tile;
					break;
				case TilePosition.BottomLeft:
					bottomLeftTile = tile;
					break;
				case TilePosition.BottomRight:
					bottomRightTile = tile;
					break;
				case TilePosition.Core:
					coreTile = tile;
					break;
				case TilePosition.Left:
					leftTile = tile;
					break;
				case TilePosition.Right:
					rightTile = tile;
					break;
				case TilePosition.Top:
					topTile = tile;
					break;
				case TilePosition.TopLeft:
					topLeftTile = tile;
					break;
				case TilePosition.TopRight:
					topRightTile = tile;
					break;
				}
			}
		}
	}

	private void LoadInnerTileFromCurrentSelection()
	{
		int num = Mathf.Min(_selectionEnd.x, _selectionStart.x);
		int num2 = Mathf.Min(_selectionEnd.y, _selectionStart.y);
		new Vector3Int(num, num2, 0);
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				Vector3Int position = new Vector3Int(i + num, j + num2);
				TileBase tile = tilemap.GetTile(position);
				if (!(tile == null))
				{
					switch (CalcInnerPosition(new Vector2Int(i, j)))
					{
					case TilePosition.InnerBottomLeft:
						innerBottomLeftTile = tile;
						break;
					case TilePosition.InnerBottomRight:
						innerBottomRightTile = tile;
						break;
					case TilePosition.InnerTopLeft:
						innerTopLeftTile = tile;
						break;
					case TilePosition.InnerTopRight:
						innerTopRightTile = tile;
						break;
					}
				}
			}
		}
	}

	private TilePosition CalcPosition(Vector2Int pos)
	{
		if (pos == new Vector2Int(1, 2))
		{
			return TilePosition.Top;
		}
		if (pos == new Vector2Int(0, 1))
		{
			return TilePosition.Left;
		}
		if (pos == new Vector2Int(2, 1))
		{
			return TilePosition.Right;
		}
		if (pos == new Vector2Int(1, 0))
		{
			return TilePosition.Bottom;
		}
		if (pos == new Vector2Int(0, 2))
		{
			return TilePosition.TopLeft;
		}
		if (pos == new Vector2Int(2, 2))
		{
			return TilePosition.TopRight;
		}
		if (pos == new Vector2Int(0, 0))
		{
			return TilePosition.BottomLeft;
		}
		if (pos == new Vector2Int(2, 0))
		{
			return TilePosition.BottomRight;
		}
		if (pos == new Vector2Int(1, 1))
		{
			return TilePosition.Core;
		}
		return TilePosition.None;
	}

	private TilePosition CalcInnerPosition(Vector2Int pos)
	{
		if (pos == Vector2Int.zero)
		{
			return TilePosition.InnerBottomLeft;
		}
		if (pos == Vector2Int.one)
		{
			return TilePosition.InnerTopRight;
		}
		if (pos == new Vector2Int(1, 0))
		{
			return TilePosition.InnerBottomRight;
		}
		if (pos == new Vector2Int(0, 1))
		{
			return TilePosition.InnerTopLeft;
		}
		return TilePosition.None;
	}

	private TilePosition CalcPosition(Tilemap tilemap, Vector2Int pos, int depth = 0)
	{
		if (depth > 1)
		{
			return TilePosition.None;
		}
		Vector3Int vector3Int = new Vector3Int(pos.x, pos.y);
		if (!tilemap.HasTile(vector3Int))
		{
			return TilePosition.Empty;
		}
		bool top = tilemap.HasTile(vector3Int + Vector3Int.up);
		bool left = tilemap.HasTile(vector3Int + Vector3Int.left);
		bool right = tilemap.HasTile(vector3Int + Vector3Int.right);
		bool bottom = tilemap.HasTile(vector3Int + Vector3Int.down);
		bool topLeft = tilemap.HasTile(vector3Int + Vector3Int.up + Vector3Int.left);
		bool topRight = tilemap.HasTile(vector3Int + Vector3Int.up + Vector3Int.right);
		bool bottomLeft = tilemap.HasTile(vector3Int + Vector3Int.down + Vector3Int.left);
		bool bottomRight = tilemap.HasTile(vector3Int + Vector3Int.down + Vector3Int.right);
		if (IsTop(top, left, right, bottom, topLeft, topRight, bottomLeft, bottomRight))
		{
			return TilePosition.Top;
		}
		if (IsLeft(top, left, right, bottom, topLeft, topRight, bottomLeft, bottomRight))
		{
			return TilePosition.Left;
		}
		if (IsRight(top, left, right, bottom, topLeft, topRight, bottomLeft, bottomRight))
		{
			return TilePosition.Right;
		}
		if (IsBottom(top, left, right, bottom, topLeft, topRight, bottomLeft, bottomRight))
		{
			return TilePosition.Bottom;
		}
		if (IsTopLeft(top, left, right, bottom, topLeft, topRight, bottomLeft, bottomRight))
		{
			return TilePosition.TopLeft;
		}
		if (IsTopRight(top, left, right, bottom, topLeft, topRight, bottomLeft, bottomRight))
		{
			return TilePosition.TopRight;
		}
		if (IsBottomLeft(top, left, right, bottom, topLeft, topRight, bottomLeft, bottomRight))
		{
			return TilePosition.BottomLeft;
		}
		if (IsBottomRight(top, left, right, bottom, topLeft, topRight, bottomLeft, bottomRight))
		{
			return TilePosition.BottomRight;
		}
		if (IsCore(top, left, right, bottom, topLeft, topRight, bottomLeft, bottomRight))
		{
			Vector2Int pos2 = new Vector2Int(pos.x - 1, pos.y);
			Vector2Int pos3 = new Vector2Int(pos.x + 1, pos.y);
			Vector2Int pos4 = new Vector2Int(pos.x, pos.y + 1);
			Vector2Int pos5 = new Vector2Int(pos.x, pos.y - 1);
			TilePosition tilePosition = CalcPosition(tilemap, pos2, depth + 1);
			TilePosition tilePosition2 = CalcPosition(tilemap, pos3, depth + 1);
			TilePosition tilePosition3 = CalcPosition(tilemap, pos4, depth + 1);
			TilePosition tilePosition4 = CalcPosition(tilemap, pos5, depth + 1);
			if ((tilePosition == TilePosition.Left || tilePosition == TilePosition.TopLeft || tilePosition == TilePosition.Top) && (tilePosition3 == TilePosition.Top || tilePosition3 == TilePosition.TopLeft || tilePosition3 == TilePosition.Left))
			{
				return TilePosition.InnerTopLeft;
			}
			if ((tilePosition2 == TilePosition.Right || tilePosition2 == TilePosition.TopRight || tilePosition2 == TilePosition.Top) && (tilePosition3 == TilePosition.Top || tilePosition3 == TilePosition.TopRight || tilePosition3 == TilePosition.Right))
			{
				return TilePosition.InnerTopRight;
			}
			if ((tilePosition == TilePosition.Left || tilePosition == TilePosition.BottomLeft || tilePosition == TilePosition.Bottom) && (tilePosition4 == TilePosition.Bottom || tilePosition4 == TilePosition.BottomLeft || tilePosition4 == TilePosition.Left))
			{
				return TilePosition.InnerBottomLeft;
			}
			if ((tilePosition2 == TilePosition.Right || tilePosition2 == TilePosition.BottomRight || tilePosition2 == TilePosition.Bottom) && (tilePosition4 == TilePosition.Bottom || tilePosition4 == TilePosition.BottomRight || tilePosition4 == TilePosition.Right))
			{
				return TilePosition.InnerBottomRight;
			}
			return TilePosition.Core;
		}
		return TilePosition.None;
	}

	private bool IsCore(bool top, bool left, bool right, bool bottom, bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
	{
		int num = 0;
		if (top)
		{
			num++;
		}
		if (left)
		{
			num++;
		}
		if (right)
		{
			num++;
		}
		if (bottom)
		{
			num++;
		}
		if (topLeft)
		{
			num++;
		}
		if (topRight)
		{
			num++;
		}
		if (bottomLeft)
		{
			num++;
		}
		if (bottomRight)
		{
			num++;
		}
		return num >= 7;
	}

	private bool IsBottom(bool top, bool left, bool right, bool bottom, bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
	{
		return !bottom && left && right && top && topLeft && topRight;
	}

	private bool IsTop(bool top, bool left, bool right, bool bottom, bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
	{
		return !top && left && right && bottom && bottomLeft && bottomRight;
	}

	private bool IsLeft(bool top, bool left, bool right, bool bottom, bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
	{
		return !left && top && bottom && right && topRight && bottomRight;
	}

	private bool IsRight(bool top, bool left, bool right, bool bottom, bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
	{
		return !right && top && bottom && left && topLeft && bottomLeft;
	}

	private bool IsBottomRight(bool top, bool left, bool right, bool bottom, bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
	{
		if (left && top && topLeft && !right && !bottom)
		{
			return !bottomRight;
		}
		return false;
	}

	private bool IsBottomLeft(bool top, bool left, bool right, bool bottom, bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
	{
		if (right && top && topRight && !left && !bottom)
		{
			return !bottomLeft;
		}
		return false;
	}

	private bool IsTopRight(bool top, bool left, bool right, bool bottom, bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
	{
		if (left && bottom && bottomLeft && !right && !top)
		{
			return !topRight;
		}
		return false;
	}

	private bool IsTopLeft(bool top, bool left, bool right, bool bottom, bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
	{
		if (right && bottom && bottomRight && !left && !top)
		{
			return !topLeft;
		}
		return false;
	}

	private void AutoPaint()
	{
		if (!IsAllTileSet())
		{
			Debug.LogWarning("TilemapAutoPainter:至少设置所有九宫格瓦片");
			return;
		}
		Vector2Int vector2Int = new Vector2Int(Mathf.Min(_selectionStart.x, _selectionEnd.x), Mathf.Min(_selectionStart.y, _selectionEnd.y));
		Vector2Int vector2Int2 = new Vector2Int(Mathf.Max(_selectionStart.x, _selectionEnd.x), Mathf.Max(_selectionStart.y, _selectionEnd.y));
		for (int i = vector2Int.x; i <= vector2Int2.x; i++)
		{
			for (int j = vector2Int.y; j < vector2Int2.y; j++)
			{
				switch (CalcPosition(pos: new Vector2Int(i, j), tilemap: tilemap))
				{
				case TilePosition.Core:
					tilemap.SetTile(new Vector3Int(i, j, 0), coreTile);
					break;
				case TilePosition.Top:
					tilemap.SetTile(new Vector3Int(i, j, 0), topTile);
					break;
				case TilePosition.Left:
					tilemap.SetTile(new Vector3Int(i, j, 0), leftTile);
					break;
				case TilePosition.Right:
					tilemap.SetTile(new Vector3Int(i, j, 0), rightTile);
					break;
				case TilePosition.Bottom:
					tilemap.SetTile(new Vector3Int(i, j, 0), bottomTile);
					break;
				case TilePosition.TopLeft:
					tilemap.SetTile(new Vector3Int(i, j, 0), topLeftTile);
					break;
				case TilePosition.TopRight:
					tilemap.SetTile(new Vector3Int(i, j, 0), topRightTile);
					break;
				case TilePosition.BottomLeft:
					tilemap.SetTile(new Vector3Int(i, j, 0), bottomLeftTile);
					break;
				case TilePosition.BottomRight:
					tilemap.SetTile(new Vector3Int(i, j, 0), bottomRightTile);
					break;
				case TilePosition.InnerBottomLeft:
					tilemap.SetTile(new Vector3Int(i, j, 0), (innerBottomLeftTile == null) ? coreTile : innerBottomLeftTile);
					break;
				case TilePosition.InnerBottomRight:
					tilemap.SetTile(new Vector3Int(i, j, 0), (innerBottomRightTile == null) ? coreTile : innerBottomRightTile);
					break;
				case TilePosition.InnerTopLeft:
					tilemap.SetTile(new Vector3Int(i, j, 0), (innerTopLeftTile == null) ? coreTile : innerTopLeftTile);
					break;
				case TilePosition.InnerTopRight:
					tilemap.SetTile(new Vector3Int(i, j, 0), (innerTopRightTile == null) ? coreTile : innerTopRightTile);
					break;
				}
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (!(tilemap == null))
		{
			Gizmos.color = Color.cyan;
			Vector2 lB = (Vector2)_selectionStart * (Vector2)tilemap.cellSize;
			Vector2 rT = (Vector2)_selectionEnd * (Vector2)tilemap.cellSize;
			GizmosHelper.DrawBox2Point(lB, rT);
		}
	}
}
