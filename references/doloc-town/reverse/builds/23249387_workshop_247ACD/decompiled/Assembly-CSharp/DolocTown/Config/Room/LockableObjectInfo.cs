using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Room;

public sealed class LockableObjectInfo : BeanBase
{
	public const int __ID__ = -661802139;

	public string Id { get; private set; }

	public string RewardInfo { get; private set; }

	public string RewardInfo_l10n_key { get; }

	public string UnlockInfo { get; private set; }

	public string UnlockInfo_l10n_key { get; }

	public SpriteAsset RewardIcon { get; private set; }

	public LockableObjectInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["reward_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfo_l10n_key = _json["reward_info"]["key"];
		if (!_json["reward_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfo = _json["reward_info"]["text"];
		if (!_json["unlock_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UnlockInfo_l10n_key = _json["unlock_info"]["key"];
		if (!_json["unlock_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UnlockInfo = _json["unlock_info"]["text"];
		if (!_json["reward_icon"].IsObject)
		{
			throw new SerializationException();
		}
		RewardIcon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["reward_icon"]));
	}

	public LockableObjectInfo(string id, string reward_info, string unlock_info, SpriteAsset reward_icon)
	{
		Id = id;
		RewardInfo = reward_info;
		UnlockInfo = unlock_info;
		RewardIcon = reward_icon;
	}

	public static LockableObjectInfo DeserializeLockableObjectInfo(JSONNode _json)
	{
		return new LockableObjectInfo(_json);
	}

	public override int GetTypeId()
	{
		return -661802139;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		RewardInfo = translator(RewardInfo_l10n_key, RewardInfo);
		UnlockInfo = translator(UnlockInfo_l10n_key, UnlockInfo);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",RewardInfo:" + RewardInfo + ",UnlockInfo:" + UnlockInfo + ",RewardIcon:" + RewardIcon?.ToString() + ",}";
	}
}
