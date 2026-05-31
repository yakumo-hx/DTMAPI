using System;
using System.Collections;
using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Settings;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DolocTown.UI;

public class RebindActionSlot : DolocNavigationButton
{
	[SerializeField]
	private InputActionKeyIcon inputActionKeyIcon;

	private InputAction action;

	private InputAction[] linkedActions;

	private int bindingIndex;

	private RebindActionMask listeningMask;

	private Coroutine rebindInterruptCoroutine;

	private string oldBindingPath;

	private int pendingBindingIndex;

	private InputActionRebindingExtensions.RebindingOperation rebindOperation;

	private static List<RebindActionSlot> s_RebindActionUIs;

	public string EffectivePath
	{
		get
		{
			if (!CheckValid())
			{
				return string.Empty;
			}
			return action.bindings[bindingIndex].effectivePath;
		}
	}

	public string OriginPath
	{
		get
		{
			if (!CheckValid())
			{
				return string.Empty;
			}
			return action.bindings[bindingIndex].path;
		}
	}

	public string OverridePath
	{
		get
		{
			if (!CheckValid())
			{
				return string.Empty;
			}
			return action.bindings[bindingIndex].overridePath;
		}
	}

	public bool CanRevert => EffectivePath != OriginPath;

	private InputBinding.DisplayStringOptions displayStringOptions => InputBinding.DisplayStringOptions.DontOmitDevice | InputBinding.DisplayStringOptions.DontIncludeInteractions;

	public UnityEvent<RebindActionSlot> updateBindingUIEvent { get; private set; } = new UnityEvent<RebindActionSlot>();


	public UnityEvent<RebindActionSlot> startRebindEvent { get; private set; } = new UnityEvent<RebindActionSlot>();


	public UnityEvent<RebindActionSlot> stopRebindEvent { get; private set; } = new UnityEvent<RebindActionSlot>();


	public bool isRebindPending { get; private set; }

	private DolocInputSource actionAsset => DolocAPI.UserInput.inputSource;

	private InputSchemaType currentInputSchema => rebindActionUI.currentInputSchema;

	private DolocInputDeviceType currentDeviceType => rebindActionUI.currentDeviceType;

	public bool IsLocked
	{
		get
		{
			return inputActionKeyIcon.IsLocked;
		}
		set
		{
			inputActionKeyIcon.IsLocked = value;
		}
	}

	public bool IsConflict
	{
		get
		{
			return inputActionKeyIcon.IsConflict;
		}
		set
		{
			inputActionKeyIcon.IsConflict = value;
		}
	}

	public RebindActionUI rebindActionUI { get; private set; }

	public string boxTitle => rebindActionUI?.config?.Title ?? "";

	public RebindActionInfo rebindActionInfo => rebindActionUI?.rebindActionInfo;

	protected override void __Init()
	{
		base.__Init();
		inputActionKeyIcon.Init();
	}

	public bool CheckValid()
	{
		if (action != null && bindingIndex >= 0)
		{
			return bindingIndex < action.bindings.Count;
		}
		return false;
	}

	protected void OnEnable()
	{
		if (s_RebindActionUIs == null)
		{
			s_RebindActionUIs = new List<RebindActionSlot>();
		}
		s_RebindActionUIs.Add(this);
		if (s_RebindActionUIs.Count == 1)
		{
			InputSystem.onActionChange += OnActionChange;
		}
	}

	protected void OnDisable()
	{
		rebindOperation?.Dispose();
		rebindOperation = null;
		oldBindingPath = string.Empty;
		pendingBindingIndex = -1;
		s_RebindActionUIs.Remove(this);
		if (s_RebindActionUIs.Count == 0)
		{
			s_RebindActionUIs = null;
			InputSystem.onActionChange -= OnActionChange;
		}
	}

	private void UpdateBindingDisplay()
	{
		inputActionKeyIcon.Render(action, bindingIndex, currentDeviceType);
		updateBindingUIEvent?.Invoke(this);
	}

	private static void OnActionChange(object obj, InputActionChange change)
	{
		if (change != InputActionChange.BoundControlsChanged)
		{
			return;
		}
		InputAction inputAction = obj as InputAction;
		InputActionMap inputActionMap = inputAction?.actionMap ?? (obj as InputActionMap);
		InputActionAsset inputActionAsset = inputActionMap?.asset ?? (obj as InputActionAsset);
		for (int i = 0; i < s_RebindActionUIs.Count; i++)
		{
			RebindActionSlot rebindActionSlot = s_RebindActionUIs[i];
			InputAction inputAction2 = rebindActionSlot.action;
			if (inputAction2 != null && (inputAction2 == inputAction || inputAction2.actionMap == inputActionMap || inputAction2.actionMap?.asset == inputActionAsset))
			{
				rebindActionSlot.UpdateBindingDisplay();
			}
		}
	}

	public void ResetToDefault(bool updateDisplay = true)
	{
		if (!CheckValid())
		{
			return;
		}
		if (action.bindings[bindingIndex].isComposite)
		{
			for (int i = bindingIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; i++)
			{
				action.RemoveBindingOverride(i);
			}
		}
		else
		{
			action.RemoveBindingOverride(bindingIndex);
		}
		RevertLinkedActions();
	}

	public void RemoveBind()
	{
		if (OriginPath == "")
		{
			action.RemoveBindingOverride(bindingIndex);
			return;
		}
		action.ApplyBindingOverride(bindingIndex, "");
		RemoveLinkedActions();
	}

