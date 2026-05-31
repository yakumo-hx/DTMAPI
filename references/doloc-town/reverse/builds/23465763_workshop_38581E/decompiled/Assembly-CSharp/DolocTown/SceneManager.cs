using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RedSaw.CommandLineInterface;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DolocTown;

public class SceneManager
{
	private struct SceneHandleCache
	{
		public readonly Scene scene;

		public readonly ISceneHandle handle;

		public readonly Dictionary<string, ISceneHandle> subHandles;

		public SceneHandleCache(Scene scene)
		{
			this.scene = scene;
			handle = null;
			GameObject[] rootGameObjects = scene.GetRootGameObjects();
			foreach (GameObject gameObject in rootGameObjects)
			{
				handle = gameObject.GetComponentInChildren<ISceneHandle>(includeInactive: true);
				if (handle != null)
				{
					break;
				}
			}
			subHandles = new Dictionary<string, ISceneHandle>();
			rootGameObjects = scene.GetRootGameObjects();
			for (int i = 0; i < rootGameObjects.Length; i++)
			{
				ISceneHandle[] componentsInChildren = rootGameObjects[i].GetComponentsInChildren<ISceneHandle>(includeInactive: true);
				foreach (ISceneHandle sceneHandle in componentsInChildren)
				{
					if (subHandles.ContainsKey(sceneHandle.GetType().Name))
					{
						Debug.LogWarning("场景\"" + scene.name + "\"中存在同名RoomHandle\"" + sceneHandle.GetType().Name + "\"");
					}
					subHandles.Add(sceneHandle.GameObject.name, sceneHandle);
				}
			}
			if (DolocAPI.QuerySceneInfo(scene.name, out var proto))
			{
				handle?.LoadScene(proto);
			}
		}
	}

	private readonly SceneInfo[] scenes;

	private readonly Dictionary<string, int> rawNameMap;

	private readonly Dictionary<int, SceneHandleCache> loadedScenes;

	public SceneManager()
	{
		scenes = SceneManagement.LoadScenes();
		Debug.Log($"所有场景加载完毕,共{scenes.Length}个场景");
		rawNameMap = new Dictionary<string, int>();
		SceneInfo[] array = scenes;
		for (int i = 0; i < array.Length; i++)
		{
			SceneInfo sceneInfo = array[i];
			rawNameMap.Add(sceneInfo.name, sceneInfo.id);
		}
		loadedScenes = new Dictionary<int, SceneHandleCache>();
		foreach (KeyValuePair<int, Scene> loadedScene in SceneManagement.LoadedScenes)
		{
			loadedScenes.Add(loadedScene.Key, new SceneHandleCache(loadedScene.Value));
		}
	}

	public bool QuerySceneBuildIndex(string sceneName, out int index)
	{
		return rawNameMap.TryGetValue(sceneName, out index);
	}

	public bool QuerySceneName(int buildIdx, out string name)
	{
		if (buildIdx >= 0 && buildIdx < scenes.Length)
		{
			name = scenes[buildIdx].name;
			return true;
		}
		name = string.Empty;
		return false;
	}

	public void SetSceneEnabled(int buildIndex, bool value)
	{
		if (loadedScenes.ContainsKey(buildIndex))
		{
			GameObject[] rootGameObjects = loadedScenes[buildIndex].scene.GetRootGameObjects();
			for (int i = 0; i < rootGameObjects.Length; i++)
			{
				rootGameObjects[i].SetActive(value);
			}
		}
	}

	private bool IsSceneExists(int sceneIndex)
	{
		if (sceneIndex >= 0)
		{
			return sceneIndex < scenes.Length;
		}
		return false;
	}

	private bool _LoadSceneAsync(int buildIdx, Action callback = null)
	{
		if (!IsSceneExists(buildIdx))
		{
			return false;
		}
		if (loadedScenes.ContainsKey(buildIdx))
		{
			Debug.LogWarning($"场景{buildIdx}已经加载");
			return true;
		}
		DolocAPI.StartCoroutine(CoroutineLoadSceneAsync(buildIdx, callback));
		return true;
	}

