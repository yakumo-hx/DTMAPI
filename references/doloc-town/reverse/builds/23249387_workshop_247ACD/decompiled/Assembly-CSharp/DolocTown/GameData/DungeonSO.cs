using System.Collections.Generic;
using UnityEngine;

namespace DolocTown.GameData;

public class DungeonSO : ScriptableObject
{
	[SerializeField]
	public string entryName;

	[SerializeField]
	protected SceneInfoSO sceneInfo;

	[SerializeField]
	protected EnvBackgroundSO backgroundSO;

	[SerializeField]
	private List<RoomSO> rooms = new List<RoomSO>();

	public IEnumerable<string> AllRoomNames
	{
		get
		{
			if (rooms.IsNullOrEmpty())
			{
				yield break;
			}
			foreach (RoomSO room in rooms)
			{
				yield return room.RoomName;
			}
		}
	}

	public IEnumerable<RoomSO> AllRooms => rooms;

	public void ResetDungeonDatas(SceneInfoSO sceneInfo, string entryName, EnvBackgroundSO backgroundSO)
	{
		this.sceneInfo = sceneInfo;
		this.entryName = entryName;
		this.backgroundSO = backgroundSO;
	}

	public bool CreateProto(out DungeonProto proto)
	{
		proto = null;
		SceneInfo proto2 = sceneInfo.Proto;
		List<RoomProto> list = new List<RoomProto>();
		foreach (RoomSO room in rooms)
		{
			if (room.CreateProto(out var proto3))
			{
				list.Add(proto3);
			}
		}
		proto = new DungeonProto(base.name, proto2, list.ToArray(), entryName, backgroundSO);
		return true;
	}
}
