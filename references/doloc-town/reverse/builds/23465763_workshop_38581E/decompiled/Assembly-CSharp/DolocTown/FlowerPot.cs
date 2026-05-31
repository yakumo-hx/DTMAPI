using DolocTown.Config.Equipment;
using DolocTown.Config.Plant;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class FlowerPot : Equipment
{
	private SeedInfo seedProto;

	[JsonProperty]
	[DebugInfo]
	private Item item;

	[JsonProperty]
	[DebugInfo]
	private int currentCropLv;

	private readonly GameEntitySlot<SingleSpriteRender> cropRendererSlot = new GameEntitySlot<SingleSpriteRender>();

	private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();

	[JsonProperty]
	[DebugInfo]
	private string seedName => seedProto?.Id ?? string.Empty;

	private Sprite CurrentCropSprite
	{
		get
		{
			if (seedProto == null)
			{
				return null;
			}
			return seedProto.GetLevelSprite(currentCropLv);
		}
	}

	[DebugInfo]
	public bool IsPlanted => seedProto != null;

	public Vector3 PositionCrop => new Vector3(base.Position.x, base.HeightLevel.z, base.PositionCenter.z - proto.WorldSpriteSize.y * 0.499f);

	public FlowerPot(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		seedProto = null;
		currentCropLv = 0;
		item = null;
	}

	[JsonConstructor]
	protected FlowerPot(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, string seedName, int currentCropLv, Item item = null)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto == null)
		{
			return;
		}
		Debug.Log("<color=red>花盆加载:" + seedName + "</color>");
		if (item == null)
		{
			if (!seedName.IsNullOrEmpty() && DolocAPI.QuerySeedProto(seedName, out var seedInfo))
			{
				this.item = DolocAPI.GenerateItem(seedName);
				seedProto = seedInfo;
				this.currentCropLv = currentCropLv;
			}
			return;
		}
		this.item = item;
		if (!(item is ItemSeed itemSeed))
		{
			this.item = null;
			seedProto = null;
			this.currentCropLv = 0;
		}
		else
		{
			seedProto = itemSeed.seedProto;
			this.currentCropLv = currentCropLv;
		}
	}

	private void UpdateRenderer(bool isInit = true)
	{
		if (cropRendererSlot.Entity == null)
		{
			return;
		}
		if (isInit)
		{
			cropRendererSlot.Entity.sprite = CurrentCropSprite;
			cropRendererSlot.Entity.position = PositionCrop;
			Transform transform = cropRendererSlot.Entity.transform;
			transform.rotation = Quaternion.identity;
			transform.localRotation = Quaternion.identity;
			transform.localScale = new Vector3(1f, 1f, 1f);
		}
		else
		{
			cropRendererSlot.Entity.SpriteRenderer.GrowToNext(CurrentCropSprite);
		}
		if (item != null)
		{
			if (item is ItemSeed itemSeed && itemSeed.GeneGroup.ContainsGene("firefly"))
			{
				cropRendererSlot.Entity.SpriteRenderer.RenderAsFirefly(_propertyBlock, CurrentCropSprite);
			}
			else
			{
				cropRendererSlot.Entity.SpriteRenderer.RenderAsNormalCrop();
			}
		}
	}

	public void Plant(Item item)
	{
		if (item is ItemSeed itemSeed)
		{
			this.item = item;
			seedProto = itemSeed.seedProto;
			currentCropLv = 0;
			UpdateRenderer();
		}
	}

	protected override void OnUnRender()
	{
		base.OnUnRender();
		cropRendererSlot.Release();
	}

	protected override void OnRender()
	{
		base.OnRender();
		UpdateRenderer();
	}

	protected override void OnManualWater()
	{
		if (seedProto != null)
		{
			currentCropLv++;
			if (currentCropLv >= seedProto.LevelCount)
			{
				currentCropLv = 0;
			}
			UpdateRenderer(isInit: false);
		}
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		if (item != null)
		{
			this.PlaceItemInBagOrCreateDropItem(item, putInBackpack, sendMessage: false);
		}
	}
}
