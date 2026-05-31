using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Buff;

public sealed class SpiritThresholdInfo : BeanBase
{
	public const int __ID__ = 377863469;

	public int SpiritThreshold { get; private set; }

	public string SmallInfo { get; private set; }

	public string SmallInfo_l10n_key { get; }

	public string LargeInfo { get; private set; }

	public string LargeInfo_l10n_key { get; }

	public SpiritThresholdInfo(JSONNode _json)
	{
		if (!_json["spirit_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		SpiritThreshold = _json["spirit_threshold"];
		if (!_json["small_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SmallInfo_l10n_key = _json["small_info"]["key"];
		if (!_json["small_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SmallInfo = _json["small_info"]["text"];
		if (!_json["large_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		LargeInfo_l10n_key = _json["large_info"]["key"];
		if (!_json["large_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		LargeInfo = _json["large_info"]["text"];
	}

	public SpiritThresholdInfo(int spirit_threshold, string small_info, string large_info)
	{
		SpiritThreshold = spirit_threshold;
		SmallInfo = small_info;
		LargeInfo = large_info;
	}

	public static SpiritThresholdInfo DeserializeSpiritThresholdInfo(JSONNode _json)
	{
		return new SpiritThresholdInfo(_json);
	}

	public override int GetTypeId()
	{
		return 377863469;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		SmallInfo = translator(SmallInfo_l10n_key, SmallInfo);
		LargeInfo = translator(LargeInfo_l10n_key, LargeInfo);
	}

	public override string ToString()
	{
		return "{ SpiritThreshold:" + SpiritThreshold + ",SmallInfo:" + SmallInfo + ",LargeInfo:" + LargeInfo + ",}";
	}
}
