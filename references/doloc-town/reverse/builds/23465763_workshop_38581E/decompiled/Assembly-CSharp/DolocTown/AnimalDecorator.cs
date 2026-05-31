using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class AnimalDecorator : Equipment, IAnimalMoodAffector
{
	private readonly EquipmentFuncAnimalDecorator func;

	public int MoodContribution => func?.MoodContribution ?? 0;

	public AnimalDecorator(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncAnimalDecorator)proto.Function;
	}

	[JsonConstructor]
	public AnimalDecorator(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null && proto.Function is EquipmentFuncAnimalDecorator equipmentFuncAnimalDecorator)
		{
			func = equipmentFuncAnimalDecorator;
		}
	}
}
