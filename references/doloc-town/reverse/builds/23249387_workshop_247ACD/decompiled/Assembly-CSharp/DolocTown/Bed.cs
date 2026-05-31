using DolocTown.Config;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class Bed : Equipment
{
	public Bed(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 worldPos, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, worldPos, anchor, turn)
	{
	}

	[JsonConstructor]
	protected Bed(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
	}

	protected override void OnTouch()
	{
		ShowTip(DolocConfig.StaticTexts.UiOperationSleep);
	}

	protected override void OnInteract()
	{
		PushTip();
		DolocAPI.ShowSleepMenu(SendUseEquipmentMessage);
	}

	protected override void OnDisTouch()
	{
		HideTip();
	}
}
