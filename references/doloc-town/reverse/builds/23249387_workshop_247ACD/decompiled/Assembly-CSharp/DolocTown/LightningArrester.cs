using System.Collections.Generic;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class LightningArrester : Affector
{
	private readonly ElectronicComponentGeneratorCustom customGenerator;

	private HashSet<Vector2Int> _currentAffectedPositions;

	public override AffectorType AffectType => AffectorType.LightningArrester;

	public LightningArrester(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		if (electronicComponent is ElectronicComponentGeneratorCustom electronicComponentGeneratorCustom)
		{
			customGenerator = electronicComponentGeneratorCustom;
		}
	}

	[JsonConstructor]
	public LightningArrester(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (base.electronicComponent is ElectronicComponentGeneratorCustom electronicComponentGeneratorCustom)
		{
			customGenerator = electronicComponentGeneratorCustom;
		}
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		ShineArea(Color.white);
	}

	public override void OnThunder(bool shouldRender)
	{
		customGenerator?.Charge(DolocAPI.GlobalParameter.ThunderPower);
	}
}
