using DolocTown.Config.Drone;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DroneFunctionBrustCore : DroneFunction
{
	private int counter;

	private readonly DroneFunctionProtoBrustCore protoBrustCore;

	public DroneFunctionBrustCore(Drone drone, DroneFunctionProto proto)
		: base(drone, proto)
	{
		protoBrustCore = (DroneFunctionProtoBrustCore)proto;
	}

	public override bool OnReceiveMessage(DroneEventType type, GameEventArgs args)
	{
		if (!drone.weapon.IsGun || type != DroneEventType.Shoot)
		{
			return false;
		}
		if (++counter < protoBrustCore.BulletInterval)
		{
			return false;
		}
		counter = 0;
		BrustAttack(protoBrustCore.BulletCount);
		return false;
	}

	private void BrustAttack(int count)
	{
		foreach (Vector2 item in Vector2.right.SplitIntoSector(count, 360f))
		{
			drone.weapon.GunAttackForce(item, shouldRaiseSound: false);
		}
	}
}
