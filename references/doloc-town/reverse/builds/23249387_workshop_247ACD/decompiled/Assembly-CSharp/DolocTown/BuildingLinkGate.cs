using System;
using DolocTown.Config.Room;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public class BuildingLinkGate : DolocObject, IGate
{
	[SerializeField]
	private BuildingLinkType linkType;

	[SerializeField]
	[Min(0f)]
	private int index;

	[SerializeField]
	private TouchIndicator indicator;

	[SerializeField]
	private Transform tipPos;

	[SerializeField]
	public Transform agentReference;

	public BuildingLinkGateId GateId => new BuildingLinkGateId(linkType, index);

	public BuildingLinkInfo LinkInfo { get; private set; }

	private string RemoteBuildingTitle
	{
		get
		{
			if (LinkInfo == null)
			{
				return "未设置联通信息";
			}
			return LinkInfo.targetBuilding.proto.Title + "(" + LinkInfo.targetBuilding.room.Title + ")";
		}
	}

	private string RemoteBuildingPrompt => LinkInfo?.targetBuilding?.Title ?? "";

	public bool AvailableToMotor => false;

	public bool NeedInteract
	{
		get
		{
			if (LinkInfo == null)
			{
				return true;
			}
			return LinkInfo.linkType == BuildingLinkType.Bottom;
		}
	}

	public bool KeepHorizontalSpeed => false;

	public bool KeepVerticalSpeed => false;

	public PortalInteractKey InteractKey => PortalInteractKey.Exit;

	public void RepairRotation()
	{
		Transform child = base.transform.GetChild(0);
		if (child == null)
		{
			Debug.LogError("BuildingLinkGate.RepairRotation: 没有找到传送门精灵对象");
			return;
		}
		RepairColliderRotation();
		base.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
		float gateRotationZ = linkType.GetGateRotationZ();
		child.rotation = Quaternion.Euler(0f, 0f, gateRotationZ);
		ResetAgentReference();
	}

	private void RepairColliderRotation()
	{
		BoxCollider2D component = GetComponent<BoxCollider2D>();
		if (component == null)
		{
			return;
		}
		float z = base.transform.rotation.eulerAngles.z;
		if (z == 0f)
		{
			return;
		}
		Vector2 offset = component.offset;
		Vector2 size = component.size;
		if (z <= 0f)
		{
			if (z == -180f)
			{
				goto IL_00ac;
			}
			if (z != -90f)
			{
				_ = 0f;
				return;
			}
		}
		else
		{
			if (z == 90f)
			{
				component.offset = new Vector2(0f - offset.y, offset.x);
				component.size = new Vector2(size.y, size.x);
				return;
			}
			if (z == 180f)
			{
				goto IL_00ac;
			}
			if (z != 270f)
			{
				return;
			}
		}
		component.offset = new Vector2(offset.y, offset.x);
		component.size = new Vector2(size.y, size.x);
		return;
		IL_00ac:
		component.offset = new Vector2(offset.x, 0f - offset.y);
	}

	private void ResetAgentReference()
	{
		if (agentReference != null)
		{
			agentReference.rotation = Quaternion.Euler(0f, 0f, 0f);
		}
		else
		{
			Debug.LogError($"{GateId} 没有设置参考对象");
		}
	}

	public void SetTouchIndicatorShowRemoteBuildingTitle()
	{
		GetComponentInChildren<TouchIndicator>(includeInactive: true).showRemoteDoorInformation = linkType != BuildingLinkType.Bottom;
	}

	public void Setup(BuildingLinkInfo linkInfo, Vector2 position)
	{
		position2d = position;
		LinkInfo = linkInfo;
	}

	public void Setup(BuildingLinkInfo linkInfo)
	{
		SetVisible(value: true);
		LinkInfo = linkInfo;
		if (agentReference != null)
		{
			agentReference.gameObject.SetActive(value: false);
		}
		GetComponent<Collider2D>().isTrigger = true;
	}

	public void SetupCollider(Vector2 roomPosition, Vector2 roomSize, BuildingLinkType linkType)
	{
		float boxColliderHeight = linkType.GetBoxColliderHeight();
		float num = boxColliderHeight / 2f;
		BoxCollider2D component = GetComponent<BoxCollider2D>();
		component.isTrigger = true;
		component.size = new Vector2(3f, boxColliderHeight);
		float y = linkType switch
		{
			BuildingLinkType.Bottom => roomPosition.y - position2d.y + num, 
			BuildingLinkType.Left => roomPosition.x - position2d.x + num, 
			BuildingLinkType.Top => position2d.y - (roomPosition.y + roomSize.y) + num, 
			BuildingLinkType.Right => position2d.x - (roomPosition.x + roomSize.x) + num, 
			_ => throw new ArgumentOutOfRangeException("linkType", linkType, null), 
		};
		base.transform.rotation = Quaternion.Euler(0f, 0f, linkType.GetGateRotationZ());
		component.offset = new Vector2(0f, y);
	}

	private void EnterTargetBuilding()
	{
		if (LinkInfo?.targetBuilding == null)
		{
			Debug.LogError("未设置目标房间信息..");
			return;
		}
		Debug.Log("进入目标房间:" + LinkInfo.targetBuilding.BuildingName);
		DolocAPI.EnterRoom(LinkInfo.targetBuilding.room, LinkInfo.remotePosition);
	}

	private void Start()
	{
		indicator.onTouchCallback = ShowTip;
		indicator.onDisTouchCallback = HideTip;
	}

	private void ShowTip()
	{
		switch (linkType)
		{
		case BuildingLinkType.Left:
		{
			Vector2 vector = (Vector2)tipPos.position + new Vector2(0f, 4.5f);
			this.ShowSceneOperationTip(vector, RemoteBuildingPrompt, DolocAPI.GetAsset<Sprite>("keyboard_left_arrow_small"));
			break;
		}
		case BuildingLinkType.Right:
		{
			Vector2 vector = (Vector2)tipPos.position + new Vector2(0f, 4.5f);
			this.ShowSceneOperationTip(vector, RemoteBuildingPrompt, DolocAPI.GetAsset<Sprite>("keyboard_right_arrow_small"));
			break;
		}
		case BuildingLinkType.Top:
		{
			Vector2 vector = (Vector2)tipPos.position + new Vector2(0f, 6f);
			this.ShowSceneOperationTip(vector, RemoteBuildingPrompt, DolocAPI.GetAsset<Sprite>("keyboard_up_arrow_small"));
			break;
		}
		case BuildingLinkType.Bottom:
		{
			Vector2 vector = (Vector2)tipPos.position + new Vector2(0f, 4.5f);
			this.ShowSceneOperationTip(vector, RemoteBuildingPrompt, DolocAPI.UserInput.NormalRoomInteractQuitActionName);
			break;
		}
		}
	}

	private void HideTip()
	{
		this.HideSceneOperationTip();
	}

	private void OnDisable()
	{
		HideTip();
	}

	public void OnTouch()
	{
		if (LinkInfo != null && LinkInfo.linkType != BuildingLinkType.Bottom)
		{
			EnterTargetBuilding();
		}
	}

	public void OnDisTouch()
	{
		this.HideSceneOperationTip();
	}

	public void OnInteract()
	{
		this.HideSceneOperationTip();
		EnterTargetBuilding();
	}
}
