using System.Linq;
using DolocTown.Config.Drone;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DroneWeaponGunDragon : DroneWeaponGun
{
	private readonly Vector2[] _directions;

	private int _dirIdx;

	public DroneWeaponGunDragon(Drone drone, DroneWeaponInfo proto, DroneWeaponParams weaponParams, BulletManager bulletManager)
		: base(drone, proto, weaponParams, bulletManager)
	{
		WeaponFunctionGunDragonShooter weaponFunctionGunDragonShooter = (WeaponFunctionGunDragonShooter)proto.Function;
		if (weaponFunctionGunDragonShooter.DirCount == 1)
		{
			_directions = null;
		}
		_directions = Vector2.right.SplitIntoSector(weaponFunctionGunDragonShooter.DirCount, weaponFunctionGunDragonShooter.Angle).ToArray();
	}

	private Vector2 GetNextDirection(Vector2 originDir)
	{
		float z = Vector2.SignedAngle(Vector2.right, originDir);
		if (_dirIdx == _directions.Length)
		{
			_dirIdx = 0;
		}
		Vector2 vector = _directions[_dirIdx++];
		return Quaternion.Euler(0f, 0f, z) * vector;
	}

	public override bool GunAttack(Vector2 direction, bool isAuto = false, bool shouldRaiseSound = true)
	{
		Vector2 shootPosition = drone.renderer.ShootPosition;
		return Attack(shootPosition, GetNextDirection(direction), isAuto);
	}
}
