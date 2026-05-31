using System;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public static class DecalExtension
{
	public static void AttachToDecalHost(this IDecal decal, IDecalHost host, int index)
	{
		decal.SetDecalHost(host);
		decal.DecalInfo = new DecalInfo(decal.DecalHost.GetType().Name, decal.DecalHost.index, index);
		decal.DecalHost._AttachDecal(decal);
	}

	public static void RemoveFromDecalHost(this IDecal decal)
	{
		decal.DecalHost?._RemoveDecal(decal);
	}

	public static Vector2 GetPositionAroundHost(this IDecal decal, float distance = 2f)
	{
		float num = UnityEngine.Random.Range(0f - distance, distance);
		Vector3 vector = decal.DecalHost?.WorldPosition ?? decal.WorldPosition;
		RoomGeometry geometry = decal.CurrentRoom.Geometry;
		vector.x += num;
		vector.x = Math.Clamp(vector.x, geometry.roomPosition.x + 1.5f, geometry.roomPosition.x + geometry.roomSize.x - 1.5f);
		return new Vector2(vector.x, vector.y + 1.5f);
	}

	public static bool TakeOffDecal(this IDecal decal, bool putInBackpack)
	{
		decal.RemoveFromDecalHost();
		decal.OnTakeOff();
		Equipment equipment = (Equipment)decal;
		equipment.Host.RemoveEquipment(equipment, putInBackpack);
		return true;
	}
}
