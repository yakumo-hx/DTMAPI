using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Mission;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class CfgRewardProto : BeanBase
{
	public const int __ID__ = -1860454096;

	public RewardType RewardType { get; private set; }

	public string TargetId { get; private set; }

	public int TargetCount { get; private set; }

	public CfgRewardProto(JSONNode _json)
	{
		if (!_json["reward_type"].IsNumber)
		{
			throw new SerializationException();
		}
		RewardType = (RewardType)_json["reward_type"].AsInt;
		if (!_json["target_id"].IsString)
		{
			throw new SerializationException();
		}
		TargetId = _json["target_id"];
		if (!_json["target_count"].IsNumber)
		{
			throw new SerializationException();
		}
		TargetCount = _json["target_count"];
	}

	public CfgRewardProto(RewardType reward_type, string target_id, int target_count)
	{
		RewardType = reward_type;
		TargetId = target_id;
		TargetCount = target_count;
	}

	public static CfgRewardProto DeserializeCfgRewardProto(JSONNode _json)
	{
		return new CfgRewardProto(_json);
	}

	public override int GetTypeId()
	{
		return -1860454096;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ RewardType:" + RewardType.ToString() + ",TargetId:" + TargetId + ",TargetCount:" + TargetCount + ",}";
	}
}
