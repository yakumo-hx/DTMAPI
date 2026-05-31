using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public struct PlantBasinSupply
{
	private static Dictionary<ItemInfo, Sprite[]> ProtectorSpriteCache = new Dictionary<ItemInfo, Sprite[]>();

	[JsonProperty]
	[JsonConverter(typeof(VectorConverter))]
	[DebugInfo("补给容量")]
	private Vector2Int capacity;

	[JsonProperty]
	[JsonConverter(typeof(VectorConverter))]
	[DebugInfo("当前补给值")]
	private Vector2Int currentValues;

	[JsonProperty]
	[DebugInfo("当前护盾值")]
	private int protectedCounter;

	[JsonProperty]
	private float currentProtectedAmountReciprocal;

	[JsonProperty]
	[DebugInfo("塑料薄膜")]
	private Item protectorItem;

	[JsonProperty]
	[DebugInfo("肥料数据")]
	private FertilizerData fertilizerData;

	public bool IsFertilized => fertilizerData.duration > 0;

	public FertilizerData FertilizerData => fertilizerData;

	public Sprite FertilizerSprite => fertilizerData.FertilizerSprite;

	private int ProtectorLv
	{
		get
		{
			float protectedPercent = ProtectedPercent;
			if (!(protectedPercent < 0.3333f))
			{
				if (protectedPercent < 0.6666f)
				{
					return 1;
				}
				return 2;
			}
			return 0;
		}
	}

	public Sprite ProtectorSprite
	{
		get
		{
			if (protectorItem == null)
			{
				return null;
			}
			if (ProtectorSpriteCache.TryGetValue(protectorItem.proto, out var value))
			{
				int num = Mathf.Clamp(ProtectorLv, 0, value.Length - 1);
				return value[num];
			}
			if (!(protectorItem.proto.Function is ItemFunctionFilm itemFunctionFilm))
			{
				return null;
			}
			value = itemFunctionFilm.LevelSprites.assets;
			ProtectorSpriteCache[protectorItem.proto] = value;
			return value[Mathf.Clamp(ProtectorLv, 0, value.Length - 1)];
		}
	}

	public int ProtectedCounter
	{
		get
		{
			return protectedCounter;
		}
		set
		{
			protectedCounter = value;
		}
	}

	public int WaterCapacity => capacity.y;

	public bool IsLighting => currentValues.x > 0;

	public readonly bool IsProtected => protectedCounter > 0;

	public readonly bool IsMoist => currentValues.y > 0;

	public readonly float ProtectedPercent => (float)protectedCounter * 0.0027777778f;

	public readonly bool IsProtectedFull => protectedCounter >= 240;

	public readonly float WaterRatio => (float)currentValues.y / (float)capacity.y;

	public readonly int WaterValue => currentValues.y;

	public PlantBasinSupply(Vector2Int capacity)
	{
		this.capacity = capacity;
		currentValues = Vector2Int.zero;
		protectedCounter = 0;
		currentProtectedAmountReciprocal = 1f;
		fertilizerData = default(FertilizerData);
		protectorItem = null;
	}

	[JsonConstructor]
	public PlantBasinSupply(Vector2Int capacity, Vector2Int currentValues, int protectedCounter, float currentProtectedAmountReciprocal, Item protectorItem = null, FertilizerData fertilizerData = default(FertilizerData))
	{
		this.capacity = capacity;
		this.currentValues = currentValues;
		this.protectedCounter = protectedCounter;
		this.protectedCounter = Mathf.Max(0, protectedCounter);
		this.currentProtectedAmountReciprocal = Mathf.Max(1f, currentProtectedAmountReciprocal);
		this.protectorItem = LoadProtectorItemInfo(protectorItem, Mathf.Max(0, protectedCounter));
		this.fertilizerData = fertilizerData;
	}

	private static Item LoadProtectorItemInfo(Item protectorItem, int counter)
	{
		if (counter <= 0)
		{
			return null;
		}
		if (protectorItem != null)
		{
			return protectorItem;
		}
		ItemInfo itemInfo = DolocConfig.Tables.TbItem.DataList.FirstOrDefault((ItemInfo x) => x.Function is ItemFunctionFilm);
		if (itemInfo == null)
		{
			return null;
		}
		return DolocAPI.GenerateItem(itemInfo);
	}

	public void CostSW()
	{
		if (currentValues.x > 0)
		{
			currentValues.x--;
		}
		if (currentValues.y > 0)
		{
			currentValues.y--;
		}
	}

	public bool GetProtected()
	{
		if (protectedCounter <= 0)
		{
			return false;
		}
		protectedCounter--;
		return true;
	}

	public bool GetMoist()
	{
		if (currentValues.y <= 0)
		{
			return false;
		}
		currentValues.y--;
		return true;
	}

	public bool GetMoist(int value)
	{
		if (currentValues.y < value)
		{
			return false;
		}
		currentValues.y -= value;
		return true;
	}

	public bool GetLighting()
	{
		if (currentValues.x <= 0)
		{
			return false;
		}
		currentValues.x--;
		return true;
	}

	public void ClearWater()
	{
		currentValues.y = 0;
	}

	public void Water()
	{
		currentValues.y = capacity.y;
	}

	public void Water(int value)
	{
		currentValues.y = Mathf.Min(capacity.y, currentValues.y + value);
	}

	public void Light()
	{
		currentValues.x = capacity.x;
	}

	public void Protect(ItemFilm item)
	{
		if (item != null)
		{
			protectorItem = item;
			protectedCounter += Mathf.Max(0, ((ItemFunctionFilm)item.proto.Function).HealingAmount);
			currentProtectedAmountReciprocal = 1f / (float)protectedCounter;
		}
	}

	public float GetFertilizerGrowthAddition()
	{
		if (fertilizerData.duration <= 0)
		{
			return 0f;
		}
		fertilizerData.duration--;
		return fertilizerData.growthAddition;
	}

	public void Fertilizer(int duration, float addition, ItemInfo fertilizerItemInfo)
	{
		fertilizerData = new FertilizerData(duration, addition, fertilizerItemInfo);
	}
}
