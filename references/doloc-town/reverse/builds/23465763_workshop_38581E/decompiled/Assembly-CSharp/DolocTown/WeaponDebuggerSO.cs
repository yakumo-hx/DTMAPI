using System;
using System.Collections.Generic;
using DolocTown.Config.Asset;
using DolocTown.Config.Drone;
using RedSaw;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DolocTown;

public class WeaponDebuggerSO : SerializedScriptableObject
{
	[Serializable]
	public abstract class _WeaponFunction
	{
		public abstract WeaponFunction CreateFunction();
	}

	public abstract class _WeaponFunctionGun : _WeaponFunction
	{
	}

	public class _WeaponFunctionGunDefault : _WeaponFunctionGun
	{
		public override WeaponFunction CreateFunction()
		{
			return new WeaponFunctionGunDefault();
		}
	}

	public class _WeaponFunctionGunDragonShooter : _WeaponFunctionGun
	{
		[SerializeField]
		private int dirCount;

		[SerializeField]
		private float angle;

		public override WeaponFunction CreateFunction()
		{
			return new WeaponFunctionGunDragonShooter(angle, dirCount);
		}
	}

	public class _WeaponFunctionSword : _WeaponFunction
	{
		public override WeaponFunction CreateFunction()
		{
			return new WeaponFunctionSword();
		}
	}

	[SerializeField]
	private _WeaponFunction weaponfunction;

	[SerializeField]
	private Sprite weaponSprite;

	[SerializeField]
	private string shootingEffects;

	[SerializeField]
	private string extraRendererId;

	[SerializeField]
	[Min(0f)]
	private int attack;

	[SerializeField]
	[Range(0f, 1f)]
	private float criticalRate;

	[SerializeField]
	[Range(0.1f, 20f)]
	private float attackSpeed;

	[SerializeField]
	[Range(0f, 1f)]
	private float accuracy;

	[SerializeField]
	[Min(1f)]
	private float powerCost;

	[SerializeField]
	[Min(5f)]
	private float attackDistance;

	[SerializeField]
	[Min(1f)]
	private float moveSpeed;

	[SerializeField]
	[Min(1f)]
	private int clipCapacity;

	[SerializeField]
	[Min(0.1f)]
	private float reloadDuration;

	[SerializeField]
	[Min(0f)]
	private int extraBullets;

	[SerializeField]
	[Range(0f, 360f)]
	private float extraBulletAngle;

	[SerializeField]
	private string bulletId;

	[SerializeField]
	private string bulletMoverId = "linear";

	[SerializeField]
	private Vector2Int bulletOffset;

	private float duration => attackDistance / moveSpeed;

	private float attackInterval => 1f / attackSpeed;

	private IEnumerable<Type> AvailableWeaponTypes()
	{
		return typeof(_WeaponFunction).GetSubTypes();
	}

	public DroneWeaponInfo RemakeWeapon()
	{
		return new DroneWeaponInfo("debugger_weapon", WeaponType.Gun, new SpriteAsset(new CfgSpriteAsset(weaponSprite.name)), Vector2Int.zero, shootingEffects, "", extraRendererId, attack, criticalRate, attackSpeed, accuracy, powerCost, attackDistance, moveSpeed, reloadDuration, "", clipCapacity, extraBullets, extraBulletAngle, bulletId, bulletMoverId, bulletOffset, weaponfunction.CreateFunction());
	}
}
