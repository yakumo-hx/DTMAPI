using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Item;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncCase : EquipmentFuncCaseBase
{
	public const int __ID__ = -1448111326;

	public List<CaseSkin> Skins = new List<CaseSkin>();

	public string PlaceCondition { get; private set; }

	public ItemPlaceConditionInfo PlaceCondition_Ref { get; private set; }

	public Vector2Int LabelOffset { get; private set; }

	public string[] AutomateTypes { get; private set; }

	public ItemAutomationTypeInfo[] AutomateTypes_Ref { get; private set; }

	public Color[] SkinColor { get; private set; }

	public SpriteAsset[] SkinSprite { get; private set; }

	public int SkinCount => Skins.Count;

	public EquipmentFuncCase(JSONNode _json)
		: base(_json)
	{
		if (!_json["place_condition"].IsString)
		{
			throw new SerializationException();
		}
		PlaceCondition = _json["place_condition"];
		if (!_json["label_offset"].IsObject)
		{
			throw new SerializationException();
		}
		LabelOffset = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["label_offset"]));
		JSONNode jSONNode = _json["automate_types"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		AutomateTypes = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			AutomateTypes[num++] = text;
		}
		JSONNode jSONNode2 = _json["skin_color"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		SkinColor = new Color[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			Color color = ExternalTypeUtil.ColorConverter(CfgHexColor.DeserializeCfgHexColor(child2));
			SkinColor[num2++] = color;
		}
		JSONNode jSONNode3 = _json["skin_sprite"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode3.Count;
		SkinSprite = new SpriteAsset[count3];
		int num3 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			SpriteAsset spriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(child3));
			SkinSprite[num3++] = spriteAsset;
		}
	}

	public EquipmentFuncCase(int total_capacity, int line_capacity, string place_condition, Vector2Int label_offset, string[] automate_types, Color[] skin_color, SpriteAsset[] skin_sprite)
		: base(total_capacity, line_capacity)
	{
		PlaceCondition = place_condition;
		LabelOffset = label_offset;
		AutomateTypes = automate_types;
		SkinColor = skin_color;
		SkinSprite = skin_sprite;
	}

	public static EquipmentFuncCase DeserializeEquipmentFuncCase(JSONNode _json)
	{
		return new EquipmentFuncCase(_json);
	}

	public override int GetTypeId()
	{
		return -1448111326;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		PlaceCondition_Ref = (_tables["Equipment.TbItemPlaceCondition"] as TbItemPlaceCondition).GetOrDefault(PlaceCondition);
		int num = AutomateTypes.Length;
		TbItemAutomationType tbItemAutomationType = (TbItemAutomationType)_tables["Item.TbItemAutomationType"];
		AutomateTypes_Ref = new ItemAutomationTypeInfo[num];
		for (int i = 0; i < num; i++)
		{
			AutomateTypes_Ref[i] = tbItemAutomationType.GetOrDefault(AutomateTypes[i]);
		}
		PostResolve();
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ TotalCapacity:" + base.TotalCapacity + ",LineCapacity:" + base.LineCapacity + ",PlaceCondition:" + PlaceCondition + ",LabelOffset:" + LabelOffset.ToString() + ",AutomateTypes:" + StringUtil.CollectionToString(AutomateTypes) + ",SkinColor:" + StringUtil.CollectionToString(SkinColor) + ",SkinSprite:" + StringUtil.CollectionToString(SkinSprite) + ",}";
	}

	private void PostResolve()
	{
		int num = Mathf.Min(SkinColor.Length, SkinSprite.Length);
		for (int i = 0; i < num; i++)
		{
			Skins.Add(new CaseSkin(i, SkinColor[i], SkinSprite[i]));
		}
	}

	public Sprite GetSkinSprite(int skinIndex)
	{
		if (Skins.Count == 0)
		{
			return null;
		}
		int index = Mathf.Clamp(skinIndex, 0, Skins.Count - 1);
		return Skins[index].sprite.Asset;
	}
}
