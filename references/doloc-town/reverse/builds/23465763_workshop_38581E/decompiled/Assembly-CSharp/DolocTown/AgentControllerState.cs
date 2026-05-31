using System;
using DolocTown.Config;
using DolocTown.Config.Room;
using DolocTown.GameData;
using DolocTown.UI;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class AgentControllerState
{
	private readonly Transform transform;

	private readonly DolocUserInput userInput;

	private readonly QuickInventoryPanel quickInventory;

	private readonly CameraController cameraController;

	private readonly BodyController body;

	private readonly MotorController motorController;

	private readonly Timer quickInventoryTimer;

	private readonly RSTimer secondTimer;

	private readonly Counter tuCounter;

	private readonly RSTimer weakLightTimer = new RSTimer(10f);

	private readonly Timer useItemTimer = new Timer(DolocAPI.GlobalParameter.ContinuouslyUseItemTimer_Ref);

	private readonly Timer interactTimer = new Timer(DolocAPI.GlobalParameter.ContinuouslyInteractTimer_Ref);

	private readonly ScannerInteractable interactableScanner;

	private readonly InteractableManagerEx interactableManager = new InteractableManagerEx();

	private readonly ScannerDriver ScannerDriver;

	private readonly ScannerGate GateScanner;

	private readonly ScannerGate GateScannerOfMotor;

	private readonly ScannerInteractableOfMotor interactableScannerOfMotor;

	private readonly MapScanner mapScanner;

	private readonly CollectorDropItem collectorDropItem;

	private readonly ScannerDriver ScannerDriverOfMotor;

	public readonly AgentSkillManager skillManager = new AgentSkillManager();

	public readonly DroneController droneController;

	private AgentBehaviorSettings defaultBehaviorSettings;

	private AgentBehaviorSettings festivalBehaviorSettings = new AgentBehaviorSettings
	{
		disableUseItem = true,
		disableDisposeItem = true
	};

	private float lastLeftPressedTime;

	private float lastRightPressedTime;

	public bool CanScrollItemInLine = true;

	public RoomScanner RoomScanner { get; private set; }

	public BuildingScanner BuildingScanner { get; }

	public ScannerInteractable ScannerInteractable => interactableScanner;

	public CharacterRenderer CharacterRenderer { get; private set; }

	public bool IsRidingNow { get; private set; }

	private Vector2 DashDirection
	{
		get
		{
			float num = userInput.NormalMoveFactorY;
			float num2 = userInput.NormalMoveFactor;
			if (Mathf.Abs(num2) < 0.1f)
			{
				num2 = 0f;
			}
			if (Mathf.Abs(num) < 0.1f)
			{
				num = 0f;
				if (num2 == 0f)
				{
					num2 = transform.localScale.x;
				}
			}
			return new Vector2(num2, num);
		}
	}

	private float holdThreshold => DolocAPI.GlobalParameter.CombinationKeyHoldDuration;

	public AgentControllerState(BodyController body, MotorController motor, QuickInventoryPanel quickInventoryPanel, CameraController cameraController, DolocUserInput userInputEx)
	{
		this.body = body;
		motorController = motor;
		transform = body.transform;
		userInput = userInputEx;
		secondTimer = new RSTimer();
		tuCounter = DolocAPI.GlobalParameter.NewTuCounter;
		quickInventory = quickInventoryPanel;
		this.cameraController = cameraController;
		this.cameraController.Init();
		quickInventoryTimer = new Timer(DolocAPI.GlobalParameter.QuickInventoryTimer_Ref);
		ScannerDriver = new ScannerDriver();
		RoomScanner = new RoomScanner(interactableManager);
		mapScanner = new MapScanner();
		BuildingScanner = new BuildingScanner();
		ScannerDriver.AddScanner(RoomScanner);
		ScannerDriver.AddScanner(mapScanner);
		ScannerDriver.AddScanner(BuildingScanner);
		ScannerDriverOfMotor = new ScannerDriver();
		ScannerDriverOfMotor.AddScanner(mapScanner);
		interactableScanner = body.GetComponentInChildren<ScannerInteractable>(includeInactive: true);
		interactableScanner.Init();
		interactableScanner.Manager = interactableManager;
		GateScanner = body.GetComponentInChildren<ScannerGate>(includeInactive: true);
		GateScanner.Init();
		GateScannerOfMotor = motorController.scannerGate;
		interactableScannerOfMotor = motor.GetComponentInChildren<ScannerInteractableOfMotor>(includeInactive: true);
		interactableScannerOfMotor.Manager = interactableManager;
		collectorDropItem = body.GetComponentInChildren<CollectorDropItem>(includeInactive: true);
		collectorDropItem.Init();
		motorController.GetComponentInChildren<CollectorDropItem>(includeInactive: true).Init();
		droneController = new DroneController(userInputEx);
		CharacterRenderer = body.GetComponent<CharacterRenderer>();
	}

	public void SetRoom(Room room)
	{
		ResetDronePosition(room);
		IGate lastTouchedGate = GateScanner.lastTouchedGate;
		if (lastTouchedGate != null && !lastTouchedGate.KeepHorizontalSpeed)
		{
			body.Status.ClearHorizontalInput();
		}
		GateScanner.ClearBuffer();
		interactableManager.Clear();
		DolocAPI.uiSystem.sceneOperationTipMultiManager.Clear();
		BuildingScanner.ClearBuffer();
		ScannerDriver.SetRoom(room);
		ScannerDriverOfMotor.SetRoom(room);
		body.SetRoom(room, motorController.IsRiding);
		body.WeakLightEnabled = DolocAPI.archiveHandle.ShouldAgentLightUp;
	}

	private void ResetDronePosition(Room room)
	{
		if (room != null)
		{
			if (room.Type != RoomType.Dungeon)
			{
				droneController.ResetRendererPosition();
			}
			else if (!(droneController.CurrentDrone?.renderer == null) && Vector2.Distance(droneController.CurrentDrone.renderer.transform.position, body.position2d) > 10f)
			{
				droneController.ResetRendererPosition();
			}
		}
	}

	public void OnUpdateInNormalState(float dt)
	{
		OnUpdate(dt, defaultBehaviorSettings);
	}

	public void OnUpdateInShipState(float dt)
	{
		if (!EnterUICheck(dt, defaultBehaviorSettings))
		{
			UseToolOrItem(dt);
			InventoryInputCheck(dt);
		}
	}

	public void OnUpdateInKillTimeState(float dt)
	{
		if (!EnterUICheck(dt, defaultBehaviorSettings))
		{
			InventoryInputCheck(dt);
		}
	}

	public void OnUpdateInFestivalState(float dt)
	{
		OnUpdate(dt, festivalBehaviorSettings);
	}

	private void OnUpdate(float dt, AgentBehaviorSettings settings)
	{
		droneController.OnUpdate(dt);
		UpdateExtra(dt);
		if (IsRidingNow)
		{
			OnUpdateRiding(dt, settings);
		}
		else
		{
			OnUpdateNormal(dt, settings);
		}
	}

	private void UpdateExtra(float dt)
	{
		if (secondTimer.Tick(dt) || tuCounter.Tick())
		{
			if (IsRidingNow)
			{
				motorController.UpdatePerTU();
			}
			else
			{
				body.UpdatePerTU();
			}
		}
	}

	private void OnUpdateRiding(float dt, AgentBehaviorSettings settings)
	{
		if (EnterUICheck(dt, settings))
		{
			return;
		}
		body.position = motorController.position;
		ScannerDriverOfMotor.UpdatePosition(motorController.position);
		float normalMoveFactorY = userInput.NormalMoveFactorY;
		float inputY = (userInput.NormalJumpInProgress ? 1f : ((normalMoveFactorY < 0f) ? normalMoveFactorY : 0f));
		motorController.Control(userInput.NormalMoveFactor, inputY);
		if (userInput.NormalInteract)
		{
			if (!GateScannerOfMotor.manager.TryInteractOnMotor())
			{
				GetOffMotor();
			}
		}
		else if (userInput.NormalRoomInteract)
		{
			if (GateScannerOfMotor.manager.CurrentGate != null && !GateScannerOfMotor.manager.TryEnterOnMotor() && GateScannerOfMotor.manager.CurrentGate.InteractKey == PortalInteractKey.Enter)
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiTipNotAvailableToMotor);
			}
		}
		else if (userInput.NormalJumpDown && GateScannerOfMotor.manager.CurrentGate != null && !GateScannerOfMotor.manager.TryQuitOnMotor() && GateScannerOfMotor.manager.CurrentGate.InteractKey == PortalInteractKey.Exit)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiTipNotAvailableToMotor);
		}
		if (!settings.disableUseItem)
		{
			UseToolOrItem(dt);
		}
		if (!settings.disableQuickInventory && body.IsCurrentStateSupportScrollQuickInventoryUI)
		{
			InventoryInputCheck(dt);
		}
	}

	private void OnUpdateNormal(float dt, AgentBehaviorSettings settings)
	{
		if (EnterUICheck(dt, settings))
		{
			return;
		}
		ScannerDriver.UpdatePosition(body.transform.position);
		body.Status.HorizontalMoveFactor = userInput.NormalMoveFactor;
		if (body.Status.ToolLatch && userInput.NormalIsMovePressed)
		{
			body.Status.ToolLatch = false;
		}
		if (AgentMotion())
		{
			return;
		}
		if (userInput.NormalInteract)
		{
			interactTimer.ReStart();
			if (!body.IsCurrentStateSupportInteract)
			{
				return;
			}
			body.Status.Velocity = Vector2.zero;
			if (GateScanner.manager.TryInteract() || interactableManager.TryInteract(continues: false) || BuildingScanner.TryInteract())
			{
				return;
			}
		}
		else if (userInput.NormalInteractInProgress)
		{
			if (InteractContinues(dt))
			{
				return;
			}
		}
		else if (userInput.NormalRoomInteract)
		{
			if (body.IsCurrentStateSupportInteract && GateScanner.manager.TryEnter())
			{
				body.Status.Velocity = Vector2.zero;
				return;
			}
			if (body.IsInAirState)
			{
				if (DolocAPI.userSettings.allowEnterRoomInAir)
				{
					BuildingScanner.TryEnter();
				}
			}
			else
			{
				BuildingScanner.TryEnter();
			}
		}
		else if (!settings.disableUseItem && UseToolOrItem(dt))
		{
			return;
		}
		if (!settings.disableQuickInventory && body.IsCurrentStateSupportScrollQuickInventoryUI)
		{
			InventoryInputCheck(dt);
		}
	}

	private bool AgentMotion()
	{
		body.CheckJumpDownReleased(userInput.NormalJumpDownReleased);
		if (userInput.NormalDash && DolocAPI.archiveHandle.CouldDash() && body.Dash(DashDirection))
		{
			return true;
		}
		if (userInput.NormalJump && body.Jump())
		{
			return true;
		}
		if (userInput.NormalJumpDown)
		{
			if (body.JumpDown())
			{
				return true;
			}
			if (body.IsCurrentStateSupportInteract && GateScanner.manager.TryQuit())
			{
				body.Status.Velocity = Vector2.zero;
				return true;
			}
		}
		return false;
	}

	private bool InteractContinues(float dt)
	{
		if (!body.IsCurrentStateSupportInteract)
		{
			return false;
		}
		if (!interactTimer.Update(dt))
		{
			return interactableManager.TryInteract(continues: true);
		}
		return false;
	}

	private void UseToolContinues()
	{
		if (!body.IsCurrentStateSupportUseItem || !(DolocAPI.SelectedItem is ItemTool tool))
		{
			return;
		}
		body.Status.Velocity = Vector2.zero;
		if (body.StateManager.current is AgentStateTool agentStateTool)
		{
			agentStateTool.tool = tool;
			if (agentStateTool.animationNormalizedTime >= 0.95f)
			{
				body.UseTool(tool);
			}
		}
		else if (!body.StateManager.CheckState<AgentStateMove>())
		{
			body.UseTool(tool);
		}
	}

	private void UseItemContinues(float dt)
	{
		Item selectedItem = DolocAPI.SelectedItem;
		if (selectedItem != null && body.IsCurrentStateSupportUseItem && useItemTimer.Update(dt))
		{
			body.Status.Velocity = Vector2.zero;
			selectedItem.UseAsItem();
		}
	}

	public void OnFixedUpdate(float dt)
	{
		droneController.OnFixedUpdate(dt);
		motorController.OnFixedUpdate(dt);
		cameraController.UpdateCamPosition(dt);
		collectorDropItem.OnFixedUpdate(dt);
		if (weakLightTimer.Tick(dt))
		{
			body.WeakLightEnabled = DolocAPI.archiveHandle.ShouldAgentLightUp;
		}
	}

	public void ClearPhysicalStatus()
	{
		body.Status.ClearHorizontalInput();
	}

	public void SetAttackable(bool value)
	{
		body.SetAttackable(value);
	}

	public void Water(ItemWaterCan waterCan, Action callback = null)
	{
		if (body.IsCurrentStateSupportUseItem)
		{
			body.Status.Velocity = Vector2.zero;
			body.Status.HorizontalMoveFactor = 0f;
			body._Water(waterCan, delegate
			{
				callback?.Invoke();
			});
		}
	}

	public void GetOnMotor()
	{
		IsRidingNow = true;
		GateScanner.ClearBuffer();
		BuildingScanner.ClearBuffer();
		RoomScanner.ClearBuffer();
		body.SetVisible(value: false);
		motorController.SetIsRiding(value: true);
		motorController.driverRenderer.SetHatInfo(body.CurrentHatRenderInfo);
		cameraController.SetFollow(motorController);
		droneController.SetDroneFollower(motorController.DroneFollowPoint);
		DolocAPI.SelectedItem?.QuickDeselect();
		DolocAPI.ClearSceneOperationTips();
	}

	public void GetOffMotor()
	{
		IsRidingNow = false;
		motorController.SetIsRiding(value: false);
		body.SetVisible(value: true);
		body.position2d = motorController.position2d;
		body.StateManager.Overwrite<AgentStateDrop>();
		body.ToolRenderer.SetVisible(value: false);
		cameraController.SetFollow(body);
		droneController.SetDroneFollower(body.DroneFollower);
		DolocAPI.archiveHandle.UpdateMotorRoom(DolocAPI.CurrentRoom);
		DolocAPI.ClearSceneOperationTips();
	}

	public void GetOffIfRiding()
	{
		if (IsRidingNow)
		{
			GetOffMotor();
		}
	}

	public void EnterState<T>() where T : AgentStateBase
	{
		body.EnterState<T>();
	}

	private bool UseToolOrItem(float dt)
	{
		if (userInput.NormalUseTool)
		{
			UseTool();
			return true;
		}
		if (userInput.NormalUseToolInProgress)
		{
			UseToolContinues();
			return true;
		}
		if (userInput.NormalUseItem)
		{
			UseItem();
			return true;
		}
		if (userInput.NormalUseItemInProgress)
		{
			UseItemContinues(dt);
			return true;
		}
		return false;
	}

	private void InventoryInputCheck(float dt)
	{
		if (userInput.GlobalQuickSelectPrev || userInput.GlobalQuickSelectNext)
		{
			CanScrollItemInLine = true;
		}
		if (userInput.GlobalQuickSelectPrev && userInput.GlobalQuickSelectNext)
		{
			if (DolocAPI.userSettings.enableBackpackShortcut)
			{
				DolocAPI.gameUiStates.EnterUI<EquipmentBarUiState>();
			}
		}
		else if (Time.time - lastLeftPressedTime < holdThreshold && userInput.GlobalQuickSelectNext)
		{
			if (DolocAPI.userSettings.enableBackpackShortcut)
			{
				quickInventory.MoveNext();
				DolocAPI.gameUiStates.EnterUI<EquipmentBarUiState>();
			}
		}
		else if (Time.time - lastRightPressedTime < holdThreshold && userInput.GlobalQuickSelectPrev)
		{
			if (DolocAPI.userSettings.enableBackpackShortcut)
			{
				quickInventory.MovePrev();
				DolocAPI.gameUiStates.EnterUI<EquipmentBarUiState>();
			}
		}
		else if (CanScrollItemInLine && userInput.GlobalQuickSelectPrev)
		{
			lastLeftPressedTime = Time.time;
			quickInventory.MovePrev();
			quickInventoryTimer.ReStart();
		}
		else if (CanScrollItemInLine && userInput.GlobalQuickSelectNext)
		{
			lastRightPressedTime = Time.time;
			quickInventory.MoveNext();
			quickInventoryTimer.ReStart();
		}
		else if (CanScrollItemInLine && userInput.GlobalQuickSelectPrevInProgress)
		{
			lastLeftPressedTime = Time.time;
			if (quickInventoryTimer.Update(dt))
			{
				quickInventory.MovePrev();
			}
		}
		else if (CanScrollItemInLine && userInput.GlobalQuickSelectNextInProgress)
		{
			lastRightPressedTime = Time.time;
			if (quickInventoryTimer.Update(dt))
			{
				quickInventory.MoveNext();
			}
		}
		else if (userInput.NormalScrollInventoryUp)
		{
			if (DolocAPI.archiveHandle.InventorySystem.inventory.capacity > 10)
			{
				DolocAPI.PrevLine();
			}
		}
		else if (userInput.NormalScrollInventoryDown)
		{
			if (DolocAPI.archiveHandle.InventorySystem.inventory.capacity > 10)
			{
				DolocAPI.NextLine();
			}
		}
		else if (userInput.NormalSelectedDrone && DolocAPI.UserInput.DeviceType == DolocInputDeviceType.KeyboardMouse)
		{
			quickInventory.SelectDrone();
		}
		else if (userInput.NormalSelectedActive)
		{
			quickInventory.SelectActiveItem();
		}
		else if (userInput.NormalQuickSelect0)
		{
			quickInventory.SelectInCurrentLine(9);
		}
		else if (userInput.NormalQuickSelect1)
		{
			quickInventory.SelectInCurrentLine(0);
		}
		else if (userInput.NormalQuickSelect2)
		{
			quickInventory.SelectInCurrentLine(1);
		}
		else if (userInput.NormalQuickSelect3)
		{
			quickInventory.SelectInCurrentLine(2);
		}
		else if (userInput.NormalQuickSelect4)
		{
			quickInventory.SelectInCurrentLine(3);
		}
		else if (userInput.NormalQuickSelect5)
		{
			quickInventory.SelectInCurrentLine(4);
		}
		else if (userInput.NormalQuickSelect6)
		{
			quickInventory.SelectInCurrentLine(5);
		}
		else if (userInput.NormalQuickSelect7)
		{
			quickInventory.SelectInCurrentLine(6);
		}
		else if (userInput.NormalQuickSelect8)
		{
			quickInventory.SelectInCurrentLine(7);
		}
		else if (userInput.NormalQuickSelect9)
		{
			quickInventory.SelectInCurrentLine(8);
		}
	}

	private bool EnterUICheck(float dt, AgentBehaviorSettings settings)
	{
		if (userInput.GlobalIsCancelPressed && DolocAPI.uiSystem.sceneBoxGroup.isRender)
		{
			DolocAPI.HideSceneBox();
			return true;
		}
		if (userInput.GlobalToggleMenu)
		{
			body.Status.HorizontalMoveFactor = 0f;
			DolocAPI.gameUiStates.EnterUI<MainMenuUiState>();
			return true;
		}
		if (userInput.GlobalDisposeItemInBackpack)
		{
			if (!settings.disableDisposeItem)
			{
				DolocAPI.DisposeItem(DolocAPI.SelectedItemIndex);
			}
			return true;
		}
		if (userInput.GlobalToggleBackpack)
		{
			DolocAPI.gameUiStates.EnterUI<EquipmentBarUiState>();
			return true;
		}
		if (settings.enableBackpackShortcutNoRollback && DolocAPI.userSettings.enableBackpackShortcut && userInput.GlobalQuickSelectPrev && userInput.GlobalQuickSelectNext)
		{
			DolocAPI.gameUiStates.EnterUI<EquipmentBarUiState>();
		}
		if (userInput.GlobalToggleMap)
		{
			DolocAPI.OpenMap();
			return true;
		}
		if (userInput.GlobalToggleMissionPanel)
		{
			DolocAPI.EnterUI<MissionPanelUiState>();
			return true;
		}
		if (userInput.GlobalToggleTechTree)
		{
			DolocAPI.EnterUI<TechTreeUiState>();
			return true;
		}
		if (userInput.GlobalToggleCollectionBook)
		{
			DolocAPI.EnterUI<CollectionBookUiState>();
			return true;
		}
		if (userInput.NormalDebug && DolocAPI.gameManager.gameOuterConfig.allowFastTravelMenu)
		{
			DolocAPI.ShowSmallTextMenu(DolocAPI.devHelper.DevMenuItemTitles, new Vector2(50f, 50f), DolocAPI.devHelper.DevMenuCallback);
		}
		return false;
	}

	public void UseTool(bool force = false)
	{
		if (!body.IsCurrentStateSupportUseItem)
		{
			if (body.IsFishingNow && DolocAPI.SelectedItem is ItemFishingRod)
			{
				body.StateManager.Overwrite(delegate(AgentStateFishingPull s)
				{
					s.IsFailed = true;
				});
			}
		}
		else if (!DolocAPI.archiveHandle.InventorySystem.buffer.IsFull && (force || !DolocUtils.IsInteractWithUI()))
		{
			DolocAPI.SelectedItem?.UseAsTool();
		}
	}

	public void UseItem(bool force = false)
	{
		if (body.IsCurrentStateSupportUseItem && !DolocAPI.archiveHandle.InventorySystem.buffer.IsFull && (force || !DolocUtils.IsInteractWithUI()))
		{
			useItemTimer.ReStart();
			body.Status.Velocity = Vector2.zero;
			DolocAPI.SelectedItem?.UseAsItem();
		}
	}

	public void OnPause()
	{
		SetAttackable(value: false);
		IGate lastGate = GateScanner.lastGate;
		if ((lastGate != null && lastGate.NeedInteract) || body.ShouldClearPhysicalStatusWhileTransit)
		{
			ClearPhysicalStatus();
		}
		else if ((DolocAPI.CurrentRoom?.Type ?? RoomType.None) != RoomType.Dungeon)
		{
			IGate lastGate2 = GateScanner.lastGate;
			Vector2 velocity = body.Status.Velocity;
			Vector2 zero = Vector2.zero;
			if (lastGate2 != null)
			{
				if (lastGate2.KeepHorizontalSpeed)
				{
					zero.x = velocity.x;
				}
				if (lastGate2.KeepVerticalSpeed)
				{
					zero.y = velocity.y;
				}
				body.Status.Velocity = zero;
			}
			else
			{
				ClearPhysicalStatus();
			}
		}
		motorController.PauseRigidbody();
		droneController.OnPause();
		if (body.StateManager.CheckState<AgentStateFishing>())
		{
			body.StateManager.Overwrite<AgentStateIdle>();
			AgentStateFishing.BreakFishing();
		}
	}

	public void OnResume()
	{
		SetAttackable(value: true);
		motorController.ResumeRigidbody();
		droneController.OnResume();
		cameraController.SetSmoothMoving(DolocAPI.userSettings.cameraSmoothMove);
	}
}
