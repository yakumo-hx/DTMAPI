using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DungeonManager
{
	private Dictionary<string, Dungeon> dungeons = new Dictionary<string, Dungeon>();

	public IEnumerable<Dungeon> totalDungeons => dungeons.Values;

	[JsonProperty("dungeons")]
	private Dungeon[] dungeonArray => dungeons.Values.ToArray();

	public DungeonManager()
	{
	}

	[JsonConstructor]
	public DungeonManager(Dungeon[] dungeons)
	{
		if (dungeons == null)
		{
			return;
		}
		foreach (Dungeon dungeon in dungeons)
		{
			if (dungeon.proto != null)
			{
				this.dungeons.Add(dungeon.ProtoName, dungeon);
			}
		}
	}

	public void AfterNewGame()
	{
		foreach (Dungeon value in dungeons.Values)
		{
			value.AfterNewGame();
		}
	}

	public void AfterLoadData()
	{
		VerifyDungeons();
		foreach (Dungeon value in dungeons.Values)
		{
			value.AfterLoadData();
		}
	}

	private void VerifyDungeons()
	{
		foreach (DungeonProto totalDungeon in DolocAPI.assets.dungeons.totalDungeons)
		{
			if (dungeons.ContainsKey(totalDungeon.name))
			{
				continue;
			}
			foreach (DungeonRoom allRoom in CreateDungeon(totalDungeon).AllRooms)
			{
				allRoom.__AfterNewGame();
			}
		}
	}

	public Dungeon CreateDungeon(DungeonProto proto)
	{
		if (dungeons.TryGetValue(proto.name, out var value))
		{
			return value;
		}
		value = new Dungeon(proto);
		dungeons.Add(proto.name, value);
		return value;
	}

	public Dungeon GetDungeon(string sceneShortName)
	{
		if (dungeons.ContainsKey(sceneShortName))
		{
			return dungeons[sceneShortName];
		}
		return null;
	}

	public bool GetDungeon(string sceneShortName, out Dungeon dungeon)
	{
		return dungeons.TryGetValue(sceneShortName, out dungeon);
	}

	public bool GetDungeonRoom(string sceneShortName, string roomName, out DungeonRoom room)
	{
		if (sceneShortName == null || roomName == null)
		{
			room = null;
			return false;
		}
		if (!dungeons.TryGetValue(sceneShortName, out var value))
		{
			room = null;
			return false;
		}
		room = value.GetRoom(roomName);
		return room != null;
	}
}
