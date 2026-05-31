using DolocTown.Config;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class LivestockNursery : Equipment, IAnimalLivestockNursery, IAnimalInteractable
{
	private readonly EquipmentFuncLivestockNursery _func;

	[JsonProperty]
	[DebugInfo("繁育计数器")]
	public readonly Counter counter = new Counter();

	private int _tmp_animal_index;

	public override bool IsOccupy => isBreeding;

	[JsonProperty]
	[DebugInfo("是否正在使用")]
	public bool isBreeding { get; private set; }

	public override Sprite EquipmentSprite
	{
		get
		{
			if (!isBreeding)
			{
				return proto.Sprite;
			}
			if (!_func.SpriteInUse.Asset)
			{
				return proto.Sprite;
			}
			return _func.SpriteInUse.Asset;
		}
	}

	public Room AnimalInteractableRoom => base.CurrentRoom;

	public Vector2Int AnimalInteractablePosition => base.Anchor + new Vector2Int(proto.CoverSize.x / 2, 0);

	public Vector2 AnimalInteractablePositionWS => base.PositionBottom;

	public int AnimalInteractableWidth => proto.CoverSize.x;

	public bool AnimalInteractableIsValid => base.index >= 0;

	public bool IsAnimalInteractableLocked { get; set; }

	public int AnimalCounter { get; set; }

	public bool IsLivestockNurseryFree => !isBreeding;

	public override string GetOccupyInfo()
	{
		return DolocUtils.Format(DolocConfig.StaticTexts.AnimalInUse, Title);
	}

	public LivestockNursery(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		_func = (EquipmentFuncLivestockNursery)proto.Function;
	}

	[JsonConstructor]
	public LivestockNursery(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool isBreeding, Counter counter)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			_func = (EquipmentFuncLivestockNursery)proto.Function;
			this.isBreeding = isBreeding;
			this.counter = counter;
		}
	}

	protected override void Update()
	{
		if (isBreeding && counter.Tick())
		{
			isBreeding = false;
			base.Renderer.Sprite = EquipmentSprite;
		}
	}

	protected override void UpdateNoRender()
	{
		if (isBreeding && counter.Tick())
		{
			isBreeding = false;
		}
	}

	public void StartBreed(int breedDuration)
	{
		if (!isBreeding)
		{
			isBreeding = true;
			counter.SetInterval(breedDuration * DolocAPI.GlobalParameter.TULength);
			if (base.IsRender)
			{
				base.Renderer.Sprite = EquipmentSprite;
			}
		}
	}
}
