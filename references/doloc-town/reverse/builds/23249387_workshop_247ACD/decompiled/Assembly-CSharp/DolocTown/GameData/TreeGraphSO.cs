using System;
using System.Collections.Generic;
using DolocTown.TreeGraph;
using NodeCanvas.Framework;
using UnityEngine;

namespace DolocTown.GameData;

[GraphInfo(packageName = "NodeCanvas", docsURL = "https://nodecanvas.paradoxnotion.com/documentation/", resourcesURL = "https://nodecanvas.paradoxnotion.com/downloads/", forumsURL = "https://nodecanvas.paradoxnotion.com/forums-page/")]
public abstract class TreeGraphSO<TGroup, TNodeSO, TData> : Graph where TGroup : TreeGraphNodeSO<TData> where TNodeSO : TGroup
{
	[SerializeField]
	public string defaultNodeName;

	public override Type baseNodeType => typeof(TGroup);

	public override bool requiresAgent => false;

	public override bool requiresPrimeNode => false;

	public override bool isTree => true;

	public override bool allowBlackboardOverrides => true;

	public override bool canAcceptVariableDrops => true;

	public abstract string[] AvailableNodeNames { get; }

	public Vector2Int MaxIndex
	{
		get
		{
			Vector2Int zero = Vector2Int.zero;
			foreach (Node allNode in base.allNodes)
			{
				if (allNode is TNodeSO val)
				{
					zero.x = Mathf.Max(zero.x, val.treeCanvasPos.x);
					zero.y = Mathf.Max(zero.y, val.treeCanvasPos.y);
				}
			}
			return zero;
		}
	}

	public TreeGraph<TData> CreateTree(Sprite defaultIcon)
	{
		List<TreeGraphNode<TData>> list = new List<TreeGraphNode<TData>>();
		Vector2Int maxIndex = MaxIndex;
		TNodeSO val = null;
		foreach (Node allNode in base.allNodes)
		{
			if (!(allNode is TNodeSO val2))
			{
				continue;
			}
			list.Add(val2.CreateNode(maxIndex.y, defaultIcon));
			if (val2.isEntryNode)
			{
				if (val == null)
				{
					val = val2;
				}
				else if (val2.treeCanvasPos.y < val.treeCanvasPos.y)
				{
					val = val2;
				}
			}
		}
		return new TreeGraph<TData>(base.name, list.ToArray(), maxIndex + Vector2Int.one, defaultNodeName);
	}
}
