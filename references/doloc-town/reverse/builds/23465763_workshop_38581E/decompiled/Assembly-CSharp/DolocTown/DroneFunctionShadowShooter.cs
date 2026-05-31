using DolocTown.Config.Drone;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DroneFunctionShadowShooter : DroneFunction
{
	private readonly DroneFunctionProtoShadowShooter protoShadowShooter;

	private readonly BulletManager bulletManager;

	private readonly DroneWeaponGun gun;

	private readonly bool shouldWork;

	public DroneFunctionShadowShooter(Drone drone, DroneFunctionProto proto)
		: base(drone, proto)
	{
		protoShadowShooter = (DroneFunctionProtoShadowShooter)proto;
		bulletManager = DolocAPI.battleSystem.CreateBulletManager(protoShadowShooter.BulletId, "linear");
		shouldWork = true;
		if (bulletManager == null)
		{
			shouldWork = false;
			Debug.LogError("影子射手: 无法创建子弹管理器:" + protoShadowShooter.BulletId);
		}
		if (!(drone.weapon is DroneWeaponGun droneWeaponGun))
		{
			shouldWork = false;
			Debug.LogWarning("影子射手: 目标武器不是枪械类型");
		}
		else
		{
			gun = droneWeaponGun;
		}
	}

	public override bool OnReceiveMessage(DroneEventType type, GameEventArgs args)
	{
		if (!shouldWork)
		{
			return false;
		}
		if (type != DroneEventType.Shoot || !RandomUtils.Dice(protoShadowShooter.Probability))
		{
			return false;
		}
		if (!(args is GameEventArgs<AttackInfo> gameEventArgs))
		{
			return false;
		}
		AttackInfo attackInfo = gameEventArgs.value;
		DolocAPI.Delay(0.15f, delegate
		{
			gun.ShootByCurrentGun(bulletManager, attackInfo.direction);
		});
		return false;
	}
}
