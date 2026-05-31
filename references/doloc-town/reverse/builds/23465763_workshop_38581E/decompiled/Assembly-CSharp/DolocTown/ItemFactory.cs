using System;
using System.Collections.Generic;
using System.Reflection;
using DolocTown.Config.Item;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class ItemFactory
{
	private static readonly Dictionary<string, Type> InstanceTypes = ReflectionUtils.LoadIntoDict(ReflectionUtils.FindTypesWithAttributeInExecutingAsm<ItemAttribute>(BindingFlags.Public | BindingFlags.NonPublic));

	public static IEnumerable<string> ItemTypes => InstanceTypes.Keys;

	public static string GetProtoInstanceName(ItemInfo proto)
	{
		if (proto == null)
		{
			return "Item";
		}
		return proto.Function.GetType().Name.Replace("Function", "");
	}

	public static bool ValidateItem(Item item, out Item validItem)
	{
		if (item?.proto == null)
		{
			validItem = null;
			return false;
		}
		if (item.GetType().Name != GetProtoInstanceName(item.proto))
		{
			validItem = DolocAPI.GenerateItem(item.proto, item.count);
			if (validItem.invalid)
			{
				validItem = null;
			}
			return false;
		}
		validItem = item;
		return true;
	}

	public static Item GenerateItem(ItemInfo proto, int count)
	{
		if (proto == null)
		{
			return null;
		}
		count = Mathf.Clamp(count, 1, proto.Overlay);
		if (InstanceTypes.TryGetValue(GetProtoInstanceName(proto), out var value))
		{
			return (Item)Activator.CreateInstance(value, proto, count);
		}
		return new Item(proto, count);
	}

	public static bool GenerateItem(string name, int count, out Item item)
	{
		item = null;
		if (DolocAPI.QueryItemProto(name, out var proto))
		{
			item = GenerateItem(proto, count);
			return item != null;
		}
		return false;
	}

	public static bool CheckProtoInstance(string type)
	{
		string key = type.Replace("Function", "");
		return InstanceTypes.ContainsKey(key);
	}
}
