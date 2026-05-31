using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[GraphInfo(packageName = "NodeCanvas", docsURL = "https://nodecanvas.paradoxnotion.com/documentation/", resourcesURL = "https://nodecanvas.paradoxnotion.com/downloads/", forumsURL = "https://nodecanvas.paradoxnotion.com/forums-page/")]
[CreateAssetMenu(menuName = "多洛可小镇[城镇]/任务链")]
public class MissionGraph : Graph
{
	[SerializeField]
	private MissionRequireTemplate defaultMissionRequireTemplate;

	[SerializeField]
	public bool ignoreNoneImportantLogs = true;

	public override Type baseNodeType => typeof(MissionNode);

	public override bool requiresAgent => false;

	public override bool requiresPrimeNode => true;

	public override bool isTree => true;

	public override bool allowBlackboardOverrides => false;

	public override bool canAcceptVariableDrops => false;

	public string DefaultTemplateName
	{
		get
		{
			if (defaultMissionRequireTemplate != null)
			{
				return defaultMissionRequireTemplate.Name;
			}
			return "通用任务需求";
		}
	}

	public bool IsMissionChain => base.primeNode is MissionNodeEntrance;

	public IEnumerable<string> AllExplicitMissionIds
	{
		get
		{
			foreach (MissionNode allMissionNode in AllMissionNodes)
			{
				if (allMissionNode is MissionNodeListener { IsImplicit: false } missionNodeListener)
				{
					yield return missionNodeListener.MissionId;
				}
			}
		}
	}

	public IEnumerable<string> AllMissionIds
	{
		get
		{
			foreach (MissionNode allMissionNode in AllMissionNodes)
			{
				if (allMissionNode is MissionNodeListener missionNodeListener)
				{
					yield return missionNodeListener.MissionId;
				}
			}
		}
	}

	public IEnumerable<MissionNodeListener> AllRootMissionNodes
	{
		get
		{
			if (base.primeNode == null)
			{
				yield break;
			}
			foreach (Node childNode in base.primeNode.GetChildNodes())
			{
				if (childNode is MissionNodeListener missionNodeListener)
				{
					yield return missionNodeListener;
				}
			}
		}
	}

