using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionFishingRod : ItemFunctionBase
{
	public const int __ID__ = 1088868995;

	public int Level { get; private set; }

	public float CatchSpeed { get; private set; }

	public float AntiEscapeMultiplier { get; private set; }

	public float AntiStruggleMultiplier { get; private set; }

	public float BonusMultiplier { get; private set; }

	public float InitProgress { get; private set; }

	public string LineColor { get; private set; }

	public ItemFunctionFishingRod(JSONNode _json)
		: base(_json)
	{
		if (!_json["level"].IsNumber)
		{
			throw new SerializationException();
		}
		Level = _json["level"];
		if (!_json["catch_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		CatchSpeed = _json["catch_speed"];
		if (!_json["anti_escape_multiplier"].IsNumber)
		{
			throw new SerializationException();
		}
		AntiEscapeMultiplier = _json["anti_escape_multiplier"];
		if (!_json["anti_struggle_multiplier"].IsNumber)
		{
			throw new SerializationException();
		}
		AntiStruggleMultiplier = _json["anti_struggle_multiplier"];
		if (!_json["bonus_multiplier"].IsNumber)
		{
			throw new SerializationException();
		}
		BonusMultiplier = _json["bonus_multiplier"];
		if (!_json["init_progress"].IsNumber)
		{
			throw new SerializationException();
		}
		InitProgress = _json["init_progress"];
		if (!_json["line_color"].IsString)
		{
			throw new SerializationException();
		}
		LineColor = _json["line_color"];
	}

	public ItemFunctionFishingRod(int level, float catch_speed, float anti_escape_multiplier, float anti_struggle_multiplier, float bonus_multiplier, float init_progress, string line_color)
	{
		Level = level;
		CatchSpeed = catch_speed;
		AntiEscapeMultiplier = anti_escape_multiplier;
		AntiStruggleMultiplier = anti_struggle_multiplier;
		BonusMultiplier = bonus_multiplier;
		InitProgress = init_progress;
		LineColor = line_color;
	}

	public static ItemFunctionFishingRod DeserializeItemFunctionFishingRod(JSONNode _json)
	{
		return new ItemFunctionFishingRod(_json);
	}

	public override int GetTypeId()
	{
		return 1088868995;
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
		return "{ Level:" + Level + ",CatchSpeed:" + CatchSpeed + ",AntiEscapeMultiplier:" + AntiEscapeMultiplier + ",AntiStruggleMultiplier:" + AntiStruggleMultiplier + ",BonusMultiplier:" + BonusMultiplier + ",InitProgress:" + InitProgress + ",LineColor:" + LineColor + ",}";
	}
}
