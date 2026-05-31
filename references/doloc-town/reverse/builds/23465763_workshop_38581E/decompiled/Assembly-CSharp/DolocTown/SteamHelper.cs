using System.Collections.Generic;
using RedSaw.CommandLineInterface;
using Steamworks;
using UnityEngine;

namespace DolocTown;

public static class SteamHelper
{
	public static bool TryGetUsername(out string userId, out string failedReason)
	{
		if (TryGetUserID(out var userId2, out failedReason))
		{
			userId = userId2.ToString();
			return true;
		}
		userId = string.Empty;
		return false;
	}

	public static bool TryGetUserID(out CSteamID userId, out string failedReason)
	{
		failedReason = string.Empty;
		userId = CSteamID.Nil;
		if (!SteamManager.Initialized)
		{
			failedReason = "获取Steam用户ID失败：Steamworks未初始化";
			return false;
		}
		CSteamID steamID = SteamUser.GetSteamID();
		if (steamID == CSteamID.Nil)
		{
			failedReason = "获取Steam用户ID失败：Steam ID无效";
			return false;
		}
		userId = steamID;
		return true;
	}

	private static bool IsSteamworkAvailable()
	{
		if (!SteamManager.Initialized)
		{
			return false;
		}
		return SteamUserStats.RequestCurrentStats();
	}

	[Command("check_dlc", Desc = "检查DLC是否已经安装")]
	public static bool IsDLCUnlocked(uint dlcId)
	{
		if (!SteamManager.Initialized)
		{
			Debug.LogError("检查DLC解锁状态失败：Steamworks未初始化");
			return false;
		}
		return SteamApps.BIsDlcInstalled((AppId_t)dlcId);
	}

	public static bool ResetAchievement(string achievementId)
	{
		if (achievementId.IsNullOrEmpty() || !SteamManager.Initialized)
		{
			Debug.LogError("重置Steam成就失败：成就ID为空或者Steamworks未初始化");
			return false;
		}
		if (!SteamUserStats.ClearAchievement(achievementId))
		{
			Debug.LogError("重置Steam成就\"" + achievementId + "\"失败：成就不存在或者网络问题");
			return false;
		}
		if (!SteamUserStats.StoreStats())
		{
			Debug.LogError("重置Steam成就失败：云端状态更新失败");
			return false;
		}
		Debug.Log("\"" + achievementId + "\"已被重置");
		return true;
	}

	public static bool ResetAllAchievements(out string[] achievements)
	{
		achievements = null;
		if (!SteamManager.Initialized)
		{
			Debug.LogError("重置Steam成就：重置失败，SteamManager未初始化");
			return false;
		}
		uint numAchievements = SteamUserStats.GetNumAchievements();
		if (numAchievements == 0)
		{
			Debug.Log("没有成就可以重置");
			return true;
		}
		List<string> list = new List<string>();
		for (uint num = 0u; num < numAchievements; num++)
		{
			string achievementName = SteamUserStats.GetAchievementName(num);
			if (!string.IsNullOrEmpty(achievementName))
			{
				if (!SteamUserStats.ClearAchievement(achievementName))
				{
					Debug.LogError("重置成就\"" + achievementName + "\"失败：成就不存在或者网络问题");
				}
				else
				{
					list.Add(achievementName);
				}
			}
		}
		if (!SteamUserStats.StoreStats())
		{
			Debug.LogError("重置成就失败：云端状态更新失败");
			return false;
		}
		achievements = list.ToArray();
		return true;
	}

	public static bool UnlockAchievement(string achievementId, out bool isUnlocked)
	{
		isUnlocked = false;
		if (achievementId.IsNullOrEmpty())
		{
			return false;
		}
		Debug.Log("完成成就：" + achievementId);
		if (!SteamManager.Initialized)
		{
			Debug.LogError("上传成就\"" + achievementId + "\"失败：Steamworks 未初始化");
			return false;
		}
		if (!SteamUserStats.GetAchievement(achievementId, out isUnlocked))
		{
			Debug.LogError("检查成就\"" + achievementId + "\"云端状态失败..");
			return false;
		}
		if (isUnlocked)
		{
			return true;
		}
		return __UnlockSteamAchievement(achievementId, out isUnlocked);
	}

	private static bool __UnlockSteamAchievement(string achievementId, out bool isUnlocked)
	{
		isUnlocked = false;
		if (!SteamUserStats.SetAchievement(achievementId))
		{
			Debug.LogError("上传成就失败：成就 \"" + achievementId + "\" 不存在或者网络问题");
			return false;
		}
		if (!SteamUserStats.StoreStats())
		{
			Debug.LogError("上传成就失败：云端状态更新失败");
			return false;
		}
		Debug.Log("上传成就\"" + achievementId + "\"成功");
		isUnlocked = true;
		return true;
	}

	public static bool GetAllSteamAchievements(out AchievementInfo[] achievements, out string reason)
	{
		achievements = null;
		if (!SteamManager.Initialized)
		{
			reason = "Steam未初始化";
			return false;
		}
		if (!SteamUserStats.RequestCurrentStats())
		{
			reason = "获取成就系统失败，请检查用户是否登录";
			return false;
		}
		uint numAchievements = SteamUserStats.GetNumAchievements();
		achievements = new AchievementInfo[numAchievements];
		for (uint num = 0u; num < numAchievements; num++)
		{
			string achievementName = SteamUserStats.GetAchievementName(num);
			if (SteamUserStats.GetAchievement(achievementName, out var pbAchieved))
			{
				achievements[num] = new AchievementInfo(achievementName, pbAchieved);
			}
		}
		reason = string.Empty;
		return true;
	}

	public static IEnumerable<string> GetUnlockedAchievements()
	{
		if (!SteamManager.Initialized)
		{
			yield break;
		}
		uint numAchievements = SteamUserStats.GetNumAchievements();
		if (numAchievements == 0)
		{
			yield break;
		}
		for (uint i = 0u; i < numAchievements; i++)
		{
			string achievementName = SteamUserStats.GetAchievementName(i);
			if (!string.IsNullOrEmpty(achievementName) && SteamUserStats.GetAchievement(achievementName, out var pbAchieved) && pbAchieved)
			{
				yield return achievementName;
			}
		}
	}
}
