using System;
using System.Collections.Generic;
using System.Linq;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[DebugObject]
public class ElectricSystem
{
	private readonly List<IElectronicComponentGenerator> generators = new List<IElectronicComponentGenerator>();

	private readonly List<IElectronicComponentAppliance> appliances = new List<IElectronicComponentAppliance>();

	private readonly List<IElectronicComponentBattery> batteries = new List<IElectronicComponentBattery>();

	private readonly List<IElectronicComponentBattery> notFullBatteries = new List<IElectronicComponentBattery>();

	private float _lastTotalBatteryPower;

	[DebugInfo("本轮产出的电量")]
	public float TotalPowerGeneration { get; private set; }

	[DebugInfo("本轮消耗的电量")]
	public float TotalPowerConsumption { get; private set; }

	[DebugInfo("本轮电池总容量")]
	public float TotalBatteryPowerCapacity => batteries.Sum((IElectronicComponentBattery battery) => battery.Capacity);

	[DebugInfo("本轮电池总储量")]
	public float TotalBatteryPower => batteries.Sum((IElectronicComponentBattery battery) => battery.Power);

	[DebugInfo("本轮电力系统状态")]
	public ElectricSystemStatus LastStatus { get; private set; }

	[DebugInfo("发电机数量")]
	public int GeneratorCount => generators.Count;

	[DebugInfo("用电器数量")]
	public int ApplianceCount => appliances.Count;

	[DebugInfo("电池数量")]
	public int BatteryCount => batteries.Count;

	public bool HasBattery => batteries.Count > 0;

	public bool ShouldShowElectricPanel
	{
		get
		{
			if (generators.Count <= 0 && batteries.Count <= 0)
			{
				return appliances.Count > 0;
			}
			return true;
		}
	}

	public bool IsBatteryCharingNow => TotalBatteryPower > _lastTotalBatteryPower;

	public bool IsBatteryStayNow => Math.Abs(TotalBatteryPower - _lastTotalBatteryPower) < 0.01f;

	public void Update()
	{
		_Update();
	}

	private void _Update()
	{
		_lastTotalBatteryPower = TotalBatteryPower;
		LastStatus = ElectricSystemStatus.None;
		float totalPowerGeneration = generators.Sum((IElectronicComponentGenerator g) => g.PowerGenerated);
		TotalPowerGeneration = totalPowerGeneration;
		TotalPowerConsumption = appliances.Sum((IElectronicComponentAppliance x) => x.PowerGap);
		float num = TotalPowerConsumption - TotalPowerGeneration;
		if (num > 0f)
		{
			LastStatus = ElectricSystemStatus.LackOfGeneration;
			float num2 = batteries.Sum((IElectronicComponentBattery b) => b.Discharge());
			if (num2 > 0f)
			{
				LastStatus = ElectricSystemStatus.LackOfGenerationBatteryCost;
			}
			if (num2 < num)
			{
				_ChargeAllAppliances(num2 + TotalPowerGeneration);
				return;
			}
			_ChargeAllAppliancesToFull();
			_ChargeAllBatteries(num2 - num);
		}
		else
		{
			float power = Mathf.Abs(num);
			_ChargeAllAppliancesToFull();
			if (_ChargeAllBatteries(power))
			{
				LastStatus = (HasBattery ? ElectricSystemStatus.BatteryFull : ElectricSystemStatus.PowerLoss);
			}
			else
			{
				LastStatus = ((generators.Count <= 0) ? ElectricSystemStatus.LackOfGeneration : ElectricSystemStatus.BatterySaving);
			}
		}
	}

	public bool TryCostPower(float require)
	{
		if (require <= 0f)
		{
			return true;
		}
		if (TotalBatteryPower < require)
		{
			return false;
		}
		foreach (IElectronicComponentBattery battery in batteries)
		{
			if (require <= 0f)
			{
				break;
			}
			float power = battery.Power;
			if (power < require)
			{
				require -= power;
				battery.ClearPower();
				continue;
			}
			battery.Charge(0f - require);
			break;
		}
		return true;
	}

