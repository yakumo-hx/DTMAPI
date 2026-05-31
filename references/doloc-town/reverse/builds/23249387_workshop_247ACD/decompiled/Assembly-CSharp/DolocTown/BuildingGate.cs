using DolocTown.Config;
using DolocTown.Config.Room;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
[GameEntityManager("/farm/building_gate", DolocGameAssets.GAME_ENTITY_BUILDING_GATE)]
[GameEntityManager("/farm/building_gate", DolocGameAssets.GAME_ENTITY_BUILDING_GATE_CELLAR, Alias = "cellar_gate")]
public class BuildingGate : GameEntity, IGate
{
	[SerializeField]
	private SpriteRenderer sr;

	[SerializeField]
	private GameObject arrow;

	[SerializeField]
	private float fadeTime = 0.15f;

	public Building TargetBuilding { get; set; }

	public bool UseCacheBuilding { get; set; }

	public bool AvailableToMotor => false;

	public bool NeedInteract => true;

	public bool KeepHorizontalSpeed => false;

	public bool KeepVerticalSpeed => false;

	public PortalInteractKey InteractKey => PortalInteractKey.Exit;

	private void Start()
	{
		Hide();
	}

	private void Show()
	{
		arrow.SetActive(value: true);
	}

	private void Hide()
	{
		arrow.SetActive(value: false);
	}

	public void OnTouch()
	{
		Vector2 vector = base.transform.position;
		vector.y += 4.5f;
		this.ShowSceneOperationTip(vector, DolocConfig.StaticTexts.UiOperationExit, DolocAPI.UserInput.NormalRoomInteractQuitActionName);
		Show();
	}

	public void OnDisTouch()
	{
		this.HideSceneOperationTip();
		Hide();
	}

	public void OnInteract()
	{
		if (DolocAPI.CurrentRoom.Type != RoomType.Farm)
		{
			Debug.LogError("当前建筑不是农场建筑");
		}
		else if (DolocAPI.agent.IsCurrentStateSupportTeleport)
		{
			TemplateRoomInHouse templateRoomInHouse = (TemplateRoomInHouse)DolocAPI.CurrentRoom;
			if (templateRoomInHouse.Building.IsUnique && UseCacheBuilding && Building.LastEnterBuilding != null)
			{
				QuitToBuilding(Building.LastEnterBuilding);
				return;
			}
			Building building = ((IBuildingHost)DolocAPI.CurrentRoom.RootRoom).GetBuilding(templateRoomInHouse.Title) ?? TargetBuilding;
			QuitToBuilding(building);
		}
	}

	private void QuitToBuilding(Building building)
	{
		if (building == null)
		{
			Debug.LogError("没有找到目标建筑");
			return;
		}
		DolocAPI.EnterRoom(building.room.RootRoom, building.EntryPosition);
		DolocAPI.Broadcast(OperationEventType.ENTER_BUILDING);
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		TargetBuilding = null;
		UseCacheBuilding = false;
	}
}
