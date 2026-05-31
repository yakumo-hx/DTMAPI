using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Archives;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Animal;

public sealed class AnimalDocumentInfo : BeanBase
{
	public readonly Dictionary<string, DocumentNodeInfo> DocumentInfos_Index = new Dictionary<string, DocumentNodeInfo>();

	public const int __ID__ = 1084191159;

	public string Id { get; private set; }

	public AnimalInfo Id_Ref { get; private set; }

	public SpriteAsset UiSpriteAsset { get; private set; }

	public SpriteAsset UiAdultSpriteAsset { get; private set; }

	public SpriteAsset InfancyAsset { get; private set; }

	public SpriteAsset AdultAsset { get; private set; }

	public DocumentNodeInfo[] DocumentInfos { get; private set; }

	public bool Display { get; private set; }

	public AnimalDocumentInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["ui_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		UiSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_sprite_asset"]));
		if (!_json["ui_adult_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		UiAdultSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_adult_sprite_asset"]));
		if (!_json["infancy_asset"].IsObject)
		{
			throw new SerializationException();
		}
		InfancyAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["infancy_asset"]));
		if (!_json["adult_asset"].IsObject)
		{
			throw new SerializationException();
		}
		AdultAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["adult_asset"]));
		JSONNode jSONNode = _json["document_infos"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		DocumentInfos = new DocumentNodeInfo[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			DocumentNodeInfo documentNodeInfo = DocumentNodeInfo.DeserializeDocumentNodeInfo(child);
			DocumentInfos[num++] = documentNodeInfo;
		}
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo2 in documentInfos)
		{
			DocumentInfos_Index.Add(documentNodeInfo2.Id, documentNodeInfo2);
		}
		if (!_json["display"].IsBoolean)
		{
			throw new SerializationException();
		}
		Display = _json["display"];
	}

	public AnimalDocumentInfo(string id, SpriteAsset ui_sprite_asset, SpriteAsset ui_adult_sprite_asset, SpriteAsset infancy_asset, SpriteAsset adult_asset, DocumentNodeInfo[] document_infos, bool display)
	{
		Id = id;
		UiSpriteAsset = ui_sprite_asset;
		UiAdultSpriteAsset = ui_adult_sprite_asset;
		InfancyAsset = infancy_asset;
		AdultAsset = adult_asset;
		DocumentInfos = document_infos;
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo in documentInfos)
		{
			DocumentInfos_Index.Add(documentNodeInfo.Id, documentNodeInfo);
		}
		Display = display;
	}

	public static AnimalDocumentInfo DeserializeAnimalDocumentInfo(JSONNode _json)
	{
		return new AnimalDocumentInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1084191159;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Animal.TbAnimal"] as TbAnimal).GetOrDefault(Id);
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		for (int i = 0; i < documentInfos.Length; i++)
		{
			documentInfos[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		for (int i = 0; i < documentInfos.Length; i++)
		{
			documentInfos[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",UiSpriteAsset:" + UiSpriteAsset?.ToString() + ",UiAdultSpriteAsset:" + UiAdultSpriteAsset?.ToString() + ",InfancyAsset:" + InfancyAsset?.ToString() + ",AdultAsset:" + AdultAsset?.ToString() + ",DocumentInfos:" + StringUtil.CollectionToString(DocumentInfos) + ",Display:" + Display + ",}";
	}
}
