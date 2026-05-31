using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class Sprinkler : AffectorElectric
{
	private readonly EquipmentFuncSprinkler sprinklerProto;

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
			Sprite asset = sprinklerProto.OffSprite.Asset;
			if (!(asset == null))
			{
				return asset;
			}
			return base.EquipmentSprite;
		}
	}

	public override AffectorType AffectType => AffectorType.Sprinkler;

	public Sprinkler(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
		sprinklerProto = (EquipmentFuncSprinkler)proto.Function;
	}

	[JsonConstructor]
	protected Sprinkler(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, Counter workCounter, bool isIdle, bool isTurnOn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, workCounter, isIdle, isTurnOn)
	{
		sprinklerProto = (EquipmentFuncSprinkler)proto.Function;
	}

	protected override void RenderOnSwitch(bool value)
	{
		base.Renderer.Sprite = (value ? base.EquipmentSprite : SpriteOff);
	}

	protected override void OnInvokeAffect()
	{
		DolocAPI.RaiseContinuesPS((Vector2)base.Position + ((EquipmentFuncSprinkler)proto.Function).EffectOffset, ContinuesParticleEffectsType.SPRINKLER, Random.Range(4, 6));
	}
}
