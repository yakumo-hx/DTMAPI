using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using DolocTown.GameData;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class FactionMissionInfo : BeanBase
{
	public const int __ID__ = -997843840;

	public string Id { get; private set; }

	public FactionType MainSeries { get; private set; }

	public FactionMissionType SubSeries { get; private set; }

	public int OrderInType { get; private set; }

	public bool DefaultUnlock { get; private set; }

	public bool UnlockBeforeSettled { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Sender { get; private set; }

	public string Sender_l10n_key { get; }

	public string Description { get; private set; }

	public string Description_l10n_key { get; }

	public int ReputationValue { get; private set; }

	public List<CountItem> RequiredItems { get; private set; }

	public int RequiredMoney { get; private set; }

	public List<RewardProto> ExtraRewards { get; private set; }

	public FactionMissionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["main_series"].IsNumber)
		{
			throw new SerializationException();
		}
		MainSeries = (FactionType)_json["main_series"].AsInt;
		if (!_json["sub_series"].IsNumber)
		{
			throw new SerializationException();
		}
		SubSeries = (FactionMissionType)_json["sub_series"].AsInt;
		if (!_json["order_in_type"].IsNumber)
		{
			throw new SerializationException();
		}
		OrderInType = _json["order_in_type"];
		if (!_json["default_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultUnlock = _json["default_unlock"];
		if (!_json["unlock_before_settled"].IsBoolean)
		{
			throw new SerializationException();
		}
		UnlockBeforeSettled = _json["unlock_before_settled"];
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
		if (!_json["description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Description_l10n_key = _json["description"]["key"];
		if (!_json["description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Description = _json["description"]["text"];
		if (!_json["reputation_value"].IsNumber)
		{
			throw new SerializationException();
		}
		ReputationValue = _json["reputation_value"];
		JSONNode jSONNode = _json["required_items"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		RequiredItems = new List<CountItem>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			CountItem item = ExternalTypeUtil.CountItemConverter(CfgCountItem.DeserializeCfgCountItem(child));
			RequiredItems.Add(item);
		}
		if (!_json["required_money"].IsNumber)
		{
			throw new SerializationException();
		}
		RequiredMoney = _json["required_money"];
		JSONNode jSONNode2 = _json["extra_rewards"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		ExtraRewards = new List<RewardProto>(jSONNode2.Count);
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			RewardProto item2 = ExternalTypeUtil.RewardProtoConverter(CfgRewardProto.DeserializeCfgRewardProto(child2));
			ExtraRewards.Add(item2);
		}
	}

	public FactionMissionInfo(string id, FactionType main_series, FactionMissionType sub_series, int order_in_type, bool default_unlock, bool unlock_before_settled, string title, string sender, string description, int reputation_value, List<CountItem> required_items, int required_money, List<RewardProto> extra_rewards)
	{
		Id = id;
		MainSeries = main_series;
		SubSeries = sub_series;
		OrderInType = order_in_type;
		DefaultUnlock = default_unlock;
		UnlockBeforeSettled = unlock_before_settled;
		Title = title;
		Sender = sender;
		Description = description;
		ReputationValue = reputation_value;
		RequiredItems = required_items;
		RequiredMoney = required_money;
		ExtraRewards = extra_rewards;
	}

	public static FactionMissionInfo DeserializeFactionMissionInfo(JSONNode _json)
	{
		return new FactionMissionInfo(_json);
	}

	public override int GetTypeId()
	{
		return -997843840;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Sender = translator(Sender_l10n_key, Sender);
		Description = translator(Description_l10n_key, Description);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",MainSeries:" + MainSeries.ToString() + ",SubSeries:" + SubSeries.ToString() + ",OrderInType:" + OrderInType + ",DefaultUnlock:" + DefaultUnlock + ",UnlockBeforeSettled:" + UnlockBeforeSettled + ",Title:" + Title + ",Sender:" + Sender + ",Description:" + Description + ",ReputationValue:" + ReputationValue + ",RequiredItems:" + StringUtil.CollectionToString(RequiredItems) + ",RequiredMoney:" + RequiredMoney + ",ExtraRewards:" + StringUtil.CollectionToString(ExtraRewards) + ",}";
	}
}
