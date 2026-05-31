using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemBottle : Item
{
	public ItemBottle(ItemInfo item, int count)
		: base(item, count)
	{
	}

	[JsonConstructor]
	protected ItemBottle(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		Use();
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		Use();
	}

	private void Use()
	{
		if (!CollectMilkFromAnimal())
		{
			DrawWater();
		}
	}

	protected override void OnQuickSelect()
	{
		base.OnQuickSelect();
		ShowCellTip(Vector2Int.zero, Vector2Int.one, flipWhenFaceLeft: true);
	}

	protected override void OnQuickDeselect()
	{
		base.OnQuickDeselect();
		HideCellTip();
	}

	protected override void RefreshCellTip()
	{
		base.cellTip.CellTipValid = IsCellValid();
	}

	private bool IsCellValid()
	{
		if (DolocAPI.IsAgentInWater)
		{
			return true;
		}
		if (TryGetSelectedEquipment(out var equipment) && equipment is IWaterContainer)
		{
			return true;
		}
		if (DolocAPI.CurrentAnimal != null)
		{
			Animal currentAnimal = DolocAPI.CurrentAnimal;
			if (currentAnimal.proto.Id == "marsh_pangolin" && currentAnimal.NeedMetabolism)
			{
				return true;
			}
		}
		return false;
	}

	private void DrawWater()
	{
		if (DolocAPI.IsAgentInWater)
		{
			DrawWaterInWater();
		}
		else
		{
			DrawWaterInContainer();
		}
	}

	private void DrawWaterInContainer()
	{
		if (!TryGetSelectedEquipment(out var equipment))
		{
			return;
		}
		IWaterContainer container = equipment as IWaterContainer;
		if (container == null)
		{
			return;
		}
		int capacity = ((ItemFunctionBottle)base.proto.Function).Capacity;
		if (container.Water < capacity)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrRunoutWaterAround);
			return;
		}
		DolocAPI.agent._Interact(delegate
		{
			CostSelf();
			container.TakeWater(capacity);
			DolocAPI.GenerateDropItems(DolocAPI.CurrentRoom, DolocAPI.GlobalParameter.ItemRefBottleOfWater, DolocAPI.AgentPosition);
		});
	}

	private void DrawWaterInWater()
	{
		if (DolocAPI.agent.IsCurrentStateSupportInteract)
		{
			DolocAPI.agent._Interact(delegate
			{
				CostSelf();
				DolocAPI.GenerateDropItems(DolocAPI.CurrentRoom, DolocAPI.GlobalParameter.ItemRefBottleOfWater, DolocAPI.AgentPosition);
			});
		}
	}

	private bool CollectMilkFromAnimal()
	{
		Animal currentAnimal = DolocAPI.CurrentAnimal;
		if (currentAnimal == null)
		{
			return false;
		}
		if (currentAnimal.protoName != "marsh_pangolin")
		{
			return false;
		}
		CountItem[] array = currentAnimal.ProduceAsItems();
		if (array.IsNullOrEmpty())
		{
			return false;
		}
		CountItem[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			CountItem countItem = array2[i];
			if (!countItem.isValid)
			{
				continue;
			}
			if (countItem.itemName == DolocAPI.GlobalParameter.ItemRefMilk)
			{
				CostSelf();
				DolocAPI.GenerateDropItems(DolocAPI.CurrentRoom, countItem.itemName, currentAnimal.PositionDropItem);
				continue;
			}
			for (int j = 0; j < countItem.itemCount; j++)
			{
				DolocAPI.GenerateDropItems(DolocAPI.CurrentRoom, countItem.itemName, currentAnimal.PositionDropItem);
			}
		}
		return true;
	}
}
