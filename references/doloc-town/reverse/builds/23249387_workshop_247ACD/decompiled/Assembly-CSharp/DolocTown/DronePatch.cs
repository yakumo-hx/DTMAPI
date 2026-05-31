using System.Collections.Generic;
using DolocTown.Config.Drone;
using UnityEngine;

namespace DolocTown;

public static class DronePatch
{
	private static readonly Vector2[] rayDirections = new Vector2[4]
	{
		Vector2.up,
		Vector2.down,
		Vector2.left,
		Vector2.right
	};

	public static DroneWeaponParams GetDroneWeaponParams(this DroneWeaponInfo proto, DroneChipInfo[] chips = null)
	{
		if (chips.IsNullOrEmpty())
		{
			return new DroneWeaponParams(proto.Attack, proto.CriticalRate, proto.Accuracy, 1f / proto.AttackSpeed, proto.PowerCost, proto.MoveSpeed, proto.AttackDistance, proto.ClipCapacity, proto.ReloadDuration, proto.ExtraBullets, proto.ExtraSectorAngle, (proto.AttackEffects == null) ? null : new string[1] { proto.AttackEffects });
		}
		float num = proto.CriticalRate;
		float num2 = proto.Accuracy;
		float num3 = proto.AttackSpeed;
		float num4 = proto.MoveSpeed;
		float num5 = proto.AttackDistance;
		int num6 = proto.ClipCapacity;
		int extraBullets = proto.ExtraBullets;
		float extraSectorAngle = proto.ExtraSectorAngle;
		float num7 = 0f;
		float num8 = 0f;
		float num9 = 0f;
		int num10 = 0;
		List<string> list = new List<string>();
		if (proto.AttackEffects != null)
		{
			list.Add(proto.AttackEffects);
		}
		foreach (DroneChipInfo droneChipInfo in chips)
		{
			num7 += droneChipInfo.AttackIncrease;
			num10 += droneChipInfo.AttackIncreaseFixed;
			num += droneChipInfo.CriticalRateIncrease;
			num2 += droneChipInfo.AccuracyIncrease;
			num3 += droneChipInfo.AttackSpeedIncrease;
			num4 += droneChipInfo.MoveSpeedIncrease;
			num5 += droneChipInfo.AttackDistanceIncrease;
			num6 += droneChipInfo.ClipCapacityAddition;
			num8 += droneChipInfo.PowerCostDecrease;
			num9 += droneChipInfo.ReloadDurationDecrease;
			if (droneChipInfo.AttackEffectsExternal != null)
			{
				list.Add(droneChipInfo.AttackEffectsExternal);
			}
		}
		int attack = proto.Attack + (int)(num7 * (float)proto.Attack) + num10;
		float powerCost = proto.PowerCost * (1f - Mathf.Clamp01(num8));
		float gunReloadTime = proto.ReloadDuration * (1f - Mathf.Clamp01(num9));
		float attackInterval = 1f / num3;
		return new DroneWeaponParams(attack, num, num2, attackInterval, powerCost, num4, num5, num6, gunReloadTime, extraBullets, extraSectorAngle, list.ToArray());
	}

	public static DroneStructureParams GetDroneParams(this DroneStructureInfo structureProto, DroneEngineInfo[] engines)
	{
		if (engines.IsNullOrEmpty())
		{
			return new DroneStructureParams(structureProto.PowerCapacity, structureProto.PowerRecv, structureProto.MoveSpeed);
		}
		float num = structureProto.PowerCapacity;
		float num2 = structureProto.PowerRecv;
		float num3 = structureProto.MoveSpeed;
		foreach (DroneEngineInfo droneEngineInfo in engines)
		{
			num += droneEngineInfo.PowerCapacityIncrease;
			num2 += droneEngineInfo.PowerRecvIncrease;
			num3 += droneEngineInfo.MoveSpeedIncrease;
		}
		return new DroneStructureParams(num, num2, num3);
	}

	public static float RayForNearestWall(this DroneRenderer droneRenderer, float rayDistance)
	{
		float num = rayDistance;
		Vector2[] array = rayDirections;
		foreach (Vector2 direction in array)
		{
			RaycastHit2D raycastHit2D = Physics2D.Raycast(droneRenderer.transform.position, direction, rayDistance, DolocAPI.gameConfig.groundMask);
			if (raycastHit2D.collider != null && raycastHit2D.distance < num)
			{
				num = raycastHit2D.distance;
			}
		}
		return num;
	}
}
