using RedSaw;
using UnityEngine;

namespace DolocTown;

public class BuildingLinkInfo
{
	public readonly Building targetBuilding;

	public readonly BuildingLinkType linkType;

	public readonly int diff;

	public Vector2 remotePosition { get; private set; }

	public bool IsValid { get; private set; }

	public int originIndex { get; private set; }

	public int index { get; private set; }

	public BuildingLinkInfo(Building targetBuilding, BuildingLinkType linkType, int diff)
	{
		this.targetBuilding = targetBuilding;
		this.linkType = linkType;
		this.diff = diff;
	}

	public void __ResetIndex(int index, int originIndex)
	{
		this.originIndex = Mathf.Max(0, originIndex);
		this.index = Mathf.Max(0, index);
		if (targetBuilding != null)
		{
			remotePosition = GetDefaultLinkGateRemotePosition(targetBuilding, linkType, index);
		}
	}

	public void __RecalcRemotePosition(Vector2 position)
	{
		remotePosition = position;
		IsValid = true;
	}

	public void __RecalcRemotePosition(Building current)
	{
		if (targetBuilding.GenLinkMap(shouldCalculateRemote: false).TryGetLinkTo(current, out var linkInfo) && targetBuilding.TryGetLinkGatePosition(linkInfo.linkType, linkInfo.index, out var position))
		{
			remotePosition = position;
			IsValid = true;
		}
		else
		{
			IsValid = false;
			remotePosition = ConstraintToRoom(GetDefaultLinkGateRemotePosition(targetBuilding, linkType, index));
		}
	}

	private Vector2 ConstraintToRoom(Vector2 position)
	{
		if (!Geometry.GetClosestPointOnPolygon(targetBuilding.room.Geometry.polygon, position, out var closestPoint))
		{
			Vector2 vector = targetBuilding.InnerSize * DolocTransform.TILE_WORLD_SIZE;
			Vector2 roomPosition = targetBuilding.room.RoomPosition;
			Vector2 lhs = roomPosition + new Vector2(2.5f, 2.5f);
			Vector2 lhs2 = roomPosition + vector - new Vector2(2.5f, 2.5f);
			return Vector2.Max(lhs, Vector2.Min(lhs2, position));
		}
		Vector2 normalized = (closestPoint - position).normalized;
		return closestPoint + normalized * 2.5f;
	}

	private Vector2 GetDefaultLinkGateRemotePosition(Building building, BuildingLinkType type, int idx)
	{
		Vector2 vector = building.InnerSize * DolocTransform.TILE_WORLD_SIZE;
		Vector2 roomPosition = building.room.RoomPosition;
		return type switch
		{
			BuildingLinkType.Left => roomPosition + new Vector2(vector.x - 1.5f, vector.y * 0.5f), 
			BuildingLinkType.Right => roomPosition + new Vector2(1.5f, vector.y * 0.5f), 
			BuildingLinkType.Top => roomPosition + new Vector2(vector.x * 0.5f, 1.5f), 
			BuildingLinkType.Bottom => roomPosition + new Vector2(vector.x * 0.5f, vector.y - 1.5f), 
			_ => default(Vector2), 
		};
	}

	public override string ToString()
	{
		return $"\"{targetBuilding.templateRoomName}({targetBuilding.room.Title})\" Type:{linkType} Index:{index} OriginIndex: {originIndex} IsValid:{IsValid} RemotePos:{remotePosition}";
	}
}