	private void _ChargeAllAppliancesToFull()
	{
		foreach (IElectronicComponentAppliance appliance in appliances)
		{
			appliance.ChargeToFull();
		}
	}

	private void _ChargeAllAppliances(float power)
	{
		foreach (IElectronicComponentAppliance appliance in appliances)
		{
			if (power > appliance.PowerGap)
			{
				power -= appliance.PowerGap;
				appliance.ChargeToFull();
				continue;
			}
			appliance.Charge(power);
			break;
		}
	}

	private bool _ChargeAllBatteries(float power)
	{
		if (power <= 0f)
		{
			return false;
		}
		notFullBatteries.Clear();
		foreach (IElectronicComponentBattery item in batteries.Where((IElectronicComponentBattery battery) => battery.IsNotFull))
		{
			notFullBatteries.Add(item);
		}
		if (notFullBatteries.Count == 0)
		{
			return true;
		}
		float num = 0f;
		float minPowerGap = float.MaxValue;
		foreach (float item2 in notFullBatteries.Select((IElectronicComponentBattery battery) => battery.PowerGap))
		{
			num += item2;
			if (item2 < minPowerGap)
			{
				minPowerGap = item2;
			}
		}
		if (num < power)
		{
			notFullBatteries.ForEach(delegate(IElectronicComponentBattery x)
			{
				x.ChargeToFull();
			});
			return true;
		}
		float avgPower = power / (float)notFullBatteries.Count;
		if (minPowerGap > avgPower)
		{
			notFullBatteries.ForEach(delegate(IElectronicComponentBattery x)
			{
				x.Charge(avgPower);
			});
			return false;
		}
		power -= minPowerGap * (float)notFullBatteries.Count;
		notFullBatteries.ForEach(delegate(IElectronicComponentBattery x)
		{
			x.Charge(minPowerGap);
		});
		return _ChargeAllBatteries(power);
	}

	public bool TryAddElectronicComponent(Equipment equipment)
	{
		if (!equipment.IsElectric)
		{
			return false;
		}
		IElectronicComponent iElectronicComponent = equipment.IElectronicComponent;
		if (iElectronicComponent is IElectronicComponentContainer electronicComponentContainer)
		{
			iElectronicComponent = electronicComponentContainer.IElectronicComponent;
		}
		return AddElectronicComponent(iElectronicComponent);
	}

	public bool TryRemoveElectronicComponent(Equipment equipment)
	{
		if (!equipment.IsElectric)
		{
			return false;
		}
		IElectronicComponent iElectronicComponent = equipment.IElectronicComponent;
		if (iElectronicComponent is IElectronicComponentContainer electronicComponentContainer)
		{
			iElectronicComponent = electronicComponentContainer.IElectronicComponent;
		}
		return TryRemoveElectronicComponent(iElectronicComponent);
	}

	private bool AddElectronicComponent(IElectronicComponent component)
	{
		if (!(component is IElectronicComponentGenerator item))
		{
			if (!(component is IElectronicComponentAppliance item2))
			{
				if (!(component is IElectronicComponentBattery item3))
				{
					return false;
				}
				if (batteries.Contains(item3))
				{
					return false;
				}
				batteries.Add(item3);
			}
			else
			{
				if (appliances.Contains(item2))
				{
					return false;
				}
				appliances.Add(item2);
			}
		}
		else
		{
			if (generators.Contains(item))
			{
				return false;
			}
			generators.Add(item);
		}
		return true;
	}

	public bool TryRemoveElectronicComponent(IElectronicComponent component)
	{
		if (!(component is IElectronicComponentGenerator item))
		{
			if (!(component is IElectronicComponentAppliance item2))
			{
				if (component is IElectronicComponentBattery item3)
				{
					batteries.Remove(item3);
				}
			}
			else
			{
				appliances.Remove(item2);
			}
		}
		else
		{
			generators.Remove(item);
		}
		return true;
	}

	public void Rebuild(IEnumerable<Equipment> equipments)
	{
		generators.Clear();
		appliances.Clear();
		batteries.Clear();
		foreach (Equipment equipment in equipments)
		{
			TryAddElectronicComponent(equipment);
		}
	}
}
