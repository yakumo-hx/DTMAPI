using UnityEngine;

namespace DolocTown;

public static class ISceneHandleExtension
{
	public static void InvokeResidentTip(this ISceneHandle handle, string id, string customPrompt = null)
	{
		ResidentOperationTipTriggerManager.Load(handle).Invoke(id, customPrompt);
	}

	public static void InvokeResidentTip(this ISceneHandle handle, string id, Vector2 position, string customPrompt = null)
	{
		ResidentOperationTipTriggerManager.Load(handle).Invoke(id, position, customPrompt);
	}

	public static void HideResidentTip(this ISceneHandle handle, string id)
	{
		ResidentOperationTipTriggerManager.Load(handle).Hide(id);
	}

	public static void ClearResidentTip(this ISceneHandle handle)
	{
		ResidentOperationTipTriggerManager.Load(handle).Clear();
	}

	public static void RefreshLocalizationText(this ISceneHandle handle)
	{
		ResidentOperationTipTrigger[] componentsInScene = handle.GetComponentsInScene<ResidentOperationTipTrigger>();
		for (int i = 0; i < componentsInScene.Length; i++)
		{
			componentsInScene[i].RefreshPrompt();
		}
	}
}
