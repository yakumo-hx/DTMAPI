using System;
using System.Collections.Generic;
using RedSaw;
using UnityEngine;

namespace DolocTown.UI;

public class OperationTipInSceneManager : DolocUiEntity
{
	private NashObjectPoolEx<OperationTipInScene> pool;

	private Dictionary<object, OperationTipInScene> loadedTips = new Dictionary<object, OperationTipInScene>();

	private bool isEnable = true;

	protected override void __Init()
	{
		base.__Init();
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_OPERATION_TIP_IN_SCENE_02);
		if (asset == null)
		{
			throw new DolocInitError("无法获取操作提示符预制体");
		}
		pool = new NashObjectPoolEx<OperationTipInScene>(asset, base.transform);
		SetVisible(value: true);
	}

	private bool TryGetOperationTip(object caller, out OperationTipInScene tip)
	{
		return loadedTips.TryGetValue(caller, out tip);
	}

	private OperationTipInScene FetchOperationTip(object caller)
	{
		if (loadedTips.TryGetValue(caller, out var value))
		{
			return value;
		}
		value = pool.Next;
		loadedTips.Add(caller, value);
		return value;
	}

	public void Clear()
	{
		pool.RecycleAll();
		isEnable = true;
		loadedTips.Clear();
	}

	public void SetEnabled(bool value)
	{
		if (isEnable == value)
		{
			return;
		}
		isEnable = value;
		foreach (OperationTipInScene value2 in loadedTips.Values)
		{
			value2.SetVisible(value);
		}
	}

	public void RaiseOperationTip(object caller, Vector2 ws, string prompt, Sprite sprite)
	{
		if (caller != null && isEnable)
		{
			OperationTipInScene operationTipInScene = FetchOperationTip(caller);
			operationTipInScene.Show(ws, prompt, sprite);
			operationTipInScene.transform.SetAsLastSibling();
		}
	}

	public void RaiseOperationTip(object caller, Vector2 ws, string prompt, string key)
	{
		if (caller != null && isEnable)
		{
			OperationTipInScene operationTipInScene = FetchOperationTip(caller);
			if (DolocAPI.GetActionKeyIconGroup(DolocAPI.UserInput.DeviceType, key, out var iconGroup))
			{
				Sprite smallIcon = iconGroup.smallIcon;
				operationTipInScene.Show(ws, prompt, smallIcon);
				operationTipInScene.transform.SetAsLastSibling();
			}
		}
	}

	public void RaiseOperationTip(object caller, Func<Vector2> ws, string prompt, string key)
	{
		if (caller != null && isEnable)
		{
			OperationTipInScene operationTipInScene = FetchOperationTip(caller);
			if (DolocAPI.GetActionKeyIconGroup(DolocAPI.UserInput.DeviceType, key, out var iconGroup))
			{
				operationTipInScene.Show(ws, prompt, iconGroup.smallIcon);
				operationTipInScene.transform.SetAsLastSibling();
			}
		}
	}

	public void ChangeOperationTipPrompt(object caller, string prompt)
	{
		if (caller != null && isEnable && TryGetOperationTip(caller, out var tip))
		{
			tip.Prompt = prompt;
		}
	}

	public void HideOperationTip(object caller)
	{
		if (caller != null && isEnable && loadedTips.ContainsKey(caller))
		{
			loadedTips[caller].Hide();
		}
	}

	public void PushOperationTip(object caller)
	{
		if (caller != null && isEnable && loadedTips.TryGetValue(caller, out var value))
		{
			value.Push();
		}
	}

	public void PushOperationTipFast(object caller)
	{
		if (caller != null && isEnable && loadedTips.TryGetValue(caller, out var value))
		{
			value.PushFast();
		}
	}

	public void PushOperationTipToHide(object caller)
	{
		if (caller != null && isEnable && loadedTips.TryGetValue(caller, out var tip))
		{
			tip.Push(delegate
			{
				tip.SetVisible(value: false);
			});
		}
	}

	public void PushOperationTipToDisappear(object caller)
	{
		if (caller != null && isEnable && loadedTips.TryGetValue(caller, out var tip))
		{
			tip.Push(delegate
			{
				pool.Recycle(tip);
				loadedTips.Remove(caller);
			});
		}
	}
}
