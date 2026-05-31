using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Archives;

public sealed class ChipDocumentInfo : BeanBase
{
	public const int __ID__ = 967150386;

	public string Id { get; private set; }

	public int PoolId { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Author { get; private set; }

	public string Author_l10n_key { get; }

	public string Content { get; private set; }

	public string Content_l10n_key { get; }

	public ChipDocumentInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["pool_id"].IsNumber)
		{
			throw new SerializationException();
		}
		PoolId = _json["pool_id"];
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
		if (!_json["author"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Author_l10n_key = _json["author"]["key"];
		if (!_json["author"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Author = _json["author"]["text"];
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
	}

	public ChipDocumentInfo(string id, int pool_id, string title, string author, string content)
	{
		Id = id;
		PoolId = pool_id;
		Title = title;
		Author = author;
		Content = content;
	}

	public static ChipDocumentInfo DeserializeChipDocumentInfo(JSONNode _json)
	{
		return new ChipDocumentInfo(_json);
	}

	public override int GetTypeId()
	{
		return 967150386;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Author = translator(Author_l10n_key, Author);
		Content = translator(Content_l10n_key, Content);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",PoolId:" + PoolId + ",Title:" + Title + ",Author:" + Author + ",Content:" + Content + ",}";
	}
}
