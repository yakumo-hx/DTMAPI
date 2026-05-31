using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Room;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MapText : DolocUiObject
{
	[SerializeField]
	private Text roomName;

	[SerializeField]
	private Text label;

	[SerializeField]
	private TextAnchor alignment = TextAnchor.MiddleCenter;

	[SerializeField]
	private string mapId = string.Empty;

	[SerializeField]
	private string areaId;

	[SerializeField]
	private bool alwaysShow;

	private string[] AreaIds => (from data in DolocConfig.Tables.TbMapArea.GetRoomAreasByMapId(mapId)
		select data.AreaId).ToArray();

	public void WriteByConfig(MapAreaInfo proto)
	{
		mapId = proto.MapId;
		areaId = proto.AreaId;
		OnAreaIdChange();
	}

	public void RefreshText(out string str)
	{
		str = string.Empty;
		MapAreaInfo mapAreaInfo = DolocConfig.Tables.TbMapArea.Get(mapId, areaId);
		roomName.gameObject.SetActive(value: false);
		label.gameObject.SetActive(value: false);
		if (mapAreaInfo == null)
		{
			return;
		}
		switch (mapAreaInfo.MapAreaType)
		{
		case MapAreaType.Room:
			if (!alwaysShow && !DolocAPI.archiveHandle.farmData.mapManager.IsRoomVisited(mapAreaInfo.RoomName ?? ""))
			{
				return;
			}
			str = DolocConfig.Tables.TbScene.GetOrDefault(mapAreaInfo.RoomName)?.Title;
			roomName.text = str;
			roomName.gameObject.SetActive(value: true);
			break;
		case MapAreaType.Label:
			if (!alwaysShow && !mapAreaInfo.CoveredRooms.IsNullOrEmpty() && mapAreaInfo.CoveredRooms.All((string x) => !DolocAPI.archiveHandle.farmData.mapManager.IsRoomVisited(x ?? "")))
			{
				return;
			}
			str = mapAreaInfo.Label;
			label.text = str;
			label.gameObject.SetActive(value: true);
			break;
		}
		roomName.alignment = alignment;
		label.alignment = alignment;
	}

	private void OnAreaIdChange()
	{
		RefreshText(out var str);
		base.gameObject.name = str;
	}
}
