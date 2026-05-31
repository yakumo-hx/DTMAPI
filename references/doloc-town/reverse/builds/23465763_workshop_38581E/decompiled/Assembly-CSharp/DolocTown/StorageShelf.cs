using DolocTown.Config;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using ParadoxNotion.Design;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class StorageShelf : Equipment, IContainer, ILocatable
{
	[JsonProperty]
	[Name("inventory", 0)]
	protected LinearInventory _inventory;

	protected EquipmentFuncStorageShelf func;

	protected SingleSpriteRender[] boxRenders;

	public string title => proto.Title;

	protected int boxCount => _inventory.filledCount;

	protected bool isFull => _inventory.filledCount == _inventory.capacity;

	public int totalCapacity => func.TotalCapacity;

	public LinearInventory inventory => _inventory;

	public int lineCapacity { get; }

	public override bool IsDirty => inventory.filledCount > 0;

	public override Sprite OverrideUiSprite
	{
		get
		{
			if (!IsDirty)
			{
				return null;
			}
			return func.FullUiIcon.Asset;
		}
	}

	[DebugInfo("共享库存")]
	public bool IsShared { get; set; }

	public StorageShelf(IEquipmentHost room, int instanceId, EquipmentInfo proto, Vector3 worldPos, Vector2Int anchor, bool turn)
		: base(room, instanceId, proto, worldPos, anchor, turn)
	{
		func = (EquipmentFuncStorageShelf)proto.Function;
		_inventory = new LinearInventory(totalCapacity);
		boxRenders = new SingleSpriteRender[totalCapacity];
	}

	[JsonConstructor]
	protected StorageShelf(int id, Vector2Int anchor, Vector3 position, string equipmentName, LinearInventory inventory, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		func = (EquipmentFuncStorageShelf)proto.Function;
		_inventory = inventory;
		boxRenders = new SingleSpriteRender[totalCapacity];
	}

	protected override void OnRender()
	{
		base.OnRender();
		_inventory.AddReceiver(OnBoxChange);
	}

	protected override void OnUnRender()
	{
		_inventory.RemoveReceiver(OnBoxChange);
		for (int num = boxRenders.Length - 1; num >= 0; num--)
		{
			SingleSpriteRender singleSpriteRender = boxRenders[num];
			if (singleSpriteRender != null)
			{
				DolocAPI.EntitySystem.Recycle(singleSpriteRender);
			}
			boxRenders[num] = null;
		}
	}

	private void OnBoxChange(int index, Item item, bool _)
	{
		if (item == null || item is ItemBox)
		{
			SetBoxRenderVisible(index, item != null);
		}
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		ShowTip(DolocConfig.StaticTexts.UiOperationStorageShelf);
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		HideTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		PushTipToHide();
		if (DolocAPI.SelectedItem is ItemBox && !isFull)
		{
			ItemBox itemBox = DolocAPI.archiveHandle.InventorySystem.Take(DolocAPI.SelectedItemIndex) as ItemBox;
			PlaceBox(itemBox);
			return;
		}
		DolocAPI.EnterUI((StorageShelfUiState state) => state.HandleStartUpArgs(this));
		SendUseEquipmentMessage();
	}

	private void PlaceBox(ItemBox itemBox)
	{
		if (!isFull)
		{
			int num = FindSlot();
			_inventory.SwapItem(num, itemBox);
		}
	}

	private int FindSlot()
	{
		float x = DolocAPI.AgentPosition.x;
		if (totalCapacity == 6)
		{
			int[] array = ((!(x < base.Position.x)) ? new int[6] { 3, 5, 1, 2, 4, 0 } : new int[6] { 2, 4, 0, 3, 5, 1 });
			for (int i = 0; i < totalCapacity; i++)
			{
				if (_inventory.Read(array[i]) == null)
				{
					return array[i];
				}
			}
		}
		else
		{
			for (int j = 0; j < totalCapacity; j++)
			{
				if (_inventory.Read(j) == null)
				{
					return j;
				}
			}
		}
		return -1;
	}

	private void SetBoxRenderVisible(int index, bool value)
	{
		if (!value)
		{
			boxRenders[index]?.SetVisible(value: false);
			return;
		}
		if (boxRenders[index] == null)
		{
			SingleSpriteRender singleSpriteRender = DolocAPI.EntitySystem.Next<SingleSpriteRender>();
			singleSpriteRender.sprite = func.Appearance.assets[index];
			singleSpriteRender.position = base.Position + new Vector3(0f, 0f, -1E-05f * (float)(index + 1));
			boxRenders[index] = singleSpriteRender;
		}
		boxRenders[index].SetVisible(value: true);
	}

	public bool ContentFilter(Item item)
	{
		if (item != null)
		{
			return item is ItemBox;
		}
		return true;
	}

	protected override void OnRemove()
	{
		base.OnRemove();
		IsShared = false;
	}
}
