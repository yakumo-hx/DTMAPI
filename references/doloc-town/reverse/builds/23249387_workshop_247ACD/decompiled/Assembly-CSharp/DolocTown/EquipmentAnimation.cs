using DolocTown.Config;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class EquipmentAnimation : Equipment
{
	public EquipmentFuncEquipmentAnimation func => proto.Function as EquipmentFuncEquipmentAnimation;

	public EquipmentAnimation(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	protected EquipmentAnimation(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
	}

	private string GetTipText()
	{
		if (func?.TipText == null || func.TipText.IsNullOrEmpty())
		{
			return DolocConfig.StaticTexts.UiOperationView;
		}
		return func.TipText;
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		ShowTip(GetTipText());
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		PushTip();
		DolocAPI.StartDialogueNode(func?.DialogueNode);
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		HideTip();
	}
}
