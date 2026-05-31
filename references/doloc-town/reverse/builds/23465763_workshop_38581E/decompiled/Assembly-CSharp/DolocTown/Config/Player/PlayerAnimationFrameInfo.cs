using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class PlayerAnimationFrameInfo : BeanBase
{
	public const int __ID__ = -1218174011;

	public string Id { get; private set; }

	public int FrameCount { get; private set; }

	public string OverrideId { get; private set; }

	public bool ForceShowHair { get; private set; }

	public PlayerAnimationFrameInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["frame_count"].IsNumber)
		{
			throw new SerializationException();
		}
		FrameCount = _json["frame_count"];
		if (!_json["override_id"].IsString)
		{
			throw new SerializationException();
		}
		OverrideId = _json["override_id"];
		if (!_json["force_show_hair"].IsBoolean)
		{
			throw new SerializationException();
		}
		ForceShowHair = _json["force_show_hair"];
	}

	public PlayerAnimationFrameInfo(string id, int frame_count, string override_id, bool force_show_hair)
	{
		Id = id;
		FrameCount = frame_count;
		OverrideId = override_id;
		ForceShowHair = force_show_hair;
	}

	public static PlayerAnimationFrameInfo DeserializePlayerAnimationFrameInfo(JSONNode _json)
	{
		return new PlayerAnimationFrameInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1218174011;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",FrameCount:" + FrameCount + ",OverrideId:" + OverrideId + ",ForceShowHair:" + ForceShowHair + ",}";
	}
}
