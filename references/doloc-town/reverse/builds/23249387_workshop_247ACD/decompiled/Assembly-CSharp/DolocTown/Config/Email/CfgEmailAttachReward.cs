using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Item;
using DolocTown.GameData;
using SimpleJSON;

namespace DolocTown.Config.Email;

public sealed class CfgEmailAttachReward : CfgEmailAttachBase
{
	public const int __ID__ = -42858786;

	public RewardProto RewardProto { get; private set; }

	public CfgEmailAttachReward(JSONNode _json)
		: base(_json)
	{
		if (!_json["reward_proto"].IsObject)
		{
			throw new SerializationException();
		}
		RewardProto = ExternalTypeUtil.RewardProtoConverter(CfgRewardProto.DeserializeCfgRewardProto(_json["reward_proto"]));
	}

	public CfgEmailAttachReward(RewardProto reward_proto)
	{
		RewardProto = reward_proto;
	}

	public static CfgEmailAttachReward DeserializeCfgEmailAttachReward(JSONNode _json)
	{
		return new CfgEmailAttachReward(_json);
	}

	public override int GetTypeId()
	{
		return -42858786;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ RewardProto:" + RewardProto.ToString() + ",}";
	}
}
