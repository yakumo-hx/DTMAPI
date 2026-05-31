using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemMotorKey : Item, IActiveItem
{
	public ItemMotorKey(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemMotorKey(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		OnUse();
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		OnUse();
	}

	private Vector2 GetMotorFlyPosition()
	{
		return DolocAPI.agent.PositionCenter;
	}

	public void OnUse()
	{
		if (!DolocAPI.archiveHandle.IsMotorUnlocked())
		{
			Debug.Log("摩托还未解锁");
			return;
		}
		Room currentRoom = DolocAPI.CurrentRoom;
		if (currentRoom == null)
		{
			return;
		}
		if (currentRoom.baseProto.isInHouse || currentRoom.DisableMotor)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrCannotCallMotor);
			return;
		}
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_MOTOR_KEY);
		if (DolocAPI.archiveHandle.farmData.agentData.motorData.CurrentRoom != currentRoom || !(Vector2.Distance(DolocAPI.agent.PositionCenter, DolocAPI.Motor.position) <= 1.5f))
		{
			Vector2 vector = _GetDirToAgent();
			DolocAPI.IntersectEdgeFromScreenPoint(DolocAPI.WorldToScreen(DolocAPI.agent.PositionCenter), -vector, DolocAPI.screenManager.screenSize * DolocAPI.screenManager.screenScale, out var hit, out var _);
			Vector2 vector2 = DolocAPI.ScreenToWorld(hit) - vector * 8f;
			if (DolocAPI.archiveHandle.farmData.agentData.motorData.CurrentRoom != currentRoom || (vector2 - DolocAPI.agent.PositionCenter).magnitude < (DolocAPI.agent.PositionCenter - (Vector2)DolocAPI.Motor.position).magnitude)
			{
				DolocAPI.SetMotorPosition(currentRoom, vector2);
			}
			DolocAPI.Motor.AutoFlyTo(GetMotorFlyPosition);
			DolocAPI.archiveHandle.UpdateMotorRoom(currentRoom);
		}
	}

	private Vector2 _GetDirToAgent()
	{
		Room currentRoom = DolocAPI.archiveHandle.farmData.agentData.motorData.CurrentRoom;
		Room currentRoom2 = DolocAPI.CurrentRoom;
		if (currentRoom2 == currentRoom || currentRoom2.SceneRawName == currentRoom?.SceneRawName)
		{
			return (DolocAPI.agent.PositionCenter - (Vector2)DolocAPI.Motor.position).normalized;
		}
		if (DolocAPI.GetMotorDirToAgentInMap(out var dir))
		{
			return dir;
		}
		return Vector2.down;
	}

	public override bool IsSame(Item other)
	{
		return false;
	}
}
