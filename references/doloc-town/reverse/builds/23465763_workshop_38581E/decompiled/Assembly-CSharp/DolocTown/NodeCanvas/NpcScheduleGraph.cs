using System;
using System.Collections.Generic;
using DolocTown.GameData;
using NodeCanvas.Framework;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[GraphInfo(packageName = "NodeCanvas", docsURL = "https://nodecanvas.paradoxnotion.com/documentation/", resourcesURL = "https://nodecanvas.paradoxnotion.com/downloads/", forumsURL = "https://nodecanvas.paradoxnotion.com/forums-page/")]
[CreateAssetMenu(menuName = "多洛可小镇[城镇]/Npc计划表")]
public class NpcScheduleGraph : Graph, INpcScheduleGraph
{
	public override Type baseNodeType => typeof(NpcScheduleNode);

	public override bool requiresAgent => false;

	public override bool requiresPrimeNode => true;

	public override bool isTree => true;

	public override bool allowBlackboardOverrides => false;

	public override bool canAcceptVariableDrops => true;

	public bool QuerySchedule(NpcScheduleParams status, out NpcScheduleResult result)
	{
		result = default(NpcScheduleResult);
		if (!(base.primeNode is NpcScheduleNodeEntrance npcScheduleNodeEntrance))
		{
			return false;
		}
		foreach (Connection outConnection in npcScheduleNodeEntrance.outConnections)
		{
			Node targetNode = outConnection.targetNode;
			if (!(targetNode is INpcScheduleNodeFilter filterNode))
			{
				if (targetNode is INpcScheduleNodeTargetScene npcScheduleNodeTargetScene)
				{
					result = new NpcScheduleResult(npcScheduleNodeTargetScene.MarkPointName, npcScheduleNodeTargetScene.Work);
					return true;
				}
			}
			else if (InvokeFilter(status, filterNode, out result))
			{
				return true;
			}
		}
		return false;
	}

	public IEnumerable<string> GetPortalPoints()
	{
		if (base.primeNode is NpcScheduleNodeEntrance npcScheduleNodeEntrance)
		{
			return npcScheduleNodeEntrance.PortalNames;
		}
		return Array.Empty<string>();
	}

	private bool InvokeFilter(NpcScheduleParams status, INpcScheduleNodeFilter filterNode, out NpcScheduleResult result)
	{
		result = default(NpcScheduleResult);
		if (filterNode == null || !filterNode.IsMatch(status))
		{
			return false;
		}
		foreach (INpcScheduleNode child in filterNode.Children)
		{
			if (!(child is INpcScheduleNodeFilter filterNode2))
			{
				if (child is INpcScheduleNodeTargetScene npcScheduleNodeTargetScene)
				{
					result = new NpcScheduleResult(npcScheduleNodeTargetScene.MarkPointName, npcScheduleNodeTargetScene.Work);
					return true;
				}
			}
			else if (InvokeFilter(status, filterNode2, out result))
			{
				return true;
			}
		}
		return false;
	}
}
