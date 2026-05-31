using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Archives;
using DolocTown.Config.Asset;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Monster;

public sealed class MonsterDocumentInfo : BeanBase
{
	public readonly Dictionary<string, DocumentNodeInfo> DocumentInfos_Index = new Dictionary<string, DocumentNodeInfo>();

	public const int __ID__ = -910142193;

	public string Id { get; private set; }

	public MonsterInfo Id_Ref { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public MonsterType MonsterType { get; private set; }

	public SpriteAsset UiSpriteAsset { get; private set; }

	public SpriteAsset SpriteAsset { get; private set; }

	public string Habitat { get; private set; }

	public string Habitat_l10n_key { get; }

	public string[] SpecialDrop { get; private set; }

	public ItemInfo[] SpecialDrop_Ref { get; private set; }

	public DocumentNodeInfo[] DocumentInfos { get; private set; }

	public bool DefaultUnlock { get; private set; }

	public bool Display { get; private set; }

	public MonsterDocumentInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		if (!_json["monster_type"].IsNumber)
		{
			throw new SerializationException();
		}
		MonsterType = (MonsterType)_json["monster_type"].AsInt;
		if (!_json["ui_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		UiSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_sprite_asset"]));
		if (!_json["sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["sprite_asset"]));
		if (!_json["habitat"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Habitat_l10n_key = _json["habitat"]["key"];
		if (!_json["habitat"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Habitat = _json["habitat"]["text"];
		JSONNode jSONNode = _json["special_drop"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		SpecialDrop = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			SpecialDrop[num++] = text;
		}
		JSONNode jSONNode2 = _json["document_infos"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		DocumentInfos = new DocumentNodeInfo[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			DocumentNodeInfo documentNodeInfo = DocumentNodeInfo.DeserializeDocumentNodeInfo(child2);
			DocumentInfos[num2++] = documentNodeInfo;
		}
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo2 in documentInfos)
		{
			DocumentInfos_Index.Add(documentNodeInfo2.Id, documentNodeInfo2);
		}
		if (!_json["default_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultUnlock = _json["default_unlock"];
		if (!_json["display"].IsBoolean)
		{
			throw new SerializationException();
		}
		Display = _json["display"];
	}

	public MonsterDocumentInfo(string id, string title, MonsterType monster_type, SpriteAsset ui_sprite_asset, SpriteAsset sprite_asset, string habitat, string[] special_drop, DocumentNodeInfo[] document_infos, bool default_unlock, bool display)
	{
		Id = id;
		Title = title;
		MonsterType = monster_type;
		UiSpriteAsset = ui_sprite_asset;
		SpriteAsset = sprite_asset;
		Habitat = habitat;
		SpecialDrop = special_drop;
		DocumentInfos = document_infos;
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo in documentInfos)
		{
			DocumentInfos_Index.Add(documentNodeInfo.Id, documentNodeInfo);
		}
		DefaultUnlock = default_unlock;
		Display = display;
	}

	public static MonsterDocumentInfo DeserializeMonsterDocumentInfo(JSONNode _json)
	{
		return new MonsterDocumentInfo(_json);
	}

	public override int GetTypeId()
	{
		return -910142193;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Monster.TbMonster"] as TbMonster).GetOrDefault(Id);
		int num = SpecialDrop.Length;
		TbItem tbItem = (TbItem)_tables["Item.TbItem"];
		SpecialDrop_Ref = new ItemInfo[num];
		for (int i = 0; i < num; i++)
		{
			SpecialDrop_Ref[i] = tbItem.GetOrDefault(SpecialDrop[i]);
		}
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		for (int j = 0; j < documentInfos.Length; j++)
		{
			documentInfos[j]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Habitat = translator(Habitat_l10n_key, Habitat);
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		for (int i = 0; i < documentInfos.Length; i++)
		{
			documentInfos[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",MonsterType:" + MonsterType.ToString() + ",UiSpriteAsset:" + UiSpriteAsset?.ToString() + ",SpriteAsset:" + SpriteAsset?.ToString() + ",Habitat:" + Habitat + ",SpecialDrop:" + StringUtil.CollectionToString(SpecialDrop) + ",DocumentInfos:" + StringUtil.CollectionToString(DocumentInfos) + ",DefaultUnlock:" + DefaultUnlock + ",Display:" + Display + ",}";
	}
}
