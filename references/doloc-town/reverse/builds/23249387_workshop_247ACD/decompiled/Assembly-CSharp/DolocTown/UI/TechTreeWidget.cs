using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using RedSaw;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace DolocTown.UI;

public class TechTreeWidget : DolocUIPanel, IBeginDragHandler, IEventSystemHandler
{
	[SerializeField]
	private Transform arrowTrans;

	[SerializeField]
	private ScrollRect scrollRect;

	[SerializeField]
	private Transform nodeContainer;

	[SerializeField]
	private Transform lineContainer;

	[SerializeField]
	private Vector2 ltPadding = new Vector2(72f, 96f);

	[SerializeField]
	private Vector2 nodePadding = new Vector2(132f, 36f);

	[SerializeField]
	private Vector2 nodeSize = new Vector2(100f, 128f);

	[SerializeField]
	private Color lineColor;

	[SerializeField]
	private Color disabledLineColor;

	[SerializeField]
	private LineType lineType;

	[SerializeField]
	private float lineOffsetRate;

	private Sequence arrowMove;

	private Tweener contentMove;

	private ObjectPool<TechNodeSlot> nodes;

	private ObjectPool<LineRenderer> lines;

	private readonly Dictionary<Vector2Int, TechNodeSlot> _nodeLut = new Dictionary<Vector2Int, TechNodeSlot>();

	private Vector2Int treeSize;

	public Vector2Int selectedPosition { get; private set; }

	public bool isFocused { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		GameObject uI_ENTITY_TECHNODE = LocPfbs.UI_ENTITY_TECHNODE;
		nodes = new ObjectPool<TechNodeSlot>(uI_ENTITY_TECHNODE, nodeContainer, usePreset: true);
		lines = new ObjectPool<LineRenderer>(LocPfbs.UI_PFB_LINE, lineContainer, usePreset: true);
	}

	public void RenderAllNode(TechNodeData[] datas, Vector2Int treeSize)
	{
		_nodeLut.Clear();
		nodes.RecycleAll();
		lines.RecycleAll();
		this.treeSize = treeSize;
		scrollRect.content.sizeDelta = ltPadding + new Vector2(8f, 0f) + treeSize * (nodePadding + nodeSize) - nodePadding;
		for (int i = 0; i < datas.Length; i++)
		{
			TechNodeData data = datas[i];
			TechNodeSlot next = nodes.Next;
			Vector2 vector = ltPadding + data.nodePos * (nodePadding + nodeSize);
			next.positionLocal = new Vector2(vector.x, 0f - vector.y);
			_nodeLut.Add(data.nodePos, next);
			next.Render(data);
		}
		BuildNavigation();
	}

	public void RenderLine(Vector2Int parent, Vector2Int child, bool isParentUnlocked)
	{
		LineRenderer next = lines.Next;
		next.rectTransform.sizeDelta = scrollRect.content.sizeDelta;
		next.rectTransform.localPosition = Vector3.zero;
		next.raycastTarget = false;
		next.color = (isParentUnlocked ? lineColor : disabledLineColor);
		Vector2 vector = new Vector2(nodeSize.x / 2f, (0f - nodeSize.x) / 2f);
		Vector2 vector2 = GetSlot(parent).positionLocal + vector;
		Vector2 vector3 = GetSlot(child).positionLocal + vector;
		switch (lineType)
		{
		case LineType.RIGHTANGLE:
			next.BezierMode = UILineRenderer.BezierType.Catenary;
			next.Points = GetRightAnglePoints(vector2, vector3, lineOffsetRate);
			break;
		case LineType.BEZIER:
			next.BezierMode = UILineRenderer.BezierType.Improved;
			next.BezierSegmentsPerCurve = 30;
			next.Points = GetBezierCurvePoints(vector2, vector3);
			break;
		case LineType.STAIGHT:
			next.BezierMode = UILineRenderer.BezierType.None;
			next.Points = new Vector2[2] { vector2, vector3 };
			break;
		}
	}

