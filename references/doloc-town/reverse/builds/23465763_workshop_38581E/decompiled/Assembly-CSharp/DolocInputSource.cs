using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class DolocInputSource : IInputActionCollection2, IInputActionCollection, IEnumerable<InputAction>, IEnumerable, IDisposable
{
	public struct NormalInputActions
	{
		private DolocInputSource m_Wrapper;

		public InputAction Move => m_Wrapper.m_NormalInput_Move;

		public InputAction Jump => m_Wrapper.m_NormalInput_Jump;

		public InputAction JumpDown => m_Wrapper.m_NormalInput_JumpDown;

		public InputAction Dash => m_Wrapper.m_NormalInput_Dash;

		public InputAction UseTool => m_Wrapper.m_NormalInput_UseTool;

		public InputAction UseItem => m_Wrapper.m_NormalInput_UseItem;

		public InputAction ScrollInventoryUp => m_Wrapper.m_NormalInput_ScrollInventoryUp;

		public InputAction ScrollInventoryDown => m_Wrapper.m_NormalInput_ScrollInventoryDown;

		public InputAction AssistMove => m_Wrapper.m_NormalInput_AssistMove;

		public InputAction RoomInteract => m_Wrapper.m_NormalInput_RoomInteract;

		public InputAction SwitchAutoFire => m_Wrapper.m_NormalInput_SwitchAutoFire;

		public InputAction SelectedDrone => m_Wrapper.m_NormalInput_SelectedDrone;

		public InputAction SelectedActive => m_Wrapper.m_NormalInput_SelectedActive;

		public InputAction QuickSelect_1 => m_Wrapper.m_NormalInput_QuickSelect_1;

		public InputAction QuickSelect_2 => m_Wrapper.m_NormalInput_QuickSelect_2;

		public InputAction QuickSelect_3 => m_Wrapper.m_NormalInput_QuickSelect_3;

		public InputAction QuickSelect_4 => m_Wrapper.m_NormalInput_QuickSelect_4;

		public InputAction QuickSelect_5 => m_Wrapper.m_NormalInput_QuickSelect_5;

		public InputAction QuickSelect_6 => m_Wrapper.m_NormalInput_QuickSelect_6;

		public InputAction QuickSelect_7 => m_Wrapper.m_NormalInput_QuickSelect_7;

		public InputAction QuickSelect_8 => m_Wrapper.m_NormalInput_QuickSelect_8;

		public InputAction QuickSelect_9 => m_Wrapper.m_NormalInput_QuickSelect_9;

		public InputAction QuickSelect_0 => m_Wrapper.m_NormalInput_QuickSelect_0;

		public InputAction Fishing => m_Wrapper.m_NormalInput_Fishing;

		public InputAction Debug => m_Wrapper.m_NormalInput_Debug;

		public InputAction DisposeItemInBackpack => m_Wrapper.m_NormalInput_DisposeItemInBackpack;

		public InputAction Interact => m_Wrapper.m_NormalInput_Interact;

		public InputAction SpeedUp => m_Wrapper.m_NormalInput_SpeedUp;

		public InputAction QuickSelectPrev => m_Wrapper.m_NormalInput_QuickSelectPrev;

		public InputAction QuickSelectNext => m_Wrapper.m_NormalInput_QuickSelectNext;

		public InputAction ToggleMissionPanel => m_Wrapper.m_NormalInput_ToggleMissionPanel;

		public InputAction ToggleTechTree => m_Wrapper.m_NormalInput_ToggleTechTree;

		public InputAction ToggleBackpack => m_Wrapper.m_NormalInput_ToggleBackpack;

		public InputAction ToggleBackpackHold => m_Wrapper.m_NormalInput_ToggleBackpackHold;

		public InputAction ToggleMap => m_Wrapper.m_NormalInput_ToggleMap;

		public InputAction ToggleMenu => m_Wrapper.m_NormalInput_ToggleMenu;

		public InputAction ToggleCollectionBook => m_Wrapper.m_NormalInput_ToggleCollectionBook;

		public InputAction Cancel => m_Wrapper.m_NormalInput_Cancel;

		public bool enabled => Get().enabled;

		public NormalInputActions(DolocInputSource wrapper)
		{
			m_Wrapper = wrapper;
		}

		public InputActionMap Get()
		{
			return m_Wrapper.m_NormalInput;
		}

		public void Enable()
		{
			Get().Enable();
		}

		public void Disable()
		{
			Get().Disable();
		}

		public static implicit operator InputActionMap(NormalInputActions set)
		{
			return set.Get();
		}

		public void SetCallbacks(INormalInputActions instance)
		{
			if (m_Wrapper.m_NormalInputActionsCallbackInterface != null)
			{
				Move.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnMove;
				Move.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnMove;
				Move.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnMove;
				Jump.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnJump;
				Jump.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnJump;
				Jump.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnJump;
				JumpDown.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnJumpDown;
				JumpDown.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnJumpDown;
				JumpDown.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnJumpDown;
				Dash.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnDash;
				Dash.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnDash;
				Dash.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnDash;
				UseTool.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnUseTool;
				UseTool.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnUseTool;
				UseTool.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnUseTool;
				UseItem.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnUseItem;
				UseItem.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnUseItem;
				UseItem.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnUseItem;
				ScrollInventoryUp.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnScrollInventoryUp;
				ScrollInventoryUp.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnScrollInventoryUp;
				ScrollInventoryUp.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnScrollInventoryUp;
				ScrollInventoryDown.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnScrollInventoryDown;
				ScrollInventoryDown.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnScrollInventoryDown;
				ScrollInventoryDown.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnScrollInventoryDown;
				AssistMove.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnAssistMove;
				AssistMove.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnAssistMove;
				AssistMove.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnAssistMove;
				RoomInteract.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnRoomInteract;
				RoomInteract.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnRoomInteract;
				RoomInteract.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnRoomInteract;
				SwitchAutoFire.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnSwitchAutoFire;
				SwitchAutoFire.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnSwitchAutoFire;
				SwitchAutoFire.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnSwitchAutoFire;
				SelectedDrone.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnSelectedDrone;
				SelectedDrone.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnSelectedDrone;
				SelectedDrone.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnSelectedDrone;
				SelectedActive.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnSelectedActive;
				SelectedActive.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnSelectedActive;
				SelectedActive.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnSelectedActive;
				QuickSelect_1.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_1;
				QuickSelect_1.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_1;
				QuickSelect_1.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_1;
				QuickSelect_2.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_2;
				QuickSelect_2.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_2;
				QuickSelect_2.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_2;
				QuickSelect_3.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_3;
				QuickSelect_3.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_3;
				QuickSelect_3.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_3;
				QuickSelect_4.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_4;
				QuickSelect_4.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_4;
				QuickSelect_4.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_4;
				QuickSelect_5.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_5;
				QuickSelect_5.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_5;
				QuickSelect_5.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_5;
				QuickSelect_6.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_6;
				QuickSelect_6.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_6;
				QuickSelect_6.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_6;
				QuickSelect_7.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_7;
				QuickSelect_7.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_7;
				QuickSelect_7.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_7;
				QuickSelect_8.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_8;
				QuickSelect_8.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_8;
				QuickSelect_8.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_8;
				QuickSelect_9.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_9;
				QuickSelect_9.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_9;
				QuickSelect_9.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_9;
				QuickSelect_0.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_0;
				QuickSelect_0.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_0;
				QuickSelect_0.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelect_0;
				Fishing.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnFishing;
				Fishing.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnFishing;
				Fishing.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnFishing;
				Debug.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnDebug;
				Debug.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnDebug;
				Debug.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnDebug;
				DisposeItemInBackpack.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnDisposeItemInBackpack;
				DisposeItemInBackpack.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnDisposeItemInBackpack;
				DisposeItemInBackpack.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnDisposeItemInBackpack;
				Interact.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnInteract;
				Interact.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnInteract;
				Interact.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnInteract;
				SpeedUp.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnSpeedUp;
				SpeedUp.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnSpeedUp;
				SpeedUp.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnSpeedUp;
				QuickSelectPrev.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelectPrev;
				QuickSelectPrev.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelectPrev;
				QuickSelectPrev.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelectPrev;
				QuickSelectNext.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelectNext;
				QuickSelectNext.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelectNext;
				QuickSelectNext.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnQuickSelectNext;
				ToggleMissionPanel.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleMissionPanel;
				ToggleMissionPanel.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleMissionPanel;
				ToggleMissionPanel.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleMissionPanel;
				ToggleTechTree.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleTechTree;
				ToggleTechTree.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleTechTree;
				ToggleTechTree.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleTechTree;
				ToggleBackpack.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleBackpack;
				ToggleBackpack.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleBackpack;
				ToggleBackpack.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleBackpack;
				ToggleBackpackHold.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleBackpackHold;
				ToggleBackpackHold.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleBackpackHold;
				ToggleBackpackHold.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleBackpackHold;
				ToggleMap.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleMap;
				ToggleMap.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleMap;
				ToggleMap.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleMap;
				ToggleMenu.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleMenu;
				ToggleMenu.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleMenu;
				ToggleMenu.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleMenu;
				ToggleCollectionBook.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleCollectionBook;
				ToggleCollectionBook.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleCollectionBook;
				ToggleCollectionBook.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnToggleCollectionBook;
				Cancel.started -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnCancel;
				Cancel.performed -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnCancel;
				Cancel.canceled -= m_Wrapper.m_NormalInputActionsCallbackInterface.OnCancel;
			}
			m_Wrapper.m_NormalInputActionsCallbackInterface = instance;
			if (instance != null)
			{
				Move.started += instance.OnMove;
				Move.performed += instance.OnMove;
				Move.canceled += instance.OnMove;
				Jump.started += instance.OnJump;
				Jump.performed += instance.OnJump;
				Jump.canceled += instance.OnJump;
				JumpDown.started += instance.OnJumpDown;
				JumpDown.performed += instance.OnJumpDown;
				JumpDown.canceled += instance.OnJumpDown;
				Dash.started += instance.OnDash;
				Dash.performed += instance.OnDash;
				Dash.canceled += instance.OnDash;
				UseTool.started += instance.OnUseTool;
				UseTool.performed += instance.OnUseTool;
				UseTool.canceled += instance.OnUseTool;
				UseItem.started += instance.OnUseItem;
				UseItem.performed += instance.OnUseItem;
				UseItem.canceled += instance.OnUseItem;
				ScrollInventoryUp.started += instance.OnScrollInventoryUp;
				ScrollInventoryUp.performed += instance.OnScrollInventoryUp;
				ScrollInventoryUp.canceled += instance.OnScrollInventoryUp;
				ScrollInventoryDown.started += instance.OnScrollInventoryDown;
				ScrollInventoryDown.performed += instance.OnScrollInventoryDown;
				ScrollInventoryDown.canceled += instance.OnScrollInventoryDown;
				AssistMove.started += instance.OnAssistMove;
				AssistMove.performed += instance.OnAssistMove;
				AssistMove.canceled += instance.OnAssistMove;
				RoomInteract.started += instance.OnRoomInteract;
				RoomInteract.performed += instance.OnRoomInteract;
				RoomInteract.canceled += instance.OnRoomInteract;
				SwitchAutoFire.started += instance.OnSwitchAutoFire;
				SwitchAutoFire.performed += instance.OnSwitchAutoFire;
				SwitchAutoFire.canceled += instance.OnSwitchAutoFire;
				SelectedDrone.started += instance.OnSelectedDrone;
				SelectedDrone.performed += instance.OnSelectedDrone;
				SelectedDrone.canceled += instance.OnSelectedDrone;
				SelectedActive.started += instance.OnSelectedActive;
				SelectedActive.performed += instance.OnSelectedActive;
				SelectedActive.canceled += instance.OnSelectedActive;
				QuickSelect_1.started += instance.OnQuickSelect_1;
				QuickSelect_1.performed += instance.OnQuickSelect_1;
				QuickSelect_1.canceled += instance.OnQuickSelect_1;
				QuickSelect_2.started += instance.OnQuickSelect_2;
				QuickSelect_2.performed += instance.OnQuickSelect_2;
				QuickSelect_2.canceled += instance.OnQuickSelect_2;
				QuickSelect_3.started += instance.OnQuickSelect_3;
				QuickSelect_3.performed += instance.OnQuickSelect_3;
				QuickSelect_3.canceled += instance.OnQuickSelect_3;
				QuickSelect_4.started += instance.OnQuickSelect_4;
				QuickSelect_4.performed += instance.OnQuickSelect_4;
				QuickSelect_4.canceled += instance.OnQuickSelect_4;
				QuickSelect_5.started += instance.OnQuickSelect_5;
				QuickSelect_5.performed += instance.OnQuickSelect_5;
				QuickSelect_5.canceled += instance.OnQuickSelect_5;
				QuickSelect_6.started += instance.OnQuickSelect_6;
				QuickSelect_6.performed += instance.OnQuickSelect_6;
				QuickSelect_6.canceled += instance.OnQuickSelect_6;
				QuickSelect_7.started += instance.OnQuickSelect_7;
				QuickSelect_7.performed += instance.OnQuickSelect_7;
				QuickSelect_7.canceled += instance.OnQuickSelect_7;
				QuickSelect_8.started += instance.OnQuickSelect_8;
				QuickSelect_8.performed += instance.OnQuickSelect_8;
				QuickSelect_8.canceled += instance.OnQuickSelect_8;
				QuickSelect_9.started += instance.OnQuickSelect_9;
				QuickSelect_9.performed += instance.OnQuickSelect_9;
				QuickSelect_9.canceled += instance.OnQuickSelect_9;
				QuickSelect_0.started += instance.OnQuickSelect_0;
				QuickSelect_0.performed += instance.OnQuickSelect_0;
				QuickSelect_0.canceled += instance.OnQuickSelect_0;
				Fishing.started += instance.OnFishing;
				Fishing.performed += instance.OnFishing;
				Fishing.canceled += instance.OnFishing;
				Debug.started += instance.OnDebug;
				Debug.performed += instance.OnDebug;
				Debug.canceled += instance.OnDebug;
				DisposeItemInBackpack.started += instance.OnDisposeItemInBackpack;
				DisposeItemInBackpack.performed += instance.OnDisposeItemInBackpack;
				DisposeItemInBackpack.canceled += instance.OnDisposeItemInBackpack;
				Interact.started += instance.OnInteract;
				Interact.performed += instance.OnInteract;
				Interact.canceled += instance.OnInteract;
				SpeedUp.started += instance.OnSpeedUp;
				SpeedUp.performed += instance.OnSpeedUp;
				SpeedUp.canceled += instance.OnSpeedUp;
				QuickSelectPrev.started += instance.OnQuickSelectPrev;
				QuickSelectPrev.performed += instance.OnQuickSelectPrev;
				QuickSelectPrev.canceled += instance.OnQuickSelectPrev;
				QuickSelectNext.started += instance.OnQuickSelectNext;
				QuickSelectNext.performed += instance.OnQuickSelectNext;
				QuickSelectNext.canceled += instance.OnQuickSelectNext;
				ToggleMissionPanel.started += instance.OnToggleMissionPanel;
				ToggleMissionPanel.performed += instance.OnToggleMissionPanel;
				ToggleMissionPanel.canceled += instance.OnToggleMissionPanel;
				ToggleTechTree.started += instance.OnToggleTechTree;
				ToggleTechTree.performed += instance.OnToggleTechTree;
				ToggleTechTree.canceled += instance.OnToggleTechTree;
				ToggleBackpack.started += instance.OnToggleBackpack;
				ToggleBackpack.performed += instance.OnToggleBackpack;
				ToggleBackpack.canceled += instance.OnToggleBackpack;
				ToggleBackpackHold.started += instance.OnToggleBackpackHold;
				ToggleBackpackHold.performed += instance.OnToggleBackpackHold;
				ToggleBackpackHold.canceled += instance.OnToggleBackpackHold;
				ToggleMap.started += instance.OnToggleMap;
				ToggleMap.performed += instance.OnToggleMap;
				ToggleMap.canceled += instance.OnToggleMap;
				ToggleMenu.started += instance.OnToggleMenu;
				ToggleMenu.performed += instance.OnToggleMenu;
				ToggleMenu.canceled += instance.OnToggleMenu;
				ToggleCollectionBook.started += instance.OnToggleCollectionBook;
				ToggleCollectionBook.performed += instance.OnToggleCollectionBook;
				ToggleCollectionBook.canceled += instance.OnToggleCollectionBook;
				Cancel.started += instance.OnCancel;
				Cancel.performed += instance.OnCancel;
				Cancel.canceled += instance.OnCancel;
			}
		}
	}

	public struct BuilderInputActions
	{
		private DolocInputSource m_Wrapper;

		public InputAction DPadUp => m_Wrapper.m_BuilderInput_DPadUp;

		public InputAction DPadDown => m_Wrapper.m_BuilderInput_DPadDown;

		public InputAction DPadLeft => m_Wrapper.m_BuilderInput_DPadLeft;

		public InputAction DPadRight => m_Wrapper.m_BuilderInput_DPadRight;

		public InputAction BuilderLast => m_Wrapper.m_BuilderInput_BuilderLast;

		public InputAction BuilderNext => m_Wrapper.m_BuilderInput_BuilderNext;

		public InputAction BuilderSelected => m_Wrapper.m_BuilderInput_BuilderSelected;

		public InputAction BuilderRevocation => m_Wrapper.m_BuilderInput_BuilderRevocation;

		public InputAction BuilderDismantle => m_Wrapper.m_BuilderInput_BuilderDismantle;

		public InputAction BuilderDismantleHold => m_Wrapper.m_BuilderInput_BuilderDismantleHold;

		public InputAction BuilderRotate => m_Wrapper.m_BuilderInput_BuilderRotate;

		public InputAction BuilderSwitch => m_Wrapper.m_BuilderInput_BuilderSwitch;

		public InputAction BuilderSwitchInventory => m_Wrapper.m_BuilderInput_BuilderSwitchInventory;

		public bool enabled => Get().enabled;

		public BuilderInputActions(DolocInputSource wrapper)
		{
			m_Wrapper = wrapper;
		}

		public InputActionMap Get()
		{
			return m_Wrapper.m_BuilderInput;
		}

		public void Enable()
		{
			Get().Enable();
		}

		public void Disable()
		{
			Get().Disable();
		}

		public static implicit operator InputActionMap(BuilderInputActions set)
		{
			return set.Get();
		}

		public void SetCallbacks(IBuilderInputActions instance)
		{
			if (m_Wrapper.m_BuilderInputActionsCallbackInterface != null)
			{
				DPadUp.started -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnDPadUp;
				DPadUp.performed -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnDPadUp;
				DPadUp.canceled -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnDPadUp;
				DPadDown.started -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnDPadDown;
				DPadDown.performed -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnDPadDown;
				DPadDown.canceled -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnDPadDown;
				DPadLeft.started -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnDPadLeft;
				DPadLeft.performed -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnDPadLeft;
				DPadLeft.canceled -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnDPadLeft;
				DPadRight.started -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnDPadRight;
				DPadRight.performed -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnDPadRight;
				DPadRight.canceled -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnDPadRight;
				BuilderLast.started -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderLast;
				BuilderLast.performed -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderLast;
				BuilderLast.canceled -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderLast;
				BuilderNext.started -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderNext;
				BuilderNext.performed -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderNext;
				BuilderNext.canceled -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderNext;
				BuilderSelected.started -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderSelected;
				BuilderSelected.performed -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderSelected;
				BuilderSelected.canceled -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderSelected;
				BuilderRevocation.started -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderRevocation;
				BuilderRevocation.performed -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderRevocation;
				BuilderRevocation.canceled -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderRevocation;
				BuilderDismantle.started -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderDismantle;
				BuilderDismantle.performed -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderDismantle;
				BuilderDismantle.canceled -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderDismantle;
				BuilderDismantleHold.started -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderDismantleHold;
				BuilderDismantleHold.performed -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderDismantleHold;
				BuilderDismantleHold.canceled -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderDismantleHold;
				BuilderRotate.started -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderRotate;
				BuilderRotate.performed -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderRotate;
				BuilderRotate.canceled -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderRotate;
				BuilderSwitch.started -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderSwitch;
				BuilderSwitch.performed -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderSwitch;
				BuilderSwitch.canceled -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderSwitch;
				BuilderSwitchInventory.started -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderSwitchInventory;
				BuilderSwitchInventory.performed -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderSwitchInventory;
				BuilderSwitchInventory.canceled -= m_Wrapper.m_BuilderInputActionsCallbackInterface.OnBuilderSwitchInventory;
			}
			m_Wrapper.m_BuilderInputActionsCallbackInterface = instance;
			if (instance != null)
			{
				DPadUp.started += instance.OnDPadUp;
				DPadUp.performed += instance.OnDPadUp;
				DPadUp.canceled += instance.OnDPadUp;
				DPadDown.started += instance.OnDPadDown;
				DPadDown.performed += instance.OnDPadDown;
				DPadDown.canceled += instance.OnDPadDown;
				DPadLeft.started += instance.OnDPadLeft;
				DPadLeft.performed += instance.OnDPadLeft;
				DPadLeft.canceled += instance.OnDPadLeft;
				DPadRight.started += instance.OnDPadRight;
				DPadRight.performed += instance.OnDPadRight;
				DPadRight.canceled += instance.OnDPadRight;
				BuilderLast.started += instance.OnBuilderLast;
				BuilderLast.performed += instance.OnBuilderLast;
				BuilderLast.canceled += instance.OnBuilderLast;
				BuilderNext.started += instance.OnBuilderNext;
				BuilderNext.performed += instance.OnBuilderNext;
				BuilderNext.canceled += instance.OnBuilderNext;
				BuilderSelected.started += instance.OnBuilderSelected;
				BuilderSelected.performed += instance.OnBuilderSelected;
				BuilderSelected.canceled += instance.OnBuilderSelected;
				BuilderRevocation.started += instance.OnBuilderRevocation;
				BuilderRevocation.performed += instance.OnBuilderRevocation;
				BuilderRevocation.canceled += instance.OnBuilderRevocation;
				BuilderDismantle.started += instance.OnBuilderDismantle;
				BuilderDismantle.performed += instance.OnBuilderDismantle;
				BuilderDismantle.canceled += instance.OnBuilderDismantle;
				BuilderDismantleHold.started += instance.OnBuilderDismantleHold;
				BuilderDismantleHold.performed += instance.OnBuilderDismantleHold;
				BuilderDismantleHold.canceled += instance.OnBuilderDismantleHold;
				BuilderRotate.started += instance.OnBuilderRotate;
				BuilderRotate.performed += instance.OnBuilderRotate;
				BuilderRotate.canceled += instance.OnBuilderRotate;
				BuilderSwitch.started += instance.OnBuilderSwitch;
				BuilderSwitch.performed += instance.OnBuilderSwitch;
				BuilderSwitch.canceled += instance.OnBuilderSwitch;
				BuilderSwitchInventory.started += instance.OnBuilderSwitchInventory;
				BuilderSwitchInventory.performed += instance.OnBuilderSwitchInventory;
				BuilderSwitchInventory.canceled += instance.OnBuilderSwitchInventory;
			}
		}
	}

	public struct BaseInputActions
	{
		private DolocInputSource m_Wrapper;

		public InputAction Move => m_Wrapper.m_BaseInput_Move;

		public InputAction MoveLeft => m_Wrapper.m_BaseInput_MoveLeft;

		public InputAction MoveRight => m_Wrapper.m_BaseInput_MoveRight;

		public InputAction MoveUp => m_Wrapper.m_BaseInput_MoveUp;

		public InputAction MoveDown => m_Wrapper.m_BaseInput_MoveDown;

		public InputAction MoveLast => m_Wrapper.m_BaseInput_MoveLast;

		public InputAction MoveNext => m_Wrapper.m_BaseInput_MoveNext;

		public InputAction Cancel => m_Wrapper.m_BaseInput_Cancel;

		public InputAction Confirm => m_Wrapper.m_BaseInput_Confirm;

		public InputAction ConfirmHold => m_Wrapper.m_BaseInput_ConfirmHold;

		public InputAction Scroll => m_Wrapper.m_BaseInput_Scroll;

		public InputAction PageUp => m_Wrapper.m_BaseInput_PageUp;

		public InputAction PageDown => m_Wrapper.m_BaseInput_PageDown;

		public InputAction SplitItem => m_Wrapper.m_BaseInput_SplitItem;

		public InputAction SortItem => m_Wrapper.m_BaseInput_SortItem;

		public InputAction SortItemHold => m_Wrapper.m_BaseInput_SortItemHold;

		public InputAction LockItem => m_Wrapper.m_BaseInput_LockItem;

		public InputAction DisposeItem => m_Wrapper.m_BaseInput_DisposeItem;

		public InputAction DisposeItemHold => m_Wrapper.m_BaseInput_DisposeItemHold;

		public InputAction DestroyItem => m_Wrapper.m_BaseInput_DestroyItem;

		public InputAction PutMaxItem => m_Wrapper.m_BaseInput_PutMaxItem;

		public InputAction PutMaxItemHold => m_Wrapper.m_BaseInput_PutMaxItemHold;

		public InputAction PutAllItem => m_Wrapper.m_BaseInput_PutAllItem;

		public InputAction SubmitItem => m_Wrapper.m_BaseInput_SubmitItem;

		public InputAction SubOne => m_Wrapper.m_BaseInput_SubOne;

		public InputAction AddOne => m_Wrapper.m_BaseInput_AddOne;

		public InputAction SubTen => m_Wrapper.m_BaseInput_SubTen;

		public InputAction AddTen => m_Wrapper.m_BaseInput_AddTen;

		public InputAction SetMin => m_Wrapper.m_BaseInput_SetMin;

		public InputAction SetMax => m_Wrapper.m_BaseInput_SetMax;

		public InputAction ContinueDialogue => m_Wrapper.m_BaseInput_ContinueDialogue;

		public InputAction ToggleDialogueHistory => m_Wrapper.m_BaseInput_ToggleDialogueHistory;

		public InputAction AssistSplit => m_Wrapper.m_BaseInput_AssistSplit;

		public InputAction Interact => m_Wrapper.m_BaseInput_Interact;

		public InputAction SpeedUp => m_Wrapper.m_BaseInput_SpeedUp;

		public InputAction QuickSelectPrev => m_Wrapper.m_BaseInput_QuickSelectPrev;

		public InputAction QuickSelectNext => m_Wrapper.m_BaseInput_QuickSelectNext;

		public InputAction ToggleMissionPanel => m_Wrapper.m_BaseInput_ToggleMissionPanel;

		public InputAction ToggleTechTree => m_Wrapper.m_BaseInput_ToggleTechTree;

		public InputAction ToggleBackpack => m_Wrapper.m_BaseInput_ToggleBackpack;

		public InputAction ToggleMap => m_Wrapper.m_BaseInput_ToggleMap;

		public InputAction ToggleMenu => m_Wrapper.m_BaseInput_ToggleMenu;

		public InputAction ToggleCollectionBook => m_Wrapper.m_BaseInput_ToggleCollectionBook;

		public InputAction MiscellaneousFunction => m_Wrapper.m_BaseInput_MiscellaneousFunction;

		public bool enabled => Get().enabled;

		public BaseInputActions(DolocInputSource wrapper)
		{
			m_Wrapper = wrapper;
		}

		public InputActionMap Get()
		{
			return m_Wrapper.m_BaseInput;
		}

		public void Enable()
		{
			Get().Enable();
		}

		public void Disable()
		{
			Get().Disable();
		}

		public static implicit operator InputActionMap(BaseInputActions set)
		{
			return set.Get();
		}

		public void SetCallbacks(IBaseInputActions instance)
		{
			if (m_Wrapper.m_BaseInputActionsCallbackInterface != null)
			{
				Move.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMove;
				Move.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMove;
				Move.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMove;
				MoveLeft.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveLeft;
				MoveLeft.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveLeft;
				MoveLeft.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveLeft;
				MoveRight.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveRight;
				MoveRight.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveRight;
				MoveRight.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveRight;
				MoveUp.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveUp;
				MoveUp.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveUp;
				MoveUp.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveUp;
				MoveDown.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveDown;
				MoveDown.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveDown;
				MoveDown.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveDown;
				MoveLast.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveLast;
				MoveLast.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveLast;
				MoveLast.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveLast;
				MoveNext.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveNext;
				MoveNext.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveNext;
				MoveNext.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMoveNext;
				Cancel.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnCancel;
				Cancel.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnCancel;
				Cancel.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnCancel;
				Confirm.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnConfirm;
				Confirm.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnConfirm;
				Confirm.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnConfirm;
				ConfirmHold.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnConfirmHold;
				ConfirmHold.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnConfirmHold;
				ConfirmHold.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnConfirmHold;
				Scroll.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnScroll;
				Scroll.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnScroll;
				Scroll.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnScroll;
				PageUp.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPageUp;
				PageUp.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPageUp;
				PageUp.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPageUp;
				PageDown.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPageDown;
				PageDown.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPageDown;
				PageDown.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPageDown;
				SplitItem.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSplitItem;
				SplitItem.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSplitItem;
				SplitItem.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSplitItem;
				SortItem.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSortItem;
				SortItem.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSortItem;
				SortItem.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSortItem;
				SortItemHold.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSortItemHold;
				SortItemHold.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSortItemHold;
				SortItemHold.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSortItemHold;
				LockItem.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnLockItem;
				LockItem.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnLockItem;
				LockItem.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnLockItem;
				DisposeItem.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnDisposeItem;
				DisposeItem.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnDisposeItem;
				DisposeItem.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnDisposeItem;
				DisposeItemHold.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnDisposeItemHold;
				DisposeItemHold.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnDisposeItemHold;
				DisposeItemHold.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnDisposeItemHold;
				DestroyItem.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnDestroyItem;
				DestroyItem.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnDestroyItem;
				DestroyItem.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnDestroyItem;
				PutMaxItem.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPutMaxItem;
				PutMaxItem.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPutMaxItem;
				PutMaxItem.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPutMaxItem;
				PutMaxItemHold.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPutMaxItemHold;
				PutMaxItemHold.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPutMaxItemHold;
				PutMaxItemHold.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPutMaxItemHold;
				PutAllItem.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPutAllItem;
				PutAllItem.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPutAllItem;
				PutAllItem.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnPutAllItem;
				SubmitItem.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSubmitItem;
				SubmitItem.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSubmitItem;
				SubmitItem.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSubmitItem;
				SubOne.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSubOne;
				SubOne.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSubOne;
				SubOne.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSubOne;
				AddOne.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnAddOne;
				AddOne.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnAddOne;
				AddOne.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnAddOne;
				SubTen.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSubTen;
				SubTen.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSubTen;
				SubTen.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSubTen;
				AddTen.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnAddTen;
				AddTen.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnAddTen;
				AddTen.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnAddTen;
				SetMin.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSetMin;
				SetMin.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSetMin;
				SetMin.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSetMin;
				SetMax.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSetMax;
				SetMax.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSetMax;
				SetMax.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSetMax;
				ContinueDialogue.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnContinueDialogue;
				ContinueDialogue.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnContinueDialogue;
				ContinueDialogue.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnContinueDialogue;
				ToggleDialogueHistory.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleDialogueHistory;
				ToggleDialogueHistory.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleDialogueHistory;
				ToggleDialogueHistory.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleDialogueHistory;
				AssistSplit.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnAssistSplit;
				AssistSplit.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnAssistSplit;
				AssistSplit.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnAssistSplit;
				Interact.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnInteract;
				Interact.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnInteract;
				Interact.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnInteract;
				SpeedUp.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSpeedUp;
				SpeedUp.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSpeedUp;
				SpeedUp.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnSpeedUp;
				QuickSelectPrev.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnQuickSelectPrev;
				QuickSelectPrev.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnQuickSelectPrev;
				QuickSelectPrev.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnQuickSelectPrev;
				QuickSelectNext.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnQuickSelectNext;
				QuickSelectNext.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnQuickSelectNext;
				QuickSelectNext.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnQuickSelectNext;
				ToggleMissionPanel.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleMissionPanel;
				ToggleMissionPanel.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleMissionPanel;
				ToggleMissionPanel.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleMissionPanel;
				ToggleTechTree.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleTechTree;
				ToggleTechTree.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleTechTree;
				ToggleTechTree.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleTechTree;
				ToggleBackpack.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleBackpack;
				ToggleBackpack.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleBackpack;
				ToggleBackpack.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleBackpack;
				ToggleMap.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleMap;
				ToggleMap.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleMap;
				ToggleMap.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleMap;
				ToggleMenu.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleMenu;
				ToggleMenu.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleMenu;
				ToggleMenu.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleMenu;
				ToggleCollectionBook.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleCollectionBook;
				ToggleCollectionBook.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleCollectionBook;
				ToggleCollectionBook.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnToggleCollectionBook;
				MiscellaneousFunction.started -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMiscellaneousFunction;
				MiscellaneousFunction.performed -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMiscellaneousFunction;
				MiscellaneousFunction.canceled -= m_Wrapper.m_BaseInputActionsCallbackInterface.OnMiscellaneousFunction;
			}
			m_Wrapper.m_BaseInputActionsCallbackInterface = instance;
			if (instance != null)
			{
				Move.started += instance.OnMove;
				Move.performed += instance.OnMove;
				Move.canceled += instance.OnMove;
				MoveLeft.started += instance.OnMoveLeft;
				MoveLeft.performed += instance.OnMoveLeft;
				MoveLeft.canceled += instance.OnMoveLeft;
				MoveRight.started += instance.OnMoveRight;
				MoveRight.performed += instance.OnMoveRight;
				MoveRight.canceled += instance.OnMoveRight;
				MoveUp.started += instance.OnMoveUp;
				MoveUp.performed += instance.OnMoveUp;
				MoveUp.canceled += instance.OnMoveUp;
				MoveDown.started += instance.OnMoveDown;
				MoveDown.performed += instance.OnMoveDown;
				MoveDown.canceled += instance.OnMoveDown;
				MoveLast.started += instance.OnMoveLast;
				MoveLast.performed += instance.OnMoveLast;
				MoveLast.canceled += instance.OnMoveLast;
				MoveNext.started += instance.OnMoveNext;
				MoveNext.performed += instance.OnMoveNext;
				MoveNext.canceled += instance.OnMoveNext;
				Cancel.started += instance.OnCancel;
				Cancel.performed += instance.OnCancel;
				Cancel.canceled += instance.OnCancel;
				Confirm.started += instance.OnConfirm;
				Confirm.performed += instance.OnConfirm;
				Confirm.canceled += instance.OnConfirm;
				ConfirmHold.started += instance.OnConfirmHold;
				ConfirmHold.performed += instance.OnConfirmHold;
				ConfirmHold.canceled += instance.OnConfirmHold;
				Scroll.started += instance.OnScroll;
				Scroll.performed += instance.OnScroll;
				Scroll.canceled += instance.OnScroll;
				PageUp.started += instance.OnPageUp;
				PageUp.performed += instance.OnPageUp;
				PageUp.canceled += instance.OnPageUp;
				PageDown.started += instance.OnPageDown;
				PageDown.performed += instance.OnPageDown;
				PageDown.canceled += instance.OnPageDown;
				SplitItem.started += instance.OnSplitItem;
				SplitItem.performed += instance.OnSplitItem;
				SplitItem.canceled += instance.OnSplitItem;
				SortItem.started += instance.OnSortItem;
				SortItem.performed += instance.OnSortItem;
				SortItem.canceled += instance.OnSortItem;
				SortItemHold.started += instance.OnSortItemHold;
				SortItemHold.performed += instance.OnSortItemHold;
				SortItemHold.canceled += instance.OnSortItemHold;
				LockItem.started += instance.OnLockItem;
				LockItem.performed += instance.OnLockItem;
				LockItem.canceled += instance.OnLockItem;
				DisposeItem.started += instance.OnDisposeItem;
				DisposeItem.performed += instance.OnDisposeItem;
				DisposeItem.canceled += instance.OnDisposeItem;
				DisposeItemHold.started += instance.OnDisposeItemHold;
				DisposeItemHold.performed += instance.OnDisposeItemHold;
				DisposeItemHold.canceled += instance.OnDisposeItemHold;
				DestroyItem.started += instance.OnDestroyItem;
				DestroyItem.performed += instance.OnDestroyItem;
				DestroyItem.canceled += instance.OnDestroyItem;
				PutMaxItem.started += instance.OnPutMaxItem;
				PutMaxItem.performed += instance.OnPutMaxItem;
				PutMaxItem.canceled += instance.OnPutMaxItem;
				PutMaxItemHold.started += instance.OnPutMaxItemHold;
				PutMaxItemHold.performed += instance.OnPutMaxItemHold;
				PutMaxItemHold.canceled += instance.OnPutMaxItemHold;
				PutAllItem.started += instance.OnPutAllItem;
				PutAllItem.performed += instance.OnPutAllItem;
				PutAllItem.canceled += instance.OnPutAllItem;
				SubmitItem.started += instance.OnSubmitItem;
				SubmitItem.performed += instance.OnSubmitItem;
				SubmitItem.canceled += instance.OnSubmitItem;
				SubOne.started += instance.OnSubOne;
				SubOne.performed += instance.OnSubOne;
				SubOne.canceled += instance.OnSubOne;
				AddOne.started += instance.OnAddOne;
				AddOne.performed += instance.OnAddOne;
				AddOne.canceled += instance.OnAddOne;
				SubTen.started += instance.OnSubTen;
				SubTen.performed += instance.OnSubTen;
				SubTen.canceled += instance.OnSubTen;
				AddTen.started += instance.OnAddTen;
				AddTen.performed += instance.OnAddTen;
				AddTen.canceled += instance.OnAddTen;
				SetMin.started += instance.OnSetMin;
				SetMin.performed += instance.OnSetMin;
				SetMin.canceled += instance.OnSetMin;
				SetMax.started += instance.OnSetMax;
				SetMax.performed += instance.OnSetMax;
				SetMax.canceled += instance.OnSetMax;
				ContinueDialogue.started += instance.OnContinueDialogue;
				ContinueDialogue.performed += instance.OnContinueDialogue;
				ContinueDialogue.canceled += instance.OnContinueDialogue;
				ToggleDialogueHistory.started += instance.OnToggleDialogueHistory;
				ToggleDialogueHistory.performed += instance.OnToggleDialogueHistory;
				ToggleDialogueHistory.canceled += instance.OnToggleDialogueHistory;
				AssistSplit.started += instance.OnAssistSplit;
				AssistSplit.performed += instance.OnAssistSplit;
				AssistSplit.canceled += instance.OnAssistSplit;
				Interact.started += instance.OnInteract;
				Interact.performed += instance.OnInteract;
				Interact.canceled += instance.OnInteract;
				SpeedUp.started += instance.OnSpeedUp;
				SpeedUp.performed += instance.OnSpeedUp;
				SpeedUp.canceled += instance.OnSpeedUp;
				QuickSelectPrev.started += instance.OnQuickSelectPrev;
				QuickSelectPrev.performed += instance.OnQuickSelectPrev;
				QuickSelectPrev.canceled += instance.OnQuickSelectPrev;
				QuickSelectNext.started += instance.OnQuickSelectNext;
				QuickSelectNext.performed += instance.OnQuickSelectNext;
				QuickSelectNext.canceled += instance.OnQuickSelectNext;
				ToggleMissionPanel.started += instance.OnToggleMissionPanel;
				ToggleMissionPanel.performed += instance.OnToggleMissionPanel;
				ToggleMissionPanel.canceled += instance.OnToggleMissionPanel;
				ToggleTechTree.started += instance.OnToggleTechTree;
				ToggleTechTree.performed += instance.OnToggleTechTree;
				ToggleTechTree.canceled += instance.OnToggleTechTree;
				ToggleBackpack.started += instance.OnToggleBackpack;
				ToggleBackpack.performed += instance.OnToggleBackpack;
				ToggleBackpack.canceled += instance.OnToggleBackpack;
				ToggleMap.started += instance.OnToggleMap;
				ToggleMap.performed += instance.OnToggleMap;
				ToggleMap.canceled += instance.OnToggleMap;
				ToggleMenu.started += instance.OnToggleMenu;
				ToggleMenu.performed += instance.OnToggleMenu;
				ToggleMenu.canceled += instance.OnToggleMenu;
				ToggleCollectionBook.started += instance.OnToggleCollectionBook;
				ToggleCollectionBook.performed += instance.OnToggleCollectionBook;
				ToggleCollectionBook.canceled += instance.OnToggleCollectionBook;
				MiscellaneousFunction.started += instance.OnMiscellaneousFunction;
				MiscellaneousFunction.performed += instance.OnMiscellaneousFunction;
				MiscellaneousFunction.canceled += instance.OnMiscellaneousFunction;
			}
		}
	}

	public struct GlobalActions
	{
		private DolocInputSource m_Wrapper;

		public InputAction Point => m_Wrapper.m_Global_Point;

		public InputAction Click => m_Wrapper.m_Global_Click;

		public InputAction RightClick => m_Wrapper.m_Global_RightClick;

		public InputAction CursorPosition => m_Wrapper.m_Global_CursorPosition;

		public bool enabled => Get().enabled;

		public GlobalActions(DolocInputSource wrapper)
		{
			m_Wrapper = wrapper;
		}

		public InputActionMap Get()
		{
			return m_Wrapper.m_Global;
		}

		public void Enable()
		{
			Get().Enable();
		}

		public void Disable()
		{
			Get().Disable();
		}

		public static implicit operator InputActionMap(GlobalActions set)
		{
			return set.Get();
		}

		public void SetCallbacks(IGlobalActions instance)
		{
			if (m_Wrapper.m_GlobalActionsCallbackInterface != null)
			{
				Point.started -= m_Wrapper.m_GlobalActionsCallbackInterface.OnPoint;
				Point.performed -= m_Wrapper.m_GlobalActionsCallbackInterface.OnPoint;
				Point.canceled -= m_Wrapper.m_GlobalActionsCallbackInterface.OnPoint;
				Click.started -= m_Wrapper.m_GlobalActionsCallbackInterface.OnClick;
				Click.performed -= m_Wrapper.m_GlobalActionsCallbackInterface.OnClick;
				Click.canceled -= m_Wrapper.m_GlobalActionsCallbackInterface.OnClick;
				RightClick.started -= m_Wrapper.m_GlobalActionsCallbackInterface.OnRightClick;
				RightClick.performed -= m_Wrapper.m_GlobalActionsCallbackInterface.OnRightClick;
				RightClick.canceled -= m_Wrapper.m_GlobalActionsCallbackInterface.OnRightClick;
				CursorPosition.started -= m_Wrapper.m_GlobalActionsCallbackInterface.OnCursorPosition;
				CursorPosition.performed -= m_Wrapper.m_GlobalActionsCallbackInterface.OnCursorPosition;
				CursorPosition.canceled -= m_Wrapper.m_GlobalActionsCallbackInterface.OnCursorPosition;
			}
			m_Wrapper.m_GlobalActionsCallbackInterface = instance;
			if (instance != null)
			{
				Point.started += instance.OnPoint;
				Point.performed += instance.OnPoint;
				Point.canceled += instance.OnPoint;
				Click.started += instance.OnClick;
				Click.performed += instance.OnClick;
				Click.canceled += instance.OnClick;
				RightClick.started += instance.OnRightClick;
				RightClick.performed += instance.OnRightClick;
				RightClick.canceled += instance.OnRightClick;
				CursorPosition.started += instance.OnCursorPosition;
				CursorPosition.performed += instance.OnCursorPosition;
				CursorPosition.canceled += instance.OnCursorPosition;
			}
		}
	}

	public interface INormalInputActions
	{
		void OnMove(InputAction.CallbackContext context);

		void OnJump(InputAction.CallbackContext context);

		void OnJumpDown(InputAction.CallbackContext context);

		void OnDash(InputAction.CallbackContext context);

		void OnUseTool(InputAction.CallbackContext context);

		void OnUseItem(InputAction.CallbackContext context);

		void OnScrollInventoryUp(InputAction.CallbackContext context);

		void OnScrollInventoryDown(InputAction.CallbackContext context);

		void OnAssistMove(InputAction.CallbackContext context);

		void OnRoomInteract(InputAction.CallbackContext context);

		void OnSwitchAutoFire(InputAction.CallbackContext context);

		void OnSelectedDrone(InputAction.CallbackContext context);

		void OnSelectedActive(InputAction.CallbackContext context);

		void OnQuickSelect_1(InputAction.CallbackContext context);

		void OnQuickSelect_2(InputAction.CallbackContext context);

		void OnQuickSelect_3(InputAction.CallbackContext context);

		void OnQuickSelect_4(InputAction.CallbackContext context);

		void OnQuickSelect_5(InputAction.CallbackContext context);

		void OnQuickSelect_6(InputAction.CallbackContext context);

		void OnQuickSelect_7(InputAction.CallbackContext context);

		void OnQuickSelect_8(InputAction.CallbackContext context);

		void OnQuickSelect_9(InputAction.CallbackContext context);

		void OnQuickSelect_0(InputAction.CallbackContext context);

		void OnFishing(InputAction.CallbackContext context);

		void OnDebug(InputAction.CallbackContext context);

		void OnDisposeItemInBackpack(InputAction.CallbackContext context);

		void OnInteract(InputAction.CallbackContext context);

		void OnSpeedUp(InputAction.CallbackContext context);

		void OnQuickSelectPrev(InputAction.CallbackContext context);

		void OnQuickSelectNext(InputAction.CallbackContext context);

		void OnToggleMissionPanel(InputAction.CallbackContext context);

		void OnToggleTechTree(InputAction.CallbackContext context);

		void OnToggleBackpack(InputAction.CallbackContext context);

		void OnToggleBackpackHold(InputAction.CallbackContext context);

		void OnToggleMap(InputAction.CallbackContext context);

		void OnToggleMenu(InputAction.CallbackContext context);

		void OnToggleCollectionBook(InputAction.CallbackContext context);

		void OnCancel(InputAction.CallbackContext context);
	}

	public interface IBuilderInputActions
	{
		void OnDPadUp(InputAction.CallbackContext context);

		void OnDPadDown(InputAction.CallbackContext context);

		void OnDPadLeft(InputAction.CallbackContext context);

		void OnDPadRight(InputAction.CallbackContext context);

		void OnBuilderLast(InputAction.CallbackContext context);

		void OnBuilderNext(InputAction.CallbackContext context);

		void OnBuilderSelected(InputAction.CallbackContext context);

		void OnBuilderRevocation(InputAction.CallbackContext context);

		void OnBuilderDismantle(InputAction.CallbackContext context);

		void OnBuilderDismantleHold(InputAction.CallbackContext context);

		void OnBuilderRotate(InputAction.CallbackContext context);

		void OnBuilderSwitch(InputAction.CallbackContext context);

		void OnBuilderSwitchInventory(InputAction.CallbackContext context);
	}

	public interface IBaseInputActions
	{
		void OnMove(InputAction.CallbackContext context);

		void OnMoveLeft(InputAction.CallbackContext context);

		void OnMoveRight(InputAction.CallbackContext context);

		void OnMoveUp(InputAction.CallbackContext context);

		void OnMoveDown(InputAction.CallbackContext context);

		void OnMoveLast(InputAction.CallbackContext context);

		void OnMoveNext(InputAction.CallbackContext context);

		void OnCancel(InputAction.CallbackContext context);

		void OnConfirm(InputAction.CallbackContext context);

		void OnConfirmHold(InputAction.CallbackContext context);

		void OnScroll(InputAction.CallbackContext context);

		void OnPageUp(InputAction.CallbackContext context);

		void OnPageDown(InputAction.CallbackContext context);

		void OnSplitItem(InputAction.CallbackContext context);

		void OnSortItem(InputAction.CallbackContext context);

		void OnSortItemHold(InputAction.CallbackContext context);

		void OnLockItem(InputAction.CallbackContext context);

		void OnDisposeItem(InputAction.CallbackContext context);

		void OnDisposeItemHold(InputAction.CallbackContext context);

		void OnDestroyItem(InputAction.CallbackContext context);

		void OnPutMaxItem(InputAction.CallbackContext context);

		void OnPutMaxItemHold(InputAction.CallbackContext context);

		void OnPutAllItem(InputAction.CallbackContext context);

		void OnSubmitItem(InputAction.CallbackContext context);

		void OnSubOne(InputAction.CallbackContext context);

		void OnAddOne(InputAction.CallbackContext context);

		void OnSubTen(InputAction.CallbackContext context);

		void OnAddTen(InputAction.CallbackContext context);

		void OnSetMin(InputAction.CallbackContext context);

		void OnSetMax(InputAction.CallbackContext context);

		void OnContinueDialogue(InputAction.CallbackContext context);

		void OnToggleDialogueHistory(InputAction.CallbackContext context);

		void OnAssistSplit(InputAction.CallbackContext context);

		void OnInteract(InputAction.CallbackContext context);

		void OnSpeedUp(InputAction.CallbackContext context);

		void OnQuickSelectPrev(InputAction.CallbackContext context);

		void OnQuickSelectNext(InputAction.CallbackContext context);

		void OnToggleMissionPanel(InputAction.CallbackContext context);

		void OnToggleTechTree(InputAction.CallbackContext context);

		void OnToggleBackpack(InputAction.CallbackContext context);

		void OnToggleMap(InputAction.CallbackContext context);

		void OnToggleMenu(InputAction.CallbackContext context);

		void OnToggleCollectionBook(InputAction.CallbackContext context);

		void OnMiscellaneousFunction(InputAction.CallbackContext context);
	}

	public interface IGlobalActions
	{
		void OnPoint(InputAction.CallbackContext context);

		void OnClick(InputAction.CallbackContext context);

		void OnRightClick(InputAction.CallbackContext context);

		void OnCursorPosition(InputAction.CallbackContext context);
	}

	private readonly InputActionMap m_NormalInput;

	private INormalInputActions m_NormalInputActionsCallbackInterface;

	private readonly InputAction m_NormalInput_Move;

	private readonly InputAction m_NormalInput_Jump;

	private readonly InputAction m_NormalInput_JumpDown;

	private readonly InputAction m_NormalInput_Dash;

	private readonly InputAction m_NormalInput_UseTool;

	private readonly InputAction m_NormalInput_UseItem;

	private readonly InputAction m_NormalInput_ScrollInventoryUp;

	private readonly InputAction m_NormalInput_ScrollInventoryDown;

	private readonly InputAction m_NormalInput_AssistMove;

	private readonly InputAction m_NormalInput_RoomInteract;

	private readonly InputAction m_NormalInput_SwitchAutoFire;

	private readonly InputAction m_NormalInput_SelectedDrone;

	private readonly InputAction m_NormalInput_SelectedActive;

	private readonly InputAction m_NormalInput_QuickSelect_1;

	private readonly InputAction m_NormalInput_QuickSelect_2;

	private readonly InputAction m_NormalInput_QuickSelect_3;

	private readonly InputAction m_NormalInput_QuickSelect_4;

	private readonly InputAction m_NormalInput_QuickSelect_5;

	private readonly InputAction m_NormalInput_QuickSelect_6;

	private readonly InputAction m_NormalInput_QuickSelect_7;

	private readonly InputAction m_NormalInput_QuickSelect_8;

	private readonly InputAction m_NormalInput_QuickSelect_9;

	private readonly InputAction m_NormalInput_QuickSelect_0;

	private readonly InputAction m_NormalInput_Fishing;

	private readonly InputAction m_NormalInput_Debug;

	private readonly InputAction m_NormalInput_DisposeItemInBackpack;

	private readonly InputAction m_NormalInput_Interact;

	private readonly InputAction m_NormalInput_SpeedUp;

	private readonly InputAction m_NormalInput_QuickSelectPrev;

	private readonly InputAction m_NormalInput_QuickSelectNext;

	private readonly InputAction m_NormalInput_ToggleMissionPanel;

	private readonly InputAction m_NormalInput_ToggleTechTree;

	private readonly InputAction m_NormalInput_ToggleBackpack;

	private readonly InputAction m_NormalInput_ToggleBackpackHold;

	private readonly InputAction m_NormalInput_ToggleMap;

	private readonly InputAction m_NormalInput_ToggleMenu;

	private readonly InputAction m_NormalInput_ToggleCollectionBook;

	private readonly InputAction m_NormalInput_Cancel;

	private readonly InputActionMap m_BuilderInput;

	private IBuilderInputActions m_BuilderInputActionsCallbackInterface;

	private readonly InputAction m_BuilderInput_DPadUp;

	private readonly InputAction m_BuilderInput_DPadDown;

	private readonly InputAction m_BuilderInput_DPadLeft;

	private readonly InputAction m_BuilderInput_DPadRight;

	private readonly InputAction m_BuilderInput_BuilderLast;

	private readonly InputAction m_BuilderInput_BuilderNext;

	private readonly InputAction m_BuilderInput_BuilderSelected;

	private readonly InputAction m_BuilderInput_BuilderRevocation;

	private readonly InputAction m_BuilderInput_BuilderDismantle;

	private readonly InputAction m_BuilderInput_BuilderDismantleHold;

	private readonly InputAction m_BuilderInput_BuilderRotate;

	private readonly InputAction m_BuilderInput_BuilderSwitch;

	private readonly InputAction m_BuilderInput_BuilderSwitchInventory;

	private readonly InputActionMap m_BaseInput;

	private IBaseInputActions m_BaseInputActionsCallbackInterface;

	private readonly InputAction m_BaseInput_Move;

	private readonly InputAction m_BaseInput_MoveLeft;

	private readonly InputAction m_BaseInput_MoveRight;

	private readonly InputAction m_BaseInput_MoveUp;

	private readonly InputAction m_BaseInput_MoveDown;

	private readonly InputAction m_BaseInput_MoveLast;

	private readonly InputAction m_BaseInput_MoveNext;

	private readonly InputAction m_BaseInput_Cancel;

	private readonly InputAction m_BaseInput_Confirm;

	private readonly InputAction m_BaseInput_ConfirmHold;

	private readonly InputAction m_BaseInput_Scroll;

	private readonly InputAction m_BaseInput_PageUp;

	private readonly InputAction m_BaseInput_PageDown;

	private readonly InputAction m_BaseInput_SplitItem;

	private readonly InputAction m_BaseInput_SortItem;

	private readonly InputAction m_BaseInput_SortItemHold;

	private readonly InputAction m_BaseInput_LockItem;

	private readonly InputAction m_BaseInput_DisposeItem;

	private readonly InputAction m_BaseInput_DisposeItemHold;

	private readonly InputAction m_BaseInput_DestroyItem;

	private readonly InputAction m_BaseInput_PutMaxItem;

	private readonly InputAction m_BaseInput_PutMaxItemHold;

	private readonly InputAction m_BaseInput_PutAllItem;

	private readonly InputAction m_BaseInput_SubmitItem;

	private readonly InputAction m_BaseInput_SubOne;

	private readonly InputAction m_BaseInput_AddOne;

	private readonly InputAction m_BaseInput_SubTen;

	private readonly InputAction m_BaseInput_AddTen;

	private readonly InputAction m_BaseInput_SetMin;

	private readonly InputAction m_BaseInput_SetMax;

	private readonly InputAction m_BaseInput_ContinueDialogue;

	private readonly InputAction m_BaseInput_ToggleDialogueHistory;

	private readonly InputAction m_BaseInput_AssistSplit;

	private readonly InputAction m_BaseInput_Interact;

	private readonly InputAction m_BaseInput_SpeedUp;

	private readonly InputAction m_BaseInput_QuickSelectPrev;

	private readonly InputAction m_BaseInput_QuickSelectNext;

	private readonly InputAction m_BaseInput_ToggleMissionPanel;

	private readonly InputAction m_BaseInput_ToggleTechTree;

	private readonly InputAction m_BaseInput_ToggleBackpack;

	private readonly InputAction m_BaseInput_ToggleMap;

	private readonly InputAction m_BaseInput_ToggleMenu;

	private readonly InputAction m_BaseInput_ToggleCollectionBook;

	private readonly InputAction m_BaseInput_MiscellaneousFunction;

	private readonly InputActionMap m_Global;

	private IGlobalActions m_GlobalActionsCallbackInterface;

	private readonly InputAction m_Global_Point;

	private readonly InputAction m_Global_Click;

	private readonly InputAction m_Global_RightClick;

	private readonly InputAction m_Global_CursorPosition;

	private int m_KeyboardMouseSchemeIndex = -1;

	private int m_GamePadSchemeIndex = -1;

	public InputActionAsset asset { get; }

	public InputBinding? bindingMask
	{
		get
		{
			return asset.bindingMask;
		}
		set
		{
			asset.bindingMask = value;
		}
	}

	public ReadOnlyArray<InputDevice>? devices
	{
		get
		{
			return asset.devices;
		}
		set
		{
			asset.devices = value;
		}
	}

	public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

	public IEnumerable<InputBinding> bindings => asset.bindings;

	public NormalInputActions NormalInput => new NormalInputActions(this);

	public BuilderInputActions BuilderInput => new BuilderInputActions(this);

	public BaseInputActions BaseInput => new BaseInputActions(this);

	public GlobalActions Global => new GlobalActions(this);

	public InputControlScheme KeyboardMouseScheme
	{
		get
		{
			if (m_KeyboardMouseSchemeIndex == -1)
			{
				m_KeyboardMouseSchemeIndex = asset.FindControlSchemeIndex("KeyboardMouse");
			}
			return asset.controlSchemes[m_KeyboardMouseSchemeIndex];
		}
	}

	public InputControlScheme GamePadScheme
	{
		get
		{
			if (m_GamePadSchemeIndex == -1)
			{
				m_GamePadSchemeIndex = asset.FindControlSchemeIndex("GamePad");
			}
			return asset.controlSchemes[m_GamePadSchemeIndex];
		}
	}

	public DolocInputSource()
	{
		asset = InputActionAsset.FromJson("{\n    \"name\": \"GameInput\",\n    \"maps\": [\n        {\n            \"name\": \"NormalInput\",\n            \"id\": \"41395cad-578b-44c2-b8e5-61b051956e01\",\n            \"actions\": [\n                {\n                    \"name\": \"Move\",\n                    \"type\": \"Value\",\n                    \"id\": \"24efa0a3-cc26-4bdc-a19a-552327be56dd\",\n                    \"expectedControlType\": \"Vector2\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"Jump\",\n                    \"type\": \"Button\",\n                    \"id\": \"10faa65d-aab6-4dc8-92a2-1114065ef9fe\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"JumpDown\",\n                    \"type\": \"Button\",\n                    \"id\": \"b65e165d-ad8e-44fd-bbc7-20b6504205e2\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Dash\",\n                    \"type\": \"Button\",\n                    \"id\": \"11ad8edd-8ecd-4d9b-8bb8-22f4da064cb4\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"UseTool\",\n                    \"type\": \"Button\",\n                    \"id\": \"1e9a3d86-ca86-42e1-80db-5ed834688e6f\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"UseItem\",\n                    \"type\": \"Button\",\n                    \"id\": \"aa78d825-e579-4bcb-b6b3-e98b2f179671\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ScrollInventoryUp\",\n                    \"type\": \"Button\",\n                    \"id\": \"03c08201-4e9a-4b20-8a2f-1f66b0319b46\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ScrollInventoryDown\",\n                    \"type\": \"Button\",\n                    \"id\": \"a651e312-4618-46f4-affd-df6303be05a9\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"AssistMove\",\n                    \"type\": \"Value\",\n                    \"id\": \"e25d5442-0da4-45da-ac54-313bff071979\",\n                    \"expectedControlType\": \"Vector2\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"RoomInteract\",\n                    \"type\": \"Button\",\n                    \"id\": \"ebbb186a-8b8d-410b-9e53-c00dd980538a\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SwitchAutoFire\",\n                    \"type\": \"Button\",\n                    \"id\": \"884ead0d-3dbd-4264-bdf2-a145983d276e\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SelectedDrone\",\n                    \"type\": \"Button\",\n                    \"id\": \"c79a9628-2640-458d-aaf8-98f5ecdebbd7\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SelectedActive\",\n                    \"type\": \"Button\",\n                    \"id\": \"a010c530-1574-4efc-a406-f0de8e39ed83\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelect_1\",\n                    \"type\": \"Button\",\n                    \"id\": \"a5326758-f200-4124-baf2-725e9e4abd87\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelect_2\",\n                    \"type\": \"Button\",\n                    \"id\": \"7910f3d2-de5d-45b5-b5f8-33afa0697ac0\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelect_3\",\n                    \"type\": \"Button\",\n                    \"id\": \"581fb11a-893c-4ee8-ad6d-b3e797f59220\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelect_4\",\n                    \"type\": \"Button\",\n                    \"id\": \"942e42d7-bcf9-449f-83c8-4bda8792dfa9\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelect_5\",\n                    \"type\": \"Button\",\n                    \"id\": \"117b457c-64b3-486d-aac0-0492166bd07c\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelect_6\",\n                    \"type\": \"Button\",\n                    \"id\": \"d94aa5b5-a47a-4b71-8442-6af01d3a9760\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelect_7\",\n                    \"type\": \"Button\",\n                    \"id\": \"503c318b-5d88-4806-bf20-1d923c02a606\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelect_8\",\n                    \"type\": \"Button\",\n                    \"id\": \"f1d84a2b-1499-4156-83f0-46dc9c323d73\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelect_9\",\n                    \"type\": \"Button\",\n                    \"id\": \"79f18114-1d4b-4fae-a0bd-a072eb621169\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelect_0\",\n                    \"type\": \"Button\",\n                    \"id\": \"fc873d99-e278-4a65-92e6-01fd7319e87c\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Fishing\",\n                    \"type\": \"Button\",\n                    \"id\": \"0f04ba59-385c-458a-b636-44d6bfbab401\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Debug\",\n                    \"type\": \"Button\",\n                    \"id\": \"cabfa602-7b60-4ba6-ae2d-526a939d788d\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"DisposeItemInBackpack\",\n                    \"type\": \"Button\",\n                    \"id\": \"c7258237-b6c0-408c-b2b2-3e81f3a52a17\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Interact\",\n                    \"type\": \"Button\",\n                    \"id\": \"a2f78fb8-e47e-4e7e-999c-f002cab02ebf\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SpeedUp\",\n                    \"type\": \"Button\",\n                    \"id\": \"38d13554-46aa-4da1-a844-5c5156d67b12\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelectPrev\",\n                    \"type\": \"Button\",\n                    \"id\": \"bdc59761-e7b9-4a28-9490-b63426d74209\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelectNext\",\n                    \"type\": \"Button\",\n                    \"id\": \"cd46fadb-398d-474d-87ca-b4ffed8caa76\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ToggleMissionPanel\",\n                    \"type\": \"Button\",\n                    \"id\": \"2c803c82-10c1-48b8-bc7c-beb12bb3267f\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ToggleTechTree\",\n                    \"type\": \"Button\",\n                    \"id\": \"b706dd7d-846f-4245-bfd7-a81568819d85\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ToggleBackpack\",\n                    \"type\": \"Button\",\n                    \"id\": \"026c711f-21e0-48d1-9e14-a56ce1bf8aa1\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"Tap\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ToggleBackpackHold\",\n                    \"type\": \"Button\",\n                    \"id\": \"4c4ccdf0-77ef-4d54-844c-ea923ec68c9f\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"Hold\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ToggleMap\",\n                    \"type\": \"Button\",\n                    \"id\": \"2d4f2620-f732-4bf2-b921-c1dfc646db20\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ToggleMenu\",\n                    \"type\": \"Button\",\n                    \"id\": \"1af44ea2-ca61-412b-ba31-7ed30f8e7da8\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ToggleCollectionBook\",\n                    \"type\": \"Button\",\n                    \"id\": \"b4d39a79-3f68-4353-ba5d-75d0f76b9c17\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Cancel\",\n                    \"type\": \"Button\",\n                    \"id\": \"7ddd62ab-b0b0-48d4-9187-a3a7e4bffd75\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                }\n            ],\n            \"bindings\": [\n                {\n                    \"name\": \"\",\n                    \"id\": \"f02922d4-3a06-40d6-a0a6-4c1efd6b8451\",\n                    \"path\": \"<Keyboard>/space\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Jump\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"9c6b2853-743b-42e1-921e-608f11d633b9\",\n                    \"path\": \"<Keyboard>/s\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"JumpDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c8451b63-041e-4a8d-9b5d-0b6fa709822f\",\n                    \"path\": \"<Gamepad>/leftStick/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"JumpDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6cf689c6-80e8-47d5-8ade-e66fdb7056ae\",\n                    \"path\": \"<Keyboard>/leftShift\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Dash\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"Keyboard\",\n                    \"id\": \"dcca9f5b-dc91-4b5e-98f7-867c3677efa4\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"Up\",\n                    \"id\": \"71d31267-c167-44f2-892a-570fce1e6329\",\n                    \"path\": \"<Keyboard>/w\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Down\",\n                    \"id\": \"57956f67-6228-4e8a-83ba-3c2072a7c3f8\",\n                    \"path\": \"<Keyboard>/s\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Left\",\n                    \"id\": \"8aa23fa0-acf9-4801-93ba-45b9daf85e37\",\n                    \"path\": \"<Keyboard>/a\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Right\",\n                    \"id\": \"6986df10-04ef-4b35-8657-fd0381818e0d\",\n                    \"path\": \"<Keyboard>/d\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"GamePad\",\n                    \"id\": \"fecd08d1-fbdf-4879-8884-f1a8076fd571\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"76ecd1a1-e820-4b15-b591-b3a847e5180b\",\n                    \"path\": \"<Gamepad>/leftStick/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"a32746f4-a42f-42a1-a1ce-2eb4bce5ad21\",\n                    \"path\": \"<Gamepad>/leftStick/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"ad371f09-7ef1-4b30-a925-3441087c1be4\",\n                    \"path\": \"<Gamepad>/leftStick/left\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"299c3ff1-dff6-4e2a-9786-b52ee9376170\",\n                    \"path\": \"<Gamepad>/leftStick/right\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Additional [Keybord]\",\n                    \"id\": \"97483b03-6d1d-461d-9681-c228e3452148\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"c9e8a2b1-b6d3-4329-9f35-5f3282ad5754\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"8c74223d-4280-4139-9bd4-0ed52cb53262\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"33f47ac9-dd24-4c4f-91ef-47ab83ab9bad\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"b13ef63d-a2e0-45fc-ad07-8cad6a009da8\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Additional [GamePad]\",\n                    \"id\": \"c3444f7a-4249-4d18-b9b7-15a64ee1349a\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"b6e09f4d-918c-4b22-897b-24cf549b168d\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"767e89e2-c0cb-4e19-9b65-8df03da868df\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"28135baa-28ed-4c9e-8784-a316868f4084\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"dd53aae3-8812-4809-a23e-e64f4c7204e8\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6f60106f-811a-4a9f-a10b-f906edcfdcea\",\n                    \"path\": \"<Keyboard>/w\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"RoomInteract\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c5e4907e-0a2b-4afe-88e3-709b66225c96\",\n                    \"path\": \"<Gamepad>/leftStick/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"RoomInteract\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c92bf6a2-e6c6-4e1d-9412-b5a048da6dc2\",\n                    \"path\": \"<Mouse>/middleButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SwitchAutoFire\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"fe08c4d2-65ea-402c-89e6-cb04c8631d3b\",\n                    \"path\": \"<Keyboard>/r\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SwitchAutoFire\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"09a8b05f-61fe-4858-84a2-f444087989e2\",\n                    \"path\": \"<Gamepad>/rightStickPress\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SwitchAutoFire\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"19974fd2-bc92-4737-9931-eb3e51e5234d\",\n                    \"path\": \"<Mouse>/leftButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"UseTool\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b769a202-c6a7-4c6c-8d1f-4247ac7ab4c8\",\n                    \"path\": \"<Gamepad>/buttonWest\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"UseTool\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c73f8025-aa0f-4a76-8178-5a686f1c5169\",\n                    \"path\": \"<Keyboard>/1\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_1\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"8ed91cbb-55e7-4ce5-ba18-57715b2db2d6\",\n                    \"path\": \"<Keyboard>/2\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_2\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e06dd121-e209-4bf7-b70e-cf320783d4a6\",\n                    \"path\": \"<Keyboard>/3\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_3\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"783c6bbd-50f8-4ce4-8d8a-8f8ec9e15c15\",\n                    \"path\": \"<Keyboard>/4\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_4\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"70fcbbe1-93e4-4fd5-9dd1-1b23e809f542\",\n                    \"path\": \"<Keyboard>/5\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_5\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f12edbbe-4a1c-4479-b805-917e693aba73\",\n                    \"path\": \"<Keyboard>/6\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_6\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f08ade89-f8fe-4b2d-8460-5b1c69cadb9a\",\n                    \"path\": \"<Keyboard>/7\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_7\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"745f15cc-906b-4f63-9ee0-5195d2806098\",\n                    \"path\": \"<Keyboard>/8\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_8\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"dc5665bc-64b0-47d5-86e7-75ae1306b09a\",\n                    \"path\": \"<Keyboard>/9\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_9\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"0b512fe6-d833-4b8a-8693-8a225b5f5772\",\n                    \"path\": \"<Keyboard>/0\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_0\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c1b5406e-33b2-425f-9a0d-35f1ca40ba23\",\n                    \"path\": \"<Mouse>/rightButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"UseItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b8f3a803-55eb-455a-ba4a-9a8bca879ca0\",\n                    \"path\": \"<Keyboard>/space\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Fishing\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6a63bcc5-c7f0-4609-b125-d35127b9d72d\",\n                    \"path\": \"<Gamepad>/leftTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Dash\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"99f49353-ec9b-4316-a512-9a94f14f728c\",\n                    \"path\": \"<Keyboard>/z\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SelectedDrone\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"fbabaf69-2270-4f1a-a268-3816f5e5a1ac\",\n                    \"path\": \"<Gamepad>/rightTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SelectedDrone\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f18b0e0b-57fa-4247-bbca-773498d72318\",\n                    \"path\": \"<Keyboard>/x\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SelectedActive\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a10edb26-bd76-4381-81aa-907406847f5b\",\n                    \"path\": \"<Gamepad>/buttonNorth\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"UseItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"0ef713aa-0149-430d-bdc5-967f3e221e08\",\n                    \"path\": \"<Gamepad>/buttonEast\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Jump\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"8f6e8a55-15fb-4bcf-80b4-bf709a61d289\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Jump\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"80bf93c0-b090-4d5f-8895-54153409bed2\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Jump\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"01ad3e4f-197c-4991-9c9b-7d22d102426f\",\n                    \"path\": \"<Keyboard>/backquote\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Debug\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"8a35a85b-d4af-40dd-9cbc-74596f96f52c\",\n                    \"path\": \"<Keyboard>/f3\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Debug\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c2630fbc-b197-4d05-9a6e-d48cc5e4df2c\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"JumpDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"9c107319-1dc1-4796-b1fe-5fc46464306b\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"JumpDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"5c785c11-0683-41fd-b5ad-4a3a7791f3f8\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Dash\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b1df767c-d461-40a8-b363-0a616cc43f6f\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Dash\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b5887c29-5bd9-4f0f-9020-0e244a530c63\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"UseTool\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"5bd7406b-0775-4265-9768-63b8a6b4ff41\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"UseTool\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"308166dd-25e7-4631-b454-b50b7ba8a5f4\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"UseItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"aaa4432d-b70f-4518-b8d6-ce623ccd3c40\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"UseItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"fd5b561d-1369-45f0-8256-607608dd1bc6\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ScrollInventoryUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"d13df9b6-74ad-43ae-a3e9-ecd0d9d84bfe\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ScrollInventoryUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"248afade-2957-4e09-9636-a1cf2c44d0fb\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"RoomInteract\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6879c1c9-8434-4947-9c55-1f7930a236da\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"RoomInteract\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ad17bd76-379e-4607-b81f-cba630be6a3d\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SwitchAutoFire\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"723cf1fe-3e45-4032-8f10-af68da13a00b\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SelectedDrone\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7120e253-2ac0-49b7-a290-0d30c7491085\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"SelectedDrone\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"bf81a0a6-08fe-4282-b1e2-9fc595991802\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SelectedActive\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ebcd8586-4561-4ccc-81c4-a3b0babe1503\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SelectedActive\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6c7a8ed9-6875-4159-bff3-91f4aba3bdb3\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_1\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"58f40eef-4813-4eb5-beb6-635d33a79e2e\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelect_1\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"38f54240-849b-4934-b79b-7cb9334f7eb5\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_2\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a978ecc2-d75e-4a83-ab56-498c2ba18d3f\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelect_2\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"90748dce-53f8-4b4d-9084-c538e42c6ac8\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_3\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"d02fd1ef-1de0-4667-9264-0a9550de97bc\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelect_3\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"abb95a8d-2e12-4a22-a965-1324a215aa33\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_4\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"15e48d0c-e37f-4509-9a24-d0280b1e0efe\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelect_4\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"627deb69-e22d-4c0d-8d85-e15aa9c20f56\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_5\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"3dce7a82-c967-4771-a035-dededbb2cdcc\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelect_5\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7635ec35-d6a3-496c-a381-19cc41cb59e0\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_6\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"3e8b86c1-f869-421f-b6e8-9c0cadaea3eb\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelect_6\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7b260af9-1e91-4d98-bcc7-a03ae6d851c2\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_7\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"5da6f062-061e-4c1d-be72-c3ded65ee7e0\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelect_7\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"4c9a21ca-abcc-4ba0-afbe-67a83b21a4fe\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_8\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e6e527cb-4793-480a-91f2-bad2ff512912\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelect_8\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"5590965b-50c0-4c6b-b2f3-1948fa972c62\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_9\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a960b84d-2447-41b3-92c9-71f2e96ebcf9\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelect_9\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7bd55f12-7764-4cc6-8267-77ecdc48dad6\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelect_0\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"2ac29f61-ea2c-4ef1-bb06-4531f5f3bb14\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelect_0\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"1c7764b1-ae8b-45c8-90a5-854ac5da5989\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Fishing\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ed415cfa-e63d-4414-b77e-4fb86197eade\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Fishing\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"983bb95d-3c17-45e5-bb83-9e76fed809e6\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Fishing\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"Keyboard\",\n                    \"id\": \"1c5439da-3c9a-4d76-8992-96774b71b1a5\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"247204bc-c7f8-4083-b942-80c15f61937b\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"35b61385-a525-4723-84b3-5bb1bc382f29\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"7bab13d1-2e87-4b90-b57c-dfceae48504a\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"b69ac319-a3eb-4b6c-9378-b696de6537f6\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"GamePad\",\n                    \"id\": \"5735b43a-44e7-427b-8cfe-97348f329526\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"b19e0026-4946-4b70-8575-f21c50bdab4b\",\n                    \"path\": \"<Gamepad>/rightStick/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"1f4c31d3-2bd7-492b-9cfb-af9ce581be4b\",\n                    \"path\": \"<Gamepad>/rightStick/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"cf002eb1-912c-432e-93fc-4a02a0b54727\",\n                    \"path\": \"<Gamepad>/rightStick/left\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"aa936546-7c89-43d9-bb12-38c6ed8167d8\",\n                    \"path\": \"<Gamepad>/rightStick/right\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Additional [Keybord]\",\n                    \"id\": \"61aec725-bf24-4111-a5f0-f757f30e09d5\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"d25df3a3-23cf-4475-9342-c3edc435181d\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"daaa85fe-06aa-4915-b8ab-230751551c6c\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"afa058d6-0185-4cb7-bc32-d0c44f53d512\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"bb9a2891-20d7-4ad2-a146-70fa069223c0\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Additional [GamePad]\",\n                    \"id\": \"c72cca69-192e-4555-a10d-861e24561505\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"58fe3607-5db1-4d44-afde-58061b9604b5\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"e47a9868-d9ac-405a-b989-820ad8c612a9\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"6fafff50-5d96-4d24-b8f8-7b43eea83069\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"e9681042-2d0a-42fc-b252-125940579546\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AssistMove\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"38b635b3-7528-4f7a-82ec-860b14491f66\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"DisposeItemInBackpack\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"4a502137-5c16-4ea6-a38e-938d1308979f\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"DisposeItemInBackpack\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"409a0cb6-1621-4d8d-94cc-87b2344ebd8c\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"DisposeItemInBackpack\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c1f3e2ed-f947-4c20-94c1-e82c08b3f635\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"DisposeItemInBackpack\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"2301caa3-e361-4ea4-b7c5-7f9e31cd33c9\",\n                    \"path\": \"<Keyboard>/e\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Interact\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ac401102-0b11-4a6e-837d-1e9f3a28f0b2\",\n                    \"path\": \"<Gamepad>/buttonSouth\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Interact\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b21598a1-964f-4c0f-8b05-8c13f0b16984\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Interact\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"dfe20744-40c0-47d9-b980-428ab6e578bd\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Interact\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"69388f9b-9d98-451b-a4e9-b2ce3170cdad\",\n                    \"path\": \"<Mouse>/rightButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SpeedUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ca671116-4cba-4125-92d5-e65aa629014c\",\n                    \"path\": \"<Keyboard>/escape\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SpeedUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"45e1c99c-0074-4c26-8dff-502d6e1f10c0\",\n                    \"path\": \"<Gamepad>/buttonEast\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SpeedUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"9680389c-15cb-4be5-8481-cd4d24c018a3\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SpeedUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"17eb39db-3783-4ddb-9042-2856259a82a3\",\n                    \"path\": \"<Mouse>/scroll/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelectPrev\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"10b603b1-068e-4e95-a1dd-05eb545bf53e\",\n                    \"path\": \"<Keyboard>/c\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelectPrev\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7e20c595-c8ff-4520-8d03-363e8a5160c8\",\n                    \"path\": \"<Gamepad>/leftShoulder\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelectPrev\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"32273fdb-b1fc-479f-853e-4bdbb7a24005\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelectPrev\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"8fafbd53-561a-4afc-b225-8e52eadb6852\",\n                    \"path\": \"<Mouse>/scroll/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelectNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"3e47fe2f-9298-4323-8eef-e9f212243664\",\n                    \"path\": \"<Keyboard>/v\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelectNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"49b0bb77-8f11-49c3-b065-f62c010eeac1\",\n                    \"path\": \"<Gamepad>/rightShoulder\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelectNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"53e50306-fd09-422c-a3ca-ae1788402b0a\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelectNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"fdcd413d-5559-41fb-8f82-fb97332ac2e6\",\n                    \"path\": \"<Keyboard>/q\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleMissionPanel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"62321750-bc13-4f8d-9ee8-d5c92a7784c4\",\n                    \"path\": \"<Gamepad>/dpad/left\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleMissionPanel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b47aa4ed-d50b-470b-8a55-22b73c1cd50e\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleMissionPanel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"97f00e8d-af9a-44c8-89d2-c47432732855\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleMissionPanel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"9d8e911a-3ff4-491e-84b9-7c908f9109e9\",\n                    \"path\": \"<Keyboard>/t\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleTechTree\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7273c936-d1a0-43e6-8051-3eb5d49db953\",\n                    \"path\": \"<Gamepad>/dpad/right\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleTechTree\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"8f79893f-3c24-4368-8a1a-541838258be5\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleTechTree\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"0daa02f5-ccf8-4053-932c-d05998654e46\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleTechTree\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"adfbcf7e-aa91-464f-a309-09eefbc5e372\",\n                    \"path\": \"<Keyboard>/b\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleBackpack\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"2be9b142-a679-4f94-ba62-7a0edbdc55c6\",\n                    \"path\": \"<Gamepad>/dpad/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleBackpack\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"4b6dbd08-a59e-44c8-ad5f-fd5eb520f2c5\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleBackpack\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e49c27b5-25c0-4020-99dd-98aa749332f4\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleBackpack\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6af44cc0-dbdd-4ce1-a100-05c297808a7d\",\n                    \"path\": \"<Keyboard>/b\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleBackpackHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"08a53396-5239-4df2-bada-903e57d259eb\",\n                    \"path\": \"<Gamepad>/dpad/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleBackpackHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"1d2e3628-1d35-4eec-9390-97abfec908b4\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleBackpackHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"048c979f-d8f6-4005-b290-5d6cea2226ba\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleBackpackHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"72ac79af-13b2-4784-b4b1-fb65966c583e\",\n                    \"path\": \"<Keyboard>/escape\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleMenu\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e48ae4b0-b553-4b74-ac7c-ab5bf700481e\",\n                    \"path\": \"<Gamepad>/start\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleMenu\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"d5cdc7da-9437-4f6d-8ac7-b0c310f98f59\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleMenu\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"953ce4f6-0e77-425f-b88d-7ea1850f327a\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleMenu\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"aa8c76cc-4af4-4449-a2c0-dc213c2f680b\",\n                    \"path\": \"<Keyboard>/m\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleMap\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"2f464ec7-4bba-44a2-b1d0-ba4abfa95676\",\n                    \"path\": \"<Gamepad>/select\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleMap\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"3295302a-427c-4eec-9b32-07503571a44e\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleMap\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7409ec21-6fb2-45ac-bb7c-62342f67d58d\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleMap\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"5f26b48c-9cee-42f4-a39e-ef1bba9d53c7\",\n                    \"path\": \"<Keyboard>/backquote\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleCollectionBook\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"964320bd-913f-4964-84e1-58d0f8e8551c\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleCollectionBook\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6de2e001-5836-4b4f-aa79-5e06c7fd0531\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleCollectionBook\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"435e1a9b-6b3f-48e7-903b-26be31493adf\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleCollectionBook\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e0f37d1b-f73f-4afc-b43c-0ab5d493dedc\",\n                    \"path\": \"<Keyboard>/tab\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ScrollInventoryDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"9123b0c1-c6ac-41c8-a473-aecffc57aacc\",\n                    \"path\": \"<Gamepad>/dpad/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ScrollInventoryDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"0643df8e-df52-4a6c-a566-736d42f009ca\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ScrollInventoryDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"06230d59-f87d-4c85-854a-ccabd8498136\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ScrollInventoryDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"4a8022a1-604e-4975-aef1-3130cacaee1f\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ScrollInventoryUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e51ede5b-3931-430e-85e5-c1e116538f08\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ScrollInventoryUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"5d5eba17-1de9-4865-82d3-e9a0e0ac63b0\",\n                    \"path\": \"<Keyboard>/escape\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Cancel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f2e67c21-8dea-4180-a073-ec2243d21af0\",\n                    \"path\": \"<Gamepad>/buttonEast\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Cancel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"5be569c6-b378-4d46-9d4e-cbd4c852d073\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Cancel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b97c00ec-56ce-44d5-85a3-4c6065ba610b\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Cancel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                }\n            ]\n        },\n        {\n            \"name\": \"BuilderInput\",\n            \"id\": \"cc736868-87a7-4f7e-af11-fa10bf5aaf64\",\n            \"actions\": [\n                {\n                    \"name\": \"D-PadUp\",\n                    \"type\": \"Button\",\n                    \"id\": \"cf9be8bc-6db7-42bd-8a0d-1eb32d85a7ec\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"D-PadDown\",\n                    \"type\": \"Button\",\n                    \"id\": \"8711af3c-5b22-41b1-a057-f165f6f74534\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"D-PadLeft\",\n                    \"type\": \"Button\",\n                    \"id\": \"44b7537c-43f0-448f-85f6-42ad0c661625\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"D-PadRight\",\n                    \"type\": \"Button\",\n                    \"id\": \"323e82dc-6502-47ee-b2f8-777d8991dad9\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"BuilderLast\",\n                    \"type\": \"Button\",\n                    \"id\": \"dacce759-bc6a-4493-b079-85f9d30e86fc\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"BuilderNext\",\n                    \"type\": \"Button\",\n                    \"id\": \"db35f5c7-5e15-4c61-adbe-8bc6a832bf95\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"BuilderSelected\",\n                    \"type\": \"Button\",\n                    \"id\": \"b92364fe-1635-4089-b004-0d9225d4e9c1\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"BuilderRevocation\",\n                    \"type\": \"Button\",\n                    \"id\": \"a8b93b46-7c80-4689-bc98-f37b3c14edb7\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"BuilderDismantle\",\n                    \"type\": \"Button\",\n                    \"id\": \"0aab91ea-dd37-4cb1-8ffa-006546d334b2\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"Tap\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"BuilderDismantleHold\",\n                    \"type\": \"Button\",\n                    \"id\": \"0eb65872-0a20-40b5-8c9c-00f4b3cfb6aa\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"Hold\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"BuilderRotate\",\n                    \"type\": \"Button\",\n                    \"id\": \"3ca73212-a6f1-4900-a06e-d962e6291273\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"BuilderSwitch\",\n                    \"type\": \"Button\",\n                    \"id\": \"aac6c914-3e68-4a12-9a0b-11a131537267\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"BuilderSwitchInventory\",\n                    \"type\": \"Button\",\n                    \"id\": \"d73fb7df-ce98-4649-b087-499b08100e1d\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                }\n            ],\n            \"bindings\": [\n                {\n                    \"name\": \"\",\n                    \"id\": \"6d1c8ff8-be7d-48a6-9afe-e54742977602\",\n                    \"path\": \"<Mouse>/rightButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderRevocation\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"2beacf01-c020-44e4-b6c8-c1108c178cf6\",\n                    \"path\": \"<Gamepad>/buttonEast\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderRevocation\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7bdb22f7-1955-409d-8972-e29a3a6f123a\",\n                    \"path\": \"<Mouse>/rightButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderDismantle\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"82390f54-83c9-4a7e-a887-a19c46a2234d\",\n                    \"path\": \"<Gamepad>/buttonNorth\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderDismantle\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7c8e6ba5-89e1-4e4b-aca6-9014cc7aaa7f\",\n                    \"path\": \"<Keyboard>/r\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderRotate\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"af8319b4-807e-4e8f-8d2e-9ab65c5bad39\",\n                    \"path\": \"<Gamepad>/buttonWest\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderRotate\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e00b5f6b-2aa8-4ed2-85f5-d15294f95465\",\n                    \"path\": \"<Gamepad>/leftStickPress\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderSwitch\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b5e4fae4-144d-420e-bb31-6108b923519c\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderSwitch\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a61d46b0-dabf-4eae-a8fc-9d37c4d47ba4\",\n                    \"path\": \"<Gamepad>/dpad/right\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"D-PadRight\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a4b5cf79-7d54-4fc1-bfdc-e6160154f784\",\n                    \"path\": \"<Gamepad>/dpad/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"D-PadDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f982df12-8687-4942-93eb-5b3e49add37e\",\n                    \"path\": \"<Keyboard>/e\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"834b58e4-d5b6-4f30-b94b-fdd44108cd10\",\n                    \"path\": \"<Gamepad>/rightTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f2b89b92-931a-44c3-9fb2-1941ad1175bf\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f3d5b75c-c70e-4fc6-9140-eb72d79f63c8\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"839c305e-908e-4eb4-ba17-11bbaf83da5b\",\n                    \"path\": \"<Mouse>/leftButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderSelected\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b4045ad0-c07e-4a48-bf72-323808ed7cae\",\n                    \"path\": \"<Gamepad>/buttonSouth\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderSelected\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"039e063c-d5a2-4d79-99fe-d5763d29ca83\",\n                    \"path\": \"<Gamepad>/dpad/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"D-PadUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"48027522-c41e-4928-8749-c95a9ee48151\",\n                    \"path\": \"<Gamepad>/dpad/left\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"D-PadLeft\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"70a0d25a-b33f-4b4d-abcb-5355ed56955d\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderSelected\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"be702b53-1aed-4356-a58d-f20c41b9b728\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderSelected\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"09fe534e-58ef-4eb9-9143-578edf4e331c\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderRevocation\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"96b43b3b-b0a6-46d7-9bbe-57f0bb58e052\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderRevocation\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"497a85a2-55bb-418d-9f0b-0cb9f0a0ad96\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderDismantle\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"39a56b3b-160f-4d95-88fc-60debc64747d\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderDismantle\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"27279b79-281c-4771-bbcc-48a425760b1e\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderRotate\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f5d7f78e-822d-4c93-bf35-0d1b8b7a6a43\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderRotate\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b6c9250c-c6c5-40e0-a90d-e55618e9bd57\",\n                    \"path\": \"<Mouse>/middleButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderSwitchInventory\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ae3d2f52-34db-4964-a1fb-242f97b62fe2\",\n                    \"path\": \"<Gamepad>/rightStickPress\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderSwitchInventory\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c56004bc-6893-4987-bef8-ada6fb00468e\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderSwitchInventory\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"3df6563b-68a5-4504-840b-0bcbfd783dc4\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderSwitchInventory\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"2d8e8a9f-f794-46ad-8941-8800f4f39319\",\n                    \"path\": \"<Mouse>/rightButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderDismantleHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c3f797e8-1753-4e0e-b982-efb7a34a8337\",\n                    \"path\": \"<Gamepad>/buttonNorth\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderDismantleHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6d3bb7b9-eef0-48a5-8cd5-827b0e5102e5\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderDismantleHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"31fd3e9f-1fa0-4669-a247-9a8a9e751b1c\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderDismantleHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"3bc30234-b24a-4c93-80a7-1ef2c64c06b4\",\n                    \"path\": \"<Keyboard>/q\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderLast\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"d8be2a61-7063-4030-a944-064f24f4511d\",\n                    \"path\": \"<Gamepad>/leftTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderLast\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7b067a4a-91f7-46b9-b970-4b4b6244c0d2\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"BuilderLast\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"29d0cf9f-73e2-4026-a07a-37ed0c779c20\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"BuilderLast\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                }\n            ]\n        },\n        {\n            \"name\": \"BaseInput\",\n            \"id\": \"fb0de463-d75b-42b5-b215-b2c86e9fa8ef\",\n            \"actions\": [\n                {\n                    \"name\": \"Move\",\n                    \"type\": \"Value\",\n                    \"id\": \"f3cebdb2-aa23-4d40-931a-fc7b55da616d\",\n                    \"expectedControlType\": \"Vector2\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"MoveLeft\",\n                    \"type\": \"Button\",\n                    \"id\": \"acffd6a7-b34c-424c-bb02-0e1b29febbf9\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"MoveRight\",\n                    \"type\": \"Button\",\n                    \"id\": \"8e2ecb29-bf3f-4256-9fea-7fe9eca8372a\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"MoveUp\",\n                    \"type\": \"Button\",\n                    \"id\": \"c4476d6a-5a34-494e-9542-2086dfbcf588\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"MoveDown\",\n                    \"type\": \"Button\",\n                    \"id\": \"36db2b7a-8589-417f-b353-7c77c7a4034d\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"MoveLast\",\n                    \"type\": \"Button\",\n                    \"id\": \"d4d77a97-1501-49a6-8711-6da130a50250\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"MoveNext\",\n                    \"type\": \"Button\",\n                    \"id\": \"a2e75ee5-f3ca-48b1-be5c-f507d2d46cfb\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Cancel\",\n                    \"type\": \"Button\",\n                    \"id\": \"0cfd1c7b-8a98-4029-8e89-7f41ebe51477\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Confirm\",\n                    \"type\": \"Button\",\n                    \"id\": \"a3aeb093-0d62-4b3c-9315-f392454f484c\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"Tap\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ConfirmHold\",\n                    \"type\": \"Button\",\n                    \"id\": \"21ab5b58-aecf-4f88-83f7-4a984e98cc67\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"Hold\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Scroll\",\n                    \"type\": \"Value\",\n                    \"id\": \"f1c1aa06-d6fc-4089-9ac6-5d58626c42a9\",\n                    \"expectedControlType\": \"Vector2\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"PageUp\",\n                    \"type\": \"Button\",\n                    \"id\": \"89682718-7f39-4fbf-a443-ec6c899304fd\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"PageDown\",\n                    \"type\": \"Button\",\n                    \"id\": \"f72ded6c-ca0c-486d-b321-ce621d5b7bc2\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SplitItem\",\n                    \"type\": \"Button\",\n                    \"id\": \"73fcf8a8-bb36-49fe-8f05-fde283c1c980\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SortItem\",\n                    \"type\": \"Button\",\n                    \"id\": \"fb654ea9-116a-4679-9d25-04bf2e94f085\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"Tap\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SortItemHold\",\n                    \"type\": \"Button\",\n                    \"id\": \"da47d5c6-017f-46c5-91a2-952ec767e50b\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"Hold\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"LockItem\",\n                    \"type\": \"Button\",\n                    \"id\": \"ff6a0d5a-6d66-4d02-be8f-4d4a32d99487\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"DisposeItem\",\n                    \"type\": \"Button\",\n                    \"id\": \"58723fc6-78a2-488c-b150-22f951a52d6c\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"Tap\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"DisposeItemHold\",\n                    \"type\": \"Button\",\n                    \"id\": \"841453d7-22dd-4435-abb3-19e12f399ea7\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"Hold\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"DestroyItem\",\n                    \"type\": \"Button\",\n                    \"id\": \"1a8f1d1f-f41f-40bd-806e-c653cd0f94de\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"PutMaxItem\",\n                    \"type\": \"Button\",\n                    \"id\": \"8f2e6272-f647-488f-8c43-2472bcdd2fac\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"Tap\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"PutMaxItemHold\",\n                    \"type\": \"Button\",\n                    \"id\": \"0f1db8e8-5873-4d38-8957-732f0dbada59\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"Hold\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"PutAllItem\",\n                    \"type\": \"Button\",\n                    \"id\": \"7ad94714-b510-4301-8d4d-8c13d0f7d8c3\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SubmitItem\",\n                    \"type\": \"Button\",\n                    \"id\": \"bb46cf5c-4b8a-441d-a7ca-a4948f558572\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SubOne\",\n                    \"type\": \"Button\",\n                    \"id\": \"ecbdb6c3-381b-4409-915d-b9ad13a8acbe\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"AddOne\",\n                    \"type\": \"Button\",\n                    \"id\": \"31efc4e4-0455-4a00-b079-2d3d4d45db13\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SubTen\",\n                    \"type\": \"Button\",\n                    \"id\": \"a7198e9c-b421-41e1-afc4-ee1a97473420\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"AddTen\",\n                    \"type\": \"Button\",\n                    \"id\": \"90c33cdb-a066-45ee-bfbb-8a20793e6402\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SetMin\",\n                    \"type\": \"Button\",\n                    \"id\": \"7043b67c-2abe-45be-938d-3baab590e91c\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SetMax\",\n                    \"type\": \"Button\",\n                    \"id\": \"b4aa4997-b01c-4f69-90da-2caff403c60b\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ContinueDialogue\",\n                    \"type\": \"Button\",\n                    \"id\": \"e37597ae-bbad-4753-9660-105bad1294be\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"ToggleDialogueHistory\",\n                    \"type\": \"Button\",\n                    \"id\": \"e6c3b03c-2136-48af-b4ee-9989b9ea4fe5\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"AssistSplit\",\n                    \"type\": \"Button\",\n                    \"id\": \"27d693aa-1fb2-4b09-aa57-74c28616815f\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Interact\",\n                    \"type\": \"Button\",\n                    \"id\": \"4666f292-d298-46c4-b16a-0c3d3eef34b3\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SpeedUp\",\n                    \"type\": \"Button\",\n                    \"id\": \"70efe6d6-f666-4605-bbde-08d7d00f2800\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelectPrev\",\n                    \"type\": \"Button\",\n                    \"id\": \"edfff60e-be69-4bde-85eb-4e55f8a9b1c7\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"QuickSelectNext\",\n                    \"type\": \"Button\",\n                    \"id\": \"b6e63df2-16cc-481e-8042-62f21733dd17\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ToggleMissionPanel\",\n                    \"type\": \"Button\",\n                    \"id\": \"fe485cb1-291c-446f-ba3b-8b7a17a69ddf\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ToggleTechTree\",\n                    \"type\": \"Button\",\n                    \"id\": \"da88fd40-6dec-4301-a3f3-04a28ee591fa\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ToggleBackpack\",\n                    \"type\": \"Button\",\n                    \"id\": \"6abb8cfc-7ded-4225-ab12-7f64bc4fe7a9\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ToggleMap\",\n                    \"type\": \"Button\",\n                    \"id\": \"00ee0001-f551-4471-9906-9b7e72c46df5\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ToggleMenu\",\n                    \"type\": \"Button\",\n                    \"id\": \"b877fc7d-5693-4d0a-976b-3b96094f1f81\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ToggleCollectionBook\",\n                    \"type\": \"Button\",\n                    \"id\": \"81fb53a4-20eb-4eb4-be3a-cddabbd7b7e4\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"MiscellaneousFunction\",\n                    \"type\": \"Button\",\n                    \"id\": \"a62be690-279a-4492-a220-06e79f3ed2bb\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                }\n            ],\n            \"bindings\": [\n                {\n                    \"name\": \"\",\n                    \"id\": \"83f24a17-d72a-4328-8fbf-572bd2d7c72f\",\n                    \"path\": \"<Keyboard>/w\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MoveUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ac3e114a-b51b-4bc9-94bf-daa1a4e01cbb\",\n                    \"path\": \"<Keyboard>/upArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MoveUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"4db56e32-4227-40e1-a01e-f74948efcaab\",\n                    \"path\": \"<Gamepad>/dpad/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MoveUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"93bb0bbe-5bc5-4256-b395-4abcfaf11706\",\n                    \"path\": \"<Gamepad>/leftStick/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MoveUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"440d8eb0-66b2-4f40-b3b1-2e8df72bacef\",\n                    \"path\": \"<Keyboard>/s\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MoveDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f0a1b64d-7748-4c07-8250-1db23bb11628\",\n                    \"path\": \"<Keyboard>/downArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MoveDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"05136cd8-16f9-430b-994d-987beb79b10d\",\n                    \"path\": \"<Gamepad>/dpad/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MoveDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"4dff74a3-1e21-4b97-a8e8-46775b732fb4\",\n                    \"path\": \"<Gamepad>/leftStick/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MoveDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"177bf0ed-3dbb-4844-a5b7-87c2efe3a518\",\n                    \"path\": \"<Keyboard>/q\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MoveLast\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7ce9c59a-a581-431d-81c3-5e364c567ed1\",\n                    \"path\": \"<Gamepad>/leftShoulder\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MoveLast\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f64a6f7f-ee59-454b-b17b-be348a851359\",\n                    \"path\": \"<Keyboard>/e\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MoveNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"3ab62e60-3c1e-4ab3-a1ba-b7c9dfc0e826\",\n                    \"path\": \"<Gamepad>/rightShoulder\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MoveNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"415c0c33-6941-4e16-adfb-90ffcbf61870\",\n                    \"path\": \"<Keyboard>/escape\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Cancel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"42aa7fc4-1669-4e80-8c9c-dedadd3357f5\",\n                    \"path\": \"<Gamepad>/buttonEast\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Cancel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c1d84ed8-171c-4e01-8833-4c73a24af264\",\n                    \"path\": \"<Keyboard>/space\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Confirm\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"1348fde2-02f1-4c82-848c-53206808b877\",\n                    \"path\": \"<Keyboard>/enter\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Confirm\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f2de84f1-4858-4931-8aa0-ec1598549e18\",\n                    \"path\": \"<Gamepad>/buttonSouth\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Confirm\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"69bfb733-e9c6-43e0-a3d7-bf083d4e3a43\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Confirm\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ba386238-3b29-4bee-afc9-df04f7f22237\",\n                    \"path\": \"<Keyboard>/z\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"PageUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"46a44f16-eb2e-4556-9b88-3c2fc3cfcf69\",\n                    \"path\": \"<Gamepad>/leftTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"PageUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"afef7d8c-ff98-4d83-a8a4-57419f9a10f0\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"PageUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"75466a9c-28e4-46ee-b95d-f6837e827240\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"PageUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ac733d38-fecc-4f45-8a5a-78dfb92e41e0\",\n                    \"path\": \"<Keyboard>/x\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"PageDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"9645df78-655e-45f8-933d-ded05f4e7a97\",\n                    \"path\": \"<Gamepad>/rightTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"PageDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"98737c33-2688-439d-8575-b30c44351516\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"PageDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"db4dba85-f3ee-43ab-aba9-a72865c1845c\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"PageDown\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"Keyboard\",\n                    \"id\": \"42a863cf-d3cb-4be9-b51d-5173b5317d8a\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"ea5de4ec-9fb5-4bd8-82cb-08931f1506aa\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"f69b84a5-58b3-4794-8468-c57a7cda989a\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"84027c75-1260-44e3-b031-776838310600\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"a2106a5c-4f60-4766-a356-a14ad7d718aa\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Gamepad\",\n                    \"id\": \"fe216fa4-c8fa-412d-8708-ad3c41330a11\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"67771214-c5aa-4c41-b09b-fdc801d61fb0\",\n                    \"path\": \"<Gamepad>/rightStick/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"babb0a60-a537-4f7f-b72b-793190d87492\",\n                    \"path\": \"<Gamepad>/rightStick/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"9b312d62-9a09-40b6-91fe-73faff404065\",\n                    \"path\": \"<Gamepad>/rightStick/left\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"acf7dfe8-7e19-425c-8914-815e199a769d\",\n                    \"path\": \"<Gamepad>/rightStick/right\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Additional [Keyboard]\",\n                    \"id\": \"e51a873c-2f0c-4f5f-b7e9-1449d1b3f045\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"cb9441fb-5415-4688-8e64-a72acf15db8c\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"7db6ab6e-0e58-4d0a-a993-3092dc63c993\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"05bfac55-98d1-431b-97f4-24d9a368af5d\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"013d65c6-349b-4f95-9870-01cfb6528433\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Additional [GamePad]\",\n                    \"id\": \"b3e0b006-7566-4292-a367-473b11d4879b\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"8e91d6f7-5734-475d-87c3-979fe8d6ad98\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"24dea68c-bb5d-469b-97bd-52588ed1bfc4\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"1a5a2a94-9694-4819-bd31-20bcdc901554\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"43d4dcd8-484a-44d9-be81-66bcd6b52d9b\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"301c445c-d243-4716-ba67-5542c09824cd\",\n                    \"path\": \"<Mouse>/scroll\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Scroll\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"cfe46ee2-65fa-4c4a-97f0-42a5e728bbfa\",\n                    \"path\": \"<Keyboard>/v\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SplitItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"d41af82f-39bf-420c-846a-623551b8164b\",\n                    \"path\": \"<Gamepad>/buttonWest\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SplitItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a7dfd209-446c-48e1-85ab-c3e36ffd4f04\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SplitItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"dbe9e8fc-4af1-432c-b6a4-29477f8be014\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SplitItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a4e4b858-223f-4f95-81ce-910d59e820f6\",\n                    \"path\": \"<Keyboard>/r\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SortItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"4071a559-dc16-43ad-acf6-9f7481be0784\",\n                    \"path\": \"<Gamepad>/buttonNorth\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SortItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"4c374227-2da4-4d9e-a7a8-fedc20bf3276\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SortItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"53bf3fb3-acfd-4844-b8d3-052ad0747569\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SortItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e1124389-092d-4b32-b969-62f7e025e2b8\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"DestroyItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"72ca0dd1-3975-4d89-ae97-c4278e99d9d9\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"DestroyItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6acbddac-ed1f-4eaf-9633-07a7b5099dbc\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"DestroyItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c63bb458-bb08-40aa-aee2-03e75ebd6b07\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"DestroyItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"cac106ab-eb03-441a-9e79-26b592dd5f18\",\n                    \"path\": \"<Keyboard>/f\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"DisposeItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7ec32bae-1466-420b-8a87-dad79c76bccd\",\n                    \"path\": \"<Gamepad>/leftTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"DisposeItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f3abc399-7c05-4c7c-8097-9a2a6e50a0f0\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"DisposeItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"2c57ee54-768b-443c-8d05-69f714c98dd8\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"DisposeItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"33e032b5-a91d-4848-97b9-8e74f132ac79\",\n                    \"path\": \"<Keyboard>/c\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"PutMaxItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6b7af67f-4d1d-4838-ab16-2984fd8281c0\",\n                    \"path\": \"<Gamepad>/rightTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"PutMaxItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"9b6058dd-8e16-4e94-98fa-d84c531226ed\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"PutMaxItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"fc08c038-2b80-4a44-be02-c2476ab71cd4\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"PutMaxItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"76b55137-4adc-42b5-8bf5-97546890500d\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"PutAllItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6509673c-0aa7-4738-ac57-ee33c5e856b1\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"PutAllItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"83352f75-0c11-45cf-b6ef-e320bf2fa5f2\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"PutAllItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6c4ef04c-2b0b-4dc0-9885-9566e50c4a42\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"PutAllItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c4391cdc-5fd7-4c08-a98c-8e25abcaca45\",\n                    \"path\": \"<Keyboard>/f\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SubmitItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"512708b0-9d24-4f8e-983f-3fd70b13da51\",\n                    \"path\": \"<Gamepad>/leftTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SubmitItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a02c6051-9d01-4cda-8bab-2488d5e92075\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SubmitItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b877de0b-ba1e-4ec8-93a7-595504e16293\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SubmitItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"22fde755-d6f1-4db7-9272-892533855079\",\n                    \"path\": \"<Keyboard>/a\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SubOne\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"0e6bb2c1-c56b-4d9a-93b4-27fcf5ccf335\",\n                    \"path\": \"<Keyboard>/leftArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SubOne\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"4d9ba1c0-55c8-479f-839f-f61f6bdd1ca7\",\n                    \"path\": \"<Gamepad>/dpad/left\",\n                    \"interactions\": \"Press\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SubOne\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a118dbbd-2670-49ff-adc6-d81091ae9a7e\",\n                    \"path\": \"<Gamepad>/leftStick/left\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SubOne\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"75697080-0b2f-4089-85b2-b70b396b5589\",\n                    \"path\": \"<Keyboard>/d\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AddOne\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"9126f7d6-28d7-40c6-a55b-1c5cc228cf8a\",\n                    \"path\": \"<Keyboard>/rightArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AddOne\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"2bc79b7a-fac4-4333-a0d5-48f3dd8d2a9b\",\n                    \"path\": \"<Gamepad>/dpad/right\",\n                    \"interactions\": \"Press\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AddOne\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f8c0e104-2302-405c-ac21-80e5f9f88fc8\",\n                    \"path\": \"<Gamepad>/leftStick/right\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AddOne\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"2c26c9eb-3c29-48e2-8539-48fd25734239\",\n                    \"path\": \"<Keyboard>/w\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AddTen\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c88d14ff-8cfa-4e06-a437-08d043498a67\",\n                    \"path\": \"<Keyboard>/upArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AddTen\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"552964c3-fef4-48d6-937e-1a362001e653\",\n                    \"path\": \"<Gamepad>/dpad/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AddTen\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"d04b31e0-b1ea-4c75-b82d-40d5788e2b52\",\n                    \"path\": \"<Gamepad>/leftStick/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AddTen\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6fd6b3bc-bb1c-49ed-a011-c6c968d34125\",\n                    \"path\": \"<Keyboard>/s\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SubTen\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"20371a61-7442-4e64-90f9-d5b5251f7aa4\",\n                    \"path\": \"<Keyboard>/downArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SubTen\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"174aa565-fdeb-428a-b35a-c4efe4ab5c65\",\n                    \"path\": \"<Gamepad>/dpad/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SubTen\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f063e9fc-bca3-4dca-a7d0-479fe1dd44a6\",\n                    \"path\": \"<Gamepad>/leftStick/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SubTen\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"01a19efe-0711-475a-94c9-b635b86a7720\",\n                    \"path\": \"<Keyboard>/e\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SetMax\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ad66cd1e-ecb7-447c-8408-86bb5964a68d\",\n                    \"path\": \"<Gamepad>/rightShoulder\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SetMax\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"070fa2f2-ea5e-4b01-a5bd-f777311f949d\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SetMax\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"45d7f442-c628-4138-a315-f035c5582105\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SetMax\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6890cc81-4d98-4882-bf45-a671781725d7\",\n                    \"path\": \"<Keyboard>/q\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SetMin\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"4dee5ac4-f766-43fe-bf44-28632c8be208\",\n                    \"path\": \"<Gamepad>/leftShoulder\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SetMin\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"cc96da16-2a35-42c4-9694-aa8e078d7d54\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SetMin\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"384b54e4-ab34-4292-bee7-ea0874eace8b\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SetMin\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"bc2b6acd-d32d-46d0-96a0-ecb5b841bf15\",\n                    \"path\": \"<Mouse>/leftButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ContinueDialogue\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"8fc6495c-1700-4c21-809c-65a4dce9aa78\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ContinueDialogue\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"22fc8ab8-883e-4f6f-be21-e7333a1845f6\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ContinueDialogue\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ff7e953e-6e46-48f9-8c5e-1262a6a19189\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ContinueDialogue\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"Keyboard\",\n                    \"id\": \"68d53a36-9b24-41d3-9d89-27e31bd9a2b5\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"Up\",\n                    \"id\": \"e585bb2f-8ddf-4179-8f23-1d44a8ca6ad2\",\n                    \"path\": \"<Keyboard>/w\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Down\",\n                    \"id\": \"1b848d67-ddad-4a75-a219-94a2a6b28d0c\",\n                    \"path\": \"<Keyboard>/s\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Left\",\n                    \"id\": \"6914a746-0bbb-4df7-a3de-3c435f820210\",\n                    \"path\": \"<Keyboard>/a\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Right\",\n                    \"id\": \"780c19f0-d404-4f7b-9896-db35d3f4751c\",\n                    \"path\": \"<Keyboard>/d\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"GamePad\",\n                    \"id\": \"956dd8d5-cb8b-4893-bd1c-eea408c4d44b\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"f28a7547-b0ea-46e5-9341-d105aabb1fdb\",\n                    \"path\": \"<Gamepad>/leftStick/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"26f3d432-4497-4971-8b72-38d921ce50fb\",\n                    \"path\": \"<Gamepad>/leftStick/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"cef22706-c23b-4a95-9077-198be32ceb6b\",\n                    \"path\": \"<Gamepad>/leftStick/left\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"ef34cdc2-81e6-46fe-8e33-ad3346c22cc4\",\n                    \"path\": \"<Gamepad>/leftStick/right\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Additional [Keybord]\",\n                    \"id\": \"c5a40998-18d9-42ce-9cf3-62607e1b32ea\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"632b6295-8e19-4cbf-8a1e-e36a1f7718e4\",\n                    \"path\": \"<Keyboard>/upArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"0d304f8e-3758-4afa-914f-ef5e44b3288f\",\n                    \"path\": \"<Keyboard>/downArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"8433bb2e-fc53-494b-a47c-63c6aa9b5ecf\",\n                    \"path\": \"<Keyboard>/leftArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"66b05695-3b24-491c-beb6-7cc33746ce8d\",\n                    \"path\": \"<Keyboard>/rightArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"Additional [GamePad]\",\n                    \"id\": \"212ed4f0-0a2a-4cb5-a63a-a978e6b8c010\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"e7dfba13-2187-4424-a4a6-f45d06d8dd6f\",\n                    \"path\": \"<Gamepad>/dpad/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"bb04b9e0-a8b8-4926-ad4b-124d5f4910b6\",\n                    \"path\": \"<Gamepad>/dpad/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"7dd9c254-3cfb-4afd-8210-0e06e5481b31\",\n                    \"path\": \"<Gamepad>/dpad/left\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"782841b7-ba7e-4a80-8d2a-1d3cdcb5c9ac\",\n                    \"path\": \"<Gamepad>/dpad/right\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"23ce7b84-bbd3-4dfc-b587-4c94fe4cd58c\",\n                    \"path\": \"<Keyboard>/d\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MoveRight\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"16a9c069-5acc-4100-992c-4273efe9edec\",\n                    \"path\": \"<Keyboard>/rightArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MoveRight\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"381b4e6c-94cb-499a-9248-c81263659f55\",\n                    \"path\": \"<Gamepad>/dpad/right\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MoveRight\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c7ae7d90-bca9-47ac-8550-171ef1fcc121\",\n                    \"path\": \"<Gamepad>/leftStick/right\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MoveRight\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"2ca97afb-d82e-4b6d-9be1-47e40779422e\",\n                    \"path\": \"<Keyboard>/a\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MoveLeft\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"695be3ed-d941-4576-996f-a800b84810b4\",\n                    \"path\": \"<Keyboard>/leftArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MoveLeft\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"8c73af83-b205-401f-a699-9af0e70a17f6\",\n                    \"path\": \"<Gamepad>/dpad/left\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MoveLeft\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"af13d2aa-af5c-471e-bbda-9dc0863da16d\",\n                    \"path\": \"<Gamepad>/leftStick/left\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MoveLeft\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"cd628212-7994-4b07-9b7a-7f920e2f4b4f\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MoveLast\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"fe203063-f202-4738-ace3-534f5a122385\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MoveLast\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"4a78fe0d-6c6c-431b-b2ae-48098e8bbef4\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MoveNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a0fd4fa0-ea77-4c37-89dc-cab98529d231\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MoveNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"76e95500-7f44-4a99-8f49-3dc758af2dac\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Cancel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"73d95f61-ae1e-4711-b550-27f7448a1084\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Cancel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a7e3285a-2955-4660-a01a-8ea1d966a664\",\n                    \"path\": \"<Keyboard>/f\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"DisposeItemHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"fabc7320-a67b-4c6d-8f27-1395f00da37e\",\n                    \"path\": \"<Gamepad>/leftTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"DisposeItemHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"be8c6bfc-b470-40ed-b098-ca6269b0fe2b\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"DisposeItemHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"97844517-75ab-4863-a409-218f3bb856bd\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"DisposeItemHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"24bf8bcd-c806-4925-a3f8-a2a4ad69c35a\",\n                    \"path\": \"<Keyboard>/c\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"PutMaxItemHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"5fa81fcb-a051-4840-a4eb-c44efeeba8e6\",\n                    \"path\": \"<Gamepad>/rightTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"PutMaxItemHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"21fc0631-e876-4a7f-b375-720845c7de10\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"PutMaxItemHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"782dfa77-9926-4a51-8e28-7b22c05c52d7\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"PutMaxItemHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"31ee511a-6e0c-4d7c-8007-4628a12ae0ea\",\n                    \"path\": \"<Keyboard>/q\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleMissionPanel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c44a50e6-8d3a-40cc-80fc-7902d6f98a1c\",\n                    \"path\": \"<Gamepad>/dpad/left\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleMissionPanel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"d5eec0d7-3076-40ae-ae90-673788f84db3\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleMissionPanel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"1c7d945c-187f-4efb-ac2b-b3e447d0dda9\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleMissionPanel\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"1ae14ca3-8282-4b26-8ac1-c027c001f666\",\n                    \"path\": \"<Keyboard>/t\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleTechTree\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"db529801-cfbb-4356-a4a7-cdbb2441b12c\",\n                    \"path\": \"<Gamepad>/dpad/right\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleTechTree\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"55d470fc-350c-4eb5-aa0c-b80efec99f7f\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleTechTree\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"50454ff4-debb-4855-b5df-3b6d0a7d9a91\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleTechTree\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ae77568a-05c8-449b-bea9-5da16505e923\",\n                    \"path\": \"<Keyboard>/b\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleBackpack\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ff5eb396-1bc0-489c-809e-74ae20fa77d7\",\n                    \"path\": \"<Gamepad>/dpad/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleBackpack\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ba7fb75b-dd0f-42ae-b5fb-d16a1261d86f\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleBackpack\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"26a0669d-41b9-4412-a811-4cd12fc10520\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleBackpack\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6bab58eb-f6f9-49fe-b813-e6aad3402fb2\",\n                    \"path\": \"<Keyboard>/m\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleMap\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a88fa7cb-ede9-4c18-bc2e-dfd622abf372\",\n                    \"path\": \"<Gamepad>/select\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleMap\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"2724b416-2dae-4637-9e15-ba4771b664d6\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleMap\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"2d007b69-7c1b-44c7-a627-af8264374ba8\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleMap\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"1dc3de2b-1048-45be-adf7-7d9f3d2c3baf\",\n                    \"path\": \"<Keyboard>/escape\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleMenu\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"fd6e024c-a61d-45e7-91c4-fe0bcbb363b7\",\n                    \"path\": \"<Gamepad>/start\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleMenu\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a95a8953-5358-4604-ba5a-1a7dbe4b2827\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleMenu\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"1e49f621-bc01-45bf-8d59-30479b26de32\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleMenu\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"fd649844-4fb2-4c67-aaaf-a1ab8e938f33\",\n                    \"path\": \"<Keyboard>/e\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Interact\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b3b8cf46-d779-4305-96db-db13637f16e9\",\n                    \"path\": \"<Gamepad>/buttonSouth\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Interact\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"85bfb8c7-8243-49a8-bd34-21fa57506d43\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Interact\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"00f91a28-9a09-4bec-b571-8f6599c6a014\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"Interact\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"d8bcd590-c85f-446e-82fa-6dcbe93d1d1d\",\n                    \"path\": \"<Mouse>/scroll/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelectPrev\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"bfcaee50-06de-447b-b37c-db01202e6d28\",\n                    \"path\": \"<Keyboard>/c\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelectPrev\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"da413a62-6e60-452a-92f0-08d8bf3017f9\",\n                    \"path\": \"<Gamepad>/leftShoulder\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelectPrev\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"4464655e-ab93-4c02-9393-47f8de15e4b4\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelectPrev\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"8719dc89-2cf9-4a20-848a-f58c53e1c7ec\",\n                    \"path\": \"<Mouse>/rightButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SpeedUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ced54858-fe0c-46e3-bda8-43646ce98e52\",\n                    \"path\": \"<Keyboard>/escape\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SpeedUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b5b5a6ec-440d-4647-99d1-3b0f6a4561a3\",\n                    \"path\": \"<Gamepad>/buttonEast\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SpeedUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c3c2f5f5-3754-4980-8167-76ff2bb711f1\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SpeedUp\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"2978791c-ebd6-4add-b62c-583e5bb6c5b5\",\n                    \"path\": \"<Keyboard>/leftShift\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AssistSplit\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"08360530-4437-4fdb-87d5-4793e2c31f0f\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"AssistSplit\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7a5d4cd6-4201-4317-a1f1-efa74c99323a\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AssistSplit\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c201d653-08d6-40c5-a0b8-9f232395fa0e\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"AssistSplit\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"023d3d7e-048e-4e62-a9d4-79598ceba3e8\",\n                    \"path\": \"<Mouse>/scroll/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelectNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f380bc5d-c7db-4e49-b853-8682b0dfb923\",\n                    \"path\": \"<Keyboard>/v\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"QuickSelectNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f5965fe6-90bc-4d5d-aaac-8ab89659e2dc\",\n                    \"path\": \"<Gamepad>/rightShoulder\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelectNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"5458d2c3-f36f-4c58-b34e-024d71c690f0\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"QuickSelectNext\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f1a36271-8a01-41d2-8f92-f7d236150e22\",\n                    \"path\": \"<Keyboard>/space\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ConfirmHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6416ba6b-a48a-442c-9eb1-4283a3f88dcc\",\n                    \"path\": \"<Keyboard>/enter\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ConfirmHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"cd79fc42-fdd2-4899-93c2-6d372c7950c8\",\n                    \"path\": \"<Gamepad>/buttonSouth\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ConfirmHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"15896f80-a4bc-4ea0-8847-0607dc8b89e8\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ConfirmHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"3313ddd5-683c-41a2-ac0b-4ac64aa7b199\",\n                    \"path\": \"<Keyboard>/backquote\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleCollectionBook\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ed55c9b4-9d32-4140-b255-f1e1fbc3b993\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleCollectionBook\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a2bcadb7-263c-43bd-87dd-9ceda859d87c\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleCollectionBook\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"fbdac76c-627a-4688-8fb8-512bed795ac4\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleCollectionBook\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"fddee558-03ec-4880-ac47-b6e6445d53c1\",\n                    \"path\": \"<Keyboard>/q\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleDialogueHistory\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c6cd0931-d8fb-4ce1-88a1-902d552a3d4f\",\n                    \"path\": \"<Gamepad>/start\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleDialogueHistory\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"3efb5962-270b-4ef4-82ba-ac19a294e313\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"ToggleDialogueHistory\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ee2cd4f1-e9cb-4492-804c-07b032d7e1cc\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"ToggleDialogueHistory\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e581e0ba-97a9-44f4-99db-5afd8beee9fb\",\n                    \"path\": \"<Keyboard>/r\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SortItemHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a0bb9363-2d12-4282-a849-d887667b261f\",\n                    \"path\": \"<Gamepad>/buttonNorth\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SortItemHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e69089d1-92c8-4747-b9da-1954e42cb276\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"SortItemHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"1109fecf-546c-4360-83d7-84a436a33158\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"SortItemHold\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"71014bdf-1599-43b6-b367-7802a811452d\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"LockItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a5cdc78f-63af-4681-b7ec-5cf95ab9765b\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"LockItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ff05c7d6-daae-479e-b236-645b84f9ae2e\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"LockItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"41ff495a-d8a3-4fc0-ae4b-af0e258b2c20\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"LockItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e6b3e004-b29c-4e21-9a98-450f18701b20\",\n                    \"path\": \"<Mouse>/middleButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MiscellaneousFunction\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b37ea51e-65d8-4ad2-9cb2-64d368aac463\",\n                    \"path\": \"<Gamepad>/rightStickPress\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MiscellaneousFunction\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"6efca78c-63d6-4d01-919b-bae218973a00\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"MiscellaneousFunction\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"d591dd45-af46-4f63-a586-ff9c720e3523\",\n                    \"path\": \"\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"GamePad\",\n                    \"action\": \"MiscellaneousFunction\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                }\n            ]\n        },\n        {\n            \"name\": \"Global\",\n            \"id\": \"44e86a58-9ed5-4eae-af9f-7e043b97ea62\",\n            \"actions\": [\n                {\n                    \"name\": \"Point\",\n                    \"type\": \"PassThrough\",\n                    \"id\": \"650f331d-d790-4ed2-a777-fdf9546e4ed0\",\n                    \"expectedControlType\": \"Vector2\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"Click\",\n                    \"type\": \"PassThrough\",\n                    \"id\": \"93c6b05f-0c6d-4efa-8d5d-64315e40cfbd\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"RightClick\",\n                    \"type\": \"PassThrough\",\n                    \"id\": \"e2ed8426-b439-4148-9f32-c81a563b0ff1\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"CursorPosition\",\n                    \"type\": \"Value\",\n                    \"id\": \"81d19834-8bb7-4dd1-9f69-d11fb81e0cc0\",\n                    \"expectedControlType\": \"Vector2\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                }\n            ],\n            \"bindings\": [\n                {\n                    \"name\": \"\",\n                    \"id\": \"8c9f4045-25f4-4e45-a6ab-247f82da3843\",\n                    \"path\": \"<Mouse>/position\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"CursorPosition\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"5a42c923-9945-46d9-95dd-2c08740aea77\",\n                    \"path\": \"<Mouse>/leftButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Click\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"5bc4f2bb-5b18-48e2-a715-e330ff013683\",\n                    \"path\": \"<Mouse>/position\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"Point\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"fd33ca46-2a1f-4877-b77b-69f728fbb6eb\",\n                    \"path\": \"<Mouse>/rightButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"KeyboardMouse\",\n                    \"action\": \"RightClick\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                }\n            ]\n        }\n    ],\n    \"controlSchemes\": [\n        {\n            \"name\": \"KeyboardMouse\",\n            \"bindingGroup\": \"KeyboardMouse\",\n            \"devices\": []\n        },\n        {\n            \"name\": \"GamePad\",\n            \"bindingGroup\": \"GamePad\",\n            \"devices\": []\n        }\n    ]\n}");
		m_NormalInput = asset.FindActionMap("NormalInput", throwIfNotFound: true);
		m_NormalInput_Move = m_NormalInput.FindAction("Move", throwIfNotFound: true);
		m_NormalInput_Jump = m_NormalInput.FindAction("Jump", throwIfNotFound: true);
		m_NormalInput_JumpDown = m_NormalInput.FindAction("JumpDown", throwIfNotFound: true);
		m_NormalInput_Dash = m_NormalInput.FindAction("Dash", throwIfNotFound: true);
		m_NormalInput_UseTool = m_NormalInput.FindAction("UseTool", throwIfNotFound: true);
		m_NormalInput_UseItem = m_NormalInput.FindAction("UseItem", throwIfNotFound: true);
		m_NormalInput_ScrollInventoryUp = m_NormalInput.FindAction("ScrollInventoryUp", throwIfNotFound: true);
		m_NormalInput_ScrollInventoryDown = m_NormalInput.FindAction("ScrollInventoryDown", throwIfNotFound: true);
		m_NormalInput_AssistMove = m_NormalInput.FindAction("AssistMove", throwIfNotFound: true);
		m_NormalInput_RoomInteract = m_NormalInput.FindAction("RoomInteract", throwIfNotFound: true);
		m_NormalInput_SwitchAutoFire = m_NormalInput.FindAction("SwitchAutoFire", throwIfNotFound: true);
		m_NormalInput_SelectedDrone = m_NormalInput.FindAction("SelectedDrone", throwIfNotFound: true);
		m_NormalInput_SelectedActive = m_NormalInput.FindAction("SelectedActive", throwIfNotFound: true);
		m_NormalInput_QuickSelect_1 = m_NormalInput.FindAction("QuickSelect_1", throwIfNotFound: true);
		m_NormalInput_QuickSelect_2 = m_NormalInput.FindAction("QuickSelect_2", throwIfNotFound: true);
		m_NormalInput_QuickSelect_3 = m_NormalInput.FindAction("QuickSelect_3", throwIfNotFound: true);
		m_NormalInput_QuickSelect_4 = m_NormalInput.FindAction("QuickSelect_4", throwIfNotFound: true);
		m_NormalInput_QuickSelect_5 = m_NormalInput.FindAction("QuickSelect_5", throwIfNotFound: true);
		m_NormalInput_QuickSelect_6 = m_NormalInput.FindAction("QuickSelect_6", throwIfNotFound: true);
		m_NormalInput_QuickSelect_7 = m_NormalInput.FindAction("QuickSelect_7", throwIfNotFound: true);
		m_NormalInput_QuickSelect_8 = m_NormalInput.FindAction("QuickSelect_8", throwIfNotFound: true);
		m_NormalInput_QuickSelect_9 = m_NormalInput.FindAction("QuickSelect_9", throwIfNotFound: true);
		m_NormalInput_QuickSelect_0 = m_NormalInput.FindAction("QuickSelect_0", throwIfNotFound: true);
		m_NormalInput_Fishing = m_NormalInput.FindAction("Fishing", throwIfNotFound: true);
		m_NormalInput_Debug = m_NormalInput.FindAction("Debug", throwIfNotFound: true);
		m_NormalInput_DisposeItemInBackpack = m_NormalInput.FindAction("DisposeItemInBackpack", throwIfNotFound: true);
		m_NormalInput_Interact = m_NormalInput.FindAction("Interact", throwIfNotFound: true);
		m_NormalInput_SpeedUp = m_NormalInput.FindAction("SpeedUp", throwIfNotFound: true);
		m_NormalInput_QuickSelectPrev = m_NormalInput.FindAction("QuickSelectPrev", throwIfNotFound: true);
		m_NormalInput_QuickSelectNext = m_NormalInput.FindAction("QuickSelectNext", throwIfNotFound: true);
		m_NormalInput_ToggleMissionPanel = m_NormalInput.FindAction("ToggleMissionPanel", throwIfNotFound: true);
		m_NormalInput_ToggleTechTree = m_NormalInput.FindAction("ToggleTechTree", throwIfNotFound: true);
		m_NormalInput_ToggleBackpack = m_NormalInput.FindAction("ToggleBackpack", throwIfNotFound: true);
		m_NormalInput_ToggleBackpackHold = m_NormalInput.FindAction("ToggleBackpackHold", throwIfNotFound: true);
		m_NormalInput_ToggleMap = m_NormalInput.FindAction("ToggleMap", throwIfNotFound: true);
		m_NormalInput_ToggleMenu = m_NormalInput.FindAction("ToggleMenu", throwIfNotFound: true);
		m_NormalInput_ToggleCollectionBook = m_NormalInput.FindAction("ToggleCollectionBook", throwIfNotFound: true);
		m_NormalInput_Cancel = m_NormalInput.FindAction("Cancel", throwIfNotFound: true);
		m_BuilderInput = asset.FindActionMap("BuilderInput", throwIfNotFound: true);
		m_BuilderInput_DPadUp = m_BuilderInput.FindAction("D-PadUp", throwIfNotFound: true);
		m_BuilderInput_DPadDown = m_BuilderInput.FindAction("D-PadDown", throwIfNotFound: true);
		m_BuilderInput_DPadLeft = m_BuilderInput.FindAction("D-PadLeft", throwIfNotFound: true);
		m_BuilderInput_DPadRight = m_BuilderInput.FindAction("D-PadRight", throwIfNotFound: true);
		m_BuilderInput_BuilderLast = m_BuilderInput.FindAction("BuilderLast", throwIfNotFound: true);
		m_BuilderInput_BuilderNext = m_BuilderInput.FindAction("BuilderNext", throwIfNotFound: true);
		m_BuilderInput_BuilderSelected = m_BuilderInput.FindAction("BuilderSelected", throwIfNotFound: true);
		m_BuilderInput_BuilderRevocation = m_BuilderInput.FindAction("BuilderRevocation", throwIfNotFound: true);
		m_BuilderInput_BuilderDismantle = m_BuilderInput.FindAction("BuilderDismantle", throwIfNotFound: true);
		m_BuilderInput_BuilderDismantleHold = m_BuilderInput.FindAction("BuilderDismantleHold", throwIfNotFound: true);
		m_BuilderInput_BuilderRotate = m_BuilderInput.FindAction("BuilderRotate", throwIfNotFound: true);
		m_BuilderInput_BuilderSwitch = m_BuilderInput.FindAction("BuilderSwitch", throwIfNotFound: true);
		m_BuilderInput_BuilderSwitchInventory = m_BuilderInput.FindAction("BuilderSwitchInventory", throwIfNotFound: true);
		m_BaseInput = asset.FindActionMap("BaseInput", throwIfNotFound: true);
		m_BaseInput_Move = m_BaseInput.FindAction("Move", throwIfNotFound: true);
		m_BaseInput_MoveLeft = m_BaseInput.FindAction("MoveLeft", throwIfNotFound: true);
		m_BaseInput_MoveRight = m_BaseInput.FindAction("MoveRight", throwIfNotFound: true);
		m_BaseInput_MoveUp = m_BaseInput.FindAction("MoveUp", throwIfNotFound: true);
		m_BaseInput_MoveDown = m_BaseInput.FindAction("MoveDown", throwIfNotFound: true);
		m_BaseInput_MoveLast = m_BaseInput.FindAction("MoveLast", throwIfNotFound: true);
		m_BaseInput_MoveNext = m_BaseInput.FindAction("MoveNext", throwIfNotFound: true);
		m_BaseInput_Cancel = m_BaseInput.FindAction("Cancel", throwIfNotFound: true);
		m_BaseInput_Confirm = m_BaseInput.FindAction("Confirm", throwIfNotFound: true);
		m_BaseInput_ConfirmHold = m_BaseInput.FindAction("ConfirmHold", throwIfNotFound: true);
		m_BaseInput_Scroll = m_BaseInput.FindAction("Scroll", throwIfNotFound: true);
		m_BaseInput_PageUp = m_BaseInput.FindAction("PageUp", throwIfNotFound: true);
		m_BaseInput_PageDown = m_BaseInput.FindAction("PageDown", throwIfNotFound: true);
		m_BaseInput_SplitItem = m_BaseInput.FindAction("SplitItem", throwIfNotFound: true);
		m_BaseInput_SortItem = m_BaseInput.FindAction("SortItem", throwIfNotFound: true);
		m_BaseInput_SortItemHold = m_BaseInput.FindAction("SortItemHold", throwIfNotFound: true);
		m_BaseInput_LockItem = m_BaseInput.FindAction("LockItem", throwIfNotFound: true);
		m_BaseInput_DisposeItem = m_BaseInput.FindAction("DisposeItem", throwIfNotFound: true);
		m_BaseInput_DisposeItemHold = m_BaseInput.FindAction("DisposeItemHold", throwIfNotFound: true);
		m_BaseInput_DestroyItem = m_BaseInput.FindAction("DestroyItem", throwIfNotFound: true);
		m_BaseInput_PutMaxItem = m_BaseInput.FindAction("PutMaxItem", throwIfNotFound: true);
		m_BaseInput_PutMaxItemHold = m_BaseInput.FindAction("PutMaxItemHold", throwIfNotFound: true);
		m_BaseInput_PutAllItem = m_BaseInput.FindAction("PutAllItem", throwIfNotFound: true);
		m_BaseInput_SubmitItem = m_BaseInput.FindAction("SubmitItem", throwIfNotFound: true);
		m_BaseInput_SubOne = m_BaseInput.FindAction("SubOne", throwIfNotFound: true);
		m_BaseInput_AddOne = m_BaseInput.FindAction("AddOne", throwIfNotFound: true);
		m_BaseInput_SubTen = m_BaseInput.FindAction("SubTen", throwIfNotFound: true);
		m_BaseInput_AddTen = m_BaseInput.FindAction("AddTen", throwIfNotFound: true);
		m_BaseInput_SetMin = m_BaseInput.FindAction("SetMin", throwIfNotFound: true);
		m_BaseInput_SetMax = m_BaseInput.FindAction("SetMax", throwIfNotFound: true);
		m_BaseInput_ContinueDialogue = m_BaseInput.FindAction("ContinueDialogue", throwIfNotFound: true);
		m_BaseInput_ToggleDialogueHistory = m_BaseInput.FindAction("ToggleDialogueHistory", throwIfNotFound: true);
		m_BaseInput_AssistSplit = m_BaseInput.FindAction("AssistSplit", throwIfNotFound: true);
		m_BaseInput_Interact = m_BaseInput.FindAction("Interact", throwIfNotFound: true);
		m_BaseInput_SpeedUp = m_BaseInput.FindAction("SpeedUp", throwIfNotFound: true);
		m_BaseInput_QuickSelectPrev = m_BaseInput.FindAction("QuickSelectPrev", throwIfNotFound: true);
		m_BaseInput_QuickSelectNext = m_BaseInput.FindAction("QuickSelectNext", throwIfNotFound: true);
		m_BaseInput_ToggleMissionPanel = m_BaseInput.FindAction("ToggleMissionPanel", throwIfNotFound: true);
		m_BaseInput_ToggleTechTree = m_BaseInput.FindAction("ToggleTechTree", throwIfNotFound: true);
		m_BaseInput_ToggleBackpack = m_BaseInput.FindAction("ToggleBackpack", throwIfNotFound: true);
		m_BaseInput_ToggleMap = m_BaseInput.FindAction("ToggleMap", throwIfNotFound: true);
		m_BaseInput_ToggleMenu = m_BaseInput.FindAction("ToggleMenu", throwIfNotFound: true);
		m_BaseInput_ToggleCollectionBook = m_BaseInput.FindAction("ToggleCollectionBook", throwIfNotFound: true);
		m_BaseInput_MiscellaneousFunction = m_BaseInput.FindAction("MiscellaneousFunction", throwIfNotFound: true);
		m_Global = asset.FindActionMap("Global", throwIfNotFound: true);
		m_Global_Point = m_Global.FindAction("Point", throwIfNotFound: true);
		m_Global_Click = m_Global.FindAction("Click", throwIfNotFound: true);
		m_Global_RightClick = m_Global.FindAction("RightClick", throwIfNotFound: true);
		m_Global_CursorPosition = m_Global.FindAction("CursorPosition", throwIfNotFound: true);
	}

	public void Dispose()
	{
		UnityEngine.Object.Destroy(asset);
	}

	public bool Contains(InputAction action)
	{
		return asset.Contains(action);
	}

	public IEnumerator<InputAction> GetEnumerator()
	{
		return asset.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Enable()
	{
		asset.Enable();
	}

	public void Disable()
	{
		asset.Disable();
	}

	public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
	{
		return asset.FindAction(actionNameOrId, throwIfNotFound);
	}

	public int FindBinding(InputBinding bindingMask, out InputAction action)
	{
		return asset.FindBinding(bindingMask, out action);
	}
}
