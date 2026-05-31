using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class SeedUnlockInfo : BeanBase
{
	public const int __ID__ = 1893155206;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public CountItem[] Costs { get; private set; }

	public string EmailId { get; private set; }

	public bool DefaultUnlock { get; private set; }

	public SeedUnlockInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["costs"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Costs = new CountItem[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			CountItem countItem = ExternalTypeUtil.CountItemConverter(CfgCountItem.DeserializeCfgCountItem(child));
			Costs[num++] = countItem;
		}
		if (!_json["email_id"].IsString)
		{
			throw new SerializationException();
		}
		EmailId = _json["email_id"];
		if (!_json["default_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultUnlock = _json["default_unlock"];
	}

	public SeedUnlockInfo(string id, CountItem[] costs, string email_id, bool default_unlock)
	{
		Id = id;
		Costs = costs;
		EmailId = email_id;
		DefaultUnlock = default_unlock;
	}

	public static SeedUnlockInfo DeserializeSeedUnlockInfo(JSONNode _json)
	{
		return new SeedUnlockInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1893155206;
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
		return "{ Id:" + Id + ",Costs:" + StringUtil.CollectionToString(Costs) + ",EmailId:" + EmailId + ",DefaultUnlock:" + DefaultUnlock + ",}";
	}
}
