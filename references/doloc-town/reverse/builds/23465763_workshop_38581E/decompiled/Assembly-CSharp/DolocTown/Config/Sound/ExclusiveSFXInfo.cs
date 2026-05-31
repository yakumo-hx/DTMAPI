using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Sound;

public sealed class ExclusiveSFXInfo : BeanBase
{
	public const int __ID__ = 169147796;

	public string SoundEvent { get; private set; }

	public string Group { get; private set; }

	public ExclusiveSFXInfo(JSONNode _json)
	{
		if (!_json["sound_event"].IsString)
		{
			throw new SerializationException();
		}
		SoundEvent = _json["sound_event"];
		if (!_json["group"].IsString)
		{
			throw new SerializationException();
		}
		Group = _json["group"];
	}

	public ExclusiveSFXInfo(string sound_event, string group)
	{
		SoundEvent = sound_event;
		Group = group;
	}

	public static ExclusiveSFXInfo DeserializeExclusiveSFXInfo(JSONNode _json)
	{
		return new ExclusiveSFXInfo(_json);
	}

	public override int GetTypeId()
	{
		return 169147796;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ SoundEvent:" + SoundEvent + ",Group:" + Group + ",}";
	}
}
