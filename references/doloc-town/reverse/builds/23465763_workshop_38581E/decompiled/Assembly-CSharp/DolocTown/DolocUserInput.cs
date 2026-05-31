using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XInput;

namespace DolocTown;

public class DolocUserInput
{
	private DolocInputSource.GlobalActions globalInputs;

	private DolocInputSource.BaseInputActions baseInputs;

	private DolocInputSource.NormalInputActions normalInputs;

	private DolocInputSource.BuilderInputActions builderInputs;

	private DolocInputType currentInputType;

	private float deviceSwitchDelay = 0.5f;

	private float lastSwitchTime;

	private DolocInputDeviceType currentDeviceType;

	private Dictionary<DolocInputType, float> deviceWeights = new Dictionary<DolocInputType, float>();

	public bool GlobalClick => globalInputs.Click.triggered;

	public bool GlobalRightClick => globalInputs.RightClick.triggered;

	public Vector2 MousePosition
	{
		get
		{
			return globalInputs.CursorPosition.ReadValue<Vector2>();
		}
		set
		{
			Mouse.current.WarpCursorPosition(value);
			InputState.Change(Mouse.current.position, value);
		}
	}

	public bool GlobalRightClickPressed => globalInputs.RightClick.triggered;

	public bool GlobalRightClickInProgress => globalInputs.RightClick.inProgress;

	public Func<Vector2> MousePositionGetter => () => globalInputs.CursorPosition.ReadValue<Vector2>();

	public bool GlobalToggleMissionPanel
	{
		get
		{
			if (!baseInputs.ToggleMissionPanel.triggered)
			{
				return normalInputs.ToggleMissionPanel.triggered;
			}
			return true;
		}
	}

	public bool GlobalToggleMenu
	{
		get
		{
			if (!baseInputs.ToggleMenu.triggered)
			{
				return normalInputs.ToggleMenu.triggered;
			}
			return true;
		}
	}

	public bool GlobalToggleBackpack
	{
		get
		{
			if (!baseInputs.ToggleBackpack.triggered)
			{
				return normalInputs.ToggleBackpack.triggered;
			}
			return true;
		}
	}

	public bool GlobalIsCancelPressed
	{
		get
		{
			if (!baseInputs.Cancel.triggered)
			{
				return normalInputs.Cancel.triggered;
			}
			return true;
		}
	}

	public bool GlobalDisposeItemInBackpack
	{
		get
		{
			if (!normalInputs.ToggleBackpackHold.triggered)
			{
				return normalInputs.DisposeItemInBackpack.triggered;
			}
			return true;
		}
	}

	public bool GlobalToggleMap
	{
		get
		{
			if (!normalInputs.ToggleMap.triggered)
			{
				return baseInputs.ToggleMap.triggered;
			}
			return true;
		}
	}

	public bool GlobalToggleTechTree
	{
		get
		{
			if (!normalInputs.ToggleTechTree.triggered)
			{
				return baseInputs.ToggleTechTree.triggered;
			}
			return true;
		}
	}

	public bool GlobalToggleCollectionBook
	{
		get
		{
			if (!normalInputs.ToggleCollectionBook.triggered)
			{
				return baseInputs.ToggleCollectionBook.triggered;
			}
			return true;
		}
	}

	public bool GlobalSpeedUpPressed
	{
		get
		{
			if (!normalInputs.SpeedUp.triggered)
			{
				return baseInputs.SpeedUp.triggered;
			}
			return true;
		}
	}

	public bool GlobalSpeedUpInProgress
	{
		get
		{
			if (!normalInputs.SpeedUp.inProgress)
			{
				return baseInputs.SpeedUp.inProgress;
			}
			return true;
		}
	}

	public bool GlobalQuickSelectPrev
	{
		get
		{
			if (!normalInputs.QuickSelectPrev.triggered)
			{
				return baseInputs.QuickSelectPrev.triggered;
			}
			return true;
		}
	}

	public bool GlobalQuickSelectPrevInProgress
	{
		get
		{
			if (!normalInputs.QuickSelectPrev.inProgress)
			{
				return baseInputs.QuickSelectPrev.inProgress;
			}
			return true;
		}
	}

