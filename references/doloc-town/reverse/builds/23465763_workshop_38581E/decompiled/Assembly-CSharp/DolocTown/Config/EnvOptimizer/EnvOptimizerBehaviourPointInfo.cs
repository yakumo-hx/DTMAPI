using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.EnvOptimizer;

public sealed class EnvOptimizerBehaviourPointInfo : BeanBase
{
	public const int __ID__ = 815719151;

	public EnvOptimizerBranchType Id { get; private set; }

	public int Point { get; private set; }

	public bool HasExtraPoint { get; private set; }

	public EnvOptimizerBehaviourPointInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (EnvOptimizerBranchType)_json["id"].AsInt;
		if (!_json["point"].IsNumber)
		{
			throw new SerializationException();
		}
		Point = _json["point"];
		if (!_json["has_extra_point"].IsBoolean)
		{
			throw new SerializationException();
		}
		HasExtraPoint = _json["has_extra_point"];
	}

	public EnvOptimizerBehaviourPointInfo(EnvOptimizerBranchType id, int point, bool has_extra_point)
	{
		Id = id;
		Point = point;
		HasExtraPoint = has_extra_point;
	}

	public static EnvOptimizerBehaviourPointInfo DeserializeEnvOptimizerBehaviourPointInfo(JSONNode _json)
	{
		return new EnvOptimizerBehaviourPointInfo(_json);
	}

	public override int GetTypeId()
	{
		return 815719151;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id.ToString() + ",Point:" + Point + ",HasExtraPoint:" + HasExtraPoint + ",}";
	}
}
