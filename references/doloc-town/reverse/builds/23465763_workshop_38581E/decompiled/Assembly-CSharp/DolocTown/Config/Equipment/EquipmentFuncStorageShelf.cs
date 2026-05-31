using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncStorageShelf : EquipmentFuncCaseBase
{
	public const int __ID__ = 469403233;

	public SpriteAssetArray Appearance { get; private set; }

	public SpriteAsset FullUiIcon { get; private set; }

	public EquipmentFuncStorageShelf(JSONNode _json)
		: base(_json)
	{
		if (!_json["appearance"].IsObject)
		{
			throw new SerializationException();
		}
		Appearance = ExternalTypeUtil.SpriteAssetArrayConverter(CfgSpriteAssetArray.DeserializeCfgSpriteAssetArray(_json["appearance"]));
		if (!_json["full_ui_icon"].IsObject)
		{
			throw new SerializationException();
		}
		FullUiIcon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["full_ui_icon"]));
	}

	public EquipmentFuncStorageShelf(int total_capacity, int line_capacity, SpriteAssetArray appearance, SpriteAsset full_ui_icon)
		: base(total_capacity, line_capacity)
	{
		Appearance = appearance;
		FullUiIcon = full_ui_icon;
	}

	public static EquipmentFuncStorageShelf DeserializeEquipmentFuncStorageShelf(JSONNode _json)
	{
		return new EquipmentFuncStorageShelf(_json);
	}

	public override int GetTypeId()
	{
		return 469403233;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ TotalCapacity:" + base.TotalCapacity + ",LineCapacity:" + base.LineCapacity + ",Appearance:" + Appearance?.ToString() + ",FullUiIcon:" + FullUiIcon?.ToString() + ",}";
	}
}