	public bool GlobalQuickSelectNext
	{
		get
		{
			if (!normalInputs.QuickSelectNext.triggered)
			{
				return baseInputs.QuickSelectNext.triggered;
			}
			return true;
		}
	}

	public bool GlobalQuickSelectNextInProgress
	{
		get
		{
			if (!normalInputs.QuickSelectNext.inProgress)
			{
				return baseInputs.QuickSelectNext.inProgress;
			}
			return true;
		}
	}

	public bool BaseNotFixedOperation
	{
		get
		{
			if (!BaseIsMove && !BaseIsMoveInProgress && !BaseIsUpPressed && !BaseIsUpInProgress && !BaseIsDownPressed && !BaseIsDownInProgress && !BaseIsLeftPressed && !BaseIsLeftInProgress && !BaseIsRightPressed && !BaseIsRightInProgress && !BaseIsConfirmPressed && !BaseIsConfirmInProgress && !BaseIsCancelPressed && !BaseIsCancelInProgress && !BaseIsLastPressed && !BaseIsLastInProgress && !BaseIsNextPressed)
			{
				return !BaseIsNextInProgress;
			}
			return false;
		}
	}

	public bool BaseNotFixedOperationExcludeHorizontalNavigation
	{
		get
		{
			if (!BaseIsUpPressed && !BaseIsUpInProgress && !BaseIsDownPressed && !BaseIsDownInProgress && !BaseIsConfirmPressed && !BaseIsConfirmInProgress && !BaseIsCancelPressed && !BaseIsCancelInProgress && !BaseIsLastPressed && !BaseIsLastInProgress && !BaseIsNextPressed)
			{
				return !BaseIsNextInProgress;
			}
			return false;
		}
	}

	public Vector2 LeftJoyStickValue => normalInputs.Move.ReadValue<Vector2>();

	public float NormalMoveFactor
	{
		get
		{
			float x = normalInputs.Move.ReadValue<Vector2>().x;
			if (x == 0f)
			{
				return 0f;
			}
			return Mathf.Sign(x);
		}
	}

	public bool NormalInteract => normalInputs.Interact.triggered;

	public bool NormalInteractInProgress => normalInputs.Interact.inProgress;

	public float NormalMoveFactorY => normalInputs.Move.ReadValue<Vector2>().y;

	public bool NormalIsMovePressed => normalInputs.Move.triggered;

	public Vector2 AssistMove => normalInputs.AssistMove.ReadValue<Vector2>();

	public bool NormalJump => normalInputs.Jump.triggered;

	public bool NormalJumpInProgress => normalInputs.Jump.inProgress;

	public bool NormalUseTool => normalInputs.UseTool.triggered;

	public bool NormalUseToolInProgress => normalInputs.UseTool.inProgress;

	public bool NormalUseItem => normalInputs.UseItem.triggered;

	public bool NormalUseItemInProgress => normalInputs.UseItem.inProgress;

	public bool NormalJumpDown => normalInputs.JumpDown.triggered;

	public bool NormalJumpDownInProgress => normalInputs.JumpDown.inProgress;

	public bool NormalJumpDownReleased => normalInputs.JumpDown.WasReleasedThisFrame();

	public bool NormalDash => normalInputs.Dash.triggered;

	public bool NormalScrollInventoryUp => normalInputs.ScrollInventoryUp.triggered;

	public bool NormalScrollInventoryDown => normalInputs.ScrollInventoryDown.triggered;

	public bool NormalFire
	{
		get
		{
			if (!normalInputs.SelectedDrone.triggered)
			{
				return normalInputs.UseTool.triggered;
			}
			return true;
		}
	}

	public bool NormalFireInProgress
	{
		get
		{
			if (!normalInputs.SelectedDrone.inProgress)
			{
				return normalInputs.UseTool.inProgress;
			}
			return true;
		}
	}

	public bool NormalFishing => normalInputs.Fishing.triggered;

	public bool NormalFishingInProgress => normalInputs.Fishing.inProgress;

	public bool NormalSwitchAutoFire => normalInputs.SwitchAutoFire.triggered;

	public bool NormalDebug => normalInputs.Debug.triggered;

	public bool NormalRoomInteract => normalInputs.RoomInteract.triggered;

	public bool NormalSelectedDrone => normalInputs.SelectedDrone.triggered;

	public bool NormalSelectedActive => normalInputs.SelectedActive.triggered;

