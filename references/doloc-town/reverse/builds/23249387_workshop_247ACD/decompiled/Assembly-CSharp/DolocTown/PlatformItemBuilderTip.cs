using DolocTown.Config.Platform;
using UnityEngine;

namespace DolocTown;

public class PlatformItemBuilderTip : BuilderTipBase
{
	private PlatformInfo platformProto;

	private PlatformBuilderRenderer renderer;

	private PlatformBuilderHelper _builderHelper;

	private bool isLocked;

	private bool canBuildNow;

	private Vector2Int currentCellPosition;

	protected override bool IsValid => platformProto != null;

	private IPlatformHost PlatformHost => base.CurrentRoom;

	protected override void OnPosMoved(Vector2Int pos)
	{
		if (isLocked)
		{
			_builderHelper.RaycastGround(currentCellPosition);
			canBuildNow = _builderHelper.canBuildNow;
			renderer.BorderPosition = BuilderUtils.GetScreenLatticePosition(currentCellPosition, base.RoomPosition);
			renderer.BorderValid = canBuildNow;
		}
		else
		{
			canBuildNow = base.CurrentRoom.DM_terrain.IsStructureConstructable(pos);
			renderer.BorderPosition = BuilderUtils.GetScreenLatticePosition(currentCellPosition, base.RoomPosition);
			renderer.BorderValid = canBuildNow;
		}
	}

	public override void OnUpdate(float deltaTime)
	{
		if (DolocAPI.IsGameInitialized)
		{
			base.__positionUpdateFunc(deltaTime);
			OnPosMoved(currentCellPosition);
		}
	}

	protected override void __UpdateFunc_KM(float dt)
	{
		Vector2 positionWS = DolocAPI.ScreenToWorld(DolocAPI.UserInput.MousePosition);
		currentCellPosition = base.CurrentRoom.Geometry.CalcMinCellPosition(positionWS);
	}

	protected override void __UpdateFunc_JoyStick(float dt)
	{
		if (_timer.Tick(dt))
		{
			Vector2Int agentRoomCellPosition = DolocAPI.AgentRoomCellPosition;
			JoyStickMoveOffset += DolocAPI.UserInput.AssistMove * 12f;
			if (JoyStickMoveOffset == Vector2.zero)
			{
				currentCellPosition = GetFrontPosition(1);
				return;
			}
			Vector2Int vector2Int = Vector2Int.FloorToInt(JoyStickMoveOffset / 12f);
			currentCellPosition = agentRoomCellPosition + vector2Int;
		}
	}

	protected override void OnEnter()
	{
		base.OnEnter();
		isLocked = false;
		renderer = new PlatformBuilderRenderer();
		renderer.DraftPosition = new Vector3(base.RoomPosition.x, base.RoomPosition.y, -10f);
		_builderHelper = new PlatformBuilderHelper(base.CurrentRoom, renderer, platformProto);
	}

	private void TryBuild()
	{
		if (!_builderHelper.IsPlatformConstructable())
		{
			return;
		}
		isLocked = false;
		canBuildNow = false;
		renderer.ClearDraft();
		renderer.DraftValid = false;
		PlatformGeometry platformGeometry = _builderHelper.platformGeometry;
		IDropItemHost currentRoom = base.CurrentRoom;
		if (_builderHelper.isReplace)
		{
			PlatformHost.ReplacePlatform(platformGeometry, platformProto, out var oldPlatformProtoName);
			foreach (Vector2Int allPosition in platformGeometry.AllPositions)
			{
				Vector2 startPos = base.CurrentRoom.Geometry.CalcWorldPositionCenter(allPosition);
				currentRoom.CreateDropItemAnimated(new CountItem(oldPlatformProtoName, 1), startPos, shouldSendMsg: false);
			}
		}
		else
		{
			PlatformHost.CreatePlatform(platformProto, platformGeometry, _builderHelper.platformCutInfos, out var returnCost);
			foreach (var item in returnCost)
			{
				currentRoom.CreateDropItemAnimated(item.Item1, item.Item2, shouldSendMsg: false);
			}
		}
		base.CurrentRoom.OnPlatformChanged();
		DolocAPI.cameraController.ShakeScreen(0.2f, DolocAPI.GlobalParameter.PlatformShakeIntensity);
		DolocAPI.BroadcastString(GameEventType.BUILD_PLATFORM, platformProto.Id);
		DolocAPI.CostSelectedItem(platformGeometry.TileCount);
	}

	public override void RunBuilder(Item item)
	{
		if (base.CurrentRoom != null && item is ItemPlatform itemPlatform)
		{
			platformProto = itemPlatform.PlatformProto;
			if (base.CurrentRoom.RoomConstructInfo.AllowBuildPlatform)
			{
				OnEnter();
			}
		}
	}

	public override void ExitBuilder()
	{
		platformProto = null;
		isLocked = false;
		renderer?.Dispose();
		renderer = null;
		base.ExitBuilder();
	}

	public override bool ConfirmBuild()
	{
		if (!IsValid)
		{
			return false;
		}
		if (!base.CurrentRoom.RoomConstructInfo.AllowBuildPlatform)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrBuildSystemNotSupport);
			return false;
		}
		if (isLocked)
		{
			TryBuild();
		}
		else if (canBuildNow)
		{
			isLocked = true;
			_builderHelper.LockedPosition = currentCellPosition;
		}
		else
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrInvalidPosition);
		}
		return true;
	}

	public void CancelBuild()
	{
		isLocked = false;
		canBuildNow = false;
		renderer?.ClearDraft();
	}
}
