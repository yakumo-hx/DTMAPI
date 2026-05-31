using System;
using DolocTown.Config;
using DolocTown.Config.Drone;
using UnityEngine;

namespace DolocTown;

public abstract class DroneWeapon
{
	protected readonly Drone drone;

	public readonly DroneWeaponInfo proto;

	public readonly DroneWeaponParams weaponParams;

	public bool _autoBattle { get; private set; }

	public float FinalCriticalRate => weaponParams.CriticalRate + DolocAPI.AgentEquipmentParams.CriticalRateChanged;

	public virtual bool IsGun => false;

	public virtual bool IsSword => false;

	public virtual bool IsSwordReadyNow => false;

	public virtual bool NeedReload => false;

	public virtual bool IsEmpty => false;

	public virtual bool IsFireNotAvailable => false;

	public static DroneWeapon CreateWeapon(Drone drone, DroneWeaponInfo proto)
	{
		if (proto == null)
		{
			return new DroneWeaponEmpty();
		}
		WeaponFunction function = proto.Function;
		if (!(function is WeaponFunctionGunDefault))
		{
			if (!(function is WeaponFunctionGunDragonShooter))
			{
				if (function is WeaponFunctionSword)
				{
					return CreateSword<DroneWeaponSword>(drone, proto);
				}
				return new DroneWeaponEmpty();
			}
			return CreateGun<DroneWeaponGunDragon>(drone, proto);
		}
		return CreateGun<DroneWeaponGun>(drone, proto);
	}

	private static DroneWeapon CreateGun<T>(Drone drone, DroneWeaponInfo proto) where T : DroneWeaponGun
	{
		BulletManager bulletManager = DolocAPI.battleSystem.CreateBulletManager(proto.BulletId, proto.BulletMoverId);
		if (bulletManager == null)
		{
			Debug.LogError("无法创建子弹管理器，原型ID\"" + proto.BulletId + "\"");
			return new DroneWeaponEmpty();
		}
		DroneWeaponParams droneWeaponParams = proto.GetDroneWeaponParams(drone.structure.AllChips);
		return (T)Activator.CreateInstance(typeof(T), drone, proto, droneWeaponParams, bulletManager);
	}

	private static DroneWeapon CreateSword<T>(Drone drone, DroneWeaponInfo proto) where T : DroneWeaponSword
	{
		DroneWeaponParams droneWeaponParams = proto.GetDroneWeaponParams(drone.structure.AllChips);
		return (T)Activator.CreateInstance(typeof(T), drone, proto, droneWeaponParams);
	}

	protected DroneWeapon(Drone drone, DroneWeaponInfo proto, DroneWeaponParams weaponParams)
	{
		this.drone = drone;
		this.proto = proto;
		this.weaponParams = weaponParams;
	}

	public void SwitchAutoFire()
	{
		_autoBattle = !_autoBattle;
		DolocAPI.ShowMessageBoxSmall(_autoBattle ? DolocConfig.StaticTexts.UiOperationSwitchAutoFire : DolocConfig.StaticTexts.UiOperationSwitchManualFire);
		DolocAPI.archiveHandle.farmData.agentData.autoBattle = _autoBattle;
		DolocAPI.Broadcast(OperationEventType.SWITCH_GUN_MODE);
	}

	public virtual void Reload()
	{
	}

	public virtual void Reset()
	{
	}

	public virtual void GunAttackForce(Vector2 direction, bool shouldRaiseSound = true)
	{
	}

	public virtual bool GunAttack(Vector2 direction, bool isAuto = false, bool shouldRaiseSound = true)
	{
		return false;
	}

	public virtual void StartSwordReady(Vector2 direction)
	{
	}

	public virtual void SwordReady(float dt)
	{
	}

	public virtual void TrySwordAttack(Vector2 direction)
	{
	}

	public virtual void Dispose()
	{
	}

	public virtual void OnUpdate(float dt)
	{
	}

	public virtual void OnFixedUpdate(float dt)
	{
	}
}
