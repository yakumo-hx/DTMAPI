using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class TrampolineEquipment : Equipment
{
	public TrampolineEquipment(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	protected TrampolineEquipment(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
	}

	protected override void OnRender()
	{
		base.Renderer.Sr.color = DolocColor.empty;
		base.Renderer.GetRenderComponent<TrampolineRenderer>().position = base.Position;
	}

	protected override void OnUnRender()
	{
		base.Renderer.RemoveRenderComponent<TrampolineRenderer>();
	}

	public override void OnFell(Vector2 hitPosition)
	{
		TrampolineRenderer renderComponent = base.Renderer.GetRenderComponent<TrampolineRenderer>();
		DolocAPI.RaiseInstantAnimEffects(hitPosition, InstAnimEffectType.HIT_SPARK, LocMaterials.GAME_MAT_EFFECTS_SHINE);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_KNOCKING_EQUIPMENT);
		if (!WaitRemove)
		{
			WaitRemove = true;
			renderComponent.Shiner(2f);
			renderComponent.Shake(2f);
			DolocAPI.Delay(2f, OnRecover);
		}
		else
		{
			base.Host.RemoveEquipment(this);
			DolocAPI.RefreshScanner();
			DolocAPI.Broadcast(OperationEventType.FELL_EQUIPMENT);
		}
	}
}
