using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class AgentCellTip : DolocBasicTip
{
	[SerializeField]
	private Image cellImg;

	[SerializeField]
	public Color validColor;

	[SerializeField]
	public Color invalidColor;

	private Vector2Int oringinOffset;

	private bool flipWhenFaceLeft;

	private int lastTotalSeconds;

	private bool updatePerSec;

	private Action updateCallBack;

	private Vector2Int _cellSize;

	private Vector2 lastWorldCellPos;

	private int currentTotalSeconds => DolocAPI.archiveHandle.timeData.totalSeconds;

	private Vector2Int offset
	{
		get
		{
			Vector2Int result = oringinOffset;
			if (flipWhenFaceLeft && !DolocAPI.AgentFaceRight)
			{
				result *= new Vector2Int(-1, 1);
				result.x -= cellSize.x;
			}
			return result;
		}
	}

	private Vector2Int cellSize
	{
		get
		{
			return _cellSize;
		}
		set
		{
			_cellSize = value;
			cellImg.rectTransform.sizeDelta = DolocAPI.TileSizeToScreenSize(value);
		}
	}

	public Vector2Int CellAnchor => DolocAPI.AgentRoomCellPosition + offset;

	public Vector2Int CellAnchorSource => DolocAPI.AgentRoomCellPosition;

	public Vector2Int CellSize => cellSize;

	public IEnumerable<Vector2Int> CellPositions
	{
		get
		{
			Vector2Int anchor = CellAnchor;
			for (int x = 0; x < cellSize.x; x++)
			{
				yield return new Vector2Int(anchor.x + x, anchor.y);
			}
		}
	}

	public Vector2 WorldCellPos => DolocAPI.AgentWorldCellPosition + (Vector2)offset * 1.5f;

	public bool CellTipValid
	{
		set
		{
			cellImg.color = (value ? validColor : invalidColor);
		}
	}

	public Color Color
	{
		set
		{
			cellImg.color = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		canvasGroup.alpha = 0f;
		Hide();
	}

	public void Show(Vector2Int offset, Vector2Int tileSize, bool flipWhenFaceLeft, Action updateCallBack = null, bool updatePerSec = false)
	{
		cellSize = tileSize;
		this.flipWhenFaceLeft = flipWhenFaceLeft;
		oringinOffset = offset;
		CellTipValid = true;
		cellImg.gameObject.SetActive(value: true);
		this.updatePerSec = updatePerSec;
		this.updateCallBack = updateCallBack;
		lastTotalSeconds = currentTotalSeconds;
	}

	public void Hide()
	{
		updatePerSec = false;
		updateCallBack = null;
		cellImg.gameObject.SetActive(value: false);
	}

	private void LateUpdate()
	{
		if (!DolocAPI.IsDataLoaded)
		{
			return;
		}
		if (!DolocAPI.IsCurrentStateSupportInteract)
		{
			canvasGroup.alpha = 0f;
			return;
		}
		canvasGroup.alpha = 1f;
		Vector2 vector = DolocAPI.WorldToScreen(WorldCellPos);
		if (lastWorldCellPos != vector)
		{
			lastWorldCellPos = vector;
			cellImg.transform.position = new Vector3(vector.x, vector.y, cellImg.transform.position.z);
			updateCallBack?.Invoke();
			lastTotalSeconds = currentTotalSeconds;
		}
		else if (updatePerSec && lastTotalSeconds != currentTotalSeconds)
		{
			lastTotalSeconds = currentTotalSeconds;
			updateCallBack?.Invoke();
		}
	}
}
