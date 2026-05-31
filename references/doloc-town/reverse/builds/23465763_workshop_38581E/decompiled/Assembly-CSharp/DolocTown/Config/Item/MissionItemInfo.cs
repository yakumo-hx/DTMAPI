using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class MissionItemInfo : BeanBase
{
	public const int __ID__ = -299635918;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public SpriteAsset SceneAsset { get; private set; }

	public MissionItemInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["scene_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SceneAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["scene_asset"]));
	}

	public MissionItemInfo(string id, SpriteAsset scene_asset)
	{
		Id = id;
		SceneAsset = scene_asset;
	}

	public static MissionItemInfo DeserializeMissionItemInfo(JSONNode _json)
	{
		return new MissionItemInfo(_json);
	}

	public override int GetTypeId()
	{
		return -299635918;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",SceneAsset:" + SceneAsset?.ToString() + ",}";
	}
}
