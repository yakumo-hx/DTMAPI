using System;
using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Drone;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public abstract class DroneFunction
{
	private static readonly Dictionary<string, Type> functionTypes = InitializeFunctionTypeCache();

	protected readonly Drone drone;

	protected readonly DroneFunctionProto proto;

	protected DroneComponentRenderer comRenderer;

	public bool IsPositive => IsFunctionRefined("OnUseSkill");

	public bool IsMessageHandle => IsFunctionRefined("OnReceiveMessage");

	public bool IsPowerFilterRecover => IsFunctionRefined("OnRecoverPower");

	public bool IsPowerFilterCost => IsFunctionRefined("OnCostPower");

	public bool IsBulletHandle => IsFunctionRefined("HandleBullet");

	public bool IsTimer => IsFunctionRefined("OnUpdatePerTu");

	public bool ShouldHandleDamage => IsFunctionRefined("HandleDamage");

	public bool IsWallCollisionHandle => IsFunctionRefined("HandleCollideWithWall");

	private static Dictionary<string, Type> InitializeFunctionTypeCache()
	{
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		Type[] subTypes = typeof(DroneFunction).GetSubTypes();
		foreach (Type type in subTypes)
		{
			dictionary[type.Name] = type;
		}
		return dictionary;
	}

	public static bool CreateDroneFunction(Drone drone, string skillId, out DroneFunction function)
	{
		function = null;
		if (skillId.IsNullOrEmpty())
		{
			return false;
		}
		DroneSkillInfo orDefault = DolocConfig.Tables.TbDroneSkill.GetOrDefault(skillId);
		if (orDefault == null)
		{
			Debug.LogError("未找到名称为\"" + skillId + "\"的无人机技能");
			return false;
		}
		function = _CreateDroneFunction(drone, orDefault.Function);
		return function != null;
	}

	private static DroneFunction _CreateDroneFunction(Drone drone, DroneFunctionProto proto)
	{
		string text = proto.GetType().Name.Replace("Proto", "");
		if (!functionTypes.TryGetValue(text, out var value))
		{
			Debug.LogError("未找到名称为 " + text + " 的无人机功能类型");
			return null;
		}
		return (DroneFunction)Activator.CreateInstance(value, drone, proto);
	}

	protected DroneFunction(Drone drone, DroneFunctionProto proto)
	{
		this.drone = drone;
		this.proto = proto;
	}

	private bool IsFunctionRefined(string name)
	{
		return typeof(DroneFunction).IsFunctionOverride(GetType(), name);
	}

	public void RenderComponent(DroneComponentRenderer comRenderer)
	{
		this.comRenderer = comRenderer;
		OnRender(comRenderer);
	}

	protected virtual void OnRender(DroneComponentRenderer renderer)
	{
	}

	public virtual void Dispose()
	{
	}

	public virtual void OnUseSkill()
	{
	}

	public virtual void OnUpdatePerTu()
	{
	}

	public virtual float OnCostPower(float power)
	{
		return power;
	}

	public virtual int HandleDamage(int originDamage)
	{
		return originDamage;
	}

	public virtual float OnRecoverPower(float power)
	{
		return power;
	}

	public virtual bool OnReceiveMessage(DroneEventType type, GameEventArgs args)
	{
		return false;
	}

	public virtual void HandleBullet(Bullet bullet)
	{
	}

	public virtual bool HandleCollideWithEnemy(Bullet bullet, AttackHitInfo hitInfo)
	{
		bullet.Recycle();
		return true;
	}

	public virtual void HandleCollideWithWall(Bullet bullet, AttackHitInfo hitInfo)
	{
		bullet.Recycle();
	}

	public virtual bool TryFetchPowerForReloading()
	{
		return false;
	}
}
