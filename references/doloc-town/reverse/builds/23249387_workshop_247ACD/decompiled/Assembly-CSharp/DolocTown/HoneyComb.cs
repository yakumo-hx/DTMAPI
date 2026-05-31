using System;
using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class HoneyComb : Equipment, IGatherableEquipment, IAnimalHoneyComb, IAnimalInteractable
{
	private readonly EquipmentFuncHoneyComb funcHoneyComb;

	[DebugInfo("产出物")]
	private List<string> _products;

	public bool IsGatherable => IsHoneyCombFull;

	[JsonProperty]
	private string[] products => _products.ToArray();

	public override Sprite EquipmentSprite
	{
		get
		{
			if (IsHoneyCombFull)
			{
				return funcHoneyComb.FullSprite.Asset ?? base.EquipmentSprite;
			}
			if (_products.Count > 0)
			{
				return funcHoneyComb.HalfSprite.Asset ?? base.EquipmentSprite;
			}
			return base.EquipmentSprite;
		}
	}

	public Vector2Int AnimalInteractablePosition => base.Anchor;

	public Vector2 AnimalInteractablePositionWS => base.PositionBottom;

	public int AnimalInteractableWidth => proto.CoverSize.x;

	public bool AnimalInteractableIsValid => base.index >= 0;

	public int AnimalCounter { get; set; }

	public bool IsHoneyCombFull => _products.Count >= funcHoneyComb.Capacity;

	public Item[] Gather()
	{
		if (_products.Count <= 0)
		{
			return Array.Empty<Item>();
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		string key;
		int value;
		foreach (string product in _products)
		{
			if (!dictionary.TryAdd(product, 1))
			{
				key = product;
				value = dictionary[key]++;
			}
		}
		List<Item> list = new List<Item>();
		foreach (KeyValuePair<string, int> item2 in dictionary)
		{
			item2.Deconstruct(out key, out value);
			string name = key;
			int count = value;
			Item item = DolocAPI.GenerateItem(name, count);
			list.Add(item);
		}
		return list.ToArray();
	}

	public HoneyComb(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		funcHoneyComb = (EquipmentFuncHoneyComb)proto.Function;
		_products = new List<string>();
	}

	[JsonConstructor]
	protected HoneyComb(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, string[] products)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			funcHoneyComb = (EquipmentFuncHoneyComb)proto.Function;
			_products = new List<string>(products ?? Array.Empty<string>());
		}
	}

	private void CollectProduct()
	{
		if (_products.Count == 0)
		{
			return;
		}
		foreach (string product in _products)
		{
			this.CreateDropItem(product, shouldRender: true, sendMessage: true);
		}
		_products.Clear();
		base.Renderer.Sprite = EquipmentSprite;
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		if (_products.Count > 0)
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
		if (_products.Count > 0)
		{
			this.PushSceneOperationTipToHide();
			CollectProduct();
		}
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		foreach (string product in _products)
		{
			this.PlaceItemInBagOrCreateDropItem(product, putInBackpack, sendMessage: true);
		}
		_products.Clear();
	}

	public void ProduceHoney(CountItem[] items)
	{
		for (int i = 0; i < items.Length; i++)
		{
			CountItem countItem = items[i];
			if (IsHoneyCombFull)
			{
				break;
			}
			for (int j = 0; j < countItem.itemCount; j++)
			{
				_products.Add(countItem.itemName);
				if (IsHoneyCombFull)
				{
					break;
				}
			}
		}
		if (base.IsRender)
		{
			base.Renderer.Sprite = EquipmentSprite;
			DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.BRUST_STARS);
		}
	}
}
