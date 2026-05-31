using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.General;
using SimpleJSON;

namespace DolocTown.Config.Resource;

public sealed class ResourceTypeInfo : BeanBase
{
	public const int __ID__ = 603658006;

	public DungeonResourceType ResourceType { get; private set; }

	public DungeonResourceClass ResourceClass { get; private set; }

	public SortingLayerInfo SortingLayer { get; private set; }

	public string TerrainLayer { get; private set; }

	public string ClassTypeName { get; private set; }

	public ResourceTypeInfo(JSONNode _json)
	{
		if (!_json["resource_type"].IsNumber)
		{
			throw new SerializationException();
		}
		ResourceType = (DungeonResourceType)_json["resource_type"].AsInt;
		if (!_json["resource_class"].IsNumber)
		{
			throw new SerializationException();
		}
		ResourceClass = (DungeonResourceClass)_json["resource_class"].AsInt;
		if (!_json["sorting_layer"].IsObject)
		{
			throw new SerializationException();
		}
		SortingLayer = SortingLayerInfo.DeserializeSortingLayerInfo(_json["sorting_layer"]);
		if (!_json["terrain_layer"].IsString)
		{
			throw new SerializationException();
		}
		TerrainLayer = _json["terrain_layer"];
		if (!_json["class_type_name"].IsString)
		{
			throw new SerializationException();
		}
		ClassTypeName = _json["class_type_name"];
	}

	public ResourceTypeInfo(DungeonResourceType resource_type, DungeonResourceClass resource_class, SortingLayerInfo sorting_layer, string terrain_layer, string class_type_name)
	{
		ResourceType = resource_type;
		ResourceClass = resource_class;
		SortingLayer = sorting_layer;
		TerrainLayer = terrain_layer;
		ClassTypeName = class_type_name;
	}

	public static ResourceTypeInfo DeserializeResourceTypeInfo(JSONNode _json)
	{
		return new ResourceTypeInfo(_json);
	}

	public override int GetTypeId()
	{
		return 603658006;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		SortingLayer?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		SortingLayer?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ ResourceType:" + ResourceType.ToString() + ",ResourceClass:" + ResourceClass.ToString() + ",SortingLayer:" + SortingLayer?.ToString() + ",TerrainLayer:" + TerrainLayer + ",ClassTypeName:" + ClassTypeName + ",}";
	}
}
