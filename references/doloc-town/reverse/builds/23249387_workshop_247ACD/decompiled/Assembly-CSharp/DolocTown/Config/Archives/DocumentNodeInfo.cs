using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Archives;

public sealed class DocumentNodeInfo : BeanBase
{
	public const int __ID__ = -1378766744;

	public string Id { get; private set; }

	public DocumentType DocumentType { get; private set; }

	public int Value { get; private set; }

	public string DescriptionAppend { get; private set; }

	public string DescriptionAppend_l10n_key { get; }

	public DocumentNodeInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["document_type"].IsNumber)
		{
			throw new SerializationException();
		}
		DocumentType = (DocumentType)_json["document_type"].AsInt;
		if (!_json["value"].IsNumber)
		{
			throw new SerializationException();
		}
		Value = _json["value"];
		if (!_json["description_append"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DescriptionAppend_l10n_key = _json["description_append"]["key"];
		if (!_json["description_append"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DescriptionAppend = _json["description_append"]["text"];
	}

	public DocumentNodeInfo(string id, DocumentType document_type, int value, string description_append)
	{
		Id = id;
		DocumentType = document_type;
		Value = value;
		DescriptionAppend = description_append;
	}

	public static DocumentNodeInfo DeserializeDocumentNodeInfo(JSONNode _json)
	{
		return new DocumentNodeInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1378766744;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		DescriptionAppend = translator(DescriptionAppend_l10n_key, DescriptionAppend);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",DocumentType:" + DocumentType.ToString() + ",Value:" + Value + ",DescriptionAppend:" + DescriptionAppend + ",}";
	}
}
