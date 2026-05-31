using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class RangedItemListInfo : BeanBase
{
	public const int __ID__ = 197667265;

	public string Id { get; private set; }

	public RangedItem[] Items { get; private set; }

	public RangedItemListInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["items"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Items = new RangedItem[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			RangedItem rangedItem = ExternalTypeUtil.RangedItemConverter(CfgRangedItem.DeserializeCfgRangedItem(child));
			Items[num++] = rangedItem;
		}
	}

	public RangedItemListInfo(string id, RangedItem[] items)
	{
		Id = id;
		Items = items;
	}

	public static RangedItemListInfo DeserializeRangedItemListInfo(JSONNode _json)
	{
		return new RangedItemListInfo(_json);
	}

	public override int GetTypeId()
	{
		return 197667265;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Items:" + StringUtil.CollectionToString(Items) + ",}";
	}
}
