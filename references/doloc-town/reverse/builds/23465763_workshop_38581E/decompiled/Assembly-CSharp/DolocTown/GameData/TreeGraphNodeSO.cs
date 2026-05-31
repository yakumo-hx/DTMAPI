using System;
using DolocTown.NodeCanvas;
using DolocTown.TreeGraph;
using ParadoxNotion;
using UnityEngine;

namespace DolocTown.GameData;

public abstract class TreeGraphNodeSO<TData> : HorizontalLinkedNode
{
	public bool isEntryNode;

	public Vector2Int treeCanvasPos;

	public override int maxInConnections => -1;

	public override int maxOutConnections => -1;

	public override Type outConnectionType => typeof(HorizontalLinkedConnection);

	public override bool allowAsPrime => false;

	public override bool canSelfConnect => false;

	public override Alignment2x2 commentsAlignment => Alignment2x2.Right;

	public override Alignment2x2 iconAlignment => Alignment2x2.Bottom;

	public void UpdateCanvasPos(TreeGraphNodeSO<TData> parentNode, int childIndex)
	{
		if (_GetParentIndex(parentNode) == 0)
		{
			treeCanvasPos = parentNode.treeCanvasPos + new Vector2Int(1, childIndex);
			UpdateChildrenPosition();
		}
	}

	protected virtual void UpdateChildrenPosition()
	{
		for (int i = 0; i < base.outConnections.Count; i++)
		{
			(base.outConnections[i].targetNode as TreeGraphNodeSO<TData>)?.UpdateCanvasPos(this, i);
		}
	}

	protected int _GetParentIndex(TreeGraphNodeSO<TData> node)
	{
		for (int i = 0; i < base.inConnections.Count; i++)
		{
			if (base.inConnections[i].sourceNode == node)
			{
				return i;
			}
		}
		return -1;
	}

	public abstract TreeGraphNode<TData> CreateNode(int maxYIndex, Sprite defaultIcon);
}
