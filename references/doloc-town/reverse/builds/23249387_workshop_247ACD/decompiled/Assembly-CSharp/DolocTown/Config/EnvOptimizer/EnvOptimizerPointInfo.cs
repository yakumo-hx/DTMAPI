using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.EnvOptimizer;

public sealed class EnvOptimizerPointInfo : BeanBase
{
	public const int __ID__ = 509883856;

	public EnvOptimizerBehaviourType Id { get; private set; }

	public EnvOptimizerBehaviourPointInfo[] PointInfos { get; private set; }

	public EnvOptimizerPointInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (EnvOptimizerBehaviourType)_json["id"].AsInt;
		JSONNode jSONNode = _json["point_infos"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		PointInfos = new EnvOptimizerBehaviourPointInfo[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			EnvOptimizerBehaviourPointInfo envOptimizerBehaviourPointInfo = EnvOptimizerBehaviourPointInfo.DeserializeEnvOptimizerBehaviourPointInfo(child);
			PointInfos[num++] = envOptimizerBehaviourPointInfo;
		}
	}

	public EnvOptimizerPointInfo(EnvOptimizerBehaviourType id, EnvOptimizerBehaviourPointInfo[] point_infos)
	{
		Id = id;
		PointInfos = point_infos;
	}

	public static EnvOptimizerPointInfo DeserializeEnvOptimizerPointInfo(JSONNode _json)
	{
		return new EnvOptimizerPointInfo(_json);
	}

	public override int GetTypeId()
	{
		return 509883856;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		EnvOptimizerBehaviourPointInfo[] pointInfos = PointInfos;
		for (int i = 0; i < pointInfos.Length; i++)
		{
			pointInfos[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		EnvOptimizerBehaviourPointInfo[] pointInfos = PointInfos;
		for (int i = 0; i < pointInfos.Length; i++)
		{
			pointInfos[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id.ToString() + ",PointInfos:" + StringUtil.CollectionToString(PointInfos) + ",}";
	}
}
