using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Resource;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class ModResourceSpawnExtensionInfo : BeanBase
{
	public const int __ID__ = 1022948598;

	public string Id { get; private set; }

	public ResourceSpawnInfo Id_Ref { get; private set; }

	public ResourceSpawnData[] ExtraResources { get; private set; }

	public ModResourceSpawnExtensionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["extra_resources"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ExtraResources = new ResourceSpawnData[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			ResourceSpawnData resourceSpawnData = ResourceSpawnData.DeserializeResourceSpawnData(child);
			ExtraResources[num++] = resourceSpawnData;
		}
	}

	public ModResourceSpawnExtensionInfo(string id, ResourceSpawnData[] extra_resources)
	{
		Id = id;
		ExtraResources = extra_resources;
	}

	public static ModResourceSpawnExtensionInfo DeserializeModResourceSpawnExtensionInfo(JSONNode _json)
	{
		return new ModResourceSpawnExtensionInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1022948598;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Resource.TbResourceSpawn"] as TbResourceSpawn).GetOrDefault(Id);
		ResourceSpawnData[] extraResources = ExtraResources;
		for (int i = 0; i < extraResources.Length; i++)
		{
			extraResources[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		ResourceSpawnData[] extraResources = ExtraResources;
		for (int i = 0; i < extraResources.Length; i++)
		{
			extraResources[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ExtraResources:" + StringUtil.CollectionToString(ExtraResources) + ",}";
	}
}
