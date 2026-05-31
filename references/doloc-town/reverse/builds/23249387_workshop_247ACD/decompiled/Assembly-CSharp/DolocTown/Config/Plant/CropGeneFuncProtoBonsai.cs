using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoBonsai : CropGeneFuncProto
{
	public const int __ID__ = -429132330;

	public int LevelThreshold { get; private set; }

	public int MoodContribution { get; private set; }

	public CropGeneFuncProtoBonsai(JSONNode _json)
		: base(_json)
	{
		if (!_json["level_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		LevelThreshold = _json["level_threshold"];
		if (!_json["mood_contribution"].IsNumber)
		{
			throw new SerializationException();
		}
		MoodContribution = _json["mood_contribution"];
	}

	public CropGeneFuncProtoBonsai(int level_threshold, int mood_contribution)
	{
		LevelThreshold = level_threshold;
		MoodContribution = mood_contribution;
	}

	public static CropGeneFuncProtoBonsai DeserializeCropGeneFuncProtoBonsai(JSONNode _json)
	{
		return new CropGeneFuncProtoBonsai(_json);
	}

	public override int GetTypeId()
	{
		return -429132330;
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
		return "{ LevelThreshold:" + LevelThreshold + ",MoodContribution:" + MoodContribution + ",}";
	}
}
