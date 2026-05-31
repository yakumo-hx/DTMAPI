using System.Linq;
using UnityEngine;

namespace DolocTown;

public class DeepWater : InteractableObjectExclude
{
	private WaterDropChecker[] waterDropCheckers;

	protected override ITouchCheckStrategy touchChecker { get; set; }

	protected override void __Init()
	{
		base.__Init();
		waterDropCheckers = GetComponentsInChildren<WaterDropChecker>();
		touchChecker = new DrowningTriggerChecker();
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		if (DolocAPI.agent.IsFaint)
		{
			return;
		}
		bool isRiding = DolocAPI.IsAgentRiding;
		Vector3 safeMotorPos = DolocAPI.Motor.position + new Vector3(0f, 6f);
		if (isRiding)
		{
			base.isTouched = false;
			DolocAPI.gameStateManager.agentController.GetOffIfRiding();
			DolocAPI.Motor.AutoFlyTo(() => safeMotorPos);
		}
		if (!isRiding && base.currentOtherCollider.gameObject.name.Contains("motor"))
		{
			base.isTouched = false;
			return;
		}
		DolocAPI.SetResidentUiVisible(showBasicTip: false, showQuickInventory: false);
		DolocAPI.agent.Faint(FaintReason.Drowning);
		DolocAPI.archiveHandle.PassTime(DolocAPI.GlobalParameter.GameMinutes2Secs(DolocAPI.GlobalParameter.FaintMinutesAfterDrowning), delegate
		{
			if (isRiding)
			{
				DolocAPI.Motor.AutoFlyTo(() => safeMotorPos);
			}
			DolocAPI.OnWakeUp(saveData: false, sendEvent: false);
			WaterDropChecker latestWaterDropChecker = GetLatestWaterDropChecker();
			if (!(latestWaterDropChecker == null))
			{
				DolocAPI.AgentPosition = latestWaterDropChecker.wakeupPosition;
				DolocAPI.AgentFaceRight = !latestWaterDropChecker.faceLeftAfterWakeup;
				HandleRoomChange(latestWaterDropChecker.targetRoomId, latestWaterDropChecker.wakeupPosition);
				RefreshWater();
				DolocAPI.agent.Stand();
				DolocAPI.agent.UnsetFaint();
				base.isTouched = false;
				DolocAPI.SetResidentUiVisible(showBasicTip: true, showQuickInventory: true);
				if (DolocAPI.AgentEquipmentManager.TryGetAgentEquipmentFunction<AgentEquipmentFunctionFishTank>(out var function))
				{
					function.TryRollFish();
				}
			}
		});
	}

	private void RefreshWater()
	{
		if (DolocAPI.archiveHandle.dungeonData.currentDungeon != null)
		{
			foreach (DungeonRoom allRenderedRoom in DolocAPI.archiveHandle.dungeonData.currentDungeon.AllRenderedRooms)
			{
				allRenderedRoom.LoadSceneHandle().RefreshWaters();
			}
			return;
		}
		DolocAPI.CurrentRoom?.LoadSceneHandle()?.RefreshWaters();
	}

	private WaterDropChecker GetLatestWaterDropChecker()
	{
		if (waterDropCheckers.IsNullOrEmpty())
		{
			return null;
		}
		return waterDropCheckers.OrderByDescending((WaterDropChecker c) => c.passThroughTime).FirstOrDefault();
	}

	private void HandleRoomChange(string targetRoomId, Vector2 remotePosition)
	{
		if (!(DolocAPI.CurrentRoom.RoomId == targetRoomId) && DolocAPI.QueryRoom(targetRoomId, out var room))
		{
			DolocAPI.QuitCurrentRoom(room, shouldUnloadScene: false);
			DolocAPI.RoomGizmos.CurrentRoom = room;
			DolocAPI.AgentPosition = remotePosition;
			DolocAPI.AgentEnabled = true;
			DolocAPI.effectProvider.ClearAllContinusEffects();
			room.OnEnterRoom();
		}
	}
}
