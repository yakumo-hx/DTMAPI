namespace DolocTown.GameData;

public static class ArchiveOperationDungeon
{
	public static bool QueryDungeon(this ArchiveDataHandle handle, string name, out Dungeon dungeon)
	{
		return handle.dungeonData.dungeonManager.GetDungeon(name, out dungeon);
	}

	public static bool QueryDungeonRoom(this ArchiveDataHandle handle, string dungeonRoomName, out DungeonRoom room)
	{
		room = null;
		string[] array = dungeonRoomName.Split(".");
		string text = array[0];
		if (text.IsNullOrEmpty() || !handle.QueryDungeon(text, out var dungeon))
		{
			return false;
		}
		if (array.Length == 1)
		{
			room = dungeon?.entryRoom;
			return true;
		}
		string text2 = array[^1];
		if (text2.IsNullOrEmpty() || !dungeon.QueryRoom(text2, out var room2))
		{
			return false;
		}
		room = room2;
		return true;
	}
}
