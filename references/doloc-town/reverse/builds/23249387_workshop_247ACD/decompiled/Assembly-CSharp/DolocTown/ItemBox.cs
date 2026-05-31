using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class ItemBox : Item, IContainer, IDurability
{
	public ItemFunctionBox func;

	private Dictionary<string, int> boxItemsBackup = new Dictionary<string, int>();

	public override Sprite uiSprite => func.GetSkinSprite(skinIndex, inventory == null || inventory.isEmpty) ?? base.proto.UiSpriteAsset.Asset;

	[JsonProperty]
	public LinearInventory inventory { get; private set; }

	public int totalCapacity => func.TotalCapacity;

	public int lineCapacity => func.LineCapacity;

	public int maxDurability => func.Durability;

	[JsonProperty]
	public int currentDurability { get; protected set; }

	[JsonProperty]
	public int skinIndex { get; protected set; }

	private LinearInventory backpack => DolocAPI.archiveHandle.InventorySystem.inventory;

	public bool canBoxPutIn => currentDurability > 0;

	public bool isBoxUsedUp
	{
		get
		{
			if (!canBoxPutIn)
			{
				return inventory.filledCount == 0;
			}
			return false;
		}
	}

	public ItemBox(ItemInfo item, int count)
		: base(item, count)
	{
		func = item.Function as ItemFunctionBox;
		inventory = new LinearInventory(totalCapacity);
		currentDurability = maxDurability;
	}

	[JsonConstructor]
	protected ItemBox(string itemName, int itemCount, LinearInventory inventory, int currentDurability, int skinIndex)
		: base(itemName, itemCount)
	{
		func = base.proto.Function as ItemFunctionBox;
		this.inventory = inventory;
		this.currentDurability = currentDurability;
		this.skinIndex = skinIndex;
	}

	private bool HasEnoughSpaceToThrow(Room room, int dir, int heightThreshold = 5, int widthThreshold = 5)
	{
		Vector3 vector = DolocAPI.AgentPosition + new Vector3(0f, 0.75f);
		Vector2Int position = room.Geometry.CalcCellPosition(vector);
		if (!room.Geometry.RaycastGround(position, out var groundPosition))
		{
			return false;
		}
		if (Mathf.Abs(groundPosition.y - position.y) > heightThreshold || groundPosition.y == 0)
		{
			return false;
		}
		GizmosHelper.DebugDrawBoxLB(room.Geometry.CalcWorldPosition(groundPosition, Vector2.zero), DolocTransform.TILE_WORLD_SIZE, Color.magenta);
		for (int i = 1; i < widthThreshold; i++)
		{
			groundPosition.x += dir;
			GizmosHelper.DebugDrawBoxLB(room.Geometry.CalcWorldPosition(groundPosition, Vector2.zero), DolocTransform.TILE_WORLD_SIZE, Color.magenta);
			if (!room.Geometry.IsOnGround(groundPosition))
			{
				return false;
			}
		}
		return true;
	}

	private bool FindNearestGroundPositionBase(Room room, Vector2 positionWS, out Vector2 nearestPositionWS)
	{
		nearestPositionWS = default(Vector2);
		Vector2Int vector2Int = room.Geometry.CalcCellPosition(positionWS);
		if (!room.Geometry.IsOnGround(vector2Int))
		{
			return false;
		}
		for (int i = 0; i < 2; i++)
		{
			Vector2Int vector2Int2 = vector2Int + new Vector2Int(i, 0);
			if (HasEnoughSpace(vector2Int2, 1))
			{
				nearestPositionWS = room.Geometry.CalcWorldPosition(vector2Int2, Vector2.right);
				return true;
			}
			if (HasEnoughSpace(vector2Int2, -1))
			{
				nearestPositionWS = room.Geometry.CalcWorldPosition(vector2Int2, Vector2.zero);
				return true;
			}
		}
		return false;
		bool HasEnoughSpace(Vector2Int cellpos, int dir)
		{
			if (room.Geometry.IsOnGround(cellpos))
			{
				return room.Geometry.IsOnGround(cellpos + new Vector2Int(dir, 0));
			}
			return false;
		}
	}

	private bool FindNearestGroundPositionForce(Room room, Vector2 positionWS, out Vector2 nearestPositionWS)
	{
		nearestPositionWS = default(Vector2);
		Vector2Int pos = room.Geometry.CalcCellPosition(positionWS);
		if (!room.Geometry.TryGetNearestGroundPosition(pos, out var targetPosition))
		{
			return false;
		}
		nearestPositionWS = room.Geometry.CalcWorldPosition(targetPosition, Vector2.right * 0.5f);
		return true;
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		OpenBox();
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		OpenBox();
	}

	private void OpenBox()
	{
		Use();
		DolocAPI.Broadcast(OperationEventType.USE_ITEM);
	}

	private void Use()
	{
		foreach (ItemBox item in GetAllBoxInBackpack())
		{
			item.BackUpContent();
		}
		DolocAPI.EnterUI((BoxUiState state) => state.HandleStartUpArgs(this, delegate
		{
			foreach (ItemBox item2 in GetAllBoxInBackpack())
			{
				item2.CheckContent();
			}
			backpack.InvokeAll();
		}));
	}

	public bool ReduceDurability()
	{
		if (currentDurability == 0)
		{
			return false;
		}
		currentDurability--;
		return true;
	}

	public override string GetExtraInfo1()
	{
		string arg = ((currentDurability > 1) ? "ffffff" : "ff0000");
		int num = Mathf.Max(0, currentDurability);
		return DolocUtils.Format(DolocConfig.StaticTexts.ItemBoxValueFormat, arg, num, maxDurability);
	}

	public override string GetDetailInfo()
	{
		return DolocUtils.Format(DolocConfig.StaticTexts.ItemMaxDurability, maxDurability.ToString().Colored(DolocUiColor.TEXTCOLOR_STD));
	}

	public override bool IsSame(Item other)
	{
		if (!base.IsSame(other))
		{
			return false;
		}
		if (!(other is ItemBox itemBox))
		{
			return false;
		}
		bool flag = currentDurability != maxDurability;
		bool flag2 = itemBox.currentDurability != itemBox.maxDurability;
		if (inventory.isEmpty && itemBox.inventory.isEmpty)
		{
			return flag == flag2;
		}
		return false;
	}

	protected override bool CanBuyback()
	{
		if (base.CanBuyback())
		{
			return currentDurability == maxDurability;
		}
		return false;
	}

	public override Item Clone(int count)
	{
		return new ItemBox(name, count, inventory.Copy(), currentDurability, skinIndex);
	}

	public bool ContentFilter(Item item)
	{
		return ContentFilter(item, ignoreDurability: true);
	}

	public bool ContentFilter(Item item, bool ignoreDurability)
	{
		if (!ignoreDurability && currentDurability <= 0)
		{
			return false;
		}
		return DolocAPI.IsItemCanPutInToContainer(item);
	}

	public void SetSkinIndex(int index)
	{
		skinIndex = Mathf.Clamp(index, 0, func.SkinCount - 1);
	}

	public List<ItemBox> GetAllBoxInBackpack()
	{
		return backpack.ReadAll().OfType<ItemBox>().ToList();
	}

	private void BackUpContent()
	{
		boxItemsBackup.Clear();
		for (int i = 0; i < inventory.capacity; i++)
		{
			Item item = inventory.Read(i);
			if (item != null)
			{
				boxItemsBackup.TryAdd(item.name, 0);
				boxItemsBackup[item.name] += item.count;
			}
		}
	}

	private void CheckContent()
	{
		if (isBoxUsedUp)
		{
			OnBoxUsedUp();
			return;
		}
		if (CheckBoxContentChanged())
		{
			ReduceDurability();
		}
		if (isBoxUsedUp)
		{
			OnBoxUsedUp();
		}
		else
		{
			inventory.Invoke(this);
		}
	}

	private bool CheckBoxContentChanged()
	{
		for (int i = 0; i < inventory.capacity; i++)
		{
			Item item = inventory.Read(i);
			if (item != null)
			{
				if (!boxItemsBackup.ContainsKey(item.name) || boxItemsBackup[item.name] < item.count)
				{
					return true;
				}
				if (boxItemsBackup[item.name] == item.count)
				{
					boxItemsBackup.Remove(item.name);
				}
				else
				{
					boxItemsBackup[item.name] -= item.count;
				}
			}
		}
		return boxItemsBackup.Count != 0;
	}

	private void OnBoxUsedUp()
	{
		CostSelf();
		DolocAPI.ShowMessageBoxSmall(DolocConfig.StaticTexts.BoxPanelBroken);
		DolocAPI.Delay(0.2f, delegate
		{
			Vector3 agentPosition = DolocAPI.AgentPosition;
			DolocAPI.RaiseSpriteFadeUp(agentPosition, uiSprite);
			DolocAPI.effectProvider.RaiseInstPS(new Vector2(agentPosition.x, agentPosition.y + 0.5f), InstantParticleEffectsType.CRATE_CRACK_SMALL);
			CountItem itemAfterBreak = func.ItemAfterBreak;
			DolocAPI.GenerateDropItems(DolocAPI.CurrentRoom, itemAfterBreak.itemName, agentPosition, itemAfterBreak.itemCount);
		});
	}

	public void Repair()
	{
		currentDurability = Mathf.Min(currentDurability + func.RepairValue, maxDurability);
	}
}
