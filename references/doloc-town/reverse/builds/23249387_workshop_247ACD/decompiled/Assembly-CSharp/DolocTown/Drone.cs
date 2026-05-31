using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Drone;
using RedSaw;

namespace DolocTown;

public class Drone
{
	private class DroneFunctionGroup
	{
		public readonly bool shouldWork;

		public readonly DroneFunction[] functions;

		public DroneFunctionGroup(bool shouldWork, DroneFunction[] functions)
		{
			this.shouldWork = shouldWork;
			this.functions = functions;
		}
	}

	public readonly DroneEnv Env;

	public readonly DroneStruct structure;

	public readonly DroneRenderer renderer;

	public DroneExtraWeaponRenderer extraWeaponRenderer;

	public DroneWeapon weapon;

	private readonly RSTimer batteryTipUpdateTimer = RSTimer.SecondTimer;

	private readonly RSTimer batteryTipVisibleTimer = new RSTimer(3f);

	private readonly RSTimer tuTimer = new RSTimer(DolocAPI.GlobalParameter.TULength);

	private readonly DroneFunction[] _functions;

	private readonly Dictionary<DroneSlot, DroneFunction> _slotFunctions;

	private readonly DroneFunctionGroup _messageGroup;

	private readonly DroneFunctionGroup _powerFilterGroup;

	private readonly DroneFunctionGroup _bulletFilterGroup;

	private readonly DroneFunctionGroup _timerGroup;

	private readonly DroneFunctionGroup _collisionWithWallGroup;

	private readonly DroneFunctionGroup _powerFilterCostGroup;

	private readonly DroneFunctionGroup _damageGroup;

	private readonly DroneFunction _positiveFunction;

	private DroneStructureInfo StructureProto => structure?.proto;

	public bool IsEmptyWeapon { get; private set; }

	public string FunctionInfos
	{
		get
		{
			string text = "";
			DroneFunction[] functions = _functions;
			foreach (DroneFunction droneFunction in functions)
			{
				text = text + droneFunction.GetType().Name + "\n";
			}
			return text;
		}
	}

	public Drone(DroneStruct structure, DroneRenderer renderer, DroneEnv env)
	{
		this.structure = structure;
		this.renderer = renderer;
		Env = env;
		DroneWeaponInfo weaponProto = structure.GetWeaponProto();
		weapon = DroneWeapon.CreateWeapon(this, weaponProto);
		IsEmptyWeapon = weapon is DroneWeaponEmpty;
		extraWeaponRenderer = RenderExtraWeaponUnits(weaponProto);
		structure.InitializeParams(out var moveSpeed);
		this.renderer.SetMoveSpeed(moveSpeed);
		this.renderer.SetBatteryPercent(structure.PowerProcess);
		_functions = InitializeDroneFunctions(out _slotFunctions);
		_positiveFunction = _functions.FirstOrDefault((DroneFunction func) => func.IsPositive);
		_messageGroup = CreateFunctionGroup(_functions, (DroneFunction func) => func.IsMessageHandle);
		_powerFilterGroup = CreateFunctionGroup(_functions, (DroneFunction func) => func.IsPowerFilterRecover);
		_powerFilterCostGroup = CreateFunctionGroup(_functions, (DroneFunction func) => func.IsPowerFilterCost);
		_bulletFilterGroup = CreateFunctionGroup(_functions, (DroneFunction func) => func.IsBulletHandle);
		_timerGroup = CreateFunctionGroup(_functions, (DroneFunction func) => func.IsTimer);
		_collisionWithWallGroup = CreateFunctionGroup(_functions, (DroneFunction func) => func.IsWallCollisionHandle);
		_damageGroup = CreateFunctionGroup(_functions, (DroneFunction func) => func.ShouldHandleDamage);
	}

	public void DebuggerWeapon(DroneWeaponInfo proto)
	{
		weapon = DroneWeapon.CreateWeapon(this, proto);
		extraWeaponRenderer?.Dispose();
		extraWeaponRenderer = RenderExtraWeaponUnits(proto);
	}