	private IEnumerator CoroutineLoadSceneAsync(int buildIdx, Action callback = null)
	{
		UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
		AsyncOperation handle = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIdx, LoadSceneMode.Additive);
		while (!handle.isDone)
		{
			yield return null;
		}
		callback?.Invoke();
	}

	private IEnumerator CoroutineLoadSceneAsyncEx(int buildIdx, Action callback = null)
	{
		float startTime = Time.realtimeSinceStartup;
		UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedHandler;
		AsyncOperation handle = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIdx, LoadSceneMode.Additive);
		handle.allowSceneActivation = false;
		handle.priority = 100;
		while (handle.progress < 0.9f)
		{
			yield return null;
		}
		Debug.Log($"Scene load progress 90% took {Time.realtimeSinceStartup - startTime} seconds");
		handle.allowSceneActivation = true;
		while (!handle.isDone)
		{
			yield return null;
		}
		Debug.Log($"Total scene load took {Time.realtimeSinceStartup - startTime} seconds");
		UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedHandler;
		callback?.Invoke();
		void OnSceneLoadedHandler(Scene scene, LoadSceneMode mode)
		{
			OnSceneLoaded(scene, mode);
		}
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
		loadedScenes.Add(scene.buildIndex, new SceneHandleCache(scene));
	}

	private void _UnloadSceneAsync(int buildIdx, Action callback = null)
	{
		if (loadedScenes.ContainsKey(buildIdx))
		{
			DolocAPI.StartCoroutine(CoroutineUnloadSceneAsync(buildIdx, callback));
		}
	}

	private IEnumerator CoroutineUnloadSceneAsync(int buildIdx, Action callback = null)
	{
		loadedScenes.Remove(buildIdx);
		AsyncOperation handle = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(buildIdx);
		while (!handle.isDone)
		{
			yield return null;
		}
		callback?.Invoke();
	}

	public SceneType GetSceneType(string sceneName)
	{
		if (QueryScene(sceneName, out var sceneInfo))
		{
			return sceneInfo.type;
		}
		return SceneType.NONE;
	}

	public bool QueryScene(string rawName, out SceneInfo sceneInfo)
	{
		if (!rawNameMap.TryGetValue(rawName, out var value))
		{
			sceneInfo = default(SceneInfo);
			return false;
		}
		sceneInfo = scenes[value];
		return true;
	}

	[Command("show_all_scene_handles")]
	public static void DEBUG_ShowAllSceneHandles(string sceneRawName)
	{
		ISceneHandle[] allSceneHandles = DolocAPI.sceneManager.GetAllSceneHandles(sceneRawName);
		if (allSceneHandles == null)
		{
			Debug.LogError("场景" + sceneRawName + "未加载或不存在");
			return;
		}
		Debug.Log("场景" + sceneRawName + "的所有SceneHandle:");
		ISceneHandle[] array = allSceneHandles;
		for (int i = 0; i < array.Length; i++)
		{
			Debug.Log(array[i].GameObject.name);
		}
	}

	public ISceneHandle[] GetAllSceneHandles(string sceneRawName)
	{
		if (!rawNameMap.TryGetValue(sceneRawName, out var value))
		{
			return null;
		}
		if (!loadedScenes.TryGetValue(value, out var value2))
		{
			return null;
		}
		List<ISceneHandle> list = new List<ISceneHandle>();
		list.Add(value2.handle);
		list.AddRange(value2.subHandles.Values);
		return list.ToArray();
	}

	public ISceneHandle GetSceneHandle(string sceneRawName, string roomName = null)
	{
		if (!rawNameMap.TryGetValue(sceneRawName, out var value))
		{
			return null;
		}
		if (roomName.IsNullOrEmpty())
		{
			if (loadedScenes.TryGetValue(value, out var value2))
			{
				return value2.handle;
			}
			return null;
		}
		if (!loadedScenes.TryGetValue(value, out var value3))
		{
			return null;
		}
		return value3.subHandles.GetValueOrDefault(roomName);
	}

	public bool IsSceneExists(string sceneName)
	{
		return rawNameMap.ContainsKey(sceneName);
	}

	public bool IsSceneLoaded(string sceneName)
	{
		return loadedScenes.Any((KeyValuePair<int, SceneHandleCache> x) => x.Value.scene.name == sceneName);
	}

	public bool LoadSceneAsync(string name, Action callback = null)
	{
		if (!rawNameMap.TryGetValue(name, out var value))
		{
			Debug.LogError("场景" + name + "不存在");
			return false;
		}
		return _LoadSceneAsync(value, callback);
	}

	public void UnloadSceneAsync(string name, Action callback = null)
	{
		if (rawNameMap.TryGetValue(name, out var value))
		{
			_UnloadSceneAsync(value, callback);
		}
	}

	public void UnloadAll()
	{
		int num = rawNameMap["sys_global"];
		Queue<int> queue = new Queue<int>();
		foreach (int key in loadedScenes.Keys)
		{
			if (key != num)
			{
				queue.Enqueue(key);
			}
		}
		while (queue.Count > 0)
		{
			int buildIdx = queue.Dequeue();
			_UnloadSceneAsync(buildIdx, delegate
			{
				Debug.Log("场景" + scenes[buildIdx].shortName + "被卸载了");
			});
		}
	}
}
