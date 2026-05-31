using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Archives;

public sealed class CustomizedDocumentNodeInfo : BeanBase
{
	public const int __ID__ = 700104393;

	public string Id { get; private set; }

	public DocumentType DocumentType { get; private set; }

	public int Value { get; private set; }

	public int EnvOptimizerPoint { get; private set; }

	public string DescriptionAppend { get; private set; }

	public string DescriptionAppend_l10n_key { get; }

	public CustomizedDocumentNodeInfo(JSONNode _json)
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
		if (!_json["env_optimizer_point"].IsNumber)
		{
			throw new SerializationException();
		}
		EnvOptimizerPoint = _json["env_optimizer_point"];
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

	public CustomizedDocumentNodeInfo(string id, DocumentType document_type, int value, int env_optimizer_point, string description_append)
	{
		Id = id;
		DocumentType = document_type;
		Value = value;
		EnvOptimizerPoint = env_optimizer_point;
		DescriptionAppend = description_append;
	}

	public static CustomizedDocumentNodeInfo DeserializeCustomizedDocumentNodeInfo(JSONNode _json)
	{
		return new CustomizedDocumentNodeInfo(_json);
	}

	public override int GetTypeId()
	{
		return 700104393;
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
		return "{ Id:" + Id + ",DocumentType:" + DocumentType.ToString() + ",Value:" + Value + ",EnvOptimizerPoint:" + EnvOptimizerPoint + ",DescriptionAppend:" + DescriptionAppend + ",}";
	}
}