	private DroneExtraWeaponRenderer RenderExtraWeaponUnits(DroneWeaponInfo proto)
	{
		if (proto == null || renderer == null)
		{
			return DroneExtraWeaponRenderer.FallbackRenderer;
		}
		DroneExtraWeaponRenderer droneExtraWeaponRenderer = DroneExtraWeaponRenderer.CreateRenderer(proto.ExtraRenderer);
		if (!droneExtraWeaponRenderer.OnRender(renderer.transform, proto))
		{
			return null;
		}
		return droneExtraWeaponRenderer;
	}

	public void Charge(float power)
	{
		structure.Charge(GetPowerRecover(power));
		if (!renderer.BatteryTipVisible)
		{
			renderer.BatteryTipVisible = true;
			renderer.SetBatteryPercent(structure.PowerProcess);
			batteryTipVisibleTimer.Reset();
		}
	}

	public void AfterPassTime()
	{
		structure.ChargeToFull();
		renderer.shouldLightUp = DolocAPI.archiveHandle.ShouldLightUp;
	}

	public bool TryCostPower(float power)
	{
		if (structure.CostPower(GetPowerCost(power)))
		{
			return true;
		}
		DroneFunction[] functions = _functions;
		for (int i = 0; i < functions.Length; i++)
		{
			if (functions[i].TryFetchPowerForReloading())
			{
				return true;
			}
		}
		return false;
	}

	public void OnFixedUpdate(float dt)
	{
		if (structure.IsNotFull)
		{
			float power = structure.droneParams.PowerRecoveryPerSecond * dt;
			Charge(power);
		}
		else if (renderer.BatteryTipVisible && batteryTipVisibleTimer.Tick(dt))
		{
			renderer.BatteryTipVisible = false;
		}
		weapon?.OnFixedUpdate(dt);
		if (DolocAPI.UserInput.NormalFireInProgress)
		{
			bool useBatterMode = DolocAPI.userSettings.useBatterMode;
			renderer.OnFixedUpdate(dt, !useBatterMode);
		}
		else
		{
			renderer.OnFixedUpdate(dt, followTarget: true);
		}
		if (tuTimer.Tick(dt))
		{
			UpdateDroneFunctionsPerTu();
		}
	}

	public void OnUpdate(float dt)
	{
		weapon?.OnUpdate(dt);
		if (batteryTipUpdateTimer.Tick(dt) && renderer.BatteryTipVisible)
		{
			renderer.SetBatteryPercent(structure.PowerProcess);
		}
	}

	public void Dispose()
	{
		weapon?.Dispose();
		extraWeaponRenderer.Dispose();
		extraWeaponRenderer = null;
		DroneFunction[] functions = _functions;
		for (int i = 0; i < functions.Length; i++)
		{
			functions[i].Dispose();
		}
	}

	public void OnPause()
	{
		renderer.BatteryTipVisible = false;
	}

	public void OnResume()
	{
		if (!structure.IsFull)
		{
			renderer.BatteryTipVisible = true;
		}
	}

	private DroneFunction[] InitializeDroneFunctions(out Dictionary<DroneSlot, DroneFunction> slotFunctions)
	{
		slotFunctions = new Dictionary<DroneSlot, DroneFunction>();
		List<DroneFunction> list = new List<DroneFunction>();
		HashSet<Type> hashSet = new HashSet<Type>();
		if (DroneFunction.CreateDroneFunction(this, structure.proto.SkillId, out var function))
		{
			list.Add(function);
			hashSet.Add(function.GetType());
		}
		DroneSlot[] slots = structure.slots;
		foreach (DroneSlot droneSlot in slots)
		{
			if (droneSlot.TryGetSkillId(out var skillId) && (DroneFunction.CreateDroneFunction(this, skillId, out var function2) || hashSet.Contains(function2.GetType())))
			{
				list.Add(function2);
				slotFunctions.Add(droneSlot, function2);
				hashSet.Add(function2.GetType());
			}
		}
		return list.ToArray();
	}

