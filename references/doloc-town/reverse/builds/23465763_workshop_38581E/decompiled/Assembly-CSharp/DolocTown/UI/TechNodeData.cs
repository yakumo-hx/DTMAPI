using System;
using DolocTown.GameData;
using DolocTown.TreeGraph;
using UnityEngine;

namespace DolocTown.UI;

public struct TechNodeData : IUIData
{
	public bool notEmpty { get; }

	public Vector2Int nodePos { get; }

	public TechNodeStatus nodeStatus { get; }

	public Sprite icon { get; }

	public string title { get; }

	public TechNodeData(TreeGraphNode<TechNodeProto> node, Func<TreeGraphNode<TechNodeProto>, bool> preLockChecker, Func<TreeGraphNode<TechNodeProto>, bool> canAffordChecker)
	{
		this = default(TechNodeData);
		if (node != null)
		{
			notEmpty = true;
			nodePos = node.pos;
			icon = node.data.icon;
			title = node.data.Title;
			if (!node.data.isUseful)
			{
				nodeStatus = TechNodeStatus.VersionUnavailable;
			}
			else if (DolocAPI.archiveHandle.GetTechNodeUnlockState(node.id))
			{
				nodeStatus = TechNodeStatus.Unlocked;
			}
			else if (!preLockChecker(node))
			{
				nodeStatus = TechNodeStatus.PreLocked;
			}
			else
			{
				nodeStatus = (canAffordChecker(node) ? TechNodeStatus.PreUnlockedAndAvailable : TechNodeStatus.PreUnlockedButUnavailable);
			}
		}
	}
}
