using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.GameData;
using DolocTown.GameDataTracker;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class ParkingApron : Equipment, IContainer
{
	private readonly EquipmentFuncParkingApron func;

	[JsonProperty]
	[DebugInfo("是否可以发射", Color = "#51b341")]
	private bool couldLaunch = true;

	[JsonProperty]
	[DebugInfo("等待计时器", Color = "#ff7f4f")]
	private readonly Counter waitCounter = new Counter();

	[JsonProperty]
	[DebugInfo("是否正在返航", Color = "#ff7f4f")]
	private bool isReturn;

	[JsonProperty]
	[DebugInfo("是否正在送货", Color = "#ff7f4f")]
	private bool isTakeOff;

	[JsonProperty]
	[DebugInfo("未结算货款", Color = "#ff7f4f")]
	private int pendingPayment;

	[JsonProperty]
	private Queue<Item> itemsToReturn;

	private int inventoryTotalPrice;

	private SingleSpriteRender goodsRenderer;

	private SingleSpriteRender droneRenderer;

	private SignalLight signalLightRenderer;

	private bool openDoorProtectionFlag;

	private Store _store;

	public string title => proto.Title;

	[JsonProperty]
	public LinearInventory inventory { get; private set; }

	public int InventoryTotalPrice => inventoryTotalPrice;

	private int inventoryFilledCount => inventory.filledCount;

	private Vector2 LandingPosition => base.PositionBottom + new Vector3(func.ExpressDroneOffset, -0.0001f);

	private Vector3 PositionCargo
	{
		get
		{
			float num = proto.WorldSpriteSize.y - 0.375f;
			return base.PositionBottom + new Vector3(0f, num, 0f - num - 0.0001f);
		}
	}

	public bool CouldLaunch => couldLaunch;

	public bool IsReturn => isReturn;

	public bool IsTakeOff => isTakeOff;

	public override bool IsOccupy => !couldLaunch;

	private Vector2 RemotePosition => new Vector2(-50f, 50f);

	private Vector2 ReturnRemotePosition => new Vector2(200f, 30f);

	public int totalCapacity => func.TotalCapacity;

	public int lineCapacity => func.LineCapacity;

	private Store store
	{
		get
		{
			if (_store == null)
			{
				DolocAPI.archiveHandle.QueryStore(func.StoreName, out _store);
			}
			return _store;
		}
	}

	public string GetCurrentContainerInfo()
	{
		string text = DolocConfig.StaticTexts.DropoffBoxTotalMoney.Format(InventoryTotalPrice);
		if (func.PriceIncreasedMonths.Contains(DolocAPI.archiveHandle.DateNow.Month))
		{
			text = text + " " + DolocConfig.StaticTexts.DropoffBoxPriceIncreased.Colored(DolocUiColor.EYECATCHCOLOR_CYAN);
		}
		return text;
	}

	public override string GetOccupyInfo()
	{
		return DolocConfig.StaticTexts.FarmbuilderErrParkingApronOccupied;
	}

	public ParkingApron(IEquipmentHost host, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, id, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncParkingApron)proto.Function;
		pendingPayment = 0;
		inventoryTotalPrice = 0;
		isReturn = false;
		inventory = new LinearInventory(func.TotalCapacity);
		itemsToReturn = new Queue<Item>();
	}

	[JsonConstructor]
	public ParkingApron(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool couldLaunch, Counter waitCounter, LinearInventory inventory, bool isReturn, bool isTakeOff, int pendingPayment, Queue<Item> itemsToReturn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		func = (EquipmentFuncParkingApron)proto.Function;
		this.couldLaunch = couldLaunch;
		this.waitCounter = waitCounter;
		this.inventory = inventory;
		this.isReturn = isReturn;
		this.isTakeOff = isTakeOff;
		this.pendingPayment = pendingPayment;
		this.itemsToReturn = itemsToReturn ?? new Queue<Item>();
	}

	[DebugButton("唤回无人机")]
	public void Recall()
	{
		if (!couldLaunch && !isReturn && !isTakeOff)
		{
			Recycle();
		}
	}

	[DebugButton("发射无人机")]
	public void Launch()
	{
		if (couldLaunch && !inventory.isEmpty)
		{
			isTakeOff = true;
			couldLaunch = false;
			_StartLaunchAnimation();
			SellAllItemInInventory();
			waitCounter.SetInterval(func.GetGoodsLvDuration(pendingPayment) * DolocAPI.GlobalParameter.TULength);
			DolocAPI.Broadcast(GameEventType.LAUNCH_FREIGHT_DRONE);
		}
	}

	private void _StartLaunchAnimation()
	{
		if (!(base.Renderer == null))
		{
			ExpressDrone renderComponent = base.Renderer.GetRenderComponent<ExpressDrone>();
			droneRenderer.SetVisible(value: false);
			renderComponent.SetLandingPosition(LandingPosition);
			renderComponent.Reset();
			renderComponent.Launch(RemotePosition, AfterArrived);
			goodsRenderer.SetVisible(value: false);
		}
	}

	private void Recycle()
	{
		if (!couldLaunch && !isReturn)
		{
			isReturn = true;
			if (base.Renderer == null)
			{
				AfterReturnedNoRender();
				return;
			}
			ExpressDrone renderComponent = base.Renderer.GetRenderComponent<ExpressDrone>();
			renderComponent.SetLandingPosition(LandingPosition);
			renderComponent.Recycle(ReturnRemotePosition, AfterReturned);
			signalLightRenderer.Switch(value: true);
		}
	}

	private void AfterArrived()
	{
		isTakeOff = false;
		base.Renderer.RemoveRenderComponent<ExpressDrone>();
	}

	private void AfterReturned()
	{
		openDoorProtectionFlag = true;
		base.Renderer.GetRenderComponent<ExpressDrone>().OpenDoor(AfterOpenDoor);
		signalLightRenderer.Switch(value: false);
		isReturn = false;
	}

	private void AfterReturnedNoRender()
	{
		SpawnMoneyItemsNoRender();
		couldLaunch = true;
		isReturn = false;
	}

	private void AfterOpenDoor()
	{
		openDoorProtectionFlag = false;
		SpawnMoneyItems(delegate
		{
			couldLaunch = true;
			if (base.IsRender)
			{
				RenderIndicatedDrone();
				base.Renderer.RemoveRenderComponent<ExpressDrone>();
			}
		}).Forget();
	}

	private void UpdateGoodsSprite()
	{
		if (goodsRenderer == null)
		{
			goodsRenderer = DolocAPI.EntitySystem.Next<SingleSpriteRender>();
		}
		if (!(goodsRenderer == null))
		{
			if (inventoryFilledCount == 0)
			{
				goodsRenderer.SetVisible(value: false);
				return;
			}
			goodsRenderer.position = PositionCargo;
			goodsRenderer.sprite = func.GetGoodsLvSprite(inventoryFilledCount);
			goodsRenderer.SetVisible(value: true);
		}
	}

	private void SpawnMoneyItemsNoRender()
	{
		while (itemsToReturn.Count > 0)
		{
			this.CreateDropItem(itemsToReturn.Dequeue(), base.IsRender, sendMessage: false);
		}
		if (pendingPayment <= 0)
		{
			return;
		}
		foreach (int item in DolocAPI.SpawnMoneyData(pendingPayment))
		{
			this.CreateDropItemMoney(item, base.IsRender, sendMessage: true);
		}
		pendingPayment = 0;
	}

	private async UniTaskVoid SpawnMoneyItems(Action callback = null)
	{
		while (itemsToReturn.Count > 0)
		{
			this.CreateDropItem(itemsToReturn.Dequeue(), base.IsRender, sendMessage: false);
			await UniTask.Delay(100);
		}
		foreach (int item in DolocAPI.SpawnMoneyData(pendingPayment))
		{
			this.CreateDropItemMoney(item, base.IsRender, sendMessage: true);
			await UniTask.Delay(100);
		}
		callback?.Invoke();
	}

	private void RenderIndicatedDrone()
	{
		droneRenderer = DolocAPI.EntitySystem.Next<SingleSpriteRender>();
		Vector2 landingPosition = LandingPosition;
		droneRenderer.position = new Vector3(landingPosition.x, landingPosition.y, 0.5f);
		droneRenderer.sprite = func.ExpressDroneAsset.Asset;
		droneRenderer.SortingLayerName = "Default";
		droneRenderer.SetVisible(value: true);
	}

	private void SellAllItemInInventory()
	{
		itemsToReturn.Clear();
		Item[] array = inventory.ReadAll();
		List<(Item, int)> list = new List<(Item, int)>();
		Item[] array2 = array;
		foreach (Item item in array2)
		{
			float priceScale;
			int itemUnitSellingPrice = store.GetItemUnitSellingPrice(item, out priceScale);
			list.Add((item, itemUnitSellingPrice));
			if (item.name == DolocAPI.GlobalParameter.ItemRefSturdySack && item is ItemAnimalPackage { isFull: not false })
			{
				itemsToReturn.Enqueue(DolocAPI.GenerateItem(DolocAPI.GlobalParameter.ItemRefSturdySack));
			}
		}
		DataUploader.TraceExpressDrone(list.ToArray());
		store.SellItems(array, out pendingPayment);
		inventory.Clear();
		inventoryTotalPrice = 0;
	}

	public void RefreshTotalMoney()
	{
		inventoryTotalPrice = GetTotalMoney();
	}

	private void RefreshTotalMoney(int i, Item item, bool _)
	{
		RefreshTotalMoney();
	}

	private int GetTotalMoney()
	{
		int price = 0;
		inventory.ForEach(delegate(Item item)
		{
			price += store.GetItemTotalSellingPrice(item);
		});
		return price;
	}

	public int GetItemUnitPrice(Item item, out float priceScale)
	{
		return store.GetItemUnitSellingPrice(item, out priceScale);
	}

	public bool ContentFilter(Item item)
	{
		return DolocAPI.IsItemSalable(item);
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		ShowTip(DolocConfig.StaticTexts.UiOperationSell);
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		HideTip();
	}

	protected override void Update()
	{
		UpdateNoRender();
	}

	protected override void UpdateNoRender()
	{
		if (!couldLaunch && !isReturn && !isTakeOff && waitCounter.Tick())
		{
			Recycle();
		}
	}

	protected override void OnInteract()
	{
		PushTipToHide();
		DolocAPI.EnterUI((ParkingApronUiState state) => state.HandleStartUpArgs(this, UpdateGoodsSprite));
		SendUseEquipmentMessage();
	}

	protected override void OnRender()
	{
		UpdateGoodsSprite();
		signalLightRenderer = DolocAPI.EntitySystem.Next<SignalLight>();
		signalLightRenderer.position2d = base.PositionBottom + new Vector3(func.SignalLightOffset, -0.001f);
		signalLightRenderer.Setup(func.SignalLightAsset.Asset, func.SignalLightMask.Asset);
		signalLightRenderer.Switch(value: false);
		signalLightRenderer.SetVisible(value: true);
		if (couldLaunch)
		{
			RenderIndicatedDrone();
		}
		inventory.AddReceiver(RefreshTotalMoney);
	}

	protected override void OnUnRender()
	{
		DolocAPI.EntitySystem.Recycle(goodsRenderer);
		goodsRenderer = null;
		DolocAPI.EntitySystem.Recycle(droneRenderer);
		droneRenderer = null;
		DolocAPI.EntitySystem.Recycle(signalLightRenderer);
		signalLightRenderer = null;
		base.Renderer.RemoveRenderComponent<ExpressDrone>();
		if (isReturn)
		{
			isReturn = false;
			couldLaunch = true;
			SpawnMoneyItemsNoRender();
		}
		else if (openDoorProtectionFlag)
		{
			openDoorProtectionFlag = false;
			SpawnMoneyItemsNoRender();
		}
		if (isTakeOff)
		{
			isTakeOff = false;
		}
		inventory.RemoveReceiver(RefreshTotalMoney);
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		for (int i = 0; i < inventory.capacity; i++)
		{
			Item item = inventory.Take(i);
			if (item != null)
			{
				this.PlaceItemInBagOrCreateDropItem(item, putInBackpack, sendMessage: false);
			}
		}
	}
}