	public bool NormalQuickSelect0 => normalInputs.QuickSelect_0.triggered;

	public bool NormalQuickSelect1 => normalInputs.QuickSelect_1.triggered;

	public bool NormalQuickSelect2 => normalInputs.QuickSelect_2.triggered;

	public bool NormalQuickSelect3 => normalInputs.QuickSelect_3.triggered;

	public bool NormalQuickSelect4 => normalInputs.QuickSelect_4.triggered;

	public bool NormalQuickSelect5 => normalInputs.QuickSelect_5.triggered;

	public bool NormalQuickSelect6 => normalInputs.QuickSelect_6.triggered;

	public bool NormalQuickSelect7 => normalInputs.QuickSelect_7.triggered;

	public bool NormalQuickSelect8 => normalInputs.QuickSelect_8.triggered;

	public bool NormalQuickSelect9 => normalInputs.QuickSelect_9.triggered;

	public bool BaseInteract => baseInputs.Interact.triggered;

	public bool BaseIsMove => baseInputs.Move.triggered;

	public bool BaseIsMoveInProgress => baseInputs.Move.inProgress;

	public Vector2 BaseMove => baseInputs.Move.ReadValue<Vector2>();

	public bool BaseIsRightPressed => baseInputs.MoveRight.triggered;

	public bool BaseIsRightInProgress => baseInputs.MoveRight.inProgress;

	public bool BaseIsLeftPressed => baseInputs.MoveLeft.triggered;

	public bool BaseIsLeftInProgress => baseInputs.MoveLeft.inProgress;

	public bool BaseIsDownPressed => baseInputs.MoveDown.triggered;

	public bool BaseIsDownInProgress => baseInputs.MoveDown.inProgress;

	public bool BaseIsUpPressed => baseInputs.MoveUp.triggered;

	public bool BaseIsUpInProgress => baseInputs.MoveUp.inProgress;

	public bool BaseIsNextPressed => baseInputs.MoveNext.triggered;

	public bool BaseIsNextInProgress => baseInputs.MoveNext.inProgress;

	public bool BaseIsLastPressed => baseInputs.MoveLast.triggered;

	public bool BaseIsLastInProgress => baseInputs.MoveLast.inProgress;

	public bool BaseIsCancelPressed => baseInputs.Cancel.triggered;

	public bool BaseIsCancelInProgress => baseInputs.Cancel.inProgress;

	public bool BaseIsConfirmPressed => baseInputs.Confirm.triggered;

	public bool BaseIsConfirmHold => baseInputs.ConfirmHold.triggered;

	public bool BaseIsConfirmInProgress => baseInputs.Confirm.inProgress;

	public bool BaseIsSplitPressed => baseInputs.SplitItem.triggered;

	public bool BaseIsSplitInProgress => baseInputs.SplitItem.inProgress;

	public bool BasePageUpPressed => baseInputs.PageUp.triggered;

	public bool BasePageDownPressed => baseInputs.PageDown.triggered;

	public Vector2 BaseScrollDir => baseInputs.Scroll.ReadValue<Vector2>();

	public bool BaseIsScrollInProgress => baseInputs.Scroll.inProgress;

	public bool BaseSortItem => baseInputs.SortItem.triggered;

	public bool BaseLockItem
	{
		get
		{
			if (!baseInputs.SortItemHold.triggered)
			{
				return baseInputs.LockItem.triggered;
			}
			return true;
		}
	}

	public bool BaseDisposeItem => baseInputs.DisposeItem.triggered;

	public bool BaseDestroyItem
	{
		get
		{
			if (!baseInputs.DisposeItemHold.triggered)
			{
				return baseInputs.DestroyItem.triggered;
			}
			return true;
		}
	}

	public bool BasePutMaxItem => baseInputs.PutMaxItem.triggered;

	public bool BasePutAllItem
	{
		get
		{
			if (!baseInputs.PutMaxItemHold.triggered)
			{
				return baseInputs.PutAllItem.triggered;
			}
			return true;
		}
	}

	public bool BaseSubmitItem => baseInputs.SubmitItem.triggered;

	public bool BaseAssistSplitInProgress => baseInputs.AssistSplit.inProgress;

	public bool BaseAddOne => baseInputs.AddOne.triggered;