	public void ReBuildLinks()
	{
		List<LineRenderer> list = lines.Instances.OrderBy((LineRenderer line) => (line.color == lineColor) ? 1 : 0).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			list[i].transform.SetSiblingIndex(i);
		}
	}

	public void RefreshSelectCallback(Action<Vector2Int> callBack)
	{
		for (int i = 0; i < nodes.ActiveCount; i++)
		{
			TechNodeSlot slot = nodes[i];
			slot.button.onSelect.RemoveAllListeners();
			slot.button.onSelect.AddListener(delegate
			{
				selectedPosition = slot.nodePos;
				callBack?.Invoke(selectedPosition);
				OnSlotSelect(slot);
			});
		}
	}

	public void Select(Vector2Int pos)
	{
		if (nodes.ActiveCount != 0)
		{
			TechNodeSlot slot = GetSlot(pos);
			if (EventSystem.current?.currentSelectedGameObject == slot.gameObject)
			{
				EventSystem.current?.SetSelectedGameObject(null);
			}
			slot.Select();
		}
	}

	public void GetFocus()
	{
		isFocused = true;
		Select(selectedPosition);
		arrowTrans.gameObject.SetActive(value: true);
	}

	public void LoseFocus()
	{
		isFocused = false;
		arrowTrans.gameObject.SetActive(value: false);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		contentMove?.Kill();
	}

	private void HoverTo(TechNodeSlot slot, bool useAnimation = true)
	{
		if (!(slot == null))
		{
			arrowTrans.gameObject.SetActive(value: true);
			arrowTrans.SetParent(slot.rectTransform);
			Vector2 localPositionByAnchor = slot.rectTransform.GetLocalPositionByAnchor(UIAlignmentType.LeftTop);
			if (!useAnimation)
			{
				arrowTrans.localPosition = localPositionByAnchor;
				return;
			}
			arrowMove?.Kill();
			arrowMove = DOTween.Sequence();
			arrowMove.Append(arrowTrans.DOLocalMove(localPositionByAnchor, 0.2f)).SetEase(Ease.OutExpo);
		}
	}

	private void OnSlotSelect(TechNodeSlot slot)
	{
		isFocused = true;
		Vector2 vector = scrollRect.viewport.rect.size / 2f;
		contentMove = DOTweenModuleUI.DOAnchorPos(endValue: new Vector2(vector.x - slot.anchoredPositionX, 0f - slot.anchoredPositionY - vector.y), target: scrollRect.content, duration: 0.3f);
		HoverTo(slot);
	}

	private void BuildNavigation()
	{
		for (int i = 0; i < treeSize.x; i++)
		{
			for (int j = 0; j < treeSize.y; j++)
			{
				Navigation navigation = default(Navigation);
				navigation.mode = Navigation.Mode.Explicit;
				Navigation navigation2 = navigation;
				Vector2Int nodePos = new Vector2Int(i, j);
				TechNodeSlot slot = GetSlot(nodePos);
				if (!(slot == null))
				{
					navigation2.selectOnUp = GetNearSlot(nodePos, Vector2Int.down)?.button;
					navigation2.selectOnDown = GetNearSlot(nodePos, Vector2Int.up)?.button;
					navigation2.selectOnLeft = GetNearSlotOnHorizontal(nodePos, right: false)?.button;
					navigation2.selectOnRight = GetNearSlotOnHorizontal(nodePos, right: true)?.button;
					slot.button.navigation = navigation2;
				}
			}
		}
	}

	private TechNodeSlot GetSlot(Vector2Int nodePos)
	{
		if (_nodeLut.ContainsKey(nodePos))
		{
			return _nodeLut[nodePos];
		}
		return null;
	}

	private TechNodeSlot GetNearSlot(Vector2Int nodePos, Vector2Int dir)
	{
		Vector2Int key = nodePos;
		int num = Math.Max(treeSize.x, treeSize.y);
		for (int i = 0; i < num; i++)
		{
			key += dir;
			if (_nodeLut.ContainsKey(key))
			{
				return _nodeLut[key];
			}
		}
		return null;
	}

	private TechNodeSlot GetNearSlotOnHorizontal(Vector2Int nodePos, bool right)
	{
		Vector2Int vector2Int = nodePos;
		vector2Int.x += (right ? 1 : (-1));
		if (_nodeLut.ContainsKey(vector2Int))
		{
			return _nodeLut[vector2Int];
		}
		TechNodeSlot nearSlot = GetNearSlot(vector2Int, Vector2Int.down);
		if (nearSlot != null)
		{
			return nearSlot;
		}
		TechNodeSlot nearSlot2 = GetNearSlot(vector2Int, Vector2Int.up);
		if (nearSlot2 != null)
		{
			return nearSlot2;
		}
		return null;
	}

	private Vector2[] GetRightAnglePoints(Vector2 parent, Vector2 child, float yOffsetRate)
	{
		float num = (child.y - parent.y) * yOffsetRate;
		float x = (parent.x + child.x) / 2f - Mathf.Abs(num);
		parent.y += num;
		child.y -= num;
		return new Vector2[4]
		{
			parent,
			new Vector2(x, parent.y),
			new Vector2(x, child.y),
			child
		};
	}

	private Vector2[] GetBezierCurvePoints(Vector2 parent, Vector2 child)
	{
		float num = (child.x - parent.x) / 4f;
		return new Vector2[4]
		{
			parent,
			new Vector2(child.x - num, parent.y),
			new Vector2(parent.x + num, child.y),
			child
		};
	}
}
