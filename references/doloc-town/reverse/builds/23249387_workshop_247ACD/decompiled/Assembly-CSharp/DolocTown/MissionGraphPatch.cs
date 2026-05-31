using System;
using System.Collections.Generic;
using DolocTown.NodeCanvas;
using NodeCanvas.Framework;

namespace DolocTown;

public static class MissionGraphPatch
{
	public static bool ReInvokeMissionNode(this MissionGraph graph, string missionId)
	{
		if (!graph.QueryMissionNode(missionId, out var node))
		{
			return false;
		}
		foreach (MissionNode childrenNode in node.GetChildrenNodes(IsSequenceNode))
		{
			ExecuteNodeIgnoreMission(childrenNode);
		}
		return true;
	}

	private static void ExecuteNodeIgnoreMission(MissionNode node)
	{
		if (node == null)
		{
			return;
		}
		switch (node.nodeType)
		{
		case MissionNodeType.ACTION:
			((MissionNodeAction)node).Execute();
			break;
		case MissionNodeType.CONDITION:
		{
			MissionNode nodeByCondition = ((MissionNodeCondition)node).GetNodeByCondition();
			if (nodeByCondition != null)
			{
				ExecuteNodeIgnoreMission(nodeByCondition);
			}
			break;
		}
		case MissionNodeType.DIALOGUE:
			((IDialogueNode)node).Execute();
			break;
		}
	}

	private static bool IsParallelNode(Connection c)
	{
		if (!(c is MissionConnection missionConnection))
		{
			return false;
		}
		Node targetNode = c.targetNode;
		if (targetNode is MissionNodeAction || targetNode is MissionNodeSubGraph || targetNode is MissionNodeMultiDialogue)
		{
			if (missionConnection.IsParallel)
			{
				return missionConnection.IsAvailable;
			}
			return false;
		}
		if (c.targetNode is MissionNodeListener missionNodeListener)
		{
			if (missionNodeListener.IsDecorator || missionConnection.IsParallel)
			{
				return missionConnection.IsAvailable;
			}
			return false;
		}
		return false;
	}

	private static bool IsSequenceNode(Connection c)
	{
		if (!(c is MissionConnection missionConnection))
		{
			return false;
		}
		Node targetNode = c.targetNode;
		if (targetNode is MissionNodeAction || targetNode is MissionNodeSubGraph || targetNode is MissionNodeMultiDialogue)
		{
			if (missionConnection.IsSequence)
			{
				return missionConnection.IsAvailable;
			}
			return false;
		}
		if (c.targetNode is MissionNodeListener missionNodeListener)
		{
			if (missionNodeListener.IsDecorator)
			{
				return false;
			}
			if (missionConnection.IsSequence)
			{
				return missionConnection.IsAvailable;
			}
			return false;
		}
		return false;
	}

	public static bool TryGetNewMissions(this MissionGraph graph, Func<string, string, bool> isNewMission, out string[] newMissions)
	{
		newMissions = Array.Empty<string>();
		if (graph == null)
		{
			return false;
		}
		List<string> list = new List<string>();
		foreach (string allMissionId in graph.AllMissionIds)
		{
			if (isNewMission(graph.name, allMissionId))
			{
				list.Add(allMissionId);
			}
		}
		newMissions = list.ToArray();
		return newMissions.Length != 0;
	}
}
