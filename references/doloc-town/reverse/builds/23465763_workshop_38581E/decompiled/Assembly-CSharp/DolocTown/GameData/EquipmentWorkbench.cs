using DolocTown.Config;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown.GameData;

public class EquipmentWorkbench : Equipment
{
	public EquipmentWorkbench(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	protected EquipmentWorkbench(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
	}

	protected override void OnTouch()
	{
		ShowTip(DolocConfig.StaticTexts.UiOperationMakeEquipment);
	}

	protected override void OnDisTouch()
	{
		HideTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		PushTipToHide();
		DolocAPI.EnterUI((EquipmentPanelUiState state) => state.HandleStartUpArgs(((EquipmentFuncEquipmentWorkbench)proto.Function).RecipeGroupName_Ref, DolocAPI.GetInventoriesAroundEquipment(this)));
		SendUseEquipmentMessage();
	}
}
