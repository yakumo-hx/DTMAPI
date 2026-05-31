using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Resource;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class ModVegetationSpawnExtensionInfo : BeanBase
{
	public const int __ID__ = 1340140662;

	public string Id { get; private set; }

	public VegetationSpawnInfo Id_Ref { get; private set; }

	public VegetationSpawnData[] ExtraVegetations { get; private set; }

	public ModVegetationSpawnExtensionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["extra_vegetations"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ExtraVegetations = new VegetationSpawnData[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			VegetationSpawnData vegetationSpawnData = VegetationSpawnData.DeserializeVegetationSpawnData(child);
			ExtraVegetations[num++] = vegetationSpawnData;
		}
	}

	public ModVegetationSpawnExtensionInfo(string id, VegetationSpawnData[] extra_vegetations)
	{
		Id = id;
		ExtraVegetations = extra_vegetations;
	}

	public static ModVegetationSpawnExtensionInfo DeserializeModVegetationSpawnExtensionInfo(JSONNode _json)
	{
		return new ModVegetationSpawnExtensionInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1340140662;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Resource.TbVegetationSpawn"] as TbVegetationSpawn).GetOrDefault(Id);
		VegetationSpawnData[] extraVegetations = ExtraVegetations;
		for (int i = 0; i < extraVegetations.Length; i++)
		{
			extraVegetations[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		VegetationSpawnData[] extraVegetations = ExtraVegetations;
		for (int i = 0; i < extraVegetations.Length; i++)
		{
			extraVegetations[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ExtraVegetations:" + StringUtil.CollectionToString(ExtraVegetations) + ",}";
	}
}
