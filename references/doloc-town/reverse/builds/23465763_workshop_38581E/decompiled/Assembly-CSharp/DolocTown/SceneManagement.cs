using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DolocTown;

public static class SceneManagement
{
	private static Dictionary<string, SceneInfoSO> sceneBuildIndexCache;

	public static Dictionary<int, Scene> LoadedScenes
	{
		get
		{
			Dictionary<int, Scene> dictionary = new Dictionary<int, Scene>();
			for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
			{
				Scene sceneAt = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
				dictionary.Add(sceneAt.buildIndex, sceneAt);
			}
			return dictionary;
		}
	}

	public static bool QuerySceneInfoSO(string sceneRawName, out SceneInfoSO sceneInfo)
	{
		if (sceneBuildIndexCache == null)
		{
			SceneInfo[] array = LoadScenes();
			sceneBuildIndexCache = new Dictionary<string, SceneInfoSO>();
			SceneInfo[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				SceneInfo sceneInfo2 = array2[i];
				SceneInfoSO value = new SceneInfoSO(sceneInfo2.id, sceneInfo2.name, sceneInfo2.shortName, sceneInfo2.type, sceneInfo2.path);
				if (sceneInfo2.name != null)
				{
					sceneBuildIndexCache.Add(sceneInfo2.name, value);
				}
			}
		}
		return sceneBuildIndexCache.TryGetValue(sceneRawName, out sceneInfo);
	}

	public static SceneInfo[] LoadScenes()
	{
		string[] array = LoadBuildScenePathes();
		SceneInfo[] array2 = new SceneInfo[array.Length];
		for (int i = 0; i < array2.Length; i++)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(array[i]);
			if (!fileNameWithoutExtension.Contains('_'))
			{
				Debug.LogWarning("场景名:\"" + fileNameWithoutExtension + "\"不合法，已忽略");
				continue;
			}
			string[] array3 = fileNameWithoutExtension.Split('_', 2);
			if (array3[0].Length == 0 || array3[1].Length == 0)
			{
				Debug.LogWarning("场景名:\"" + fileNameWithoutExtension + "\"不合法，已忽略");
				continue;
			}
			if (!Enum.TryParse<SceneType>(array3[0], ignoreCase: true, out var result))
			{
				Debug.LogWarning("场景名:\"" + fileNameWithoutExtension + "\"不合法，已忽略");
				continue;
			}
			string shortName = array3[1];
			array2[i] = new SceneInfo(i, fileNameWithoutExtension, shortName, result, array[i]);
		}
		return array2;
	}

	private static string[] LoadBuildScenePathes()
	{
		string[] array = new string[UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = SceneUtility.GetScenePathByBuildIndex(i);
		}
		return array;
	}
}
