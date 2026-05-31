using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Room;
using SimpleJSON;

namespace DolocTown.Config.Festival;

public sealed class NpcFestivalInfo : BeanBase
{
	public const int __ID__ = 115494737;

	public string MarkPoint { get; private set; }

	public MarkPointInfo MarkPoint_Ref { get; private set; }

	public bool FaceLeft { get; private set; }

	public string DialogueNode { get; private set; }

	public bool AutoInit { get; private set; }

	public NpcFestivalInfo(JSONNode _json)
	{
		if (!_json["mark_point"].IsString)
		{
			throw new SerializationException();
		}
		MarkPoint = _json["mark_point"];
		if (!_json["face_left"].IsBoolean)
		{
			throw new SerializationException();
		}
		FaceLeft = _json["face_left"];
		if (!_json["dialogue_node"].IsString)
		{
			throw new SerializationException();
		}
		DialogueNode = _json["dialogue_node"];
		if (!_json["auto_init"].IsBoolean)
		{
			throw new SerializationException();
		}
		AutoInit = _json["auto_init"];
	}

	public NpcFestivalInfo(string mark_point, bool face_left, string dialogue_node, bool auto_init)
	{
		MarkPoint = mark_point;
		FaceLeft = face_left;
		DialogueNode = dialogue_node;
		AutoInit = auto_init;
	}

	public static NpcFestivalInfo DeserializeNpcFestivalInfo(JSONNode _json)
	{
		return new NpcFestivalInfo(_json);
	}

	public override int GetTypeId()
	{
		return 115494737;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		MarkPoint_Ref = (_tables["Room.TbMarkPoint"] as TbMarkPoint).GetOrDefault(MarkPoint);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ MarkPoint:" + MarkPoint + ",FaceLeft:" + FaceLeft + ",DialogueNode:" + DialogueNode + ",AutoInit:" + AutoInit + ",}";
	}
}
