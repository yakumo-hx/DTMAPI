using System;
using System.Collections.Generic;
using UnityEngine;

namespace DolocTown.UI;

public static class OperationTipInScenePatch
{
	private static Dictionary<object, ResidentOperationTip> _loadedTips = new Dictionary<object, ResidentOperationTip>();

	public static void ShowSceneOperationTip(this object obj, Vector2 position, string prompt, string actionName = null)
	{
		DolocAPI.uiSystem.sceneOperationTipManager.RaiseOperationTip(obj, position, prompt, actionName ?? DolocAPI.UserInput.GlobalInteractActionName);
	}

	public static void ShowSceneOperationTip(this object obj, Vector2 position, string prompt, Sprite sprite)
	{
		DolocAPI.uiSystem.sceneOperationTipManager.RaiseOperationTip(obj, position, prompt, sprite);
	}

	public static void ShowSceneOperationTip(this object obj, Func<Vector2> positionGetter, string prompt, string actionName = null)
	{
		DolocAPI.uiSystem.sceneOperationTipManager.RaiseOperationTip(obj, positionGetter, prompt, actionName ?? DolocAPI.UserInput.GlobalInteractActionName);
	}

	public static void ChangeSceneOperationTipPrompt(this object obj, string prompt)
	{
		DolocAPI.uiSystem.sceneOperationTipManager.ChangeOperationTipPrompt(obj, prompt);
	}

	public static void HideSceneOperationTip(this object caller)
	{
		DolocAPI.uiSystem.sceneOperationTipManager.HideOperationTip(caller);
	}

	public static void PushSceneOperationTip(this object caller)
	{
		DolocAPI.uiSystem.sceneOperationTipManager.PushOperationTip(caller);
	}

	public static void PushSceneOperationTipToHide(this object caller)
	{
		DolocAPI.uiSystem.sceneOperationTipManager.PushOperationTipToHide(caller);
	}

	public static void PushSceneOperationTipToDisappear(this object caller)
	{
		DolocAPI.uiSystem.sceneOperationTipManager.PushOperationTipToDisappear(caller);
	}

	private static ResidentOperationTip GetResidentOperationTip(object caller)
	{
		if (_loadedTips.TryGetValue(caller, out var value))
		{
			return value;
		}
		ResidentOperationTip fromPoolInScene = DolocAPI.uiSystem.GetFromPoolInScene<ResidentOperationTip>();
		_loadedTips.Add(caller, fromPoolInScene);
		return fromPoolInScene;
	}

	public static void ShowResidentOperationTip(this object caller, Vector2 positionWS, string prompt)
	{
		GetResidentOperationTip(caller).RaiseAsFollowWS(positionWS, prompt);
	}

	public static void RefreshResidentOperationTipPrompt(this object caller, string prompt)
	{
		ResidentOperationTip residentOperationTip = GetResidentOperationTip(caller);
		if (residentOperationTip != null)
		{
			residentOperationTip.text = prompt;
		}
	}

	public static void ShowResidentOperationTipAsUI(this object caller, Vector2 positionUI, string prompt)
	{
		GetResidentOperationTip(caller).RaiseAsUI(positionUI, prompt);
	}

	public static void HideResidentOperationTip(this object caller)
	{
		if (_loadedTips.TryGetValue(caller, out var value))
		{
			_loadedTips.Remove(caller);
			DolocAPI.uiSystem.RecycleToPoolInScene(value);
		}
	}
}
