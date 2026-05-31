using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Animal;

public sealed class FeedInfo : BeanBase
{
	public const int __ID__ = 246954782;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public int Energy { get; private set; }

	public FeedInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["energy"].IsNumber)
		{
			throw new SerializationException();
		}
		Energy = _json["energy"];
	}

	public FeedInfo(string id, int energy)
	{
		Id = id;
		Energy = energy;
	}

	public static FeedInfo DeserializeFeedInfo(JSONNode _json)
	{
		return new FeedInfo(_json);
	}

	public override int GetTypeId()
	{
		return 246954782;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Energy:" + Energy + ",}";
	}
}
