using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public class ScannerDriver
{
	private readonly List<IScanner> scanners = new List<IScanner>();

	private Vector2Int lastPosition = Vector2Int.zero;

	private Room currentRoom;

	private bool shouldNotUpdate = true;

	private Queue<IScanner> recycleList = new Queue<IScanner>();

	public void SetRoom(Room room)
	{
		if (currentRoom == room)
		{
			return;
		}
		currentRoom = room;
		foreach (IScanner scanner in scanners)
		{
			scanner.OnRoomChanged(room);
		}
		shouldNotUpdate = currentRoom == null;
	}

	public bool AddScanner(IScanner scanner)
	{
		if (scanners.Contains(scanner))
		{
			return false;
		}
		scanners.Add(scanner);
		return true;
	}

	public void UpdatePosition(Vector2 positionWS)
	{
		if (shouldNotUpdate)
		{
			return;
		}
		Vector2Int vector2Int = currentRoom.Geometry.CalcCellPosition(positionWS);
		foreach (IScanner scanner in scanners)
		{
			scanner.OnWorldPosChanged(positionWS);
		}
		if (vector2Int != lastPosition)
		{
			lastPosition = vector2Int;
			foreach (IScanner scanner2 in scanners)
			{
				scanner2.OnPosChanged(vector2Int);
			}
		}
		while (recycleList.Count > 0)
		{
			scanners.Remove(recycleList.Dequeue());
		}
	}
}
