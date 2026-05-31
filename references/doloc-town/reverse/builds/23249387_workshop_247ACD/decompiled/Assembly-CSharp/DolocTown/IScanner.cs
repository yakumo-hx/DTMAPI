using UnityEngine;

namespace DolocTown;

public interface IScanner
{
	void OnWorldPosChanged(Vector2 positionWS);

	void OnPosChanged(Vector2Int pos);

	void OnRoomChanged(Room newRoom);
}
