using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class ChickenNest : Equipment, IGatherableEquipment
{
	private readonly EquipmentFuncChickenNest _func;

	[JsonProperty]
	[DebugInfo("道具列表")]
	public List<string> items;

	public bool IsGatherable => IsFull;

	public override Sprite EquipmentSprite
	{
		get
		{
			Sprite sprite = proto.Sprite;
			if (items.Count == 0)
			{
				return sprite;
			}
			Sprite[] assets = _func.Sprites.assets;
			if (assets.Length == 0)
			{
				return sprite;
			}
			int num = Mathf.Min((items.Count > 2) ? 1 : 0, assets.Length - 1);
			return assets[num] ?? sprite;
		}
	}

	public bool IsFull => items.Count >= _func.Capacity;

	public Item[] Gather()
	{
		Item[] array = new Item[items.Count];
		for (int i = 0; i < items.Count; i++)
		{
			array[i] = DolocAPI.GenerateItem(items[i]);
		}
		items.Clear();
		return array.ToArray();
	}

	public ChickenNest(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		_func = (EquipmentFuncChickenNest)proto.Function;
		items = new List<string>();
	}

	[JsonConstructor]
	public ChickenNest(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, List<string> items)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			_func = (EquipmentFuncChickenNest)proto.Function;
			this.items = items ?? new List<string>();
		}
	}

	private void ClearNest()
	{
		if (items.Count <= 0)
		{
			return;
		}
		foreach (string item in items)
		{
			this.CreateDropItem(item, shouldRender: true, sendMessage: true);
		}
		items.Clear();
		if (base.IsRender)
		{
			base.Renderer.Sprite = EquipmentSprite;
		}
	}

	protected override void OnTouch()
	{
		if (items.Count > 0)
		{
			this.ShowSceneOperationTip(PositionTip, DolocConfig.StaticTexts.UiOperationPick);
		}
	}

	protected override void OnRemove()
	{
		if (base.IsRender)
		{
			DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.CHICKEN_NEST_CRACK);
		}
		AnimalEvent evt = new AnimalEvent(AnimalEventType.CHICKEN_NEST_BROKEN, new GameEventArgs<ChickenNest>(this));
		base.CurrentRoom.animalSystem.SendMessage(evt);
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		foreach (string item in items)
		{
			this.PlaceItemInBagOrCreateDropItem(item, putInBackpack, sendMessage: true);
		}
		items.Clear();
		if (RandomUtils.Dice(0.5f))
		{
			this.PlaceItemInBagOrCreateDropItem(DolocAPI.GlobalParameter.ItemRefWeeds, putInBackpack, sendMessage: true);
		}
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (items.Count > 0)
		{
			this.PushSceneOperationTipToHide();
			DolocAPI.agent._Interact(ClearNest);
		}
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
	}

	public void Produce(CountItem[] inputItems)
	{
		if (inputItems.IsNullOrEmpty())
		{
			return;
		}
		for (int i = 0; i < inputItems.Length; i++)
		{
			CountItem countItem = inputItems[i];
			for (int j = 0; j < countItem.itemCount; j++)
			{
				items.Add(countItem.itemName);
			}
		}
		if (base.IsRender)
		{
			base.Renderer.Sprite = EquipmentSprite;
			DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.BRUST_STARS);
		}
	}
}