	public bool BaseAddOneInProgress => baseInputs.AddOne.inProgress;

	public bool BaseSubOne => baseInputs.SubOne.triggered;

	public bool BaseSubOneInProgress => baseInputs.SubOne.inProgress;

	public bool BaseAddTen => baseInputs.AddTen.triggered;

	public bool BaseAddTenInProgress => baseInputs.AddTen.inProgress;

	public bool BaseSubTen => baseInputs.SubTen.triggered;

	public bool BaseSubTenInProgress => baseInputs.SubTen.inProgress;

	public bool BaseSetMin => baseInputs.SetMin.triggered;

	public bool BaseSetMax => baseInputs.SetMax.triggered;

	public bool BaseContinueDialoguePressed => baseInputs.ContinueDialogue.triggered;

	public bool BaseContinueDialogueInProgress => baseInputs.ContinueDialogue.inProgress;

	public bool BaseToggleDialogueHistory => baseInputs.ToggleDialogueHistory.triggered;

	public bool BaseMiscellaneousPressed => baseInputs.MiscellaneousFunction.triggered;

	public bool BuilderSelected => builderInputs.BuilderSelected.triggered;

	public bool BuilderSelectedInProgress => builderInputs.BuilderSelected.inProgress;

	public bool BuilderRevocationPressed => builderInputs.BuilderRevocation.WasPressedThisFrame();

	public bool BuilderRevocationTriggered => builderInputs.BuilderRevocation.triggered;

	public bool BuilderDismantlePressed => builderInputs.BuilderDismantle.WasPressedThisFrame();

	public bool BuilderDismantleReleased => builderInputs.BuilderDismantle.WasReleasedThisFrame();

	public bool BuilderDismantleHold => builderInputs.BuilderDismantleHold.triggered;

	public bool BuilderDismantleInProgress
	{
		get
		{
			if (!builderInputs.BuilderDismantle.inProgress)
			{
				return builderInputs.BuilderDismantleHold.inProgress;
			}
			return true;
		}
	}

	public bool BuilderSwitchPressed => builderInputs.BuilderSwitch.triggered;

	public bool BuilderSwitchInventoryPressed => builderInputs.BuilderSwitchInventory.triggered;

	public bool BuilderRotatePressed => builderInputs.BuilderRotate.triggered;

	public bool BuilderLastPressed => builderInputs.BuilderLast.triggered;

	public bool BuilderNextPressed => builderInputs.BuilderNext.triggered;

	public bool BuilderDPadUpPressed => builderInputs.DPadUp.triggered;

	public bool BuilderDPadUpInProgress => builderInputs.DPadUp.inProgress;

	public bool BuilderDPadDownPressed => builderInputs.DPadDown.triggered;

	public bool BuilderDPadDownInProgress => builderInputs.DPadDown.inProgress;

	public bool BuilderDPadLeftPressed => builderInputs.DPadLeft.triggered;

	public bool BuilderDPadLeftInProgress => builderInputs.DPadLeft.inProgress;

	public bool BuilderDPadRightPressed => builderInputs.DPadRight.triggered;

	public bool BuilderDPadRightInProgress => builderInputs.DPadRight.inProgress;

	public string GlobalInteractActionName => normalInputs.Interact.name;

	public string NormalRoomInteractQuitActionName => normalInputs.JumpDown.name;

	public string NormalRoomInteractActionName => normalInputs.RoomInteract.name;

	public string NormalSelectedDroneActionName => normalInputs.SelectedDrone.name;

	public string NormalSelectedActiveActionName => normalInputs.SelectedActive.name;

	public string NormalQuickSelect0ActionName => normalInputs.QuickSelect_0.name;

	public string NormalQuickSelect1ActionName => normalInputs.QuickSelect_1.name;

	public string NormalQuickSelect2ActionName => normalInputs.QuickSelect_2.name;

	public string NormalQuickSelect3ActionName => normalInputs.QuickSelect_3.name;

	public string NormalQuickSelect4ActionName => normalInputs.QuickSelect_4.name;

	public string NormalQuickSelect5ActionName => normalInputs.QuickSelect_5.name;

	public string NormalQuickSelect6ActionName => normalInputs.QuickSelect_6.name;

	public string NormalQuickSelect7ActionName => normalInputs.QuickSelect_7.name;

