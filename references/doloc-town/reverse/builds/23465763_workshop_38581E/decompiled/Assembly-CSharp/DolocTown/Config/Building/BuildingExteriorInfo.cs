using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Building;

public sealed class BuildingExteriorInfo : BeanBase
{
	public readonly Dictionary<string, BuildingExteriorData> ExteriorDatas_Index = new Dictionary<string, BuildingExteriorData>();

	public const int __ID__ = 1696906330;

	public string Id { get; private set; }

	public List<BuildingExteriorData> ExteriorDatas { get; private set; }

	public BuildingExteriorInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["exterior_datas"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		ExteriorDatas = new List<BuildingExteriorData>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			BuildingExteriorData item = BuildingExteriorData.DeserializeBuildingExteriorData(child);
			ExteriorDatas.Add(item);
		}
		foreach (BuildingExteriorData exteriorData in ExteriorDatas)
		{
			ExteriorDatas_Index.Add(exteriorData.BuildingId, exteriorData);
		}
	}

	public BuildingExteriorInfo(string id, List<BuildingExteriorData> exterior_datas)
	{
		Id = id;
		ExteriorDatas = exterior_datas;
		foreach (BuildingExteriorData exteriorData in ExteriorDatas)
		{
			ExteriorDatas_Index.Add(exteriorData.BuildingId, exteriorData);
		}
	}

	public static BuildingExteriorInfo DeserializeBuildingExteriorInfo(JSONNode _json)
	{
		return new BuildingExteriorInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1696906330;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (BuildingExteriorData exteriorData in ExteriorDatas)
		{
			exteriorData?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (BuildingExteriorData exteriorData in ExteriorDatas)
		{
			exteriorData?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ExteriorDatas:" + StringUtil.CollectionToString(ExteriorDatas) + ",}";
	}
}
