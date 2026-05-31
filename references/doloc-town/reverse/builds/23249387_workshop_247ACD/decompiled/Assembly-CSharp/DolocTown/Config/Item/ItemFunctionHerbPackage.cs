using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionHerbPackage : ItemFunctionPassiveBase
{
	public const int __ID__ = -265050957;

	public SpriteAsset FullSprite { get; private set; }

	public ItemFunctionHerbPackage(JSONNode _json)
		: base(_json)
	{
		if (!_json["full_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		FullSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["full_sprite"]));
	}

	public ItemFunctionHerbPackage(string skill, SpriteAsset full_sprite)
		: base(skill)
	{
		FullSprite = full_sprite;
	}

	public static ItemFunctionHerbPackage DeserializeItemFunctionHerbPackage(JSONNode _json)
	{
		return new ItemFunctionHerbPackage(_json);
	}

	public override int GetTypeId()
	{
		return -265050957;
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
		return "{ Skill:" + base.Skill + ",FullSprite:" + FullSprite?.ToString() + ",}";
	}
}
