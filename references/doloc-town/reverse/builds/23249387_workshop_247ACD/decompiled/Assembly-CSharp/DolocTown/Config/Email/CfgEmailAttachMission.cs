using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Email;

public sealed class CfgEmailAttachMission : CfgEmailAttachBase
{
	public const int __ID__ = -1359822915;

	public string MissionChainId { get; private set; }

	public bool AutoAccept { get; private set; }

	public CfgEmailAttachMission(JSONNode _json)
		: base(_json)
	{
		if (!_json["mission_chain_id"].IsString)
		{
			throw new SerializationException();
		}
		MissionChainId = _json["mission_chain_id"];
		if (!_json["auto_accept"].IsBoolean)
		{
			throw new SerializationException();
		}
		AutoAccept = _json["auto_accept"];
	}

	public CfgEmailAttachMission(string mission_chain_id, bool auto_accept)
	{
		MissionChainId = mission_chain_id;
		AutoAccept = auto_accept;
	}

	public static CfgEmailAttachMission DeserializeCfgEmailAttachMission(JSONNode _json)
	{
		return new CfgEmailAttachMission(_json);
	}

	public override int GetTypeId()
	{
		return -1359822915;
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
		return "{ MissionChainId:" + MissionChainId + ",AutoAccept:" + AutoAccept + ",}";
	}
}
