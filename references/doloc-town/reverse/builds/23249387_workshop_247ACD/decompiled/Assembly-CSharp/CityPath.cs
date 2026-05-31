using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown;
using DolocTown.GameData;
using RedSaw.CommandLineInterface;
using UnityEngine;

public class CityPath
{
	public readonly struct Translator
	{
		public static readonly Translator Void = new Translator(string.Empty, null, null, Vector2.zero, Vector2.zero, isRemoteInHouse: false);

		public readonly string id;

		public readonly string name;

		public readonly string sceneName;

		public readonly string remoteSceneName;

		public readonly Vector2 position;

		public readonly Vector2 remotePosition;

		public readonly bool isRemoteInHouse;

		public Translator(string name, string sceneName, string remoteSceneName, Vector2 position, Vector2 remotePosition, bool isRemoteInHouse)
		{
			this.name = name;
			this.position = position;
			this.sceneName = sceneName;
			this.remotePosition = remotePosition;
			this.remoteSceneName = remoteSceneName;
			this.isRemoteInHouse = isRemoteInHouse;
			id = $"{name}({sceneName}{position}->{remoteSceneName}{remotePosition})";
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj.GetType() != typeof(Translator))
			{
				return false;
			}
			Translator translator = (Translator)obj;
			return id == translator.id;
		}

		public override int GetHashCode()
		{
			return id.GetHashCode();
		}

		public static bool operator ==(Translator left, Translator right)
		{
			return left.id == right.id;
		}