	public string NormalQuickSelect8ActionName => normalInputs.QuickSelect_8.name;

	public string NormalQuickSelect9ActionName => normalInputs.QuickSelect_9.name;

	public string BuilderRevocationDisplayName => builderInputs.BuilderRevocation.activeControl?.displayName;

	public string BuilderDismantleDisplayName => builderInputs.BuilderDismantle.activeControl?.displayName;

	public DolocInputSource inputSource { get; private set; }

	public DolocInputDeviceType DeviceType => currentDeviceType;

	public InputSchemaType InputSchemaType => currentDeviceType.GetInputSchema();

	public DolocInputType InputType
	{
		get
		{
			return currentInputType;
		}
		set
		{
			if (currentInputType != value)
			{
				DisableAllInput();
				currentInputType = value;
				ResumeCurrentInput();
			}
		}
	}

	public event Action<DolocInputDeviceType> OnInputDeviceChanged;

	public DolocUserInput()
	{
		inputSource = new DolocInputSource();
		globalInputs = inputSource.Global;
		globalInputs.Enable();
		baseInputs = inputSource.BaseInput;
		normalInputs = inputSource.NormalInput;
		builderInputs = inputSource.BuilderInput;
		InputSystem.onActionChange += DetectDevice;
		InputSystem.onDeviceChange += DetectNewDevice;
	}

	public void DisableAllInput(bool includeGlobal = false)
	{
		if (includeGlobal)
		{
			globalInputs.Disable();
		}
		baseInputs.Disable();
		normalInputs.Disable();
		builderInputs.Disable();
	}

	public void ResumeCurrentInput()
	{
		globalInputs.Enable();
		switch (currentInputType)
		{
		case DolocInputType.BASE:
			baseInputs.Enable();
			break;
		case DolocInputType.NORMAL:
			normalInputs.Enable();
			break;
		case DolocInputType.BUILDER:
			builderInputs.Enable();
			break;
		case DolocInputType.All:
			baseInputs.Enable();
			normalInputs.Enable();
			builderInputs.Enable();
			break;
		}
	}

	public void BindDeviceChangedCallback(Action<DolocInputDeviceType> callback)
	{
		OnInputDeviceChanged -= callback;
		OnInputDeviceChanged += callback;
	}

	public void RemoveDeviceChangedCallback(Action<DolocInputDeviceType> callback)
	{
		OnInputDeviceChanged -= callback;
	}

	public void BindActionChangedCallback(Action<object, InputActionChange> callback)
	{
		InputSystem.onActionChange -= callback;
		InputSystem.onActionChange += callback;
	}

	public void RemoveActionChangedCallback(Action<object, InputActionChange> callback)
	{
		InputSystem.onActionChange -= callback;
	}

	private DolocInputDeviceType GetInputDeviceType(InputDevice device)
	{
		if (!(device is Mouse) && !(device is Keyboard))
		{
			if (!(device is SwitchProControllerHID))
			{
				if (!(device is XInputController))
				{
					if (!(device is DualShockGamepad))
					{
						if (device is Gamepad)
						{
							return DolocInputDeviceType.GamePad;
						}
						if (DolocAPI.gameManager.IsSteamEnabled && SteamInput.GetConnectedControllers(new InputHandle_t[16]) > 0)
						{
							return DolocInputDeviceType.GamePad;
						}
						return DolocInputDeviceType.Other;
					}
					return DolocInputDeviceType.PlayStationController;
				}
				return DolocInputDeviceType.XboxController;
			}
			return DolocInputDeviceType.SwitchProController;
		}
		return DolocInputDeviceType.KeyboardMouse;
	}

	private bool ShouldChangeInputDevice(InputDevice inputDevice)
	{
		DolocInputDeviceType inputDeviceType = GetInputDeviceType(inputDevice);
		if (inputDevice == null || inputDeviceType == DolocInputDeviceType.Other || inputDeviceType == currentDeviceType)
		{
			return false;
		}
		if (Time.time < lastSwitchTime + deviceSwitchDelay)
		{
			return false;
		}
		currentDeviceType = inputDeviceType;
		lastSwitchTime = Time.time;
		return true;
	}

