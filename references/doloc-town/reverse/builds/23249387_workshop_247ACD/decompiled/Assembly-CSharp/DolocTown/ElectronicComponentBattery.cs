using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class ElectronicComponentBattery : ElectronicComponent, IElectronicComponentBattery, IElectronicComponent
{
	[JsonProperty("power")]
	private float power;

	private EComProtoBattery batteryProto;

	private float Efficiency => batteryProto.Voltage;

	private float capacityReciprocal => 1f / batteryProto.Capacity;

	[DebugInfo("电池信息", Color = "#ff00ff")]
	public string PowerInfo => $"{power}/{Capacity}";

	public float PowerPercent => power * capacityReciprocal;

	public float Power => power;

	public float Capacity => batteryProto.Capacity;

	public ElectronicComponentBattery(Equipment equipment)
		: base(equipment)
	{
		batteryProto = (EComProtoBattery)equipment.proto.ElectronicComponent;
	}

	[JsonConstructor]
	protected ElectronicComponentBattery(float power)
	{
		this.power = power;
	}

	public override void SetEquipment(Equipment equipment)
	{
		base.SetEquipment(equipment);
		batteryProto = (EComProtoBattery)equipment.proto.ElectronicComponent;
	}

	public void ClearPower()
	{
		power = 0f;
	}

	public void Charge(float adder)
	{
		power = Mathf.Clamp(power + adder, 0f, Capacity);
	}

	public float Discharge()
	{
		float num = Efficiency * power;
		if (power >= num)
		{
			power -= num;
			return num;
		}
		float result = power;
		power = 0f;
		return result;
	}
}
