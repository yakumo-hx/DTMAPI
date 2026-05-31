using DolocTown.Config.TechTree;
using UnityEngine;

namespace DolocTown;

public abstract class AgentStateFishing : AgentStateBase
{
	private static bool _isUiControlled;

	protected ItemFishingRod _fishRod => body.FishingCache.FishingRod;

	public override bool SupportInteract => false;

	public override bool SupportDash => false;

	public override bool SupportUseItem => false;

	public override bool SupportJump => false;

	public override bool SupportScrollQuickInventoryUI => false;

	public static bool IsUiControlled
	{
		get
		{
			return _isUiControlled;
		}
		protected set
		{
			_isUiControlled = value;
			if (value)
			{
				UseFishCam();
				DolocAPI.uiSystem.inventoryQuick.Hide();
				DolocAPI.uiSystem.SetSceneUIVisible(value: false);
				DolocAPI.SetResidentUiInteractable(value: false);
			}
			else
			{
				ResetCam();
				DolocAPI.uiSystem.inventoryQuick.Show();
				DolocAPI.uiSystem.SetSceneUIVisible(value: true);
				DolocAPI.SetResidentUiInteractable(value: true);
			}
		}
	}

	protected AgentStateFishing(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	public static void UnsetUiControl()
	{
		IsUiControlled = false;
	}

	public static void BreakFishing()
	{
		ResetCam();
		DolocAPI.agent.fishRodRenderer.SetVisible(value: false);
		OnCompleteFishing(isBreaking: true);
	}

	protected bool RollFish()
	{
		return body.FishingCache.RollFish();
	}

	private static void UseFishCam()
	{
		DolocAPI.cameraController.SetFollow(DolocAPI.agent.fishRodRenderer.Hook.transform);
		DolocAPI.cameraController.SetMoveSpeed(1f);
		DolocAPI.cameraController.SetMoveFunction(x: true, y: false);
	}

	protected static void ReceiveItem(bool isBreaking)
	{
		BodyController agent = DolocAPI.agent;
		Item fishItem = agent.FishingCache.FishItem;
		if (fishItem != null && !agent.FishingCache.IsFailed)
		{
			Room currentRoom = DolocAPI.CurrentRoom;
			Vector3 agentPosition = DolocAPI.AgentPosition;
			agentPosition.y += 1.5f;
			bool bounce;
			Vector2 vector = ((IDropItemHost)currentRoom).GetRandomDropLocation(offset: (float)((!DolocAPI.AgentFaceRight) ? 1 : (-1)) * Random.value * 1.5f * 2f, start: (Vector2)agentPosition, bounce: out bounce);
			DropItemBase dropItemBase = ((IDropItemHost)currentRoom).CreateDropItemNoRender(fishItem, vector, shouldSendMsg: true);
			((IDropItemHost)currentRoom).RenderDropItem(DolocAPI.EntitySystem.Next<DropItemRenderer>(), dropItemBase);
			if (!isBreaking)
			{
				dropItemBase.Renderer.Parabola(agent.fishRodRenderer.Hook.transform.position, vector, useBounce: false);
			}
		}
	}

	protected static void OnCompleteFishing(bool isBreaking)
	{
		BodyController agent = DolocAPI.agent;
		if (agent.FishingCache.FishItem != null && !agent.FishingCache.IsFailed)
		{
			DolocAPI.SendCatchFishEvent(agent.FishingCache.FishProto);
			ReceiveItem(isBreaking);
			DolocAPI.AddTechExp(TechPointType.FISHING, agent.FishingCache.FishProto.ExpFishing);
		}
	}

	protected static void HandleLongDistanceFishingAchievement()
	{
		BodyController agent = DolocAPI.agent;
		if (!agent.FishingCache.IsFailed)
		{
			int value = Mathf.RoundToInt(Mathf.Abs(agent.fishRodRenderer.Hook.transform.position.y - agent.transform.position.y));
			DolocAPI.Broadcast(GameEventType.FISHING_VERTICAL_DISTANCE, new GameEventArgsInt(value));
		}
	}

	private static void ResetCam()
	{
		DolocAPI.cameraController.SetFollow(DolocAPI.agent.transform);
		DolocAPI.cameraController.SetMoveSpeed();
		Room currentRoom = DolocAPI.CurrentRoom;
		DolocAPI.cameraController.SetRoomRange(currentRoom.CameraPosition, currentRoom.CameraSize);
	}

	protected void PlayFishRodAnimation(string name)
	{
		if (_fishRod != null)
		{
			body.fishRodRenderer.Play(_fishRod.name, name);
		}
	}
}
