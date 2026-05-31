using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class FishTankExtension : Equipment
{
	public override bool TouchableAsDecal => false;

	public Equipment HostFishTank => base.DecalHost as Equipment;

	public EquipmentFuncFishTankExtension extensionFunc => proto.Function as EquipmentFuncFishTankExtension;

	public FishTankExtension(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	public FishTankExtension(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
	}

	public override void OnCreated()
	{
		base.OnCreated();
		if (HostFishTank is IFishTank fishTank)
		{
			fishTank.tank.RefreshFishTankExtensionsInventory();
		}
	}
}
