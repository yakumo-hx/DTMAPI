using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.UI;

public sealed class UIPooledObjectInfo : BeanBase
{
	public const int __ID__ = 1617353974;

	public string Id { get; private set; }

	public PrefabAsset PrefabAsset { get; private set; }

	public string PoolName { get; private set; }

	public int Order { get; private set; }

	public UIPooledObjectInfo(JSONNode _json)
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
		if (!_json["pool_name"].IsString)
		{
			throw new SerializationException();
		}
		PoolName = _json["pool_name"];
		if (!_json["order"].IsNumber)
		{
			throw new SerializationException();
		}
		Order = _json["order"];
	}

	public UIPooledObjectInfo(string id, PrefabAsset prefab_asset, string pool_name, int order)
	{
		Id = id;
		PrefabAsset = prefab_asset;
		PoolName = pool_name;
		Order = order;
	}

	public static UIPooledObjectInfo DeserializeUIPooledObjectInfo(JSONNode _json)
	{
		return new UIPooledObjectInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1617353974;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",PrefabAsset:" + PrefabAsset?.ToString() + ",PoolName:" + PoolName + ",Order:" + Order + ",}";
	}
}
