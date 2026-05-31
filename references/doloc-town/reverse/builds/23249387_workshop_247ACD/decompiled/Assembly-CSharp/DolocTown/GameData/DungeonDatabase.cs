using System.Collections.Generic;

namespace DolocTown.GameData;

public class DungeonDatabase
{
	private readonly Dictionary<string, DungeonProto> dungeons = new Dictionary<string, DungeonProto>();

	public IEnumerable<DungeonProto> totalDungeons => dungeons.Values;

	public bool AddDungeonProto(DungeonProto proto)
	{
		if (string.IsNullOrEmpty(proto.name) || dungeons.ContainsKey(proto.name))
		{
			return false;
		}
		dungeons.Add(proto.name, proto);
		return true;
	}

	public bool QueryDungeonProto(string name, out DungeonProto dungeon)
	{
		return dungeons.TryGetValue(name, out dungeon);
	}

	public bool GetDungeonProto(string fullName, out DungeonProto dungeon)
	{
		dungeon = null;
		if (!fullName.Contains('_'))
		{
			return false;
		}
		string[] array = fullName.Split('_', 2);
		if (array[0].IsNullOrEmpty() || array[1].IsNullOrEmpty())
		{
			return false;
		}
		return dungeons.TryGetValue(array[1], out dungeon);
	}
}
