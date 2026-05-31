using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.GameData;
using DolocTown.NodeCanvas;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class MissionChainManager
{
	[JsonProperty]
	private readonly Dictionary<string, MissionChainHandle> handles = new Dictionary<string, MissionChainHandle>();

	[JsonProperty]
	private readonly Dictionary<string, int> completedChains = new Dictionary<string, int>();

	[JsonProperty]
	private Dictionary<string, MissionChainInfo> chainInfos = new Dictionary<string, MissionChainInfo>();

	private bool isChainInfosSaved;

	private bool __protected_mode;

	private readonly Queue<MissionChainHandle> __buffer = new Queue<MissionChainHandle>();

	public string[] CompletedMissionChains => completedChains.Keys.ToArray();

	public string[] CompletedOrEndedMissionChains
	{
		get
		{
			List<string> list = new List<string>();
			list.AddRange(CompletedMissionChains);
			return list.ToArray();
		}
	}

	public MissionChainManager()
	{
		foreach (MissionGraph totalValue in DolocAPI.assets.missionChains.totalValues)
		{
			chainInfos.Add(totalValue.name, new MissionChainInfo(totalValue.AllMissionIds));
		}
	}

	[JsonConstructor]
	private MissionChainManager(Dictionary<string, MissionChainHandle> handles, Dictionary<string, int> completedChains = null, Dictionary<string, MissionChainInfo> chainInfos = null)
	{
		this.handles = handles;
		this.completedChains = completedChains ?? new Dictionary<string, int>();
		this.chainInfos = chainInfos ?? SaveMissionChainInfos();
	}

	private Dictionary<string, MissionChainInfo> SaveMissionChainInfos()
	{
		Dictionary<string, MissionChainInfo> dictionary = new Dictionary<string, MissionChainInfo>();
		foreach (MissionGraph totalValue in DolocAPI.assets.missionChains.totalValues)
		{
			dictionary.Add(totalValue.name, new MissionChainInfo(totalValue.AllMissionIds));
		}
		isChainInfosSaved = true;
		return dictionary;
	}

	private bool _IsNewMission(string chainName, string missionId)
	{
		if (isChainInfosSaved)
		{
			return DolocAPI.archiveHandle.farmData.missionManager.IsMissionComplete(missionId);
		}
		if (!chainInfos.TryGetValue(chainName, out var value))
		{
			return true;
		}
		return !value.ContainsMission(missionId);
	}

	public void AfterLoadData()
	{
		ClearInvalidMissionChains();
		DolocAPI.archiveHandle.AppendMissionLog("----- 任务链管理器读档开始 -----");
		foreach (MissionChainHandle value in handles.Values)
		{
			value.AfterLoadData();
		}
		DolocAPI.archiveHandle.AppendMissionLog("----- 任务链管理器读档结束 -----");
		chainInfos = SaveMissionChainInfos();
	}

	private void TryRestartCompletedMissionChains()
	{
		foreach (string key in completedChains.Keys)
		{
			if (DolocAPI.assets.missionChains.QueryData(key, out var data) && !handles.ContainsKey(key) && data.TryGetNewMissions(_IsNewMission, out var newMissions))
			{
				MissionChainHandle missionChainHandle = new MissionChainHandle(data);
				if (missionChainHandle.RestartChain(newMissions))
				{
					Debug.Log("<color=#00ff00>任务链\"" + data.name + "\"重启成功!</color>");
					handles.Add(data.name, missionChainHandle);
				}
				else
				{
					Debug.Log("<color=#ff0000>任务链\"" + data.name + "\"重启失败..</color>");
				}
			}
		}
	}

	public bool IsChainCompleted(string graphName)
	{
		return completedChains.ContainsKey(graphName);
	}

	public bool IsChainHandling(string chainId)
	{
		return handles.ContainsKey(chainId);
	}

	public bool TryGetMissionChainHandle(string chainId, out MissionChainHandle handle)
	{
		return handles.TryGetValue(chainId, out handle);
	}

	public void __ForceSetChainComplete(string chainId)
	{
		if (handles.TryGetValue(chainId, out var _))
		{
			completedChains.TryAdd(chainId, 0);
			completedChains[chainId]++;
		}
	}

	public MissionChainHandle _StartMissionChainVirtual(MissionGraph graph)
	{
		if (graph == null)
		{
			return null;
		}
		if (handles.TryGetValue(graph.name, out var value))
		{
			return value;
		}
		if (!MissionChainHandle.Create(graph, out var handle))
		{
			return null;
		}
		if (__protected_mode)
		{
			__buffer.Enqueue(handle);
		}
		else
		{
			handles.Add(graph.name, handle);
		}
		return handle;
	}

	public bool _StartMissionChain(MissionGraph graph)
	{
		DolocAPI.archiveHandle.AppendMissionLog("尝试启动任务链");
		if (graph == null)
		{
			DolocAPI.archiveHandle.AppendMissionLog("任务链为空，启动失败");
			return false;
		}
		if (handles.ContainsKey(graph.name))
		{
			DolocAPI.archiveHandle.AppendMissionLog("任务链\"" + graph.name + "\"已经存在，启动失败");
			return false;
		}
		if (!MissionChainHandle.Create(graph, out var handle))
		{
			DolocAPI.archiveHandle.AppendMissionLog("任务链\"" + graph.name + "\"操作柄创建失败");
			return false;
		}
		if (!handle.StartChain())
		{
			DolocAPI.archiveHandle.AppendMissionLog("任务链\"" + graph.name + "\"启动失败");
			return false;
		}
		if (__protected_mode)
		{
			__buffer.Enqueue(handle);
		}
		else
		{
			handles.Add(graph.name, handle);
		}
		DolocAPI.archiveHandle.AppendMissionLog("任务链\"" + graph.name + "\"启动成功");
		return true;
	}

	public void StopMissionChain(string missionChainId)
	{
		DolocAPI.archiveHandle.AppendMissionLog("尝试停止任务链\"" + missionChainId + "\"");
		if (handles.Remove(missionChainId, out var value))
		{
			DolocAPI.archiveHandle.AppendMissionLog("任务链\"" + missionChainId + "\"停止成功");
			value.StopChain();
		}
	}

	public void __OnMissionComplete(IMission mission)
	{
		DolocAPI.archiveHandle.AppendMissionLog("任务链管理器检测到任务完成事件\"" + mission.Id + "\"");
		__protected_mode = true;
		Queue<string> queue = new Queue<string>();
		foreach (MissionChainHandle value in handles.Values)
		{
			if (value.OnMissionComplete(mission.Id))
			{
				if (!completedChains.TryAdd(value.chainId, 1))
				{
					completedChains[value.chainId]++;
					int num = completedChains[value.chainId];
					DolocAPI.archiveHandle.AppendMissionLog($"任务链\"{value.chainId}\"完成，总完成次数{num}");
				}
				else
				{
					DolocAPI.archiveHandle.AppendMissionLog("任务链\"" + value.chainId + "\"完成，总完成次数1");
				}
				queue.Enqueue(value.chainId);
				DolocAPI.outputSuccess("任务完成:" + mission.Title);
				if (!mission.IsImplicit)
				{
					DolocAPI.ShowMessageBoxNodeComplete(string.Format(DolocConfig.StaticTexts.MissionComplete, mission.Title));
				}
			}
		}
		while (queue.Count > 0)
		{
			string key = queue.Dequeue();
			handles.Remove(key);
		}
		while (__buffer.Count > 0)
		{
			MissionChainHandle missionChainHandle = __buffer.Dequeue();
			handles.Add(missionChainHandle.chainId, missionChainHandle);
		}
		__protected_mode = false;
	}

	private void ClearInvalidMissionChains()
	{
		Queue<string> queue = new Queue<string>();
		foreach (MissionChainHandle item in handles.Values.Where((MissionChainHandle handle) => handle.IsInvalid))
		{
			queue.Enqueue(item.chainId);
		}
		while (queue.Count > 0)
		{
			string text = queue.Dequeue();
			handles.Remove(text);
			DolocAPI.archiveHandle.AppendMissionLog("MissionChainManager.ClearInvalidMissionChains: 移除失效的任务链\"" + text + "\"");
		}
		foreach (string key in completedChains.Keys)
		{
			if (!DolocAPI.assets.missionChains.QueryData(key, out var _))
			{
				queue.Enqueue(key);
			}
		}
		while (queue.Count > 0)
		{
			string text2 = queue.Dequeue();
			completedChains.Remove(text2);
			DolocAPI.archiveHandle.AppendMissionLog("MissionChainManager.ClearInvalidMissionChains: 移除失效的任务链记录\"" + text2 + "\"");
		}
	}

	public int GetMissionChainCompletedCount(string chainId)
	{
		return completedChains.GetValueOrDefault(chainId, 0);
	}

	public MissionChainHandle QueryMissionChain(string chainId)
	{
		return handles.GetValueOrDefault(chainId);
	}

	public void __ForceRemoveMissionChain(string chainId)
	{
		DolocAPI.archiveHandle.AppendMissionLog("强制移除任务链\"" + chainId + "\"");
		if (handles.Remove(chainId, out var value))
		{
			value.StopChain();
		}
		completedChains.Remove(chainId);
		chainInfos.Remove(chainId);
	}
}
