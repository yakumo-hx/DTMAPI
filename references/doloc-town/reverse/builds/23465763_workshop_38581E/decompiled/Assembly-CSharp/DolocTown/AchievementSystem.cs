using System.Collections.Generic;
using System.Text;
using DolocTown.GameData;
using DolocTown.NodeCanvas;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using Steamworks;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public class AchievementSystem : IMissionManagerComponent
{
	private MissionGraph missionGraph;

	private Dictionary<string, MissionNodeListener> configuredAchievements;

	private List<IMission> lockedAchievements = new List<IMission>();

	[JsonProperty]
	private bool isInitialized;

	[DebugInfo("正在监听的成就")]
	private string LockedDatasPreview
	{
		get
		{
			if (!isInitialized)
			{
				return "成就系统未初始化..";
			}
			if (lockedAchievements.Count == 0)
			{
				return "没有正在监听的成就";
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (IMission lockedAchievement in lockedAchievements)
			{
				stringBuilder.AppendLine($"{lockedAchievement}:{lockedAchievement.MissionStatus}");
			}
			return stringBuilder.ToString();
		}
	}

	public IEnumerable<IMission> LockedAchievements => lockedAchievements;

	private static Dictionary<string, MissionNodeListener> LoadAchievementMissions(MissionGraph graph)
	{
		Dictionary<string, MissionNodeListener> dictionary = new Dictionary<string, MissionNodeListener>();
		foreach (MissionNodeListener item in graph.GetMissionNodesInAchievementMode())
		{
			if (item != null)
			{
				dictionary[item.MissionId] = item;
			}
		}
		return dictionary;
	}

	public AchievementSystem()
	{
	}

	[JsonConstructor]
	public AchievementSystem(bool isInitialized)
	{
		this.isInitialized = isInitialized;
	}

	public void AfterLoadData()
	{
		Initialize();
	}

	private void CheckCompleteAchievements()
	{
		foreach (IMission lockedAchievement in lockedAchievements)
		{
			_ = lockedAchievement.IsCompleteLoadArchive;
		}
	}

	public void Initialize(bool force = false)
	{
		if (!LoadMissionGraph())
		{
			Debug.LogError("成就系统: 加载任务链\"" + DolocAPI.GlobalParameter.AchievementGraphName + "\"失败");
			return;
		}
		if (!SteamHelper.GetAllSteamAchievements(out var achievements, out var reason))
		{
			Debug.LogError("成就系统初始化失败:\"" + reason + "\"");
			return;
		}
		Debug.Log($"Steam端共定义了{achievements.Length}个成就，开始部署相关成就任务..");
		uint num = 0u;
		uint num2 = 0u;
		uint num3 = 0u;
		uint num4 = 0u;
		AchievementInfo[] array = achievements;
		for (int i = 0; i < array.Length; i++)
		{
			AchievementInfo achievementInfo = array[i];
			if (!achievementInfo.id.IsNullOrEmpty())
			{
				MissionNodeListener value;
				IMission mission;
				if (achievementInfo.isUnlocked)
				{
					num++;
				}
				else if (!configuredAchievements.TryGetValue(achievementInfo.id, out value))
				{
					num2++;
					Debug.LogError("成就\"" + achievementInfo.id + "\"在任务链中未找到");
				}
				else if (DolocAPI.archiveHandle.QueryMission(achievementInfo.id, out mission))
				{
					Debug.LogWarning("成就\"" + achievementInfo.id + "\"在任务链中已经存在");
					num3++;
				}
				else
				{
					_StartMission(value);
					num4++;
					Debug.Log("成就任务\"" + achievementInfo.id + "\"部署成功");
				}
			}
		}
		Debug.Log($"<color=#ff4f4f>成就系统: 成就任务部署完成，共部署{num4}个任务，已解锁{num}个成就，" + $"未找到任务链定义的成就{num2}个，" + $"已经存在的成就任务{num3}个</color>");
		isInitialized = true;
	}

	public void ReinvokePendingAchievements(IEnumerable<AchievementInfo> pendingAchievements)
	{
		Debug.Log("<color=#ff4f4f>重新上传所有pending状态的成就</color>");
		if (pendingAchievements == null)
		{
			Debug.Log("没有需要重新上传的成就");
			return;
		}
		foreach (AchievementInfo pendingAchievement in pendingAchievements)
		{
			SteamHelper.UnlockAchievement(pendingAchievement.id, out var _);
		}
	}

	private bool LoadMissionGraph()
	{
		string achievementGraphName = DolocAPI.GlobalParameter.AchievementGraphName;
		if (!DolocAPI.assets.missionChains.QueryData(achievementGraphName, out var data))
		{
			Debug.LogError("成就系统: 找不到任务链\"" + achievementGraphName + "\"");
			return false;
		}
		missionGraph = data;
		configuredAchievements = LoadAchievementMissions(data);
		return true;
	}

	private void _StartMission(MissionNodeListener missionNode)
	{
		if (missionNode != null)
		{
			MissionContentHandle contentHandle = missionNode.MissionContent.CreateHandle();
			Mission mission = new Mission(missionGraph.name, missionNode.MissionId, contentHandle, isImplicit: true);
			DolocAPI.archiveHandle.farmData.missionManager.StartMission(mission);
			lockedAchievements.Add(mission);
		}
	}

	public bool ResetAchievement(string achievementId)
	{
		if (!DolocAPI.IsGameInitialized)
		{
			return false;
		}
		if (achievementId.IsNullOrEmpty())
		{
			return false;
		}
		if (!SteamHelper.ResetAchievement(achievementId))
		{
			Debug.LogError("重置Steam端成就失败..");
			return false;
		}
		DolocAPI.archiveHandle.RemoveMission(achievementId);
		IMission mission = lockedAchievements.Find((IMission x) => x.Id == achievementId);
		if (mission != null)
		{
			lockedAchievements.Remove(mission);
		}
		if (!configuredAchievements.TryGetValue(achievementId, out var value))
		{
			Debug.LogError("成就\"" + achievementId + "\"在任务链中未找到");
			return false;
		}
		_StartMission(value);
		Debug.Log("成就\"" + achievementId + "\"重置成功");
		return true;
	}

	public bool ResetAllAchievements()
	{
		if (!DolocAPI.IsGameInitialized)
		{
			return false;
		}
		if (!SteamHelper.ResetAllAchievements(out var achievements))
		{
			Debug.LogError("重置Steam端成就失败..");
			return false;
		}
		lockedAchievements.Clear();
		foreach (string key in configuredAchievements.Keys)
		{
			DolocAPI.archiveHandle.RemoveMission(key);
		}
		int num = 0;
		int num2 = 0;
		string[] array = achievements;
		foreach (string text in array)
		{
			if (!configuredAchievements.TryGetValue(text, out var value))
			{
				num2++;
				Debug.LogError("成就\"" + text + "\"在任务链中未找到");
			}
			else
			{
				num++;
				_StartMission(value);
				Debug.Log("成就任务\"" + text + "\"部署成功");
			}
		}
		Debug.Log($"<color=#ff4f4f>成就系统: 成就任务重置完成，共部署{num}个任务，" + $"未找到任务链定义的成就{num2}个</color>");
		return true;
	}

	void IMissionManagerComponent.OnMissionStart(IMission mission)
	{
	}

	void IMissionManagerComponent.OnMissionCompleted(IMission mission)
	{
		if (mission != null && mission.Id.StartsWith(DolocAPI.GlobalParameter.AchievementGraphName))
		{
			Debug.Log("<color=#ff4f4f>成就\"" + mission.Id + "\"已完成</color>");
			SetSteamAchievement(mission.Id);
		}
	}

	public void SetSteamAchievement(string id)
	{
		if (!SteamManager.Initialized)
		{
			Debug.LogError("成就系统: Steam未初始化，无法同步成就");
		}
		else if (!SteamUserStats.RequestCurrentStats())
		{
			Debug.LogError("成就系统: 获取当前用户数据失败，无法同步成就");
		}
		else if (!SteamUserStats.SetAchievement(id))
		{
			Debug.LogError("成就系统: 上传成就\"" + id + "\"失败");
		}
		else if (!SteamUserStats.StoreStats())
		{
			Debug.LogError("成就系统: 云端状态更新失败，无法同步成就\"" + id + "\"");
		}
		else
		{
			Debug.Log("成就系统: 成就\"" + id + "\"上传成功");
		}
	}

	void IMissionManagerComponent.OnMissionRemoved(IMission mission)
	{
	}

	void IMissionManagerComponent.OnMissionChanged(GameEventType type, GameEventArgs args, IMission mission)
	{
	}
}
