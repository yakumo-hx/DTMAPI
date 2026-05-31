using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Localization;
using DolocTown.Config.UI;
using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown;

public class GlobalBuilderState : DolocTownGameStateBase
{
	private readonly GlobalBuilderPanel _panel;

	private readonly List<IBuilderState> _lutCache;

	private static int _currentLabelIndex;

	private IBuilderState _currentBuilderState;

	private string[] _labels;

	private readonly GraphicRaycaster _raycaster;

	private Action<float> _onUpdateFunc;

	private bool _inQuickInventory;

	private float _lastLeftPressedTime;

	private float _lastRightPressedTime;

	private bool _preciseMovementModeOfJoyStick;

	private readonly Timer _quickInventoryTimer = new Timer(DolocAPI.GlobalParameter.QuickInventoryTimer_Ref);

	public override bool ForceHideBasicTip => true;

	public override bool ForceHideOperationTip => true;

	public override bool ForceShowQuickInventory => true;

	private float CameraMoveSpeed => 1f;

	private QuickInventoryPanel quickInventory => DolocAPI.uiSystem.inventoryQuick;

	private TempBuildingViewer tempBuildingInventory => _panel.buildingViewer;

	private AgentControllerState agentController => DolocAPI.gameStateManager.agentController;

	private LinearInventory _inventory => DolocAPI.archiveHandle.InventorySystem.inventory;

	private float holdThreshold => DolocAPI.GlobalParameter.CombinationKeyHoldDuration;

	private TbStaticText StaticTexts => DolocConfig.StaticTexts;

	private int LabelCount => _lutCache.Count;

	public static float lastExitTime { get; set; }

	public static bool ValidOperation { get; set; }

	private BuildingBuilder buildingBuilder => (BuildingBuilder)_lutCache[0];

	public static bool HandleStartUp()
	{
		if (Time.time - lastExitTime < 1.5f)
		{
			return false;
		}
		new GlobalBuilderState().Startup();
		return true;
	}

	private GlobalBuilderState()
		: base(DolocAPI.userInput, DolocInputType.All, shouldPauseGame: true, shouldLateUpdate: false, supportCutscenes: false)
	{
		_panel = DolocAPI.uiSystem.GetEntity<GlobalBuilderPanel>(this);
		_lutCache = new List<IBuilderState>
		{
			new BuildingBuilder(_panel.buildingViewer),
			new EquipmentBuilder(),
			new PlatformBuilder()
		};
		_labels = new string[3] { StaticTexts.BuilderPanelLabelBuilding, StaticTexts.BuilderPanelLabelEquipment, StaticTexts.BuilderPanelLabelPlatform };
		_currentBuilderState = _lutCache[0];
		ValidOperation = false;
		_inQuickInventory = false;
		_preciseMovementModeOfJoyStick = false;
		UIEntityInfo orDefault = DolocConfig.Tables.TbUIEntity.GetOrDefault(_panel.GetType().Name);
		DolocAPI.uiSystem.GetUiGroup(orDefault.Group_Ref.Id, out var uiGroup);
		_raycaster = uiGroup.GetComponent<GraphicRaycaster>();
		Transform transform = DolocAPI.dolocBuilder.builderLight.transform;
		transform.position = new Vector3(DolocAPI.cameraController.position2d.x, DolocAPI.cameraController.position2d.y, transform.position.z);
	}

	public override void OnUpdate(float deltaTime)
	{
		_onUpdateFunc(deltaTime);
		if (!MouseClickFitter() && !_currentBuilderState.OnUpdate(deltaTime))
		{
			OperationTipTransparency();
			if (userInput.BaseIsCancelPressed)
			{
				ExitStateCheck();
			}
		}
	}

