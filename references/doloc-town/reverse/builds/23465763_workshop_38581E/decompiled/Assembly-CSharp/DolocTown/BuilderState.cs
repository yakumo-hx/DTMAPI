using System;
using DolocTown.Config;
using DolocTown.Config.Localization;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public abstract class BuilderState<T, TC> : IBuilderState where T : Item where TC : TerrainContent
{
	protected bool Turn;

	protected T CurrentItem;

	protected TC CurrentContent;

	protected TC CheckedContent;

	protected TerrainContentCheckedRenderer ContentCheckedRenderer;

	protected Vector2Int CurrentCellPosition = DolocAPI.CurrentRoom.Geometry.CalcMinCellPosition(DolocAPI.AgentPosition);

	protected Vector2Int LastCellPosition = Vector2Int.zero;

	private readonly RSTimer _joyStickTimer = new RSTimer(0.1f);

	private readonly Timer _dPanTimer = new Timer(DolocAPI.GlobalParameter.BuilderMoveTimer_Ref);

	public abstract bool Construct { get; }

	public Vector2Int BuilderCellPosition => CurrentCellPosition;

	protected Room CurrentRoom => DolocAPI.CurrentRoom;

	protected DolocUserInput userInput => DolocAPI.UserInput;

	protected TbStaticText StaticTexts => DolocConfig.StaticTexts;

	protected LinearInventory backpack => DolocAPI.archiveHandle.InventorySystem.inventory;

	protected bool PutInBackpack => true;

	protected abstract T SelectedItem { get; set; }

	protected abstract TC SelectedContent { get; set; }

	protected Action<float> positionUpdateFunc { get; set; }

	protected abstract void OnPosMoved(Vector2Int pos);

	protected abstract void ShowTerrainContentInfo(Vector2Int pos);

	protected abstract void CreateIndicator();

	protected abstract void RecycleIndicator();

	protected abstract void ConfirmBuild(bool showMessage = true);

	protected abstract void Revocation();

	protected abstract void Dismantle(bool showMessage = true);

	protected virtual void TurnIndicator()
	{
	}

	protected virtual void WaitMove()
	{
	}

	protected virtual void DrawOccupied()
	{
	}

	protected virtual void RecycleOccupiedGrid()
	{
	}

	protected virtual bool IsExistsContent(Vector2Int pos)
	{
		return CurrentRoom.DM_terrain.QueryContent<TC>(pos, out CheckedContent);
	}

	private void __UpdateFunc_KM(float dt)
	{
		if (UpdateMousePosition(userInput.MousePosition))
		{
			OnPosMoved(CurrentCellPosition);
		}
	}

	private void __UpdateFunc_JoyStick(float dt)
	{
		__UpdatePosition_JoyStick(dt);
	}

	public void SetBuilderCellPosition(Vector2Int pos)
	{
		CurrentCellPosition = pos;
	}

	public void OnInputDeviceChanged(DolocInputDeviceType type)
	{
		Action<float> action = ((type != 0) ? new Action<float>(__UpdateFunc_JoyStick) : new Action<float>(__UpdateFunc_KM));
		positionUpdateFunc = action;
	}

	public virtual void OnUpdateMoveCamera(Vector2 delta)
	{
	}

	public virtual bool ItemFilter(Item item)
	{
		if (item is T)
		{
			return Construct;
		}
		return false;
	}

	public virtual void RunBuilder(Item item)
	{
		Turn = false;
		Revocation();
		if (ContentCheckedRenderer == null)
		{
			ContentCheckedRenderer = new TerrainContentCheckedRenderer();
		}
		IsExistsContent(CurrentCellPosition);
	}

	public virtual void ExitBuilder()
	{
		ContentCheckedRenderer?.Dispose();
		ContentCheckedRenderer = null;
	}

	public virtual bool OnUpdate(float deltaTime)
	{
		Vector2Int lastCellPosition = LastCellPosition;
		positionUpdateFunc(deltaTime);
		if (ContentCheckedRenderer != null)
		{
			ShowTerrainContentInfo(CurrentCellPosition);
		}
		if (userInput.BuilderSelected)
		{
			if (SelectedItem != null)
			{
				ConfirmBuild();
				return true;
			}
			if (CheckedContent != null && SelectedContent == null)
			{
				WaitMove();
				return true;
			}
			if (SelectedContent != null)
			{
				ConfirmBuild();
				return true;
			}
		}
		else if (userInput.BuilderRevocationPressed)
		{
			if (SelectedItem != null)
			{
				SelectedItem = null;
				return true;
			}
			if (SelectedContent != null)
			{
				Revocation();
				return true;
			}
		}
		else
		{
			if (userInput.BuilderRotatePressed)
			{
				TurnIndicator();
				return true;
			}
			if (userInput.BuilderSelectedInProgress && SelectedItem != null && lastCellPosition != CurrentCellPosition)
			{
				ConfirmBuild(showMessage: false);
				return true;
			}
		}
		if (userInput.BuilderDismantlePressed)
		{
			Dismantle();
			return true;
		}
		if (userInput.BuilderDismantleInProgress && lastCellPosition != CurrentCellPosition)
		{
			Dismantle(showMessage: false);
			return true;
		}
		return false;
	}

	public virtual string[] GetOperateTip(DolocInputDeviceType type)
	{
		if (type == DolocInputDeviceType.KeyboardMouse)
		{
			return new string[9] { StaticTexts.BuilderActionMoveCamera, StaticTexts.BuilderPanelSwitchTerrainLayer, StaticTexts.BuilderActionSelectedContent, StaticTexts.BuilderActionUndo, StaticTexts.BuilderActionDismantle, StaticTexts.BuilderActionSelectedItem, StaticTexts.BuilderActionRollingBackpack, StaticTexts.BuilderActionToggleBackpack, StaticTexts.BuilderPanelExit };
		}
		return new string[9] { StaticTexts.BuilderActionMoveCamera, StaticTexts.BuilderPanelSwitchTerrainLayer, StaticTexts.BuilderActionMove, StaticTexts.BuilderActionSelectedContent, StaticTexts.BuilderPanelUndoGamepad, StaticTexts.BuilderActionDismantle, StaticTexts.BuilderActionSelectedItem, StaticTexts.BuilderActionRollingBackpack, StaticTexts.BuilderActionToggleBackpackGamepad };
	}

	protected T CostItemFromInventory(LinearInventory inventory, Item item)
	{
		int num = inventory.IndexOf(item);
		if (num < 0)
		{
			return null;
		}
		if (item.count > 1)
		{
			item.count--;
			inventory.ReEmit(num);
			return (T)item;
		}
		inventory.Take(num);
		return null;
	}

	protected void ShowErrorMessage(string message, bool show = true)
	{
		if (show)
		{
			DolocAPI.ShowMessageBoxSmallErr(message);
		}
	}

	private bool UpdateMousePosition(Vector2 mousePosition)
	{
		Vector2 positionWS = DolocAPI.ScreenToWorld(mousePosition);
		CurrentCellPosition = CurrentRoom.Geometry.CalcMinCellPosition(positionWS);
		if (CurrentCellPosition == LastCellPosition)
		{
			return false;
		}
		LastCellPosition = CurrentCellPosition;
		IsExistsContent(CurrentCellPosition);
		return true;
	}

	private void __UpdatePosition_JoyStick(float deltaTime)
	{
		if (_joyStickTimer.Tick(deltaTime))
		{
			if (userInput.LeftJoyStickValue.x <= -0.3f)
			{
				ManualMoveLeft();
			}
			if (userInput.LeftJoyStickValue.x >= 0.3f)
			{
				ManualMoveRight();
			}
			if (userInput.LeftJoyStickValue.y <= -0.3f)
			{
				ManualMoveDown();
			}
			if (userInput.LeftJoyStickValue.y >= 0.3f)
			{
				ManualMoveUp();
			}
		}
	}

	public void PreciseMovement_JoyStick(float deltaTime)
	{
		if (userInput.BuilderDPadLeftPressed)
		{
			_dPanTimer.ReStart();
			ManualMoveLeft();
		}
		else if (userInput.BuilderDPadRightPressed)
		{
			_dPanTimer.ReStart();
			ManualMoveRight();
		}
		else if (userInput.BuilderDPadDownPressed)
		{
			_dPanTimer.ReStart();
			ManualMoveDown();
		}
		else if (userInput.BuilderDPadUpPressed)
		{
			_dPanTimer.ReStart();
			ManualMoveUp();
		}
		else if (userInput.BuilderDPadUpInProgress)
		{
			if (_dPanTimer.Update(deltaTime))
			{
				ManualMoveUp();
			}
		}
		else if (userInput.BuilderDPadDownInProgress)
		{
			if (_dPanTimer.Update(deltaTime))
			{
				ManualMoveDown();
			}
		}
		else if (userInput.BuilderDPadLeftInProgress)
		{
			if (_dPanTimer.Update(deltaTime))
			{
				ManualMoveLeft();
			}
		}
		else if (userInput.BuilderDPadRightInProgress && _dPanTimer.Update(deltaTime))
		{
			ManualMoveRight();
		}
	}

	private void ManualMoveLeft()
	{
		if (CurrentContent == null && CurrentItem == null)
		{
			TC checkedContent = CheckedContent;
			while (MoveLeft() && IsExistsContent(CurrentCellPosition) && checkedContent == CheckedContent)
			{
			}
		}
		else
		{
			MoveLeft();
		}
		OnPosMoved(CurrentCellPosition);
	}

	private void ManualMoveRight()
	{
		if (CurrentContent == null && CurrentItem == null)
		{
			TC checkedContent = CheckedContent;
			while (MoveRight() && IsExistsContent(CurrentCellPosition) && checkedContent == CheckedContent)
			{
			}
		}
		else
		{
			MoveRight();
		}
		OnPosMoved(CurrentCellPosition);
	}

	private void ManualMoveDown()
	{
		if (CurrentContent == null && CurrentItem == null)
		{
			TC checkedContent = CheckedContent;
			while (MoveDown() && IsExistsContent(CurrentCellPosition) && checkedContent == CheckedContent)
			{
			}
		}
		else
		{
			MoveDown();
		}
		OnPosMoved(CurrentCellPosition);
	}

	private void ManualMoveUp()
	{
		if (CurrentContent == null && CurrentItem == null)
		{
			TC checkedContent = CheckedContent;
			while (MoveUp() && IsExistsContent(CurrentCellPosition) && checkedContent == CheckedContent)
			{
			}
		}
		else
		{
			MoveUp();
		}
		OnPosMoved(CurrentCellPosition);
	}

	private bool MoveUp()
	{
		if (CurrentCellPosition.y >= CurrentRoom.RoomGridSize.y)
		{
			return false;
		}
		LastCellPosition = CurrentCellPosition;
		CurrentCellPosition.y++;
		return true;
	}

	private bool MoveDown()
	{
		if (CurrentCellPosition.y <= 0)
		{
			return false;
		}
		LastCellPosition = CurrentCellPosition;
		CurrentCellPosition.y--;
		return true;
	}

	private bool MoveLeft()
	{
		if (CurrentCellPosition.x <= 0)
		{
			return false;
		}
		LastCellPosition = CurrentCellPosition;
		CurrentCellPosition.x--;
		return true;
	}

	private bool MoveRight()
	{
		if (CurrentCellPosition.x >= CurrentRoom.RoomGridSize.x)
		{
			return false;
		}
		LastCellPosition = CurrentCellPosition;
		CurrentCellPosition.x++;
		return true;
	}
}