	public IEnumerable<MissionNode> AllMissionNodes
	{
		get
		{
			if (base.primeNode == null)
			{
				yield break;
			}
			if (base.primeNode is MissionNode missionNode)
			{
				yield return missionNode;
			}
			foreach (Node item in base.primeNode.outConnections.Select((Connection x) => x.targetNode))
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

	public bool QueryMissionNode(string missionNodeId, out MissionNodeListenerBase node)
	{
		node = null;
		if (base.primeNode == null)
		{
			return false;
		}
		foreach (MissionNode allMissionNode in AllMissionNodes)
		{
			if (allMissionNode is MissionNodeListener missionNodeListener && !(missionNodeListener.MissionId != missionNodeId))
			{
				node = missionNodeListener;
				return true;
			}
		}
		return false;
	}

	public bool ContainsMission(string missionId)
	{
		foreach (Node item in base.allNodes.Where((Node x) => x is MissionNodeListener))
		{
			if (missionId == ((MissionNodeListener)item).MissionId)
			{
				return true;
			}
		}
		return false;
	}

	public bool ContainsMissionExcept(MissionNodeListener listener, string missionId)
	{
		foreach (Node item in base.allNodes.Where((Node x) => x is MissionNodeListener))
		{
			if (item != listener && missionId == ((MissionNodeListener)item).MissionId)
			{
				return true;
			}
		}
		return false;
	}

	private void RefreshNodeIds()
	{
		RefreshNodeIdsDepthFirst();
	}

	public bool IsPrimeNode(MissionNode node)
	{
		if (node == null || base.primeNode == null)
		{
			return false;
		}
		return base.primeNode == node;
	}

	public bool TryGetPrevMissionNode(MissionNodeListener node, out MissionNodeListener prevNode)
	{
		prevNode = null;
		if (node == null)
		{
			return false;
		}
		foreach (Node parentNode in node.GetParentNodes())
		{
			if (parentNode is MissionNodeListener missionNodeListener)
			{
				prevNode = missionNodeListener;
				return true;
			}
		}
		return false;
	}

	private Queue<MissionNodeListener> AllMissionListeners(out Queue<MissionNodeListener> implicitNodes)
	{
		Queue<MissionNodeListener> queue = new Queue<MissionNodeListener>();
		implicitNodes = new Queue<MissionNodeListener>();
		foreach (MissionNode allMissionNode in AllMissionNodes)
		{
			if (allMissionNode is MissionNodeListener missionNodeListener)
			{
				if (missionNodeListener.IsImplicit)
				{
					implicitNodes.Enqueue(missionNodeListener);
				}
				else
				{
					queue.Enqueue(missionNodeListener);
				}
			}
		}
		return queue;
	}

	private void RefreshNodeIdsDepthFirst()
	{
		if (base.primeNode == null)
		{
			return;
		}
		int num = 0;
		Queue<MissionNodeListener> implicitNodes;
		Queue<MissionNodeListener> queue = AllMissionListeners(out implicitNodes);
		while (queue.Count > 0)
		{
			MissionNodeListener missionNodeListener = queue.Dequeue();
			missionNodeListener.SetNodeId(num++);
			missionNodeListener.ClearExtendId();
		}
		int num2 = 0;
		while (implicitNodes.Count > 0)
		{
			MissionNodeListener missionNodeListener2 = implicitNodes.Dequeue();
			MissionNodeListener parent = GetParent(missionNodeListener2, (MissionNodeListener n) => !n.IsImplicit);
			if (parent == null)
			{
				if (GetParent(missionNodeListener2, (MissionNodeEntrance n) => true) != null)
				{
					missionNodeListener2.SetNodeId(-1);
					missionNodeListener2.SetExtendId(num2++);
				}
			}
			else
			{
				bool isImplicitNode;
				int num3 = parent.MarkExtendId(out isImplicitNode);
				missionNodeListener2.SetNodeId(parent.NodeId);
				missionNodeListener2.SetExtendId(isImplicitNode ? (num3 + 1) : num3);
			}
		}
	}

	private T GetParent<T>(Node node, Func<T, bool> condition) where T : MissionNode
	{
		Queue<Node> queue = new Queue<Node>();
		foreach (Node parentNode in node.GetParentNodes())
		{
			if (parentNode is T val && condition(val))
			{
				return val;
			}
			queue.Enqueue(parentNode);
		}
		while (queue.Count > 0)
		{
			foreach (Node parentNode2 in queue.Dequeue().GetParentNodes())
			{
				if (parentNode2 is T val2 && condition(val2))
				{
					return val2;
				}
				queue.Enqueue(parentNode2);
			}
		}
		return null;
	}

	public bool QueryMissionRewards(string missionId, out List<Reward> rewards, out bool shouldSendAsEmail)
	{
		rewards = null;
		shouldSendAsEmail = false;
		if (!QueryMissionNode(missionId, out var node))
		{
			return false;
		}
		if (node is MissionNodeListener missionNodeListener)
		{
			return missionNodeListener.QueryMissionRewards(out rewards, out shouldSendAsEmail);
		}
		return false;
	}

	public IEnumerable<MissionNodeListener> GetMissionNodesInAchievementMode(HashSet<string> loadedGraphs = null)
	{
		if (loadedGraphs == null)
		{
			loadedGraphs = new HashSet<string>();
		}
		foreach (Node missionNode in base.allNodes)
		{
			if (missionNode is MissionNodeListener missionNodeListener)
			{
				yield return missionNodeListener;
			}
			if (!(missionNode is MissionNodeSubGraph missionNodeSubGraph) || missionNodeSubGraph.subGraph == null || !loadedGraphs.Add(missionNodeSubGraph.subGraph.name))
			{
				continue;
			}
			foreach (MissionNodeListener item in missionNodeSubGraph.subGraph.GetMissionNodesInAchievementMode())
			{
				yield return item;
			}
		}
	}
}
