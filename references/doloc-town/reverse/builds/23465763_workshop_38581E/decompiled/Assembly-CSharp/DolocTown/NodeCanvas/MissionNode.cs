using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using ParadoxNotion;

namespace DolocTown.NodeCanvas;

public abstract class MissionNode : HorizontalLinkedNode
{
	public override int maxInConnections => 1;

	public override bool canSelfConnect => false;

	public override Alignment2x2 commentsAlignment => Alignment2x2.Bottom;

	public override Alignment2x2 iconAlignment => Alignment2x2.Bottom;

	public override Type outConnectionType => typeof(MissionConnection);

	public abstract MissionNodeType nodeType { get; }

	public override bool allowAsPrime => false;

	public MissionNode firstChild
	{
		get
		{
			if (base.outConnections.Count <= 0)
			{
				return null;
			}
			return (MissionNode)base.outConnections[0].targetNode;
		}
	}

	public MissionGraph missionGraph => (MissionGraph)base.graph;

	public IEnumerable<MissionNode> ChildMissionNodes
	{
		get
		{
			foreach (Connection item in base.outConnections.Where((Connection c) => ((MissionConnection)c).IsAvailable))
			{
				if (item.targetNode is MissionNode missionNode)
				{
					yield return missionNode;
				}
			}
		}
	}

	public IEnumerable<MissionNode> ChildMissionNodesInDepthOrder
	{
		get
		{
			foreach (Node item in base.outConnections.Select((Connection x) => x.targetNode))
			{
				if (!(item is MissionNode _node))
				{
					continue;
				}
				yield return _node;
				foreach (MissionNode item2 in _node.ChildMissionNodesInDepthOrder)
				{
					yield return item2;
				}
			}
		}
	}

	public bool IsPrimeChild
	{
		get
		{
			if (base.inConnections.Count == 0)
			{
				return false;
			}
			foreach (Connection inConnection in base.inConnections)
			{
				if (inConnection.sourceNode is MissionNode { nodeType: MissionNodeType.ENTRANCE })
				{
					return true;
				}
			}
			return false;
		}
	}

	protected override bool CanConnectFromSource(Node sourceNode)
	{
		if (base.outConnections.Count > 0)
		{
			return base.outConnections[0].targetNode != sourceNode;
		}
		return true;
	}

	public IEnumerable<MissionNode> GetChildrenNodes(Func<Connection, bool> condition)
	{
		foreach (Connection item in base.outConnections.Where(condition))
		{
			if (item.targetNode is MissionNode missionNode)
			{
				yield return missionNode;
			}
		}
	}
}
