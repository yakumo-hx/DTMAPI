using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Archives;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Fishing;

public sealed class FishDocumentInfo : BeanBase
{
	public readonly Dictionary<string, DocumentNodeInfo> DocumentInfos_Index = new Dictionary<string, DocumentNodeInfo>();

	public const int __ID__ = 1870576133;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public string Habitat { get; private set; }

	public string Habitat_l10n_key { get; }

	public DocumentNodeInfo[] DocumentInfos { get; private set; }

	public bool Display { get; private set; }

	public FishDocumentInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
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

	public FishDocumentInfo(string id, string habitat, DocumentNodeInfo[] document_infos, bool display)
	{
		Id = id;
		Habitat = habitat;
		DocumentInfos = document_infos;
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo in documentInfos)
		{
			DocumentInfos_Index.Add(documentNodeInfo.Id, documentNodeInfo);
		}
		Display = display;
	}

	public static FishDocumentInfo DeserializeFishDocumentInfo(JSONNode _json)
	{
		return new FishDocumentInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1870576133;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		for (int i = 0; i < documentInfos.Length; i++)
		{
			documentInfos[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Habitat = translator(Habitat_l10n_key, Habitat);
		DocumentNodeInfo[] documentInfos = DocumentInfos;
		for (int i = 0; i < documentInfos.Length; i++)
		{
			documentInfos[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Habitat:" + Habitat + ",DocumentInfos:" + StringUtil.CollectionToString(DocumentInfos) + ",Display:" + Display + ",}";
	}
}
