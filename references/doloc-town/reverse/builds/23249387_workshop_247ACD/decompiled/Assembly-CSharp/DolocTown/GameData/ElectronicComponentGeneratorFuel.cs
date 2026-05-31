using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown.GameData;

public class ElectronicComponentGeneratorFuel : ElectronicComponentGenerator
{
	private EComProtoGeneratorFuel proto;

	[JsonProperty]
	private float fuel;

	public float Fuel => fuel;

	public float FuelPercent => Mathf.Clamp01(fuel / proto.FuelCapacity);

	[DebugInfo("燃料信息", Color = "#ff00ff")]
	public string FuelInfo => $"{fuel}/{proto.FuelCapacity}";

	public float RealCapacity => proto.FuelCapacity + proto.ExceedFuelCapacity;

	public bool IsFull => fuel >= RealCapacity;

	public ElectronicComponentGeneratorFuel(Equipment equipment)
		: base(equipment)
	{
		proto = (EComProtoGeneratorFuel)equipment.proto.ElectronicComponent;
		fuel = 0f;
	}

	[JsonConstructor]
	public ElectronicComponentGeneratorFuel(float fuel)
	{
		this.fuel = fuel;
	}

	public override void SetEquipment(Equipment equipment)
	{
		base.SetEquipment(equipment);
		proto = (EComProtoGeneratorFuel)equipment.proto.ElectronicComponent;
	}

	public void AddFuel(float itemEnergy)
	{
		if (!(itemEnergy <= 0f))
		{
			fuel += itemEnergy * proto.Rate;
		}
	}

	protected override float GeneratePower()
	{
		if (shouldStop)
		{
			return 0f;
		}
		float num = Mathf.Min(fuel, proto.Efficiency);
		fuel -= num;
		return num;
	}
}