		public static bool operator !=(Translator left, Translator right)
		{
			return left.id != right.id;
		}
	}

	public readonly struct InwalkableArea
	{
		private readonly float from;

		private readonly float to;

		public InwalkableArea(float from, float to)
		{
			this.from = from;
			this.to = to;
		}

		private bool Touch(float x)
		{
			if (x >= from)
			{
				return x <= to;
			}
			return false;
		}

		public bool Touch(float l, float r)
		{
			if (!Touch(l) && !Touch(r))
			{
				if (l < from)
				{
					return r > to;
				}
				return false;
			}
			return true;
		}
	}

	public readonly struct CityScene
	{
		private readonly Translator[] _translators;

		private readonly InwalkableArea[] _inwalkableAreas;

		public IEnumerable<Translator> Translators => _translators;

		public CityScene(Translator[] translators, InwalkableArea[] inwalkableAreas)
		{
			_translators = translators;
			_inwalkableAreas = inwalkableAreas;
		}

		private bool InWalkableAreaTouch(float x1, float x2)
		{
			float i = Mathf.Min(x1, x2);
			float r = Mathf.Max(x1, x2);
			return _inwalkableAreas.Any((InwalkableArea a) => a.Touch(i, r));
		}

		public bool IsWalkable(float from, float to)
		{
			return !InWalkableAreaTouch(from, to);
		}

		private Translator[] GetLocalTranslatorsSortByDistance(Vector2 pos)
		{
			return (from x in _translators
				orderby x.isRemoteInHouse ? 1 : 0, Mathf.Abs(x.position.x - pos.x)
				select x).ToArray();
		}

		public IEnumerable<Translator> GetLocalTranslators(Vector2 position)
		{
			if (_inwalkableAreas.Length == 0)
			{
				return GetLocalTranslatorsSortByDistance(position);
			}
			List<Translator> list = new List<Translator>();
			Translator[] localTranslatorsSortByDistance = GetLocalTranslatorsSortByDistance(position);
			for (int i = 0; i < localTranslatorsSortByDistance.Length; i++)
			{
				Translator item = localTranslatorsSortByDistance[i];
				if (!InWalkableAreaTouch(item.position.x, position.x))
				{
					list.Add(item);
				}
			}
			return list;
		}
	}

	private static CityPath instance;

	private readonly Dictionary<string, CityScene> allScenes;

	public CityPath(Dictionary<string, CityScene> scenes)
	{
		instance = this;
		allScenes = scenes;
	}

	public PathAction[] GeneratePathActions(string src, string dest, Vector2 currentPos, Vector2 targetPos)
	{
		Translator[] array = FindPathToSceneByTrans(src, dest, currentPos, targetPos);
		if (array != null)
		{
			return GenPathActions(array, dest, targetPos);
		}
		return null;
	}

	[Command("find_city_path", Desc = "查找从当前场景到目标场景的路径")]
	public static void TestPath(string targetSceneName, float pos = 0f)
	{
		if (!SceneUtils.QuerySceneBuildIndex(targetSceneName, out var buildIndex))
		{
			Debug.LogError("未知场景名:" + targetSceneName);
			return;
		}
		string sceneRawName = DolocAPI.archiveHandle.currentRoom.SceneRawName;
		Vector2 position = DolocAPI.AgentPosition;
		Translator[] array = instance.DebugFindPathToSceneByTrans(sceneRawName, targetSceneName, position, new Vector2(pos, 0f));
		if (array == null)
		{
			Debug.LogError("无法到达目标场景");
			return;
		}
		Debug.Log($"从{sceneRawName}到{buildIndex}的路径为:");
		Translator[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Debug.Log(array2[i].id);
		}
		Debug.Log("--- 分割线 ---");
		Debug.Log("生成路径..");
		PathAction.OutputPathInfos(instance.GenPathActions(array, targetSceneName, new Vector2(pos, 0f)));
	}

	[Command("show_city_translators")]
	public static void ShowTranslators(string sceneName)
	{
		if (!instance.allScenes.ContainsKey(sceneName))
		{
			Debug.LogError("无法找到对应场景:" + sceneName);
			return;
		}
		IEnumerable<Translator> translators = instance.allScenes[sceneName].Translators;
		Debug.Log("场景" + sceneName + "的所有传送门:");
		foreach (Translator item in translators)
		{
			Debug.Log(item.id);
		}
	}

	private PathAction[] GenPathActions(Translator[] translators, string targetScene, Vector2 targetPosition)
	{
		List<PathAction> list = new List<PathAction>();
		for (int i = 0; i < translators.Length; i++)
		{
			Translator translator = translators[i];
			list.Add(new PathActionMove(translator.position));
			list.Add(new PathActionTeleport(translator.remoteSceneName, translator.remotePosition));
			if (translator.remoteSceneName == targetScene)
			{
				break;
			}
		}
		list.Add(new PathActionMove(targetPosition));
		return list.ToArray();
	}

	private Translator[] FindPathToSceneByTrans(string sceneId, string targetSceneId, Vector2 position, Vector2 targetPosition)
	{
		if (!allScenes.ContainsKey(sceneId))
		{
			Debug.LogWarning("城镇寻路:无法找到对应场景:" + sceneId);
			return null;
		}
		if (!allScenes.ContainsKey(targetSceneId))
		{
			Debug.LogWarning("城镇寻路:无法找到对应场景:" + targetSceneId);
			return null;
		}
		if (sceneId == targetSceneId && allScenes[sceneId].IsWalkable(position.x, targetPosition.x))
		{
			return Array.Empty<Translator>();
		}
		HashSet<string> hashSet = new HashSet<string>();
		Queue<Translator> queue = new Queue<Translator>();
		Dictionary<Translator, Translator> dictionary = new Dictionary<Translator, Translator>();
		foreach (Translator localTranslator in allScenes[sceneId].GetLocalTranslators(position))
		{
			queue.Enqueue(localTranslator);
			dictionary.Add(localTranslator, Translator.Void);
		}
		bool flag = false;
		Translator translator = default(Translator);
		while (queue.Count > 0)
		{
			Translator translator2 = queue.Dequeue();
			if (translator2.sceneName == targetSceneId && allScenes[translator2.sceneName].IsWalkable(translator2.position.x, targetPosition.x))
			{
				translator = translator2;
				flag = true;
				break;
			}
			hashSet.Add(translator2.id);
			if (!allScenes.ContainsKey(translator2.remoteSceneName))
			{
				continue;
			}
			foreach (Translator localTranslator2 in allScenes[translator2.remoteSceneName].GetLocalTranslators(translator2.remotePosition))
			{
				if (!hashSet.Contains(localTranslator2.id))
				{
					queue.Enqueue(localTranslator2);
					dictionary[localTranslator2] = translator2;
				}
			}
		}
		if (!flag)
		{
			Debug.LogWarning("无法从\"" + sceneId + "\"到达\"" + targetSceneId + "\"");
			return null;
		}
		List<Translator> list = new List<Translator>();
		while (translator != Translator.Void)
		{
			list.Add(translator);
			translator = dictionary[translator];
		}
		list.Reverse();
		return list.ToArray();
	}

	private Translator[] DebugFindPathToSceneByTrans(string sceneName, string targetSceneName, Vector2 position, Vector2 targetPosition)
	{
		if (!allScenes.ContainsKey(sceneName) || !allScenes.ContainsKey(targetSceneName))
		{
			Debug.LogError("无法找到对应场景:" + sceneName + "或" + targetSceneName);
			return null;
		}
		if (sceneName == targetSceneName && allScenes[sceneName].IsWalkable(position.x, targetPosition.x))
		{
			return Array.Empty<Translator>();
		}
		HashSet<string> hashSet = new HashSet<string>();
		Queue<Translator> queue = new Queue<Translator>();
		Dictionary<Translator, Translator> dictionary = new Dictionary<Translator, Translator>();
		foreach (Translator localTranslator in allScenes[sceneName].GetLocalTranslators(position))
		{
			queue.Enqueue(localTranslator);
			dictionary.Add(localTranslator, Translator.Void);
		}
		bool flag = false;
		Translator translator = default(Translator);
		while (queue.Count > 0)
		{
			Translator translator2 = queue.Dequeue();
			Debug.Log("当前传送门:" + translator2.id);
			if (translator2.sceneName == targetSceneName && allScenes[translator2.sceneName].IsWalkable(translator2.position.x, targetPosition.x))
			{
				translator = translator2;
				flag = true;
				break;
			}
			hashSet.Add(translator2.id);
			if (!allScenes.ContainsKey(translator2.remoteSceneName))
			{
				Debug.LogWarning("当前场景:" + translator2.sceneName + "的传送场景:" + translator2.remoteSceneName + "不存在");
				continue;
			}
			foreach (Translator localTranslator2 in allScenes[translator2.remoteSceneName].GetLocalTranslators(translator2.remotePosition))
			{
				if (!hashSet.Contains(localTranslator2.id))
				{
					queue.Enqueue(localTranslator2);
					dictionary[localTranslator2] = translator2;
				}
			}
		}
		if (!flag)
		{
			Debug.LogError("无法从\"" + sceneName + "\"到达\"" + targetSceneName + "\"");
			return null;
		}
		List<Translator> list = new List<Translator>();
		while (translator != Translator.Void)
		{
			list.Add(translator);
			translator = dictionary[translator];
		}
		list.Reverse();
		return list.ToArray();
	}
}
