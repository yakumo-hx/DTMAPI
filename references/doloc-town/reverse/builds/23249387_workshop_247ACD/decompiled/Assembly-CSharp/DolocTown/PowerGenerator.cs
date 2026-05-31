using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public abstract class PowerGenerator : Equipment
{
	protected readonly ElectronicComponentGenerator generator;

	public abstract bool IsFuelGenerator { get; }

	public abstract float FuelPercent { get; }

	public virtual bool ShouldStop
	{
		get
		{
			return generator.ShouldStop;
		}
		set
		{
			if (value != generator.ShouldStop)
			{
				generator.ShouldStop = value;
				OnGeneratorChangeState(!value);
			}
		}
	}

	public PowerGenerator(IEquipmentHost host, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, id, proto, wp, anchor, turn)
	{
		generator = (ElectronicComponentGenerator)electronicComponent;
	}

	[JsonConstructor]
	protected PowerGenerator(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		generator = (ElectronicComponentGenerator)electronicComponent;
	}

	public abstract bool IsSuitableFuel(Item fuelItem);

	public abstract void AddFuel(ItemInfo fuelItem, bool sendMessage = false);

	public override void OnCreated()
	{
		base.OnCreated();
		DolocAPI.uiSystem.basicTip.UpdateElectricityInfoViewer();
	}

	protected override void OnRemove()
	{
		base.OnRemove();
		DolocAPI.uiSystem.basicTip.UpdateElectricityInfoViewer();
	}

	protected virtual void OnGeneratorChangeState(bool value)
	{
	}
}