	private void DetectDevice(object sender, InputActionChange change)
	{
		if (change == InputActionChange.ActionPerformed)
		{
			InputDevice device = ((InputAction)sender).activeControl.device;
			if (ShouldChangeInputDevice(device))
			{
				OnDeviceChanged(currentDeviceType, device);
			}
		}
	}

	private string GetDolocDeviceDisplayName(DolocInputDeviceType type)
	{
		switch (type)
		{
		case DolocInputDeviceType.GamePad:
		case DolocInputDeviceType.XboxController:
		case DolocInputDeviceType.SwitchProController:
		case DolocInputDeviceType.PlayStationController:
			return "游戏手柄";
		case DolocInputDeviceType.KeyboardMouse:
			return "键盘鼠标";
		default:
			return "未知设备";
		}
	}

	private void OnDeviceChanged(DolocInputDeviceType type, InputDevice d)
	{
		DolocAPI.output("输入设备切换为:" + GetDolocDeviceDisplayName(type) + "(" + d.displayName + ")", DolocColor.pink);
		this.OnInputDeviceChanged?.Invoke(type);
	}

	private void DetectNewDevice(InputDevice device, InputDeviceChange deviceChange)
	{
		if (deviceChange == InputDeviceChange.Added)
		{
			try
			{
				DolocAPI.ShowMessageBoxInfo(DolocUtils.Format(DolocConfig.StaticTexts.SettingPanelNewDevice, device.displayName));
			}
			catch (Exception)
			{
			}
		}
	}

	public string GetActionBindingKeyName(string actionName, DolocInputDeviceType type)
	{
		InputAction inputAction = normalInputs.Get().FindAction(actionName);
		if (inputAction == null)
		{
			inputAction = baseInputs.Get().FindAction(actionName);
		}
		if (inputAction == null)
		{
			inputAction = builderInputs.Get().FindAction(actionName);
		}
		if (inputAction == null)
		{
			return string.Empty;
		}
		return inputAction.GetBindingDisplayString((int)type);
	}

	private void UpdateActiveDevice(DolocInputType device)
	{
		if (deviceWeights.ContainsKey(device))
		{
			deviceWeights[device] += Time.deltaTime;
		}
		else
		{
			deviceWeights[device] = 1f;
		}
		_ = deviceWeights.OrderByDescending((KeyValuePair<DolocInputType, float> weights) => weights.Value).FirstOrDefault().Key;
	}

	public void RevertBindingOverrides(string rebinds)
	{
		try
		{
			inputSource.LoadBindingOverridesFromJson(rebinds);
		}
		catch (Exception exception)
		{
			Debug.LogError("设置按键绑定失败");
			Debug.LogException(exception);
			SaveBindingOverrides();
		}
	}

	public void LoadBindingOverrides()
	{
		try
		{
			if (DolocAPI.dataPersistenceManager.LoadBindingOverrides(out var rebinds))
			{
				inputSource.LoadBindingOverridesFromJson(rebinds);
				RevertBindingOverrides(rebinds);
			}
			else
			{
				Debug.LogError("加载按键绑定失败");
			}
		}
		catch (Exception exception)
		{
			Debug.LogError("设置按键绑定失败");
			Debug.LogException(exception);
			SaveBindingOverrides();
		}
		HandleEventInputMapping();
	}

	public void SaveBindingOverrides()
	{
		string rebinds = inputSource.SaveBindingOverridesAsJson();
		DolocAPI.dataPersistenceManager.SaveBindingOverrides(rebinds);
	}

	public string GetCurrentOverride()
	{
		return inputSource.SaveBindingOverridesAsJson();
	}

	public void RevertToDefault()
	{
		inputSource.RemoveAllBindingOverrides();
		DolocAPI.dataPersistenceManager.SaveBindingOverrides("");
	}

	public void HandleEventInputMapping()
	{
		DolocInputSource obj = DolocAPI.UserInput?.inputSource;
		InputSystemUIInputModule inputSystemUIInputModule = EventSystem.current?.currentInputModule as InputSystemUIInputModule;
		InputAction inputAction = obj?.FindAction("BaseInput/Confirm");
		if (inputSystemUIInputModule != null && inputAction != null)
		{
			inputSystemUIInputModule.submit = InputActionReference.Create(inputAction);
			inputSystemUIInputModule.submit.action.Disable();
			inputSystemUIInputModule.submit.action.Enable();
		}
	}
}
