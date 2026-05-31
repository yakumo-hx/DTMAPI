using System.Collections.Generic;
using DolocTown;
using DolocTown.UI;
using UnityEngine;

public class ResidentOperationTipTriggerManager
{
	private static ISceneHandle currentSceneHandle;

	private static ResidentOperationTipTriggerManager _instance;

	private readonly Dictionary<string, ResidentOperationTipTrigger> _triggers;

	public static ResidentOperationTipTriggerManager Load(ISceneHandle sceneHandle)
	{
		if (currentSceneHandle == sceneHandle)
		{
			return _instance;
		}
		_instance = FromGameObject(sceneHandle.GameObject);
		currentSceneHandle = sceneHandle;
		return _instance;
	}

	public static ResidentOperationTipTriggerManager FromGameObject(GameObject container)
	{
		Dictionary<string, ResidentOperationTipTrigger> dictionary = new Dictionary<string, ResidentOperationTipTrigger>();
		GameObject[] rootGameObjects = container.scene.GetRootGameObjects();
		for (int i = 0; i < rootGameObjects.Length; i++)
		{
			ResidentOperationTipTrigger[] componentsInChildren = rootGameObjects[i].GetComponentsInChildren<ResidentOperationTipTrigger>(includeInactive: true);
			foreach (ResidentOperationTipTrigger residentOperationTipTrigger in componentsInChildren)
			{
				dictionary.Add(residentOperationTipTrigger.name, residentOperationTipTrigger);
			}
		}
		return new ResidentOperationTipTriggerManager(dictionary);
	}

	private ResidentOperationTipTriggerManager(Dictionary<string, ResidentOperationTipTrigger> triggers)
	{
		_triggers = triggers;
	}

	public void Invoke(string id, Vector2 position, string prompt = null)
	{
		if (_triggers.TryGetValue(id, out var value))
		{
			value.Invoke(position, prompt);
		}
	}

	public void Invoke(string id, string prompt = null)
	{
		if (_triggers.TryGetValue(id, out var value))
		{
			value.Invoke(prompt);
		}
	}

	public void Hide(string id)
	{
		if (_triggers.TryGetValue(id, out var value))
		{
			value.gameObject.SetActive(value: false);
			value.HideResidentOperationTip();
		}
	}

	public void Clear()
	{
		foreach (string key in _triggers.Keys)
		{
			Hide(key);
		}
	}
}
