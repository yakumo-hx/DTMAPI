using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DolocTown.Config.Equipment;
using DolocTown.Config.Plant;
using UnityEngine;

namespace DolocTown;

public static class EquipmentUtils
{
	private static readonly Dictionary<string, Type> TypeLut = LoadEquipmentTypes();

	public static Dictionary<string, Type> LoadEquipmentTypes()
	{
		Type baseClass = typeof(Equipment);
		IEnumerable<Type> enumerable = from x in Assembly.GetExecutingAssembly().GetTypes()
			where x.IsSubclassOf(baseClass)
			where !x.IsAbstract
			select x;
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		foreach (Type item in enumerable)
		{
			dictionary.Add("EquipmentFunc" + item.Name, item);
		}
		return dictionary;
	}

	public static Type GetEquipmentType(Equipment equipment)
	{
		string text = equipment.proto.Function.GetType().Name.Replace("EquipmentFunc", string.Empty);
		return Type.GetType("DolocTown." + text);
	}

	public static Vector2Int[] CoveredPositions(this EquipmentInfo proto, Vector2Int anchor)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < proto.CoverSize.x; i++)
		{
			for (int j = 0; j < proto.CoverSize.y; j++)
			{
				list.Add(new Vector2Int(i, j) + anchor);
			}
		}
		return list.ToArray();
	}

	public static void GenerateCropOutput(this Equipment equipment, SeedInfo proto, bool putInBackpack, bool sendMessage)
	{
		RangedItem[] cropOutputs = proto.CropOutputs;
		for (int i = 0; i < cropOutputs.Length; i++)
		{
			RangedItem rangedItem = cropOutputs[i];
			for (int j = 0; j < rangedItem.randomCount; j++)
			{
				equipment.PlaceItemInBagOrCreateDropItem(rangedItem.itemName, putInBackpack, sendMessage);
			}
		}
	}

	public static Vector2Int[] GetAffectedPositionsExcludeSelf(this Equipment equipment, int HorizontalRange, int VerticalRangeTop, int VerticalRangeBottom)
	{
		Vector2Int[] affectedPositions = equipment.GetAffectedPositions(HorizontalRange, VerticalRangeTop, VerticalRangeBottom);
		Vector2Int[] coveredPositions = equipment.CoveredPositions;
		return affectedPositions.Except(coveredPositions).ToArray();
	}

	public static Vector2Int[] GetAffectedPositions(this Equipment equipment, int HorizontalRange, int VerticalRangeTop, int VerticalRangeBottom)
	{
		Vector2Int anchor = equipment.Anchor;
		Vector2Int coveredSize = equipment.CoveredSize;
		int num = anchor.x - HorizontalRange;
		int num2 = anchor.y - VerticalRangeBottom;
		int num3 = HorizontalRange * 2 + coveredSize.x;
		int num4 = VerticalRangeBottom + VerticalRangeTop + coveredSize.y;
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < num3; i++)
		{
			for (int j = 0; j < num4; j++)
			{
				list.Add(new Vector2Int(num + i, num2 + j));
			}
		}
		return list.ToArray();
	}

	public static bool CheckProtoInstance(string type)
	{
		return TypeLut.ContainsKey(type);
	}
}
