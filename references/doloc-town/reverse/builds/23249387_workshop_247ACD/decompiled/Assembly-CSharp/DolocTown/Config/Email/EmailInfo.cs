using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Email;

public sealed class EmailInfo : BeanBase
{
	public const int __ID__ = -1768777128;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Content { get; private set; }

	public string Content_l10n_key { get; }

	public string Sender { get; private set; }

	public string Sender_l10n_key { get; }

	public string IconUrl { get; private set; }

	public CfgEmailAttachBase[] Attaches { get; private set; }

	public EmailInfo(JSONNode _json)
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
		if (!_json["content"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Content_l10n_key = _json["content"]["key"];
		if (!_json["content"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Content = _json["content"]["text"];
		if (!_json["sender"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Sender_l10n_key = _json["sender"]["key"];
		if (!_json["sender"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Sender = _json["sender"]["text"];
		if (!_json["icon_url"].IsString)
		{
			throw new SerializationException();
		}
		IconUrl = _json["icon_url"];
		JSONNode jSONNode = _json["attaches"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Attaches = new CfgEmailAttachBase[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			CfgEmailAttachBase cfgEmailAttachBase = CfgEmailAttachBase.DeserializeCfgEmailAttachBase(child);
			Attaches[num++] = cfgEmailAttachBase;
		}
	}

	public EmailInfo(string id, string title, string content, string sender, string icon_url, CfgEmailAttachBase[] attaches)
	{
		Id = id;
		Title = title;
		Content = content;
		Sender = sender;
		IconUrl = icon_url;
		Attaches = attaches;
	}

	public static EmailInfo DeserializeEmailInfo(JSONNode _json)
	{
		return new EmailInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1768777128;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		CfgEmailAttachBase[] attaches = Attaches;
		for (int i = 0; i < attaches.Length; i++)
		{
			attaches[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Content = translator(Content_l10n_key, Content);
		Sender = translator(Sender_l10n_key, Sender);
		CfgEmailAttachBase[] attaches = Attaches;
		for (int i = 0; i < attaches.Length; i++)
		{
			attaches[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",Content:" + Content + ",Sender:" + Sender + ",IconUrl:" + IconUrl + ",Attaches:" + StringUtil.CollectionToString(Attaches) + ",}";
	}
}
