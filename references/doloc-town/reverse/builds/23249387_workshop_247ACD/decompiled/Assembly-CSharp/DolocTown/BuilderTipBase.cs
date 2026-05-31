using System;
using DolocTown.Config;
using DolocTown.Config.Localization;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public abstract class BuilderTipBase
{
	protected Vector2 JoyStickMoveOffset;

	protected bool Turn;

	protected readonly RSTimer _timer = new RSTimer(0.1f);

	protected abstract bool IsValid { get; }

	protected Room CurrentRoom => DolocAPI.CurrentRoom;

	protected TbStaticText StaticTexts => DolocConfig.StaticTexts;

	protected Vector2 RoomPosition => CurrentRoom.RoomPosition;

	private Vector2Int RoomGridSize => CurrentRoom.RoomGridSize;

	protected Action<float> __positionUpdateFunc { get; private set; }

	public abstract void RunBuilder(Item item);

	public abstract bool ConfirmBuild();

	public abstract void OnUpdate(float deltaTime);

	public virtual void TurnIndicator()
	{
	}

	protected virtual void OnEnter()
	{
		Turn = false;
		JoyStickMoveOffset = Vector2.zero;
		__positionUpdateFunc = GetUpdateFunc(DolocAPI.UserInput.DeviceType);
		DolocAPI.dolocBuilder.EnterHelpState(RoomPosition, RoomGridSize);
	}

	public virtual void ExitBuilder()
	{
		DolocAPI.dolocBuilder.ExitHelpState();
	}

	public void OnInputDeviceChanged(DolocInputDeviceType type)
	{
		__positionUpdateFunc = GetUpdateFunc(type);
	}

	protected abstract void OnPosMoved(Vector2Int pos);

	protected abstract void __UpdateFunc_JoyStick(float dt);

	protected abstract void __UpdateFunc_KM(float dt);

	private Action<float> GetUpdateFunc(DolocInputDeviceType type)
	{
		if (type == DolocInputDeviceType.KeyboardMouse)
		{
			return __UpdateFunc_KM;
		}
		return __UpdateFunc_JoyStick;
	}

	protected Vector2Int GetFrontPosition(int width)
	{
		Vector2Int agentRoomCellPosition = DolocAPI.AgentRoomCellPosition;
		if (!DolocAPI.AgentFaceRight)
		{
			agentRoomCellPosition.x -= width;
		}
		return agentRoomCellPosition;
	}
}
