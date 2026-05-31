using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using DolocTown.GameData;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class RewardPoolInfo : BeanBase
{
	public const int __ID__ = 199384091;

	public string Id { get; private set; }

	public string Level { get; private set; }

	public RewardProto[] Rewards { get; private set; }

	public RewardPoolInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["level"].IsString)
		{
			throw new SerializationException();
		}
		Level = _json["level"];
		JSONNode jSONNode = _json["rewards"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Rewards = new RewardProto[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			RewardProto rewardProto = ExternalTypeUtil.RewardProtoConverter(CfgRewardProto.DeserializeCfgRewardProto(child));
			Rewards[num++] = rewardProto;
		}
	}

	public RewardPoolInfo(string id, string level, RewardProto[] rewards)
	{
		Id = id;
		Level = level;
		Rewards = rewards;
	}

	public static RewardPoolInfo DeserializeRewardPoolInfo(JSONNode _json)
	{
		return new RewardPoolInfo(_json);
	}

	public override int GetTypeId()
	{
		return 199384091;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Level:" + Level + ",Rewards:" + StringUtil.CollectionToString(Rewards) + ",}";
	}
}
