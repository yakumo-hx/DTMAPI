using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class PowerGeneratorWind : PowerGenerator
{
	public override bool IsFuelGenerator => false;

	public override float FuelPercent { get; }

	public PowerGeneratorWind(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	protected PowerGeneratorWind(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
	}

	public override bool IsSuitableFuel(Item fuelItem)
	{
		return false;
	}

	public override void AddFuel(ItemInfo fuelItem, bool sendMessage = false)
	{
	}

	protected override void Update()
	{
		SetWindGeneratorAnimator(base.Host.CurrentWeatherInfo.IsWindy);
	}

	protected override void OnRender()
	{
		base.Renderer.GetComponent<Animator>().runtimeAnimatorController = proto.animatorController;
		SetWindGeneratorAnimator(base.Host.CurrentWeatherInfo.IsWindy);
	}

	protected override void OnGeneratorChangeState(bool value)
	{
		SetWindGeneratorAnimator(value);
	}

	private void SetWindGeneratorAnimator(bool value)
	{
		if (!(base.Renderer == null))
		{
			if (value)
			{
				base.Renderer.PlayAnimation("running");
			}
			else
			{
				base.Renderer.Animator.enabled = false;
			}
		}
	}
}
