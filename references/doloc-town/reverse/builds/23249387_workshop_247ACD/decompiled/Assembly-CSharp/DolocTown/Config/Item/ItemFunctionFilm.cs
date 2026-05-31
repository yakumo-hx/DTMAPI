using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionFilm : ItemFunctionBase
{
	public const int __ID__ = -1823727190;

	public int HealingAmount { get; private set; }

	public SpriteAssetArray LevelSprites { get; private set; }

	public ItemFunctionFilm(JSONNode _json)
		: base(_json)
	{
		if (!_json["healing_amount"].IsNumber)
		{
			throw new SerializationException();
		}
		HealingAmount = _json["healing_amount"];
		if (!_json["level_sprites"].IsObject)
		{
			throw new SerializationException();
		}
		LevelSprites = ExternalTypeUtil.SpriteAssetArrayConverter(CfgSpriteAssetArray.DeserializeCfgSpriteAssetArray(_json["level_sprites"]));
	}

	public ItemFunctionFilm(int healing_amount, SpriteAssetArray level_sprites)
	{
		HealingAmount = healing_amount;
		LevelSprites = level_sprites;
	}

	public static ItemFunctionFilm DeserializeItemFunctionFilm(JSONNode _json)
	{
		return new ItemFunctionFilm(_json);
	}

	public override int GetTypeId()
	{
		return -1823727190;
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
		return "{ HealingAmount:" + HealingAmount + ",LevelSprites:" + LevelSprites?.ToString() + ",}";
	}
}
