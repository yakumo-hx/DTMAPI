using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;
using DolocTown.NodeCanvas;
using Newtonsoft.Json;
using NodeCanvas.Framework;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class MissionChainHandle
{
	private readonly MissionGraph graph;

	private readonly List<string> currentListenNodes = new List<string>();

	private readonly string _chainId;

	[JsonProperty]
	public string chainId
	{
		get
		{
			if (graph != null)
			{
				return graph.name;
			}
			return _chainId;
		}
	}

	[JsonProperty]
	private string[] missionNodes => currentListenNodes.ToArray();

	public bool IsFinished => currentListenNodes.Count == 0;

	public bool IsInvalid => graph == null;

	public IEnumerable<string> CurrentMissions => currentListenNodes;

	[Command("view_chain_listening_nodes", Desc = "查看当前任务链正在监听的任务节点")]
	public static void ViewMissionChainListeningNodes(string chainName)
	{
		MissionChainHandle missionChainHandle = DolocAPI.archiveHandle.farmData.missionChainManager.QueryMissionChain(chainName);
		if (missionChainHandle == null)
		{
			Debug.LogError("任务链\"" + chainName + "\"不存在");
			return;
		}
		if (missionChainHandle.currentListenNodes.Count == 0)
		{
			Debug.Log("当前任务链没有正在监听的任务节点");
			return;
		}
		Debug.Log("任务链\"" + chainName + "\"正在监听的任务节点:");
		foreach (string currentListenNode in missionChainHandle.currentListenNodes)
		{
			Debug.Log("- " + currentListenNode);
		}
	}

	public static bool Create(MissionGraph graph, out MissionChainHandle handle)
	{
		if (graph.primeNode == null || graph.primeNode.outConnections.Count == 0)
		{
			handle = null;
			return false;
		}
		handle = new MissionChainHandle(graph);
		return true;
	}

	public MissionChainHandle(MissionGraph graph)
	{
		this.graph = graph;
	}

	[JsonConstructor]
	private MissionChainHandle(string chainId, string[] missionNodes)
	{
		if (!DolocAPI.assets.missionChains.QueryData(chainId, out var data))
		{
			_chainId = chainId;
			return;
		}
		graph = data;
		foreach (string text in missionNodes)
		{
			if (data.QueryMissionNode(text, out var _))
			{
				currentListenNodes.Add(text);
			}
		}
	}

	public void AfterLoadData()
	{
		ValidateListenNodes();
		ResortOldMissions();
		ValidateNewMissions();
	}

	private void ValidateListenNodes()
	{
		DolocAPI.archiveHandle.AppendMissionLog("检查任务链\"" + chainId + "\"中是否存在已完成或已被删除的任务");
		Queue<string> queue = new Queue<string>();
		foreach (string currentListenNode in currentListenNodes)
		{
			if (!graph.QueryMissionNode(currentListenNode, out var _))
			{
				DolocAPI.archiveHandle.AppendMissionLog("任务节点\"" + currentListenNode + "\"已被删除，移除该任务");
				queue.Enqueue(currentListenNode);
			}
			else if (DolocAPI.IsMissionComplete(currentListenNode))
			{
				DolocAPI.archiveHandle.AppendMissionLog("任务节点\"" + currentListenNode + "\"已完成，移除该任务");
				queue.Enqueue(currentListenNode);
			}
		}
		while (queue.Count > 0)
		{
			currentListenNodes.Remove(queue.Dequeue());
		}
	}

	private void ResortOldMissions()
	{
		DolocAPI.archiveHandle.AppendMissionLog("检查任务链中\"" + chainId + "\"是否存在乱序的任务");
		foreach (string currentListenNode in currentListenNodes)
		{
			if (graph.QueryMissionNode(currentListenNode, out var node) && graph.TryGetPrevMissionNode((MissionNodeListener)node, out var prevNode) && currentListenNodes.Contains(prevNode.MissionId))
			{
				MissionConnection obj = prevNode.outConnections.FirstOrDefault((Connection x) => x.targetNode == node) as MissionConnection;
				if (obj != null && obj.IsSequence)
				{
					Debug.LogError("任务\"" + currentListenNode + "\"的前置任务节点\"" + prevNode.MissionId + "\"正在执行，重新排序任务");
					DolocAPI.archiveHandle.AppendMissionLog("任务\"" + currentListenNode + "\"的前置任务节点\"" + prevNode.MissionId + "\"正在执行，移除该任务");
					DolocAPI.archiveHandle.RemoveMission(currentListenNode);
				}
			}
		}
	}

	private void ValidateNewMissions()
	{
		DolocAPI.archiveHandle.AppendMissionLog("检查任务链\"" + chainId + "\"中有无新增的任务");
		Queue<MissionNodeListener> queue = new Queue<MissionNodeListener>(graph.AllRootMissionNodes);
		List<MissionNodeListener> list = new List<MissionNodeListener>();
		List<MissionNodeSubGraph> list2 = new List<MissionNodeSubGraph>();
		while (queue.Count > 0)
		{
			MissionNodeListener missionNodeListener = queue.Dequeue();
			if (currentListenNodes.Contains(missionNodeListener.MissionId) || (missionNodeListener.TryGetParentMissionNode(out var parentNode) && DolocAPI.archiveHandle.farmData.missionManager.IsMissionListening(parentNode.MissionId)))
			{
				continue;
			}
			if (!DolocAPI.archiveHandle.farmData.missionManager.IsMissionComplete(missionNodeListener.MissionId))
			{
				list.Add(missionNodeListener);
				continue;
			}
			foreach (MissionNode childMissionNode in missionNodeListener.ChildMissionNodes)
			{
				if (childMissionNode is MissionNodeListener item)
				{
					queue.Enqueue(item);
				}
				else if (childMissionNode is MissionNodeSubGraph missionNodeSubGraph && missionNodeSubGraph.subGraph != null && !DolocAPI.archiveHandle.farmData.missionChainManager.IsChainCompleted(missionNodeSubGraph.subGraph.name))
				{
					list2.Add(missionNodeSubGraph);
				}
			}
		}
		foreach (MissionNodeListener item2 in list)
		{
			Debug.Log("检查到新增的任务节点:" + item2.MissionId);
			DolocAPI.archiveHandle.AppendMissionLog("检查到新增的任务节点\"" + item2.MissionId + "\"，自动启动该任务");
			ExecuteMissionNode(item2, "<any>");
		}
		foreach (MissionNodeSubGraph item3 in list2)
		{
			Debug.Log("检查到新增的任务链:" + item3.name);
			DolocAPI.archiveHandle.AppendMissionLog("检查到新增的任务链\"" + item3.name + "\"，自动启动该任务链");
			DolocAPI.StartMissionChain(item3.name);
		}
	}

	public bool TryRemoveListenNode(string missionId)
	{
		if (!currentListenNodes.Contains(missionId))
		{
			return false;
		}
		currentListenNodes.Remove(missionId);
		return true;
	}

	public bool StartChain()
	{
		DolocAPI.archiveHandle.AppendMissionLog("开始启动任务链\"" + chainId + "\"");
		if (graph.primeNode is MissionNodeEntrance missionNodeEntrance)
		{
			DolocAPI.archiveHandle.AppendMissionLog("任务链\"" + chainId + "\"的主节点为入口节点，执行其所有子节点");
			foreach (MissionNode childMissionNode in missionNodeEntrance.ChildMissionNodes)
			{
				Execute(childMissionNode, "<root>");
			}
			return currentListenNodes.Count > 0;
		}
		if (graph.primeNode is MissionNodeListenerBase missionNodeListenerBase)
		{
			DolocAPI.archiveHandle.AppendMissionLog("任务链\"" + chainId + "\"的主节点为任务节点，执行该节点");
			ExecuteMissionNode(missionNodeListenerBase, missionNodeListenerBase.MissionId);
			return currentListenNodes.Count > 0;
		}
		return false;
	}

	public bool RestartChain(string[] uncompletedMissions)
	{
		if (uncompletedMissions.IsNullOrEmpty())
		{
			return false;
		}
		int num = 0;
		foreach (string missionNodeId in uncompletedMissions)
		{
			if (graph.QueryMissionNode(missionNodeId, out var node) && node is MissionNodeListener missionNodeListener && graph.TryGetPrevMissionNode(missionNodeListener, out var prevNode))
			{
				MissionConnection connectionToNode = prevNode.GetConnectionToNode(missionNodeListener);
				if (connectionToNode != null && connectionToNode.IsSequence && connectionToNode.IsAvailable && DolocAPI.archiveHandle.farmData.missionManager.IsMissionComplete(prevNode.MissionId))
				{
					ExecuteMissionNode(missionNodeListener, "<any>");
					num++;
				}
			}
		}
		return num > 0;
	}

	public void StopChain()
	{
		if (currentListenNodes.Count == 0)
		{
			return;
		}
		foreach (string currentListenNode in currentListenNodes)
		{
			DolocAPI.archiveHandle.RemoveMission(currentListenNode);
		}
		currentListenNodes.Clear();
	}

	public bool OnMissionComplete(string missionId)
	{
		if (!currentListenNodes.Contains(missionId))
		{
			return false;
		}
		currentListenNodes.Remove(missionId);
		if (!graph.QueryMissionNode(missionId, out var node))
		{
			Debug.LogError("任务节点\"" + missionId + "\"丢失");
			return currentListenNodes.Count == 0;
		}
		if (node is MissionNodeListener { IsDecorator: false } missionNodeListener)
		{
			StopDecorators(missionNodeListener);
		}
		DolocAPI.archiveHandle.AppendMissionLog("因任务节点\"" + missionId + "\"完成而执行其后续节点");
		foreach (MissionNode childrenNode in node.GetChildrenNodes(IsSequenceNode))
		{
			Execute(childrenNode, missionId);
		}
		return currentListenNodes.Count == 0;
	}

	private void Execute(MissionNode node, string parent = null)
	{
		if (node == null)
		{
			return;
		}
		switch (node.nodeType)
		{
		case MissionNodeType.MISSION:
			ExecuteMissionNode(node as MissionNodeListenerBase, parent);
			break;
		case MissionNodeType.ACTION:
		{
			((MissionNodeAction)node).Execute();
			MissionNodeAction missionNodeAction = (MissionNodeAction)node;
			DolocAPI.archiveHandle.AppendMissionLog("执行功能节点:" + missionNodeAction.name);
			break;
		}
		case MissionNodeType.CONDITION:
		{
			MissionNode nodeByCondition = ((MissionNodeCondition)node).GetNodeByCondition();
			if (nodeByCondition != null)
			{
				Execute(nodeByCondition, parent);
			}
			break;
		}
		case MissionNodeType.DIALOGUE:
			((IDialogueNode)node).Execute();
			break;
		case MissionNodeType.SUB_GRAPH:
			ExecuteSubGraphNode(node, parent);
			break;
		case MissionNodeType.REWARD:
		case MissionNodeType.DECORATOR:
			break;
		}
	}

	public bool ReExecuteMissionNode(string missionId)
	{
		if (!graph.QueryMissionNode(missionId, out var node))
		{
			Debug.LogError("任务节点\"" + missionId + "\"不存在重新执行失败");
			return false;
		}
		Execute(node);
		return true;
	}

	public bool ReExecuteMissionNodeFromParent(string missionId)
	{
		if (!graph.QueryMissionNode(missionId, out var node))
		{
			return false;
		}
		if (!graph.TryGetPrevMissionNode((MissionNodeListener)node, out var prevNode))
		{
			return false;
		}
		foreach (MissionNode childrenNode in prevNode.GetChildrenNodes(IsSequenceNode))
		{
			Execute(childrenNode);
		}
		return true;
	}

	private void ExecuteMissionNode(MissionNodeListenerBase node, string parent)
	{
		DolocAPI.archiveHandle.AppendMissionLog("执行节点\"" + parent + "\"的后续任务节点");
		if (!(node is MissionNodeListener missionNodeListener))
		{
			DolocAPI.archiveHandle.AppendMissionLog($"任务节点<{node.ID}>执行失败:不是<MissionNodeListener>类型");
			return;
		}
		string parent2 = parent + "." + missionNodeListener.MissionId;
		if (missionNodeListener.IsDecorator)
		{
			DolocAPI.archiveHandle.AppendMissionLog("任务节点\"" + node.MissionId + "\"为装饰器节点，无需执行..");
			return;
		}
		if (!DolocAPI.archiveHandle.StartChainMission(graph.name, node.MissionId, node.MissionContent, node.IsImplicit, null, node.AttachModules))
		{
			DolocAPI.archiveHandle.AppendMissionLog("任务节点\"" + node.MissionId + "\"启动失败");
			return;
		}
		currentListenNodes.Add(node.MissionId);
		foreach (MissionNodeListener decorator in missionNodeListener.Decorators)
		{
			ExecuteDecoratorNode(decorator, parent2);
		}
		foreach (MissionNode childrenNode in missionNodeListener.GetChildrenNodes(IsParallelNode))
		{
			Execute(childrenNode, parent2);
		}
	}

	private void ExecuteSubGraphNode(MissionNode node, string parent)
	{
		DolocAPI.archiveHandle.AppendMissionLog("执行节点\"{parent}\"的后续子任务链节点");
		if (!(node is MissionNodeSubGraph missionNodeSubGraph))
		{
			DolocAPI.archiveHandle.AppendMissionLog($"子任务链节点<{node.ID}>执行失败:不是<MissionNodeSubGraph>类型");
		}
		else if (missionNodeSubGraph.subGraph == null)
		{
			DolocAPI.archiveHandle.AppendMissionLog($"子任务链节点<{missionNodeSubGraph.ID}>执行失败:子任务链为空");
		}
		else if (!DolocAPI.archiveHandle.StartMissionChain(missionNodeSubGraph.subGraph))
		{
			Debug.LogWarning("子任务链\"" + parent + "." + missionNodeSubGraph.subGraph.name + "\"启动失败");
			DolocAPI.archiveHandle.AppendMissionLog("子任务链\"" + parent + "." + missionNodeSubGraph.subGraph.name + "\"启动失败");
		}
		else
		{
			DolocAPI.archiveHandle.AppendMissionLog("子任务链\"" + parent + "." + missionNodeSubGraph.subGraph.name + "\"启动成功");
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

	private bool ExecuteDecoratorNode(MissionNodeListener missionNode, string parent)
	{
		DolocAPI.archiveHandle.AppendMissionLog("自动执行\"" + parent + "\"的后续装饰器节点");
		if (!missionNode.IsDecorator)
		{
			return false;
		}
		DolocAPI.archiveHandle.AppendMissionLog("启动装饰器任务\"" + parent + "." + missionNode.MissionId + "\"(" + missionNode.DecoratorId + ")");
		currentListenNodes.Add(missionNode.MissionId);
		return DolocAPI.archiveHandle.StartChainMission(graph.name, missionNode.MissionId, missionNode.MissionContent, isImplicit: true, missionNode.DecoratorId);
	}

	private void StopDecorators(MissionNodeListener missionNode)
	{
		foreach (MissionNodeListener decorator in missionNode.Decorators)
		{
			DolocAPI.archiveHandle.AppendMissionLog("自动停止任务\"" + missionNode.MissionId + "\"的装饰器任务\"" + decorator.MissionId + "\"");
			DolocAPI.archiveHandle.RemoveMission(decorator.MissionId);
			currentListenNodes.Remove(decorator.MissionId);
		}
	}

	public List<Reward> GetMissionRewards(string missionId, out bool shouldSendEmail)
	{
		if (graph.QueryMissionRewards(missionId, out var rewards, out shouldSendEmail))
		{
			return rewards.ToList();
		}
		return new List<Reward>();
	}
}
