using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.General;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Resource;

public sealed class ResinCollectorOutputInfo : BeanBase
{
	public const int __ID__ = -1965364841;

	public string Id { get; private set; }

	public bool Disable { get; private set; }

	public string Output { get; private set; }

	public ItemInfo Output_Ref { get; private set; }

	public RangeInt Range { get; private set; }

	public SpriteAsset FilledAsset { get; private set; }

	public SpriteAsset FullAsset { get; private set; }

	public ResinCollectorOutputInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["disable"].IsBoolean)
		{
			throw new SerializationException();
		}
		Disable = _json["disable"];
		if (!_json["output"].IsString)
		{
			throw new SerializationException();
		}
		Output = _json["output"];
		if (!_json["range"].IsObject)
		{
			throw new SerializationException();
		}
		Range = RangeInt.DeserializeRangeInt(_json["range"]);
		if (!_json["filled_asset"].IsObject)
		{
			throw new SerializationException();
		}
		FilledAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["filled_asset"]));
		if (!_json["full_asset"].IsObject)
		{
			throw new SerializationException();
		}
		FullAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["full_asset"]));
	}

	public ResinCollectorOutputInfo(string id, bool disable, string output, RangeInt range, SpriteAsset filled_asset, SpriteAsset full_asset)
	{
		Id = id;
		Disable = disable;
		Output = output;
		Range = range;
		FilledAsset = filled_asset;
		FullAsset = full_asset;
	}

	public static ResinCollectorOutputInfo DeserializeResinCollectorOutputInfo(JSONNode _json)
	{
		return new ResinCollectorOutputInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1965364841;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Output_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Output);
		Range?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Range?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Disable:" + Disable + ",Output:" + Output + ",Range:" + Range?.ToString() + ",FilledAsset:" + FilledAsset?.ToString() + ",FullAsset:" + FullAsset?.ToString() + ",}";
	}
}
