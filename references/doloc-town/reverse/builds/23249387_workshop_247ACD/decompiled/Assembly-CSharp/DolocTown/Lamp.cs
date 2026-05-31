using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class Lamp : Equipment, ILamp
{
	private readonly LampController lampController;

	[JsonProperty]
	[DebugInfo("是否点亮", Color = "#ffff00")]
	private bool shouldLight;

	[JsonProperty]
	private bool hasInitLight;

	public LampController LampController => lampController;

	public bool ShouldLight => shouldLight;

	public Lamp(IEquipmentHost host, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, id, proto, wp, anchor, turn)
	{
		hasInitLight = false;
		EquipmentFuncLamp equipmentFuncLamp = (EquipmentFuncLamp)proto.Function;
		lampController = new LampController(this, equipmentFuncLamp.Lamp_Ref);
	}

	[JsonConstructor]
	protected Lamp(int id, string equipmentName, Vector3 position, Vector2Int anchor, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool shouldLight, bool hasInitLight)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		this.shouldLight = shouldLight;
		this.hasInitLight = hasInitLight;
		EquipmentFuncLamp equipmentFuncLamp = (EquipmentFuncLamp)proto.Function;
		lampController = new LampController(this, equipmentFuncLamp.Lamp_Ref);
	}

	public void ToggleLight(bool value, bool shouldRender)
	{
		shouldLight = value;
		if (shouldRender)
		{
			lampController.ToggleLight(value, 1.2f);
		}
	}

	protected override void OnRender()
	{
		if (!hasInitLight)
		{
			hasInitLight = true;
			shouldLight = DolocAPI.archiveHandle.ShouldLightUp;
		}
		lampController.ToggleLight(shouldLight);
	}

	protected override void OnUnRender()
	{
		lampController.StopTween();
		lampController.ToggleLight(value: false);
	}
}
