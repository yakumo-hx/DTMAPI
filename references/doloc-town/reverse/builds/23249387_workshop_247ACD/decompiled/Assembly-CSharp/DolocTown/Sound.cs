using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class Sound : AffectorElectric
{
	public override AffectorType AffectType => AffectorType.Sound;

	public Sound(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	public Sound(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, Counter workCounter, bool isIdle, bool isTurnOn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, workCounter, isIdle, isTurnOn)
	{
	}
}
