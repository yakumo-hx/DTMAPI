using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.NPC;

public sealed class NpcLikingInfo : BeanBase
{
	public const int __ID__ = -22935914;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public Dictionary<string, int> ItemMap { get; private set; }

	public NpcLikingInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["item_map"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		ItemMap = new Dictionary<string, int>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child[0].IsString)
			{
				throw new SerializationException();
			}
			string key = child[0];
			if (!child[1].IsNumber)
			{
				throw new SerializationException();
			}
			int value = child[1];
			ItemMap.Add(key, value);
		}
	}

	public NpcLikingInfo(string id, Dictionary<string, int> item_map)
	{
		Id = id;
		ItemMap = item_map;
	}

	public static NpcLikingInfo DeserializeNpcLikingInfo(JSONNode _json)
	{
		return new NpcLikingInfo(_json);
	}

	public override int GetTypeId()
	{
		return -22935914;
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
		return "{ Id:" + Id + ",ItemMap:" + StringUtil.CollectionToString(ItemMap) + ",}";
	}
}
