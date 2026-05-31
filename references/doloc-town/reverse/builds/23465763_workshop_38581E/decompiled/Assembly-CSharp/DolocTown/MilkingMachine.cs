using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class MilkingMachine : Equipment, IAnimalMilkingMachine, IAnimalInteractable
{
	private readonly EquipmentFuncMilkingMachine _func;

	[JsonProperty]
	private List<string> products;

	[DebugInfo("是否就绪", AllowEdit = true)]
	[JsonProperty]
	private bool isCharged;

	private ElectronicComponentAppliance _appliance;

	private int __animal_counter;

	public bool IsGatherable => IsFull;

	private int Capacity => _func.Capacity;

	public bool IsAvailable => isCharged;

	public override Sprite EquipmentSprite
	{
		get
		{
			if (IsFull)
			{
				return _func.FullSprite.Asset ?? base.EquipmentSprite;
			}
			if (products.Count > 0)
			{
				return _func.HalfSprite.Asset ?? base.EquipmentSprite;
			}
			return base.EquipmentSprite;
		}
	}

	public bool IsFull => products.Count >= Capacity;

	public int FeederPriority => 0;

	public Room AnimalInteractableRoom => base.Host.CurrentRoom;

	public bool AnimalInteractableIsValid => base.index >= 0;

	public Vector2 AnimalInteractablePositionWS => base.PositionBottom;

	public Vector2Int AnimalInteractablePosition
	{
		get
		{
			if (proto.CoverSize.x <= 3)
			{
				return base.Anchor;
			}
			return new Vector2Int(base.Anchor.x + proto.CoverSize.x / 2, base.Anchor.y);
		}
	}

	public int AnimalInteractableWidth => proto.CoverSize.x;

	public bool IsAnimalInteractableLocked { get; set; }

	public int AnimalCounter
	{
		get
		{
			return __animal_counter;
		}
		set
		{
			__animal_counter = Mathf.Max(0, value);
		}
	}

	public Item[] Gather()
	{
		Item[] array = new Item[products.Count];
		for (int i = 0; i < products.Count; i++)
		{
			array[i] = DolocAPI.GenerateItem(products[i]);
		}
		products.Clear();
		return array.ToArray();
	}

	public MilkingMachine(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		_func = (EquipmentFuncMilkingMachine)proto.Function;
		_appliance = (ElectronicComponentAppliance)electronicComponent;
		products = new List<string>();
	}

	[JsonConstructor]
	public MilkingMachine(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, List<string> products = null)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			_func = (EquipmentFuncMilkingMachine)proto.Function;
			_appliance = (ElectronicComponentAppliance)electronicComponent;
			this.products = products ?? new List<string>();
		}
	}

	public override void AfterLoadEquipment()
	{
		base.AfterLoadEquipment();
		products = DolocAPI.ValidateItems(products);
	}

	protected override void Update()
	{
		UpdateNoRender();
	}

	protected override void UpdateNoRender()
	{
		if (!isCharged && _appliance.Launch())
		{
			isCharged = true;
		}
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		if (products.Count > 0)
		{
			this.ShowSceneOperationTip(PositionTip, DolocConfig.StaticTexts.UiOperationCollect);
		}
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (products.Count != 0)
		{
			this.CollectAnimalProducts(products);
			PushTipToHide();
			base.Renderer.PostSoundEventByEnum(SoundEvents.PLAY_RESOURCE_HARVEST);
		}
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		this.CollectAnimalProducts(products);
	}

	public void Produce(CountItem[] items)
	{
		isCharged = false;
		this.ReceiveAnimalProducts(products, items, Capacity);
		if (base.IsRender)
		{
			base.Renderer.Sprite = EquipmentSprite;
		}
	}
}
