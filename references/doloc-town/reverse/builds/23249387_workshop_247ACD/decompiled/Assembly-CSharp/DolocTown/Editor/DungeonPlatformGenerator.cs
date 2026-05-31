using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown.Editor;

public class DungeonPlatformGenerator : MonoBehaviour
{
	private struct PlatformConfig
	{
		public Vector2Int left;

		public Vector2Int right;

		public bool IsValid
		{
			get
			{
				if (left.x < right.x)
				{
					return left.y == right.y;
				}
				return false;
			}
		}
	}

	[SerializeField]
	private GameObject platformPrefab;

	[SerializeField]
	private Tilemap tilemap;

	[SerializeField]
	private Transform container;

	private void Generate()
	{
		if (platformPrefab == null || tilemap == null || container == null)
		{
			Debug.Log("<color=red>地牢支架生成器：部分参数没有设置</color>");
			return;
		}
		PlatformConfig[] array = SearchAllPlatforms(tilemap);
		if (array.Length == 0)
		{
			Debug.Log("<color=yellow>地牢支架生成器：没有找到任何支架</color>");
			return;
		}
		EnsureChildrenCount(container, platformPrefab, array.Length);
		float num = 1.5f;
		for (int i = 0; i < array.Length; i++)
		{
			PlatformConfig platformConfig = array[i];
			EdgeCollider2D component = container.GetChild(i).GetComponent<EdgeCollider2D>();
			float y = (float)(platformConfig.left.y + 1) * num;
			component.points = new Vector2[2]
			{
				new Vector2((float)platformConfig.left.x * num, y),
				new Vector2((float)(platformConfig.right.x + 1) * num, y)
			};
		}
		Debug.Log("<color=#00ff00>地牢支架生成器：成功生成了 " + array.Length + " 个支架</color>");
	}

	private static void EnsureChildrenCount(Transform parent, GameObject prefab, int count)
	{
		int childCount = parent.childCount;
		if (childCount < count)
		{
			for (int i = childCount; i < count; i++)
			{
				Object.Instantiate(prefab, parent);
			}
		}
		else if (childCount > count)
		{
			for (int num = childCount - 1; num >= count; num--)
			{
				Object.DestroyImmediate(parent.GetChild(num).gameObject);
			}
		}
	}

	private static PlatformConfig[] SearchAllPlatforms(Tilemap tilemap)
	{
		tilemap.CompressBounds();
		List<PlatformConfig> list = new List<PlatformConfig>();
		for (int i = 0; i < tilemap.size.y + tilemap.origin.y; i++)
		{
			PlatformConfig[] collection = SearchForLine(tilemap, i);
			list.AddRange(collection);
		}
		list.RemoveAll((PlatformConfig config) => !config.IsValid);
		return list.ToArray();
	}

	private static PlatformConfig[] SearchForLine(Tilemap tilemap, int y)
	{
		List<PlatformConfig> list = new List<PlatformConfig>();
		TileBase tileBase = null;
		Vector3Int vector3Int = Vector3Int.zero;
		for (int i = 0; i < tilemap.size.x + tilemap.origin.x; i++)
		{
			Vector3Int vector3Int2 = new Vector3Int(i, y, 0);
			TileBase tile = tilemap.GetTile(vector3Int2);
			if (tileBase == null)
			{
				if (tile != null)
				{
					tileBase = tile;
					vector3Int = vector3Int2;
				}
			}
			else if (tile == null)
			{
				list.Add(new PlatformConfig
				{
					left = new Vector2Int(vector3Int.x, y),
					right = new Vector2Int(i - 1, y)
				});
				tileBase = null;
			}
		}
		if (tileBase != null)
		{
			list.Add(new PlatformConfig
			{
				left = new Vector2Int(vector3Int.x, y),
				right = new Vector2Int(tilemap.size.x + tilemap.origin.x - 1, y)
			});
		}
		return list.ToArray();
	}
}
