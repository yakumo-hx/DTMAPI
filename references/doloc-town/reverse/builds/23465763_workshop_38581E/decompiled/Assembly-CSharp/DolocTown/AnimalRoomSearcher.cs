using System.Collections.Generic;

namespace DolocTown;

public class AnimalRoomSearcher
{
	private readonly HashSet<Room> searchedRoom = new HashSet<Room>();

	public void Reset()
	{
		searchedRoom.Clear();
	}

	public bool IsSearched(Room room)
	{
		if (room == null)
		{
			return false;
		}
		return !searchedRoom.Add(room);
	}
}