	public void StartInteractiveRebind()
	{
		if (CheckValid() && !isRebindPending)
		{
			isRebindPending = true;
			if (action.bindings[bindingIndex].isComposite)
			{
				Debug.LogError(action.name + " 是组合按键！");
			}
			else
			{
				PerformInteractiveRebind(action, bindingIndex);
			}
		}
	}

	private void PerformInteractiveRebind(InputAction action, int bindingIndex)
	{
		rebindOperation?.Cancel();
		BeforeListen();
		oldBindingPath = action.bindings[bindingIndex].effectivePath;
		pendingBindingIndex = bindingIndex;
		rebindOperation = action.PerformInteractiveRebinding(bindingIndex).WithCancelingThrough("").OnCancel(delegate
		{
			CancelListen();
		})
			.OnComplete(delegate
			{
				AfterListen();
			});
		if (!(rebindActionInfo?.ActionPath.ComposePartName).IsNullOrEmpty())
		{
			rebindOperation = rebindOperation.WithExpectedControlType<ButtonControl>();
		}
		HashSet<string> pathsByDevice = DolocConfig.Tables.TbGameKeyIcon.GetPathsByDevice(currentDeviceType);
		if (pathsByDevice.IsNullOrEmpty())
		{
			rebindOperation = rebindOperation.WithControlsHavingToMatchPath("");
		}
		else
		{
			foreach (string item in pathsByDevice)
			{
				rebindOperation = rebindOperation.WithControlsHavingToMatchPath(item);
			}
		}
		startRebindEvent?.Invoke(this);
		rebindOperation.Start();
	}

	private void BeforeListen()
	{
		listeningMask.Show(ConfirmRebind, CancelRebind, boxTitle);
		DolocAPI.UserInput.DisableAllInput(includeGlobal: true);
		rebindInterruptCoroutine = StartCoroutine(WaitToInterrupt(DolocAPI.GlobalParameter.RebindActionInterruptDuration));
	}

	private void AfterListen()
	{
		StopInterruptCoroutine();
		listeningMask.RenderKey(rebindOperation.action, bindingIndex, currentDeviceType);
		DolocAPI.UserInput.ResumeCurrentInput();
	}

	private void CancelListen()
	{
		StopInterruptCoroutine();
		DolocAPI.UserInput.ResumeCurrentInput();
		CancelRebind();
		listeningMask.Hide();
		DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.SettingPanelNoValidInput);
		CleanUp();
	}

	private IEnumerator WaitToInterrupt(float seconds)
	{
		float startTime = Time.time;
		while (Time.time - startTime < seconds)
		{
			yield return null;
		}
		rebindOperation?.Cancel();
	}

	private void StopInterruptCoroutine()
	{
		if (rebindInterruptCoroutine != null)
		{
			StopCoroutine(rebindInterruptCoroutine);
		}
		rebindInterruptCoroutine = null;
	}

	private void CleanUp()
	{
		HandleLinkedActions();
		rebindOperation?.Dispose();
		rebindOperation = null;
		oldBindingPath = string.Empty;
		pendingBindingIndex = -1;
		stopRebindEvent?.Invoke(this);
	}

	private void HandleLinkedActions()
	{
		InputAction[] array = linkedActions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ApplyBindingOverride(bindingIndex, action.bindings[bindingIndex].overridePath ?? "");
		}
	}

	private void RevertLinkedActions()
	{
		InputAction[] array = linkedActions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].RemoveBindingOverride(bindingIndex);
		}
	}

	private void RemoveLinkedActions()
	{
		InputAction[] array = linkedActions;
		foreach (InputAction inputAction in array)
		{
			if (bindingIndex >= 0 && bindingIndex < inputAction.bindings.Count)
			{
				if (inputAction.bindings[bindingIndex].path == "")
				{
					inputAction.RemoveBindingOverride(bindingIndex);
					break;
				}
				inputAction.ApplyBindingOverride(bindingIndex, "");
			}
		}
	}

	private void ConfirmRebind()
	{
		if (isRebindPending)
		{
			isRebindPending = false;
			if (OverridePath == OriginPath)
			{
				ResetToDefault(updateDisplay: false);
			}
			Debug.Log("改键已应用");
			CleanUp();
		}
	}

	private void CancelRebind()
	{
		if (!isRebindPending)
		{
			return;
		}
		startRebindEvent?.Invoke(this);
		isRebindPending = false;
		if (pendingBindingIndex != -1)
		{
			if (!oldBindingPath.IsNullOrEmpty())
			{
				action.ApplyBindingOverride(pendingBindingIndex, oldBindingPath ?? "");
			}
			else
			{
				action.RemoveBindingOverride(pendingBindingIndex);
			}
		}
		Debug.Log("改键已撤销");
		CleanUp();
	}

	public void BindAction(InputAction action, InputAction[] linkedActions, int bindingIndex, RebindActionUI rebindActionUI, RebindActionMask mask)
	{
		this.action = action;
		this.linkedActions = linkedActions ?? Array.Empty<InputAction>();
		this.bindingIndex = bindingIndex;
		this.rebindActionUI = rebindActionUI;
		listeningMask = mask;
		UpdateBindingDisplay();
	}

	public void SetActive(bool value)
	{
		buttonCanvasGroup.alpha = (value ? 1f : 0f);
		buttonCanvasGroup.interactable = value;
		buttonCanvasGroup.blocksRaycasts = value;
	}
}
