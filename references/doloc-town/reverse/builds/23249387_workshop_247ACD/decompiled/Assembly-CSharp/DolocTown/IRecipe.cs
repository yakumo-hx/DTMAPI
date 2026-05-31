using UnityEngine;

namespace DolocTown;

public interface IRecipe
{
	string RecipeId { get; }

	string RecipeTitle { get; }

	CountItem[] InputItems { get; }

	int MoneyCost { get; }

	RangedItem OutputItem { get; }

	int TechPoint { get; }

	int CostTime { get; }

	int Storage { get; }

	bool isValid { get; }

	bool AffordCostInputItemsInInventory(LinearInventory[] inventories, int scale)
	{
		bool flag = true;
		CountItem[] inputItems = InputItems;
		for (int i = 0; i < inputItems.Length; i++)
		{
			CountItem countItem = inputItems[i];
			flag &= inventories.CountItem(countItem.itemName) >= countItem.itemCount * scale;
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	int MaxAffordScale(LinearInventory[] inventories, int currentMoney = 0)
	{
		int num = int.MaxValue;
		CountItem[] inputItems = InputItems;
		for (int i = 0; i < inputItems.Length; i++)
		{
			CountItem countItem = inputItems[i];
			num = Mathf.Min(inventories.CountItem(countItem.itemName) / Mathf.Max(1, countItem.itemCount), num);
		}
		if (MoneyCost > 0)
		{
			num = Mathf.Min(num, currentMoney / MoneyCost);
		}
		return num;
	}

	bool TryCostInputItemsInInventory(LinearInventory[] inventories, int scale, int startIndex = 0)
	{
		if (!AffordCostInputItemsInInventory(inventories, scale))
		{
			return false;
		}
		CountItem[] inputItems = InputItems;
		for (int i = 0; i < inputItems.Length; i++)
		{
			CountItem countItem = inputItems[i];
			inventories.MaxCostItem(countItem.itemName, countItem.itemCount * scale, startIndex);
		}
		return true;
	}

	CountItem GenerateOutputItem(int scale = 1)
	{
		int num = 0;
		for (int i = 0; i < scale; i++)
		{
			num += OutputItem.randomCount;
		}
		return new CountItem(OutputItem.itemName, num);
	}

	void GenerateOutputItemAsDropItem(int scale)
	{
		DolocAPI.QueryItemProto(OutputItem.itemName, out var proto);
		int num = 0;
		for (int i = 0; i < scale; i++)
		{
			num += OutputItem.randomCount;
		}
		int b = Mathf.Clamp(num / 10, 1, proto.Overlay);
		while (num > 0)
		{
			int num2 = Mathf.Min(num, b);
			Item item = DolocAPI.GenerateItem(OutputItem.itemName, num2);
			DolocAPI.GenerateDropItem(DolocAPI.CurrentRoom, item, DolocAPI.AgentPosition);
			num -= num2;
		}
	}
}
