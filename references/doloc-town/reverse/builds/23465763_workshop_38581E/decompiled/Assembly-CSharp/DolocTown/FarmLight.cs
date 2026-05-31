using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class FarmLight : AffectorElectric, ILamp
{
	private readonly LampController lampController;

	private readonly EquipmentFuncFarmLight farmLightProto;

	public LampController LampController => lampController;

	public bool ShouldLight => base.IsWorking;

	public override Sprite EquipmentSprite
	{
		get
		{
			if (base.IsTurnOn)
			{
				return base.EquipmentSprite;
			}
			return SpriteOff;
		}
	}

	private Sprite SpriteOff
	{
		get
		{
			Sprite asset = farmLightProto.OffSprite.Asset;
			if (!(asset == null))
			{
				return asset;
			}
			return base.EquipmentSprite;
		}
	}

	public override AffectorType AffectType => AffectorType.Light;

	public FarmLight(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
		farmLightProto = (EquipmentFuncFarmLight)proto.Function;
		lampController = new LampController(this, farmLightProto.Lamp_Ref);
	}

	[JsonConstructor]
	public FarmLight(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, Counter workCounter, bool isIdle, bool isTurnOn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, workCounter, isIdle, isTurnOn)
	{
		farmLightProto = (EquipmentFuncFarmLight)proto.Function;
		lampController = new LampController(this, farmLightProto.Lamp_Ref);
	}

	protected override void OnRender()
	{
		base.OnRender();
		lampController.ToggleLight(base.IsWorking);
	}

	protected override void OnUnRender()
	{
		base.OnUnRender();
		lampController.ToggleLight(value: false);
		lampController.StopTween();
	}

	protected override void OnInvokeAffect()
	{
		if (WaitRemove)
		{
			DolocAPI.WaitUntil(() => !WaitRemove, delegate
			{
				lampController.ToggleLight(value: true);
			});
		}
		else
		{
			lampController.ToggleLight(value: true, 1.2f);
		}
	}

	protected override void OnStopAffect()
	{
		if (WaitRemove)
		{
			DolocAPI.WaitUntil(() => !WaitRemove, delegate
			{
				lampController.ToggleLight(value: false);
			});
		}
		else
		{
			lampController.ToggleLight(value: false);
		}
	}

	protected override void RenderOnSwitch(bool value)
	{
		base.Renderer.Sprite = (value ? base.EquipmentSprite : SpriteOff);
		if (!value)
		{
			lampController.ToggleLight(value: false);
			lampController.StopTween();
		}
	}
}
