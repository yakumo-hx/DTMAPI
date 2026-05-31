using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionBox : ItemFunctionBase
{
	public const int __ID__ = -197380891;

	public List<BoxSkin> Skins = new List<BoxSkin>();

	public int TotalCapacity { get; private set; }

	public int LineCapacity { get; private set; }

	public int Durability { get; private set; }

	public CountItem ItemAfterBreak { get; private set; }

	public CountItem RepairItem { get; private set; }

	public int RepairValue { get; private set; }

	public Color[] SkinColor { get; private set; }

	public SpriteAsset[] SkinOpen { get; private set; }

	public SpriteAsset[] SkinClose { get; private set; }

	public int SkinCount => Skins.Count;

	public ItemFunctionBox(JSONNode _json)
		: base(_json)
	{
		if (!_json["total_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		TotalCapacity = _json["total_capacity"];
		if (!_json["line_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		LineCapacity = _json["line_capacity"];
		if (!_json["durability"].IsNumber)
		{
			throw new SerializationException();
		}
		Durability = _json["durability"];
		if (!_json["item_after_break"].IsObject)
		{
			throw new SerializationException();
		}
		ItemAfterBreak = ExternalTypeUtil.CountItemConverter(CfgCountItem.DeserializeCfgCountItem(_json["item_after_break"]));
		if (!_json["repair_item"].IsObject)
		{
			throw new SerializationException();
		}
		RepairItem = ExternalTypeUtil.CountItemConverter(CfgCountItem.DeserializeCfgCountItem(_json["repair_item"]));
		if (!_json["repair_value"].IsNumber)
		{
			throw new SerializationException();
		}
		RepairValue = _json["repair_value"];
		JSONNode jSONNode = _json["skin_color"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		SkinColor = new Color[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			Color color = ExternalTypeUtil.ColorConverter(CfgHexColor.DeserializeCfgHexColor(child));
			SkinColor[num++] = color;
		}
		JSONNode jSONNode2 = _json["skin_open"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		SkinOpen = new SpriteAsset[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			SpriteAsset spriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(child2));
			SkinOpen[num2++] = spriteAsset;
		}
		JSONNode jSONNode3 = _json["skin_close"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode3.Count;
		SkinClose = new SpriteAsset[count3];
		int num3 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			SpriteAsset spriteAsset2 = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(child3));
			SkinClose[num3++] = spriteAsset2;
		}
	}

	public ItemFunctionBox(int total_capacity, int line_capacity, int durability, CountItem item_after_break, CountItem repair_item, int repair_value, Color[] skin_color, SpriteAsset[] skin_open, SpriteAsset[] skin_close)
	{
		TotalCapacity = total_capacity;
		LineCapacity = line_capacity;
		Durability = durability;
		ItemAfterBreak = item_after_break;
		RepairItem = repair_item;
		RepairValue = repair_value;
		SkinColor = skin_color;
		SkinOpen = skin_open;
		SkinClose = skin_close;
	}

	public static ItemFunctionBox DeserializeItemFunctionBox(JSONNode _json)
	{
		return new ItemFunctionBox(_json);
	}

	public override int GetTypeId()
	{
		return -197380891;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		PostResolve();
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ TotalCapacity:" + TotalCapacity + ",LineCapacity:" + LineCapacity + ",Durability:" + Durability + ",ItemAfterBreak:" + ItemAfterBreak.ToString() + ",RepairItem:" + RepairItem.ToString() + ",RepairValue:" + RepairValue + ",SkinColor:" + StringUtil.CollectionToString(SkinColor) + ",SkinOpen:" + StringUtil.CollectionToString(SkinOpen) + ",SkinClose:" + StringUtil.CollectionToString(SkinClose) + ",}";
	}

	private void PostResolve()
	{
		int num = Mathf.Min(SkinColor.Length, SkinClose.Length, SkinOpen.Length);
		for (int i = 0; i < num; i++)
		{
			Skins.Add(new BoxSkin(i, SkinColor[i], SkinOpen[i], SkinClose[i]));
		}
	}

	public Sprite GetSkinSprite(int skinIndex, bool isEmpty)
	{
		if (Skins.Count == 0)
		{
			return null;
		}
		int index = Mathf.Clamp(skinIndex, 0, Skins.Count - 1);
		if (!isEmpty)
		{
			return Skins[index].fullSprite.Asset;
		}
		return Skins[index].emptySprite.Asset;
	}
}