	public override void OnInputDeviceChanged(DolocInputDeviceType type)
	{
		_lutCache.ForEach(delegate(IBuilderState builder)
		{
			builder.OnInputDeviceChanged(type);
		});
		Action<float> onUpdateFunc = ((type != 0) ? new Action<float>(OnUpdate_JoyStick) : new Action<float>(OnUpdate_KM));
		_onUpdateFunc = onUpdateFunc;
		IInputDeviceDetect[] componentsInChildren = _panel.GetComponentsInChildren<IInputDeviceDetect>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].OnRefresh(type);
		}
		RefreshOperationTip(type);
	}

	public override void OnEnter()
	{
		Debug.Log("Enter GlobalBuilderState");
		EnterGlobalBuilder();
		_panel.containerLabelUI.Render(_labels);
		for (int i = 0; i < _panel.containerLabelUI.slots.Count; i++)
		{
			_panel.containerLabelUI.GetSlot(i).grayed = !_lutCache[i].Construct;
		}
		_panel.containerLabelUI.SetClickCallbacks(OnLabelClick);
		tempBuildingInventory.SetThumbnailClickCallback(delegate
		{
			SwitchInventory(inQuickInventory: false);
		});
		tempBuildingInventory.SetOperationTipClickCallback(delegate
		{
			SwitchInventory(!_inQuickInventory);
		});
		_panel.Show();
		_panel.containerLabelUI.FireClick(FindNextConstructState());
		quickInventory.SelectInCurrentLine(0);
	}

	public override void OnExit()
	{
		ExitGlobalBuilder();
		lastExitTime = Time.time;
		_currentBuilderState.ExitBuilder();
		_currentBuilderState = null;
		DolocAPI.CurrentRoom.RefreshAnimalEnv();
		_panel.containerLabelUI.RemoveCallbacks();
		tempBuildingInventory.RemoveCallbacks();
		quickInventory.SetCanvasGroupAlpha(1f);
		quickInventory.BindQuickInventory(_inventory);
		DolocAPI.ReQuickSelectCurrentItem();
		_panel.Hide();
	}

	public override void OnPause()
	{
		if (DolocAPI.userInput.NextGameState is QuestionUiState)
		{
			bool inQuickInventory = _inQuickInventory;
			quickInventory.Show(useTween: false);
			SwitchInventory(inQuickInventory);
			EventSystem.current?.SetSelectedGameObject(null);
		}
		else
		{
			_panel.Hide();
		}
		_currentBuilderState.ExitBuilder();
	}

	public override void OnResume()
	{
		if (!_inQuickInventory)
		{
			tempBuildingInventory.Select(0);
		}
		else
		{
			_panel.Show();
		}
		_currentBuilderState.RunBuilder(null);
	}

	private void OnLabelClick(int index)
	{
		IBuilderState builderState = _lutCache[_currentLabelIndex];
		builderState.ExitBuilder();
		_currentLabelIndex = index;
		_currentBuilderState = _lutCache[_currentLabelIndex];
		if (!_currentBuilderState.Construct)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(StaticTexts.BuilderBuilderConstruct, _labels[_currentLabelIndex]));
		}
		_currentBuilderState.SetBuilderCellPosition(builderState.BuilderCellPosition);
		quickInventory.BindBuilderInventory(_inventory, (Item item) => _currentBuilderState.ItemFilter(item), delegate(int i)
		{
			SwitchInventory(inQuickInventory: true);
			_currentBuilderState.RunBuilder(_inventory.Read(i));
		});
		RefreshOperationTip(DolocAPI.UserInput.DeviceType);
	}

	private void LastLabel()
	{
		int index = (_currentLabelIndex + LabelCount - 1) % LabelCount;
		_panel.containerLabelUI.FireClick(index);
	}

	private void NextLabel()
	{
		int index = (_currentLabelIndex + 1) % LabelCount;
		_panel.containerLabelUI.FireClick(index);
	}

	private void EnterGlobalBuilder()
	{
		DolocAPI.agent.StateManager.Overwrite<AgentStateIdle>();
		DolocAPI.agent.SetVisible(value: false);
		DolocAPI.droneRenderer.SetVisible(value: false);
		DolocAPI.Motor.SetVisible(value: false);
		agentController.RoomScanner.ClearBuffer();
		agentController.BuildingScanner.ClearBuffer();
		agentController.ScannerInteractable.Manager.Clear();
		DolocAPI.ClearSceneOperationTips();
		TimeArchiveData timeData = DolocAPI.archiveHandle.timeData;
		SwitchScheduleParams param = new SwitchScheduleParams(timeData.dateNow, timeData.weather.WeatherType);
		if (DolocAPI.GlobalParameter.DefaultSwitchSchedule.IsTrue(param))
		{
			DolocAPI.dolocBuilder.builderLight.Show();
		}
		DolocAPI.dolocBuilder.EnterHelpState(DolocAPI.CurrentRoom.RoomPosition, DolocAPI.CurrentRoom.RoomGridSize);
	}

	private void ExitGlobalBuilder()
	{
		DolocAPI.AgentPosition = GetStandingPosition();
		DolocAPI.agent.SetVisible(value: true);
		AgentArchiveData agentData = DolocAPI.archiveHandle.farmData.agentData;
		DolocAPI.droneRenderer.SetVisible(agentData.agentEquipment.HasDrone);
		DolocAPI.Motor.SetVisible(agentData.motorData.isUnlocked && agentData.motorData.roomId == DolocAPI.CurrentRoom.RoomId);
		DolocAPI.RefreshScanner();
		DolocAPI.dolocBuilder.builderLight.Hide();
		DolocAPI.dolocBuilder.ExitHelpState();
	}

	private void OnUpdate_KM(float dt)
	{
		if (userInput.GlobalQuickSelectPrev || userInput.GlobalQuickSelectNext)
		{
			agentController.CanScrollItemInLine = true;
		}
		if (_inQuickInventory)
		{
			if (userInput.GlobalQuickSelectPrev && userInput.GlobalQuickSelectNext)
			{
				if (DolocAPI.userSettings.enableBackpackShortcut)
				{
					DolocAPI.gameUiStates.EnterUI<EquipmentBarUiState>();
				}
			}
			else if (Time.time - _lastLeftPressedTime < holdThreshold && userInput.GlobalQuickSelectNext)
			{
				if (DolocAPI.userSettings.enableBackpackShortcut)
				{
					quickInventory.MoveNext();
					DolocAPI.gameUiStates.EnterUI<EquipmentBarUiState>();
				}
			}
			else if (Time.time - _lastRightPressedTime < holdThreshold && userInput.GlobalQuickSelectPrev)
			{
				if (DolocAPI.userSettings.enableBackpackShortcut)
				{
					quickInventory.MovePrev();
					DolocAPI.gameUiStates.EnterUI<EquipmentBarUiState>();
				}
			}
			else if (agentController.CanScrollItemInLine && userInput.GlobalQuickSelectPrev)
			{
				_lastLeftPressedTime = Time.time;
				quickInventory.MovePrev();
				_quickInventoryTimer.ReStart();
			}
			else if (agentController.CanScrollItemInLine && userInput.GlobalQuickSelectNext)
			{
				_lastRightPressedTime = Time.time;
				quickInventory.MoveNext();
				_quickInventoryTimer.ReStart();
			}
			else if (agentController.CanScrollItemInLine && userInput.GlobalQuickSelectPrevInProgress)
			{
				_lastLeftPressedTime = Time.time;
				if (_quickInventoryTimer.Update(dt))
				{
					quickInventory.MovePrev();
				}
			}
			else if (agentController.CanScrollItemInLine && userInput.GlobalQuickSelectNextInProgress)
			{
				_lastRightPressedTime = Time.time;
				if (_quickInventoryTimer.Update(dt))
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
			else if (userInput.GlobalToggleBackpack)
			{
				DolocAPI.gameUiStates.EnterUI<EquipmentBarUiState>();
			}
		}
		else if (userInput.GlobalQuickSelectPrev)
		{
			tempBuildingInventory.MovePrev();
			_quickInventoryTimer.ReStart();
		}
		else if (userInput.GlobalQuickSelectNext)
		{
			tempBuildingInventory.MoveNext();
			_quickInventoryTimer.ReStart();
		}
		else if (userInput.GlobalQuickSelectPrevInProgress)
		{
			if (_quickInventoryTimer.Update(dt))
			{
				tempBuildingInventory.MovePrev();
			}
		}
		else if (userInput.GlobalQuickSelectNextInProgress && _quickInventoryTimer.Update(dt))
		{
			tempBuildingInventory.MoveNext();
		}
		if (userInput.BuilderLastPressed)
		{
			LastLabel();
		}
		else if (userInput.BuilderNextPressed)
		{
			NextLabel();
		}
		else if (userInput.BaseIsMoveInProgress)
		{
			MoveCamera(userInput.LeftJoyStickValue * CameraMoveSpeed);
		}
		else if (userInput.BuilderSwitchInventoryPressed)
		{
			SwitchInventory(!_inQuickInventory);
		}
	}

	private void OnUpdate_JoyStick(float dt)
	{
		if (userInput.BuilderLastPressed)
		{
			LastLabel();
		}
		else if (userInput.BuilderNextPressed)
		{
			NextLabel();
		}
		else if (userInput.BaseIsScrollInProgress)
		{
			MoveCamera(userInput.BaseScrollDir * CameraMoveSpeed);
		}
		else if (userInput.BuilderSwitchPressed)
		{
			_preciseMovementModeOfJoyStick = !_preciseMovementModeOfJoyStick;
			RefreshOperationTip(DolocAPI.UserInput.DeviceType);
		}
		else if (userInput.BuilderSwitchInventoryPressed)
		{
			SwitchInventory(!_inQuickInventory);
		}
		if (userInput.GlobalQuickSelectPrev || userInput.GlobalQuickSelectNext)
		{
			agentController.CanScrollItemInLine = true;
		}
		if (_inQuickInventory)
		{
			if (userInput.GlobalQuickSelectPrev && userInput.GlobalQuickSelectNext)
			{
				DolocAPI.gameUiStates.EnterUI<EquipmentBarUiState>();
			}
			else if (Time.time - _lastLeftPressedTime < holdThreshold && userInput.GlobalQuickSelectNext)
			{
				quickInventory.MoveNext();
				DolocAPI.gameUiStates.EnterUI<EquipmentBarUiState>();
			}
			else if (Time.time - _lastRightPressedTime < holdThreshold && userInput.GlobalQuickSelectPrev)
			{
				quickInventory.MovePrev();
				DolocAPI.gameUiStates.EnterUI<EquipmentBarUiState>();
			}
			else if (agentController.CanScrollItemInLine && userInput.GlobalQuickSelectPrev)
			{
				_lastLeftPressedTime = Time.time;
				quickInventory.MovePrev();
				_quickInventoryTimer.ReStart();
			}
			else if (agentController.CanScrollItemInLine && userInput.GlobalQuickSelectNext)
			{
				_lastRightPressedTime = Time.time;
				quickInventory.MoveNext();
				_quickInventoryTimer.ReStart();
			}
			else if (agentController.CanScrollItemInLine && userInput.GlobalQuickSelectPrevInProgress)
			{
				_lastLeftPressedTime = Time.time;
				if (_quickInventoryTimer.Update(dt))
				{
					quickInventory.MovePrev();
				}
			}
			else if (agentController.CanScrollItemInLine && userInput.GlobalQuickSelectNextInProgress)
			{
				_lastRightPressedTime = Time.time;
				if (_quickInventoryTimer.Update(dt))
				{
					quickInventory.MoveNext();
				}
			}
			if (_preciseMovementModeOfJoyStick)
			{
				_currentBuilderState.PreciseMovement_JoyStick(dt);
			}
			else if (userInput.BuilderDPadUpPressed)
			{
				if (DolocAPI.archiveHandle.InventorySystem.inventory.capacity > 10)
				{
					DolocAPI.PrevLine();
				}
			}
			else if (userInput.BuilderDPadDownPressed)
			{
				if (DolocAPI.archiveHandle.InventorySystem.inventory.capacity > 10)
				{
					DolocAPI.NextLine();
				}
			}
			else if (agentController.CanScrollItemInLine && userInput.BuilderDPadLeftPressed)
			{
				_lastLeftPressedTime = Time.time;
				quickInventory.MovePrev();
				_quickInventoryTimer.ReStart();
			}
			else if (agentController.CanScrollItemInLine && userInput.BuilderDPadRightPressed)
			{
				_lastRightPressedTime = Time.time;
				quickInventory.MoveNext();
				_quickInventoryTimer.ReStart();
			}
			else if (agentController.CanScrollItemInLine && userInput.BuilderDPadLeftInProgress)
			{
				_lastLeftPressedTime = Time.time;
				if (_quickInventoryTimer.Update(dt))
				{
					quickInventory.MovePrev();
				}
			}
			else if (agentController.CanScrollItemInLine && userInput.BuilderDPadRightInProgress)
			{
				_lastRightPressedTime = Time.time;
				if (_quickInventoryTimer.Update(dt))
				{
					quickInventory.MoveNext();
				}
			}
		}
		else if (userInput.GlobalQuickSelectPrev)
		{
			tempBuildingInventory.MovePrev();
			_quickInventoryTimer.ReStart();
		}
		else if (userInput.GlobalQuickSelectNext)
		{
			tempBuildingInventory.MoveNext();
			_quickInventoryTimer.ReStart();
		}
		else if (userInput.GlobalQuickSelectPrevInProgress)
		{
			if (_quickInventoryTimer.Update(dt))
			{
				tempBuildingInventory.MovePrev();
			}
		}
		else if (userInput.GlobalQuickSelectNextInProgress)
		{
			if (_quickInventoryTimer.Update(dt))
			{
				tempBuildingInventory.MoveNext();
			}
		}
		else if (userInput.BuilderDPadUpPressed)
		{
			tempBuildingInventory.PrevLine();
			_quickInventoryTimer.ReStart();
		}
		else if (userInput.BuilderDPadDownPressed)
		{
			tempBuildingInventory.NextLine();
			_quickInventoryTimer.ReStart();
		}
		else if (userInput.BuilderDPadLeftPressed)
		{
			tempBuildingInventory.MovePrev();
			_quickInventoryTimer.ReStart();
		}
		else if (userInput.BuilderDPadRightPressed)
		{
			tempBuildingInventory.MoveNext();
			_quickInventoryTimer.ReStart();
		}
		else if (userInput.BuilderDPadUpInProgress)
		{
			if (_quickInventoryTimer.Update(dt))
			{
				tempBuildingInventory.PrevLine();
			}
		}
		else if (userInput.BuilderDPadDownInProgress)
		{
			if (_quickInventoryTimer.Update(dt))
			{
				tempBuildingInventory.NextLine();
			}
		}
		else if (userInput.BuilderDPadLeftInProgress)
		{
			if (_quickInventoryTimer.Update(dt))
			{
				tempBuildingInventory.MovePrev();
			}
		}
		else if (userInput.BuilderDPadRightInProgress && _quickInventoryTimer.Update(dt))
		{
			tempBuildingInventory.MoveNext();
		}
	}

	private void MoveCamera(Vector2 delta)
	{
		Vector2 position = DolocAPI.cameraController.position2d + delta;
		DolocAPI.cameraController.ForceSetPosition(position, constraint: true);
		Transform transform = DolocAPI.dolocBuilder.builderLight.transform;
		transform.position = new Vector3(position.x, position.y, transform.position.z);
		_currentBuilderState.OnUpdateMoveCamera(delta);
	}

	private static Vector2 GetStandingPosition()
	{
		Room currentRoom = DolocAPI.CurrentRoom;
		Vector2Int agentRoomCellPosition = DolocAPI.AgentRoomCellPosition;
		currentRoom.DM_terrain.Raycast(agentRoomCellPosition, currentRoom.RoomGridSize.y, Vector2Int.down, TerrainLayerName.Structure | TerrainLayerName.CeilingFront, out var hitpos);
		hitpos.y++;
		return currentRoom.Geometry.CalcWorldPosition(hitpos);
	}

	private void RefreshOperationTip(DolocInputDeviceType type)
	{
		if (type == DolocInputDeviceType.KeyboardMouse)
		{
			_panel.operationTip.SetTextKey(_currentBuilderState.GetOperateTip(type));
			return;
		}
		string[] second = ((!_preciseMovementModeOfJoyStick) ? new string[3] { StaticTexts.BuilderActionSwitchPrecise, StaticTexts.BuilderActionRollingBackpack, StaticTexts.BuilderActionRollingItem } : new string[2] { StaticTexts.BuilderActionSwitchBackpack, StaticTexts.BuilderActionPreciseMovement });
		_panel.operationTip.SetTextKey(_currentBuilderState.GetOperateTip(type).Concat(second).ToArray());
	}

	private bool MouseClickFitter()
	{
		if (DolocAPI.UserInput.DeviceType != 0)
		{
			return false;
		}
		if (userInput.BuilderSelected || userInput.BuilderSelectedInProgress || userInput.BuilderRevocationTriggered)
		{
			PointerEventData eventData = new PointerEventData(EventSystem.current)
			{
				position = userInput.MousePosition
			};
			List<RaycastResult> list = new List<RaycastResult>();
			_raycaster.Raycast(eventData, list);
			if (list.Count > 0)
			{
				return true;
			}
		}
		return false;
	}

	private void OperationTipTransparency()
	{
		Vector2 screenLatticePosition = BuilderUtils.GetScreenLatticePosition(_currentBuilderState.BuilderCellPosition, DolocAPI.CurrentRoom.RoomPosition);
		Vector3[] array = new Vector3[4];
		_panel.operationTip.rectTransform.GetWorldCorners(array);
		Rect rect = new Rect(array[0].x, array[0].y, array[2].x - array[0].x, array[2].y - array[0].y);
		_panel.operationTip.canvasGroup.alpha = (rect.Contains(screenLatticePosition) ? 0.2f : 1f);
	}

	private int FindNextConstructState()
	{
		int num = _currentLabelIndex;
		while (!_lutCache[num].Construct)
		{
			num = (num + 1) % LabelCount;
		}
		return num;
	}

	private void ExitStateCheck()
	{
		LinearInventory tempBuildingList = buildingBuilder.TempBuildingList;
		if (tempBuildingList.isEmpty)
		{
			gameController.PopState();
			if (ValidOperation)
			{
				DolocAPI.StartDialogueNode(DolocAPI.GlobalParameter.BuilderAnimationNode);
			}
			return;
		}
		_panel.containerLabelUI.FireClick(0);
		SwitchInventory(inQuickInventory: false);
		Item[] array = tempBuildingList.ReadAll();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is ItemBuilding { buildingEntity: not null, buildingEntity: var buildingEntity })
			{
				if (buildingEntity.proto.IsUnique && ((IBuildingHost)DolocAPI.CurrentRoom).CountBuilding(buildingEntity.proto.Id) == 0)
				{
					DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(StaticTexts.BuilderExitErrSoleBuilding, buildingEntity.Title));
					return;
				}
				if (buildingEntity.room.DM_animal.AllAnimals.Any())
				{
					DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(StaticTexts.BuilderExitErrAnimal, buildingEntity.Title));
					return;
				}
				if (buildingEntity.room.DM_equipment.AllEquipments.Any((Equipment equipment) => equipment.IsOccupy))
				{
					DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(StaticTexts.BuilderExitErrOccupied, buildingEntity.Title));
					return;
				}
			}
		}
		DolocAPI.ShowQuestionBox(DolocUtils.Format(StaticTexts.BuilderExitConfirm, (from item in tempBuildingList.ReadAll()
			select ((ItemBuilding)item).buildingEntity.Title).HandleJoinString()), delegate
		{
			gameController.PopState();
			if (ValidOperation)
			{
				DolocAPI.StartDialogueNode(DolocAPI.GlobalParameter.BuilderAnimationNode);
			}
			buildingBuilder.DismantleAllTempBuilding();
		}, null, firstSelectConfirm: false);
	}

	private void SwitchInventory(bool inQuickInventory)
	{
		if (!(_currentBuilderState is BuildingBuilder))
		{
			_inQuickInventory = true;
			quickInventory.GetFocus();
			quickInventory.SetCanvasGroupAlpha(1f);
			buildingBuilder.TidyBuildingInventory();
			tempBuildingInventory.FoldBuildingList();
			tempBuildingInventory.Hide();
			return;
		}
		tempBuildingInventory.Show();
		if (_inQuickInventory != inQuickInventory)
		{
			_inQuickInventory = inQuickInventory;
			if (_inQuickInventory)
			{
				quickInventory.GetFocus();
				quickInventory.SetCanvasGroupAlpha(1f);
				buildingBuilder.TidyBuildingInventory();
				tempBuildingInventory.FoldBuildingList();
				_currentBuilderState.RunBuilder(null);
			}
			else
			{
				quickInventory.LoseFocus();
				quickInventory.SetCanvasGroupAlpha(0.5f);
				tempBuildingInventory.ExpandBuildingList();
			}
		}
	}
}
