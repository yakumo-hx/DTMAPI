using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Room;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class MapManager
{
	[JsonProperty]
	private HashSet<string> activeMaps;

	[JsonProperty]
	private HashSet<string> unlockedMaps;

	[JsonProperty]
	private HashSet<string> unlockedRooms;

	[JsonProperty]
	private HashSet<string> indicatedMaps;

	[JsonProperty]
	private HashSet<string> visitedRooms;

	[JsonProperty]
	private HashSet<string> visitedPortals;

	private Dictionary<string, Texture2D> visibleRoomMasks = new Dictionary<string, Texture2D>();

	[JsonProperty]
	[JsonConverter(typeof(MaskTextureDictionaryConverter))]
	private Dictionary<string, Texture2D> roomFogMaskCache;

	private Dictionary<string, Texture2D> fogMasks = new Dictionary<string, Texture2D>();

	[JsonProperty]
	private HashSet<string> visitedHiddenArea;

	private Dictionary<string, Texture2D> hiddenAreaMasks = new Dictionary<string, Texture2D>();

	private Dictionary<string, HashSet<MapAreaInfo>> roomHiddenAreaCache = new Dictionary<string, HashSet<MapAreaInfo>>();

	[JsonProperty]
	private Dictionary<string, Vector2> mapOffsets;

	private string currentRoomId;

	private string currentMapId;

	private MapAreaInfo currentMapAreaProto;

	private bool shouldClearFog;

	private Texture2D currentFogMask;

	private Texture2D currentRoomFogCache;

	private bool shouldClearHiddenAreaMask;

	private Texture2D currentHiddenAreaMask;

	private TbMapType mapTypeTable => DolocConfig.Tables.TbMapType;

	private TbMapArea mapAreaTable => DolocConfig.Tables.TbMapArea;

	[JsonConstructor]
	public MapManager(HashSet<string> activeMaps = null, HashSet<string> unlockedMaps = null, HashSet<string> unlockedRooms = null, HashSet<string> indicatedMaps = null, HashSet<string> visitedRooms = null, HashSet<string> visitedPortals = null, HashSet<string> noFogRooms = null, HashSet<string> visitedHiddenArea = null, Dictionary<string, Texture2D> roomFogMaskCache = null, Dictionary<string, Vector2> mapOffsets = null)
	{
		this.activeMaps = activeMaps ?? new HashSet<string>();
		this.unlockedMaps = unlockedMaps ?? new HashSet<string>();
		this.unlockedRooms = unlockedRooms ?? new HashSet<string>();
		this.indicatedMaps = indicatedMaps ?? new HashSet<string>();
		this.visitedRooms = visitedRooms ?? new HashSet<string>();
		this.visitedPortals = visitedPortals ?? new HashSet<string>();
		this.visitedHiddenArea = visitedHiddenArea ?? new HashSet<string>();
		this.roomFogMaskCache = roomFogMaskCache ?? new Dictionary<string, Texture2D>();
		this.mapOffsets = mapOffsets ?? new Dictionary<string, Vector2>();
		DolocAPI.OnAfterLoadArchiveData.AddListener(Init);
	}

	public void Init(bool isNewGame)
	{
		foreach (MapTypeInfo data in mapTypeTable.DataList)
		{
			if (!data.UseMask)
			{
				continue;
			}
			string id = data.Id;
			visibleRoomMasks[id] = TextureUtils.CreateTexture(data.MapSize);
			fogMasks[id] = TextureUtils.CreateTexture(data.MapSize);
			hiddenAreaMasks[id] = TextureUtils.CreateTexture(data.MapSize);
			foreach (MapAreaInfo item in mapAreaTable.GetRoomAreasByMapId(id))
			{
				string roomName = item.RoomName;
				if (item.MapAreaType != 0)
				{
					continue;
				}
				roomFogMaskCache.TryGetValue(roomName, out var value);
				if (visitedRooms.Contains(roomName))
				{
					if (value != null)
					{
						fogMasks[id].DrawAreaByTexture(item.AreaOffsetWithPadding, item.AreaSizeWithPadding, value);
						ValidateFogRate(item, roomName, fogMasks[id]);
					}
				}
				else
				{
					fogMasks[id].DrawArea(item.AreaOffsetWithPadding, item.AreaSizeWithPadding, Color.white);
				}
				if (item.NeedUnlock && !unlockedRooms.Contains(roomName) && !visitedRooms.Contains(roomName))
				{
					visibleRoomMasks[id].DrawArea(item.AreaOffset, item.AreaSize, Color.white);
				}
				if (!unlockedMaps.Contains(id) && !visitedRooms.Contains(roomName))
				{
					visibleRoomMasks[id].DrawArea(item.AreaOffset, item.AreaSize, Color.white);
				}
			}
			foreach (MapAreaInfo item2 in mapAreaTable.GetHiddenAreaByMapId(id))
			{
				if (item2.MapAreaType == MapAreaType.HiddenArea && !visitedHiddenArea.Contains(item2.AreaId))
				{
					hiddenAreaMasks[id].DrawArea(item2.AreaOffset, item2.AreaSize, Color.white);
					string[] coveredRooms = item2.CoveredRooms;
					foreach (string key in coveredRooms)
					{
						roomHiddenAreaCache.TryAdd(key, new HashSet<MapAreaInfo>());
						roomHiddenAreaCache[key].Add(item2);
					}
				}
			}
		}
	}

	public bool GetMask(string mapId, out Texture2D visibleRoomMask, out Texture2D fogMask, out Texture2D hiddenAreaMask)
	{
		return true & GetVisibleRoomMask(mapId, out visibleRoomMask) & GetFogMask(mapId, out fogMask) & GetHiddenAreaMask(mapId, out hiddenAreaMask);
	}

	private bool GetVisibleRoomMask(string mapId, out Texture2D visibleRoomMask)
	{
		return visibleRoomMasks.TryGetValue(mapId, out visibleRoomMask);
	}

	private bool GetFogMask(string mapId, out Texture2D fogMask)
	{
		return fogMasks.TryGetValue(mapId, out fogMask);
	}

	private bool GetHiddenAreaMask(string mapId, out Texture2D hiddenAreaMask)
	{
		return hiddenAreaMasks.TryGetValue(mapId, out hiddenAreaMask);
	}

	public void UnlockRoomInMap(string mapId, string roomId)
	{
		if (unlockedRooms.Add(roomId) && unlockedMaps.Contains(mapId))
		{
			TryVisitRoom(roomId);
		}
	}

	private void TryVisitRoom(string roomId)
	{
		if (visitedRooms.Add(roomId))
		{
			MapAreaInfo roomAreaByRoomId = mapAreaTable.GetRoomAreaByRoomId(roomId);
			roomFogMaskCache[roomId] = TextureUtils.CreateTexture(roomAreaByRoomId.AreaSizeWithPadding, Color.white);
			if (GetVisibleRoomMask(roomAreaByRoomId.MapId, out var visibleRoomMask))
			{
				visibleRoomMask.DrawArea(roomAreaByRoomId.AreaOffset, roomAreaByRoomId.AreaSize, DolocColor.empty);
			}
		}
	}

	public void SetMapOffset(string mapId, Vector2 offset)
	{
		mapOffsets[mapId] = offset;
	}

	public Vector2 GetMapOffset(string mapId)
	{
		if (mapOffsets.TryGetValue(mapId, out var value))
		{
			return value;
		}
		return Vector2.zero;
	}

	public void SetCurrentRoom(Room room)
	{
		if (room.SceneConfig == null)
		{
			Debug.LogError("房间<" + room.RoomId + ">没有配置场景信息");
			return;
		}
		currentMapId = room.SceneConfig.MapId;
		activeMaps.Add(currentMapId);
		currentRoomId = room.RoomId;
		currentMapAreaProto = mapAreaTable.GetRoomAreaByRoomId(room.RoomId);
		if (currentMapAreaProto == null || !currentMapAreaProto.MapId_Ref.UseMask)
		{
			visitedRooms.Add(currentRoomId);
			shouldClearFog = false;
			currentFogMask = null;
			shouldClearHiddenAreaMask = false;
			currentHiddenAreaMask = null;
		}
		else
		{
			currentMapId = currentMapAreaProto.MapId;
			TryVisitRoom(room.RoomId);
			shouldClearFog = roomFogMaskCache.ContainsKey(room.RoomId);
			roomFogMaskCache.TryGetValue(room.RoomId, out currentRoomFogCache);
			GetFogMask(currentMapId, out currentFogMask);
			shouldClearHiddenAreaMask = roomHiddenAreaCache.ContainsKey(room.RoomId);
			GetHiddenAreaMask(currentMapId, out currentHiddenAreaMask);
		}
	}

	public void ClearFog(Vector2 worldPosition, bool circle)
	{
		if (shouldClearFog && roomFogMaskCache != null && !(currentFogMask == null))
		{
			Vector2 vector = DolocAPI.CurrentRoom.Geometry.CalPosPercent(worldPosition);
			Vector2Int vector2Int = new Vector2Int(Mathf.RoundToInt((float)currentMapAreaProto.AreaSize.x * vector.x), Mathf.RoundToInt((float)currentMapAreaProto.AreaSize.y * vector.y));
			Vector2Int cameraSize = currentMapAreaProto.MapId_Ref.CameraSize;
			int xMin = Mathf.Max(0, vector2Int.x - cameraSize.x / 2);
			int yMin = Mathf.Max(0, vector2Int.y - cameraSize.y / 2);
			int xMax = Mathf.Min(currentRoomFogCache.width, vector2Int.x + cameraSize.x / 2);
			int yMax = Mathf.Min(currentRoomFogCache.height, vector2Int.y + cameraSize.y / 2);
			currentRoomFogCache.DrawArea(xMin, yMin, xMax, yMax, DolocColor.empty);
			currentFogMask.DrawAreaByTexture(currentMapAreaProto.AreaOffsetWithPadding, currentMapAreaProto.AreaSizeWithPadding, currentRoomFogCache);
			ValidateFogRate(currentMapAreaProto, currentRoomId, currentFogMask);
			if (!roomFogMaskCache.ContainsKey(currentRoomId))
			{
				currentRoomFogCache = null;
				shouldClearFog = false;
			}
		}
	}

	private void ValidateFogRate(MapAreaInfo areaProto, string roomId, Texture2D mapFogMask)
	{
		if (roomFogMaskCache.TryGetValue(roomId, out var value) && !(value == null) && (float)value.GetPixels().Count((Color color) => color.a > 0f) / (float)(areaProto.AreaSizeWithPadding.x * areaProto.AreaSizeWithPadding.y) < areaProto.FogMaxRate)
		{
			roomFogMaskCache.Remove(roomId);
			if (mapFogMask != null)
			{
				mapFogMask.DrawArea(areaProto.AreaOffsetWithPadding, areaProto.AreaSizeWithPadding, DolocColor.empty);
			}
		}
	}

	public bool ClearHiddenMask(Vector2 worldPosition)
	{
		if (!shouldClearHiddenAreaMask || currentHiddenAreaMask == null)
		{
			return true;
		}
		Vector2 vector = DolocAPI.CurrentRoom.Geometry.CalPosPercent(worldPosition);
		Vector2Int areaOffset = currentMapAreaProto.AreaOffset;
		Vector2Int areaSize = currentMapAreaProto.AreaSize;
		if (!roomHiddenAreaCache.TryGetValue(currentRoomId, out var value))
		{
			return true;
		}
		if (value.Count == 0)
		{
			roomHiddenAreaCache.Remove(currentRoomId);
			return true;
		}
		Vector2Int vector2Int = new Vector2Int(Mathf.RoundToInt((float)areaSize.x * vector.x + (float)areaOffset.x), Mathf.RoundToInt((float)areaSize.y * vector.y + (float)areaOffset.y));
		foreach (MapAreaInfo item in value)
		{
			int x = item.AreaOffset.x;
			int num = item.AreaOffset.x + item.AreaSize.x;
			int y = item.AreaOffset.y;
			int num2 = item.AreaOffset.y + item.AreaSize.y;
			if (vector2Int.x < x || vector2Int.x > num || vector2Int.y < y || vector2Int.y > num2 || !visitedHiddenArea.Add(item.AreaId))
			{
				continue;
			}
			currentHiddenAreaMask.DrawArea(item.AreaOffset, item.AreaSize, DolocColor.empty);
			Debug.Log("隐藏区域<" + item.AreaId + ">已解锁");
			string[] array = roomHiddenAreaCache.Keys.ToArray();
			foreach (string key in array)
			{
				roomHiddenAreaCache[key].Remove(item);
				if (roomHiddenAreaCache[key].Count == 0)
				{
					shouldClearHiddenAreaMask = false;
					roomHiddenAreaCache.Remove(key);
				}
			}
			return true;
		}
		return false;
	}

	public void UnlockMap(string mapId)
	{
		activeMaps.Add(mapId);
		if (!unlockedMaps.Add(mapId))
		{
			return;
		}
		MapAreaInfo[] array = (from x in mapAreaTable.GetRoomAreasByMapId(mapId)
			where x.MapAreaType == MapAreaType.Room
			select x).ToArray();
		if (array.Where((MapAreaInfo x) => x.NeedUnlock).ToArray().IsNullOrEmpty())
		{
			GetVisibleRoomMask(mapId, out var visibleRoomMask);
			visibleRoomMask.Clear();
			return;
		}
		MapAreaInfo[] array2 = array;
		foreach (MapAreaInfo mapAreaInfo in array2)
		{
			if (!mapAreaInfo.NeedUnlock || unlockedRooms.Contains(mapAreaInfo.RoomName))
			{
				TryVisitRoom(mapAreaInfo.RoomName);
			}
		}
	}

	public bool IsMapActive(string mapId)
	{
		return activeMaps.Contains(mapId);
	}

	public void ShowIndicatorsInMap(string mapId)
	{
		indicatedMaps.Add(mapId);
	}

	public MapAreaInfo[] GetIndicatedHiddenAreas(string mapId)
	{
		List<MapAreaInfo> hiddenAreaByMapId = mapAreaTable.GetHiddenAreaByMapId(mapId);
		if (!indicatedMaps.Contains(mapId))
		{
			return Array.Empty<MapAreaInfo>();
		}
		return hiddenAreaByMapId.Where((MapAreaInfo x) => !visitedHiddenArea.Contains(x.AreaId) && !x.NeedUnlock).ToArray();
	}

	public bool IsRoomVisited(string roomId)
	{
		return visitedRooms.Contains(roomId);
	}

	public void SetRoomVisited(string mapId, string roomId)
	{
		visitedRooms.Add(roomId);
		MapAreaInfo roomAreaByRoomId = mapAreaTable.GetRoomAreaByRoomId(roomId);
		if (roomAreaByRoomId != null && !roomFogMaskCache.ContainsKey(roomId))
		{
			if (fogMasks.ContainsKey(mapId))
			{
				fogMasks[mapId].DrawArea(roomAreaByRoomId.AreaOffsetWithPadding, roomAreaByRoomId.AreaSizeWithPadding, DolocColor.empty);
			}
			if (visibleRoomMasks.ContainsKey(mapId))
			{
				visibleRoomMasks[mapId].DrawArea(roomAreaByRoomId.AreaOffset, roomAreaByRoomId.AreaSize, DolocColor.empty);
			}
		}
	}

	public bool IsPortalVisited(string portalId)
	{
		return visitedPortals.Contains(portalId);
	}

	public bool SetPortalVisited(string portalId)
	{
		return visitedPortals.Add(portalId);
	}
}
