using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class BuildingScanner : Scanner<Building>
{
	private readonly CommonTip indicator;

	private IBuildingHost host;

	private Building currentBuilding;

	private Building lastContentByDoor;

	private bool shouldNotCheck = true;

	public Building CurrentBuilding => currentBuilding;

	public BuildingScanner()
	{
		indicator = DolocAPI.uiSystem.GetFromPoolInScene<CommonTip>();
		indicator.SetVisible(value: false);
	}

	public override void OnRoomChanged(Room newRoom)
	{
		shouldNotCheck = true;
		if (newRoom != null)
		{
			host = newRoom;
			shouldNotCheck = false;
		}
	}

	public override void OnWorldPosChanged(Vector2 positionWS)
	{
		if (!shouldNotCheck && lastContent != null)
		{
			indicator.position = DolocAPI.WorldToScreen(lastContent.ArrowTipPosition);
			indicator.SetVisible(lastContentByDoor == null);
		}
	}

	protected override Building GetContent(Vector2Int pos)
	{
		if (shouldNotCheck)
		{
			return null;
		}
		Building building = lastContentByDoor;
		lastContentByDoor = host.GetBuildingByDoor(pos);
		if (building != lastContentByDoor)
		{
			building?.SetTouchDoorState(value: false);
			lastContentByDoor?.SetTouchDoorState(value: true);
		}
		return host.GetBuilding(pos);
	}

	protected override void OnContentChanged(Building lastContent, Building newContent)
	{
		lastContent?.OnDisTouch();
		if (newContent != null)
		{
			newContent.OnTouch();
			currentBuilding = newContent;
		}
		else
		{
			indicator.SetVisible(value: false);
			currentBuilding = null;
		}
	}

	public void ClearBuffer()
	{
		lastContent = null;
		lastContentByDoor = null;
		indicator.SetVisible(value: false);
		currentBuilding?.OnDisTouch();
		currentBuilding = null;
	}

	public void TryEnter()
	{
		if (lastContent == lastContentByDoor)
		{
			lastContent?.OnEnter();
		}
	}

	public bool TryInteract()
	{
		if (lastContent != null && lastContent == lastContentByDoor)
		{
			lastContent.OnInteract();
			return true;
		}
		return false;
	}
}
