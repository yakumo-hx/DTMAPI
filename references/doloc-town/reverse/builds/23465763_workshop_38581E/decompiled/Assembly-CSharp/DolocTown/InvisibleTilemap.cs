using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

[RequireComponent(typeof(Tilemap))]
public class InvisibleTilemap : InteractableObjectExclude
{
	[SerializeField]
	private float alpha = 0.5f;

	[SerializeField]
	private float duration = 0.3f;

	private Tilemap tilemap;

	private HashSet<Vector3Int> tilesBuffer = new HashSet<Vector3Int>();

	private Tween currentFadeAnimation;

	protected override ITouchCheckStrategy touchChecker { get; set; }

	private MapManager mapManager => DolocAPI.archiveHandle.farmData.mapManager;

	protected override void __Init()
	{
		base.__Init();
		tilemap = GetComponent<Tilemap>();
		touchChecker = new HiddenTriggerChecker();
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		tilesBuffer.Clear();
		if (base.currentOtherCollider == null)
		{
			return;
		}
		Vector3 vector = (DolocAPI.IsAgentRiding ? DolocAPI.Motor.position : DolocAPI.AgentPosition);
		Vector3Int vector3Int = tilemap.WorldToCell(base.currentOtherCollider.ClosestPoint(vector));
		int num = Mathf.FloorToInt(vector3Int.x - 2);
		int num2 = Mathf.CeilToInt(vector3Int.x + 2);
		int num3 = Mathf.FloorToInt(vector3Int.y - 2);
		int num4 = Mathf.CeilToInt(vector3Int.y + 2);
		if (DolocAPI.IsAgentRiding)
		{
			num -= 2;
			num2 += 2;
			num4 += 2;
		}
		bool flag = false;
		for (int i = num; i <= num2; i++)
		{
			for (int j = num3; j <= num4; j++)
			{
				Vector3Int startPos = new Vector3Int(i, j, 0);
				if (tilemap.HasTile(startPos))
				{
					BFS(startPos);
					flag = true;
				}
				if (flag)
				{
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
		SetBlocksInvisible(value: true);
		foreach (Vector3Int item in tilesBuffer)
		{
			if (mapManager.ClearHiddenMask(tilemap.CellToWorld(item)))
			{
				break;
			}
		}
	}

	protected override void OnDisTouch()
	{
		SetBlocksInvisible(value: false);
		base.OnDisTouch();
	}

	private void BFS(Vector3Int startPos)
	{
		Vector3Int[] array = new Vector3Int[4]
		{
			new Vector3Int(0, 1, 0),
			new Vector3Int(0, -1, 0),
			new Vector3Int(1, 0, 0),
			new Vector3Int(-1, 0, 0)
		};
		Queue<Vector3Int> queue = new Queue<Vector3Int>();
		HashSet<Vector3Int> hashSet = new HashSet<Vector3Int>();
		queue.Enqueue(startPos);
		hashSet.Add(startPos);
		while (queue.Count > 0)
		{
			Vector3Int vector3Int = queue.Dequeue();
			tilesBuffer.Add(vector3Int);
			for (int i = 0; i < array.Length; i++)
			{
				Vector3Int item = vector3Int + array[i];
				if (tilemap.HasTile(item) && hashSet.Add(item))
				{
					queue.Enqueue(item);
				}
			}
		}
	}

	private Tween FadeTileAlpha(Vector3Int pos, float alpha, float duration)
	{
		return DOTween.ToAlpha(() => tilemap.GetColor(pos), delegate(Color color)
		{
			tilemap.SetColor(pos, color);
		}, alpha, duration);
	}

	private void SetBlocksInvisible(bool value)
	{
		float num = (value ? alpha : 1f);
		currentFadeAnimation?.Kill();
		Sequence s = DOTween.Sequence();
		foreach (Vector3Int item in tilesBuffer)
		{
			tilemap.SetTileFlags(item, TileFlags.None);
			s.Join(FadeTileAlpha(item, num, duration));
		}
		currentFadeAnimation = s;
	}
}
