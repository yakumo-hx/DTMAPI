using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace DolocTown;

public class MonsterGroupManager
{
	private readonly List<MonsterGroup> allGroups = new List<MonsterGroup>();

	public void TryAddMonster(MonsterController controller)
	{
		using List<MonsterGroup>.Enumerator enumerator = allGroups.GetEnumerator();
		while (enumerator.MoveNext() && !enumerator.Current.TryAddMonster(controller))
		{
		}
	}

	private void _ClearGroups()
	{
		foreach (MonsterGroup allGroup in allGroups)
		{
			allGroup.Dispose();
		}
		allGroups.Clear();
	}

	public void Dispose()
	{
		_ClearGroups();
	}

	public void Reset([NotNull] IMonsterHost host)
	{
		_ClearGroups();
		FwbDroneGroup item = new FwbDroneGroup();
		allGroups.Add(item);
		foreach (MonsterGroup item2 in LoadLocalGroups(host))
		{
			allGroups.Add(item2);
		}
		foreach (MonsterGroup allGroup in allGroups)
		{
			Debug.Log("怪物组织管理器:加载组织-" + allGroup.GetType().Name);
		}
	}

	private IEnumerable<MonsterGroup> LoadLocalGroups(IMonsterHost host)
	{
		if (host.CurrentRoom.SceneHandle == null)
		{
			yield break;
		}
		GameObject gameObject = host.CurrentRoom.SceneHandle.GameObject;
		if (gameObject == null)
		{
			Debug.LogError("怪物组织管理器:当前房间没有根对象");
			yield break;
		}
		foreach (StrategicPointGroup item in LoadStrategicPointGroups(gameObject))
		{
			yield return item;
		}
	}

	private IEnumerable<StrategicPointGroup> LoadStrategicPointGroups(GameObject sceneRoot)
	{
		StrategicPoint[] componentsInChildren = sceneRoot.GetComponentsInChildren<StrategicPoint>();
		Dictionary<string, StrategicPointGroup> dictionary = new Dictionary<string, StrategicPointGroup>();
		StrategicPoint[] array = componentsInChildren;
		foreach (StrategicPoint strategicPoint in array)
		{
			string monsterId = strategicPoint.MonsterId;
			if (!monsterId.IsNullOrEmpty())
			{
				if (!dictionary.ContainsKey(monsterId))
				{
					dictionary.Add(monsterId, new StrategicPointGroup(monsterId));
				}
				dictionary[monsterId].AddStrageticPoint(strategicPoint);
			}
		}
		return dictionary.Values;
	}
}
