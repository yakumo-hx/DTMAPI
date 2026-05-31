using UnityEngine;

namespace DolocTown;

public class DismantleChecker
{
	public void Check()
	{
		Room currentRoom = DolocAPI.archiveHandle.currentRoom;
		if (currentRoom.RoomConstructInfo.AllowBuildEquipment && currentRoom.RoomConstructInfo.AllowBuildPlatform)
		{
			Vector3 agentPosition = DolocAPI.AgentPosition;
			agentPosition.x += DolocAPI.agent.transform.localScale.x * 1.7f;
			agentPosition.y += 0.75f;
			Vector2Int pos = currentRoom.Geometry.CalcMinCellPosition(agentPosition);
			IDismantleable content = currentRoom.DM_terrain.GetContent<Equipment>(pos, TerrainLayerName.Equipment);
			if (content == null)
			{
				pos.y--;
				content = currentRoom.DM_terrain.GetContent<Platform>(pos, TerrainLayerName.PlatformSurface);
			}
			content?.OnFell(agentPosition);
		}
	}
}
