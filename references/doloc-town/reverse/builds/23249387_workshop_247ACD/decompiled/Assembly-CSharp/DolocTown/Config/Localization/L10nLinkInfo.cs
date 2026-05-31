using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class L10nLinkInfo : BeanBase
{
	public const int __ID__ = -2068145056;

	public string LinkId { get; private set; }

	public string LanguageId { get; private set; }

	public LocalizationInfo LanguageId_Ref { get; private set; }

	public string Url { get; private set; }

	public L10nLinkInfo(JSONNode _json)
	{
		if (!_json["link_id"].IsString)
		{
			throw new SerializationException();
		}
		LinkId = _json["link_id"];
		if (!_json["language_id"].IsString)
		{
			throw new SerializationException();
		}
		LanguageId = _json["language_id"];
		if (!_json["url"].IsString)
		{
			throw new SerializationException();
		}
		Url = _json["url"];
	}

	public L10nLinkInfo(string link_id, string language_id, string url)
	{
		LinkId = link_id;
		LanguageId = language_id;
		Url = url;
	}

	public static L10nLinkInfo DeserializeL10nLinkInfo(JSONNode _json)
	{
		return new L10nLinkInfo(_json);
	}

	public override int GetTypeId()
	{
		return -2068145056;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		LanguageId_Ref = (_tables["Localization.TbLocalization"] as TbLocalization).GetOrDefault(LanguageId);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ LinkId:" + LinkId + ",LanguageId:" + LanguageId + ",Url:" + Url + ",}";
	}
}
