using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class ElectronicComponentAppliance : ElectronicComponent, IElectronicComponentAppliance, IElectronicComponent
{
	private EComProtoAppliance proto;

	[JsonProperty]
	protected float power;

	[DebugInfo("电力缓存槽", Color = "#ff00ff")]
	public virtual string PowerInfo => $"{power}/{proto.Threshold}";

	public virtual float RatedPowerConsumption => proto.Threshold;

	public virtual float PowerGap => proto.Threshold - power;

	public virtual float PowerProgress => power / proto.Threshold;

	public bool IsActive { get; set; }

	public bool IsFull => power >= proto.Threshold;

	public ElectronicComponentAppliance(Equipment equipment)
		: base(equipment)
	{
		proto = equipment.proto.ElectronicComponent as EComProtoAppliance;
	}

	[JsonConstructor]
	protected ElectronicComponentAppliance(float power)
	{
		this.power = power;
	}

	public override void SetEquipment(Equipment equipment)
	{
		base.SetEquipment(equipment);
		proto = equipment.proto.ElectronicComponent as EComProtoAppliance;
	}

	public void Switch()
	{
	}

	public void Charge(float e)
	{
		power += e;
	}

	public void ChargeToFull()
	{
		power = proto.Threshold;
	}

	public bool Launch()
	{
		if (power < proto.Threshold)
		{
			return false;
		}
		power = Mathf.Max(0f, power - proto.Threshold);
		return true;
	}

	public void Flush()
	{
		power = 0f;
	}
}
