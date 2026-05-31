using DolocTown.Config;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class Chair : Equipment
{
	private EquipmentFuncChair func => (EquipmentFuncChair)proto.Function;

	public Chair(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 worldPos, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, worldPos, anchor, turn)
	{
	}

	[JsonConstructor]
	protected Chair(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
	}

	protected override void OnTouch()
	{
		ShowTip(DolocConfig.StaticTexts.UiOperationSit);
	}

	protected override void OnInteract()
	{
		PushTip();
		Vector3 sitPosition = base.CurrentRoom.Geometry.CalcWorldPosition(base.Anchor) + (base.Turn ? func.SitOffsetFlip : func.SitOffset) * 0.125f;
		sitPosition.z = base.Position.y - sitPosition.y - 0.001f;
		DisableOutline();
		KillTimeState.EntryKillTimeState(new SitParams(base.Position, sitPosition, func.Chair_Ref, base.Turn), SendUseEquipmentMessage);
	}

	protected override void OnDisTouch()
	{
		HideTip();
	}
}
