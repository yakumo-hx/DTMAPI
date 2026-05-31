using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Archives;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Resource;

public sealed class ResourceDocumentInfo : BeanBase
{
	public readonly Dictionary<string, CustomizedDocumentNodeInfo> DocumentInfos_Index = new Dictionary<string, CustomizedDocumentNodeInfo>();

	public const int __ID__ = 1374865783;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public ResourceDocumentType ResourceType { get; private set; }

	public SpriteAsset UiSpriteAsset { get; private set; }

	public SpriteAsset SceneSpriteAsset { get; private set; }

	public bool IsPlant { get; private set; }

	public string Habitat { get; private set; }

	public string Habitat_l10n_key { get; }

	public CustomizedDocumentNodeInfo[] DocumentInfos { get; private set; }

	public bool LeftRightLayout { get; private set; }

	public bool Display { get; private set; }

	public ResourceDocumentInfo(JSONNode _json)
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
		if (!_json["resource_type"].IsNumber)
		{
			throw new SerializationException();
		}
		ResourceType = (ResourceDocumentType)_json["resource_type"].AsInt;
		if (!_json["ui_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		UiSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_sprite_asset"]));
		if (!_json["scene_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SceneSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["scene_sprite_asset"]));
		if (!_json["is_plant"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsPlant = _json["is_plant"];
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
		JSONNode jSONNode = _json["document_infos"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		DocumentInfos = new CustomizedDocumentNodeInfo[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			CustomizedDocumentNodeInfo customizedDocumentNodeInfo = CustomizedDocumentNodeInfo.DeserializeCustomizedDocumentNodeInfo(child);
			DocumentInfos[num++] = customizedDocumentNodeInfo;
		}
		CustomizedDocumentNodeInfo[] documentInfos = DocumentInfos;
		foreach (CustomizedDocumentNodeInfo customizedDocumentNodeInfo2 in documentInfos)
		{
			DocumentInfos_Index.Add(customizedDocumentNodeInfo2.Id, customizedDocumentNodeInfo2);
		}
		if (!_json["left_right_layout"].IsBoolean)
		{
			throw new SerializationException();
		}
		LeftRightLayout = _json["left_right_layout"];
		if (!_json["display"].IsBoolean)
		{
			throw new SerializationException();
		}
		Display = _json["display"];
	}

	public ResourceDocumentInfo(string id, string title, ResourceDocumentType resource_type, SpriteAsset ui_sprite_asset, SpriteAsset scene_sprite_asset, bool is_plant, string habitat, CustomizedDocumentNodeInfo[] document_infos, bool left_right_layout, bool display)
	{
		Id = id;
		Title = title;
		ResourceType = resource_type;
		UiSpriteAsset = ui_sprite_asset;
		SceneSpriteAsset = scene_sprite_asset;
		IsPlant = is_plant;
		Habitat = habitat;
		DocumentInfos = document_infos;
		CustomizedDocumentNodeInfo[] documentInfos = DocumentInfos;
		foreach (CustomizedDocumentNodeInfo customizedDocumentNodeInfo in documentInfos)
		{
			DocumentInfos_Index.Add(customizedDocumentNodeInfo.Id, customizedDocumentNodeInfo);
		}
		LeftRightLayout = left_right_layout;
		Display = display;
	}

	public static ResourceDocumentInfo DeserializeResourceDocumentInfo(JSONNode _json)
	{
		return new ResourceDocumentInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1374865783;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		CustomizedDocumentNodeInfo[] documentInfos = DocumentInfos;
		for (int i = 0; i < documentInfos.Length; i++)
		{
			documentInfos[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Habitat = translator(Habitat_l10n_key, Habitat);
		CustomizedDocumentNodeInfo[] documentInfos = DocumentInfos;
		for (int i = 0; i < documentInfos.Length; i++)
		{
			documentInfos[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",ResourceType:" + ResourceType.ToString() + ",UiSpriteAsset:" + UiSpriteAsset?.ToString() + ",SceneSpriteAsset:" + SceneSpriteAsset?.ToString() + ",IsPlant:" + IsPlant + ",Habitat:" + Habitat + ",DocumentInfos:" + StringUtil.CollectionToString(DocumentInfos) + ",LeftRightLayout:" + LeftRightLayout + ",Display:" + Display + ",}";
	}
}
