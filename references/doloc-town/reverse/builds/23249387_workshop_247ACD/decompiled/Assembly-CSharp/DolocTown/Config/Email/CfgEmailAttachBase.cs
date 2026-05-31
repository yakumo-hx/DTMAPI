using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Email;

public abstract class CfgEmailAttachBase : BeanBase
{
	public CfgEmailAttachBase(JSONNode _json)
	{
	}

	public CfgEmailAttachBase()
	{
	}

	public static CfgEmailAttachBase DeserializeCfgEmailAttachBase(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"CfgEmailAttachNone" => new CfgEmailAttachNone(_json), 
			"CfgEmailAttachMission" => new CfgEmailAttachMission(_json), 
			"CfgEmailAttachReward" => new CfgEmailAttachReward(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public virtual void Resolve(Dictionary<string, object> _tables)
	{
	}

	public virtual void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ }";
	}
}
