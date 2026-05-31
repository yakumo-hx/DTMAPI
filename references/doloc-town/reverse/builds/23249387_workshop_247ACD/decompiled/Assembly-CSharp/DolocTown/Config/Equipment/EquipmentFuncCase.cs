using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncCase : EquipmentFuncCaseBase
{
	public const int __ID__ = -1448111326;

	public List<CaseSkin> Skins = new List<CaseSkin>();

	public Color[] SkinColor { get; private set; }

	public SpriteAsset[] SkinSprite { get; private set; }

	public int SkinCount => Skins.Count;

	public EquipmentFuncCase(JSONNode _json)
		: base(_json)
	{
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
		JSONNode jSONNode2 = _json["skin_sprite"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		SkinSprite = new SpriteAsset[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			SpriteAsset spriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(child2));
			SkinSprite[num2++] = spriteAsset;
		}
	}

	public EquipmentFuncCase(int total_capacity, int line_capacity, Color[] skin_color, SpriteAsset[] skin_sprite)
		: base(total_capacity, line_capacity)
	{
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
		PostResolve();
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ TotalCapacity:" + base.TotalCapacity + ",LineCapacity:" + base.LineCapacity + ",SkinColor:" + StringUtil.CollectionToString(SkinColor) + ",SkinSprite:" + StringUtil.CollectionToString(SkinSprite) + ",}";
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
