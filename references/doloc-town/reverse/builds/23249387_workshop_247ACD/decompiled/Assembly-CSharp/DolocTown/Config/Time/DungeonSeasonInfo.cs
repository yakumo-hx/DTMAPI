using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Time;

public sealed class DungeonSeasonInfo : BeanBase
{
	public const int __ID__ = 1718130602;

	public string Id { get; private set; }

	public int[] Seasons { get; private set; }

	public DungeonSeasonInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["seasons"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Seasons = new int[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsNumber)
			{
				throw new SerializationException();
			}
			int num2 = child;
			Seasons[num++] = num2;
		}
	}

	public DungeonSeasonInfo(string id, int[] seasons)
	{
		Id = id;
		Seasons = seasons;
	}

	public static DungeonSeasonInfo DeserializeDungeonSeasonInfo(JSONNode _json)
	{
		return new DungeonSeasonInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1718130602;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Seasons:" + StringUtil.CollectionToString(Seasons) + ",}";
	}
}
