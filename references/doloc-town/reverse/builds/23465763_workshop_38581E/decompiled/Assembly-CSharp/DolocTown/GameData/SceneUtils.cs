using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DolocTown.GameData;

public static class SceneUtils
{
	private static Dictionary<string, int> sceneBuildIndexCache;

	private static Dictionary<int, string> sceneNameCache;

	public static bool IsSceneExist(int sceneIndex)
	{
		if (sceneIndex >= 0)
		{
			return sceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
		}
		return false;
	}

	public static bool IsSceneExist(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		_LoadCache();
		return sceneBuildIndexCache.ContainsKey(name);
	}

	private static void _LoadCache()
	{
		if (sceneNameCache == null || sceneBuildIndexCache == null)
		{
			sceneNameCache = new Dictionary<int, string>();
			sceneBuildIndexCache = new Dictionary<string, int>();
			for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
				sceneNameCache.Add(i, fileNameWithoutExtension);
				sceneBuildIndexCache.Add(fileNameWithoutExtension, i);
			}
		}
	}

	public static bool QuerySceneName(int buildIndex, out string sceneName)
	{
		_LoadCache();
		return sceneNameCache.TryGetValue(buildIndex, out sceneName);
	}

	public static string GetSceneName(int buildIndex)
	{
		_LoadCache();
		if (sceneNameCache.ContainsKey(buildIndex))
		{
			return sceneNameCache[buildIndex];
		}
		return null;
	}

	public static int GetSceneBuildIndex(string sceneName)
	{
		_LoadCache();
		if (sceneBuildIndexCache.ContainsKey(sceneName))
		{
			return sceneBuildIndexCache[sceneName];
		}
		return -1;
	}

	public static bool QuerySceneBuildIndex(string sceneName, out int buildIndex)
	{
		_LoadCache();
		buildIndex = sceneBuildIndexCache.GetValueOrDefault(sceneName, -1);
		return buildIndex > 0;
	}

	public static bool QuerySceneBuildIndexRealtime(string sceneName, out int buildIndex)
	{
		for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
		{
			buildIndex = i;
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
			if (sceneName == fileNameWithoutExtension)
			{
				return true;
			}
		}
		buildIndex = -1;
		return false;
	}

	public static string GetPrefix(this SceneType type)
	{
		return type switch
		{
			SceneType.SYS => "sys_", 
			SceneType.FARM => "farm_", 
			SceneType.CITY => "city_", 
			SceneType.DUNGEON => "dungeon_", 
			_ => "none_", 
		};
	}

	public static Vector2[] LoadInWalkableAreas(GameObject root)
	{
		return (from x in root.GetComponentsInChildren<InWalkableArea>(includeInactive: true)
			select x.Proto).ToArray();
	}

	public static RoomPresetObjectSO[] LoadPresets(GameObject parent)
	{
		ObjectPreset[] componentsInChildren = parent.GetComponentsInChildren<ObjectPreset>();
		List<RoomPresetObjectSO> list = new List<RoomPresetObjectSO>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].CreatePreset(out var preset))
			{
				list.Add(preset);
			}
			else
			{
				Debug.LogWarning("Failed to create preset for " + componentsInChildren[i].name);
			}
		}
		return list.ToArray();
	}
}
