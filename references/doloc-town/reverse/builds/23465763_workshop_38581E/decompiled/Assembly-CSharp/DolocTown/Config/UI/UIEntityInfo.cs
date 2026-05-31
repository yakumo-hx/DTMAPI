using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.UI;

public sealed class UIEntityInfo : BeanBase
{
	public const int __ID__ = 993968639;

	public string Id { get; private set; }

	public PrefabAsset PrefabAsset { get; private set; }

	public string Group { get; private set; }

	public UIEntityGroupInfo Group_Ref { get; private set; }

	public int SortingOrder { get; private set; }

	public bool Preload { get; private set; }

	public bool Resident { get; private set; }

	public UIEntityInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["prefab_asset"].IsObject)
		{
			throw new SerializationException();
		}
		PrefabAsset = ExternalTypeUtil.PrefabAssetConverter(CfgPrefabAsset.DeserializeCfgPrefabAsset(_json["prefab_asset"]));
		if (!_json["group"].IsString)
		{
			throw new SerializationException();
		}
		Group = _json["group"];
		if (!_json["sorting_order"].IsNumber)
		{
			throw new SerializationException();
		}
		SortingOrder = _json["sorting_order"];
		if (!_json["preload"].IsBoolean)
		{
			throw new SerializationException();
		}
		Preload = _json["preload"];
		if (!_json["resident"].IsBoolean)
		{
			throw new SerializationException();
		}
		Resident = _json["resident"];
	}

	public UIEntityInfo(string id, PrefabAsset prefab_asset, string group, int sorting_order, bool preload, bool resident)
	{
		Id = id;
		PrefabAsset = prefab_asset;
		Group = group;
		SortingOrder = sorting_order;
		Preload = preload;
		Resident = resident;
	}

	public static UIEntityInfo DeserializeUIEntityInfo(JSONNode _json)
	{
		return new UIEntityInfo(_json);
	}

	public override int GetTypeId()
	{
		return 993968639;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Group_Ref = (_tables["UI.TbUIEntityGroup"] as TbUIEntityGroup).GetOrDefault(Group);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",PrefabAsset:" + PrefabAsset?.ToString() + ",Group:" + Group + ",SortingOrder:" + SortingOrder + ",Preload:" + Preload + ",Resident:" + Resident + ",}";
	}
}