	private DroneFunctionGroup CreateFunctionGroup(DroneFunction[] funcs, Func<DroneFunction, bool> condition)
	{
		bool shouldWork = false;
		List<DroneFunction> list = new List<DroneFunction>();
		foreach (DroneFunction droneFunction in funcs)
		{
			if (condition(droneFunction))
			{
				list.Add(droneFunction);
				shouldWork = true;
			}
		}
		return new DroneFunctionGroup(shouldWork, list.ToArray());
	}

	public void _OnRenderComponent(DroneSlot slot, DroneComponentRenderer renderer)
	{
		if (_slotFunctions.TryGetValue(slot, out var value))
		{
			value.RenderComponent(renderer);
		}
	}

	public bool UseSkill()
	{
		if (_positiveFunction == null)
		{
			return false;
		}
		_positiveFunction.OnUseSkill();
		return true;
	}

	private void UpdateDroneFunctionsPerTu()
	{
		if (_timerGroup.shouldWork)
		{
			DroneFunction[] functions = _timerGroup.functions;
			for (int i = 0; i < functions.Length; i++)
			{
				functions[i].OnUpdatePerTu();
			}
		}
	}

	public void SendMessage(DroneEventType eventType, GameEventArgs args)
	{
		if (_messageGroup.shouldWork)
		{
			_messageGroup.functions.Any((DroneFunction function) => function.OnReceiveMessage(eventType, args));
		}
	}

	private float GetPowerRecover(float power)
	{
		if (!_powerFilterGroup.shouldWork)
		{
			return power;
		}
		DroneFunction[] functions = _powerFilterGroup.functions;
		for (int i = 0; i < functions.Length; i++)
		{
			power = functions[i].OnRecoverPower(power);
		}
		return power;
	}

	private float GetPowerCost(float power)
	{
		if (!_powerFilterCostGroup.shouldWork)
		{
			return power;
		}
		DroneFunction[] functions = _powerFilterCostGroup.functions;
		for (int i = 0; i < functions.Length; i++)
		{
			power = functions[i].OnCostPower(power);
		}
		return power;
	}

	public void HandleBullet(Bullet bullet)
	{
		if (bullet.mover is BulletMoverTracking bulletMoverTracking)
		{
			bulletMoverTracking.BindTrackingGetter(Env.FindNearestTrackingObject);
		}
		if (_bulletFilterGroup.shouldWork)
		{
			DroneFunction[] functions = _bulletFilterGroup.functions;
			for (int i = 0; i < functions.Length; i++)
			{
				functions[i].HandleBullet(bullet);
			}
		}
	}

	public int HandleDamage(int dmg)
	{
		if (!_damageGroup.shouldWork)
		{
			return dmg;
		}
		DroneFunction[] functions = _damageGroup.functions;
		for (int i = 0; i < functions.Length; i++)
		{
			dmg = functions[i].HandleDamage(dmg);
		}
		return dmg;
	}

	public bool HandleCollisionWithEnemy(Bullet bullet, AttackHitInfo hitInfo)
	{
		bool flag = false;
		DroneFunction[] functions = _collisionWithWallGroup.functions;
		foreach (DroneFunction droneFunction in functions)
		{
			flag = flag || droneFunction.HandleCollideWithEnemy(bullet, hitInfo);
		}
		return flag;
	}

	public void HandleCollisionWithWall(Bullet bullet, AttackHitInfo hitInfo)
	{
		if (!bullet.collideWithWall)
		{
			return;
		}
		if (!_collisionWithWallGroup.shouldWork)
		{
			bullet.Recycle();
			return;
		}
		DroneFunction[] functions = _collisionWithWallGroup.functions;
		for (int i = 0; i < functions.Length; i++)
		{
			functions[i].HandleCollideWithWall(bullet, hitInfo);
		}
	}
}
