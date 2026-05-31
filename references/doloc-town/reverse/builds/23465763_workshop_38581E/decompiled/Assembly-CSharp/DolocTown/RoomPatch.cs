using DolocTown.Config;
using DolocTown.Config.Weather;

namespace DolocTown;

public static class RoomPatch
{
	public static WeatherInfo GetWeatherInfo(this Room room)
	{
		WeatherInfo weatherInfo = DolocAPI.archiveHandle.timeData.weather.WeatherInfo;
		if (room is DungeonRoom dungeonRoom)
		{
			DolocConfig.Tables.TbDungeonSeason.DataMap.ContainsKey(dungeonRoom.dungeonProtoName);
		}
		return weatherInfo;
	}

	public static void HandleBackground(this Room room)
	{
		bool shouldShowBackground = room.ShouldShowBackground;
		DolocAPI.envBackgroundEx.SetVisible(room.ShouldShowBackground);
		if (shouldShowBackground)
		{
			DolocAPI.LoadBackground(room.baseProto.background);
		}
	}

	public static bool QueryBuildingRoom(this Room room, string guid, out Room buildingRoom)
	{
		buildingRoom = null;
		if (room.IsInHouse || room.DM_building.Count == 0 || string.IsNullOrEmpty(guid))
		{
			return false;
		}
		foreach (Building building in room.DM_building.Buildings)
		{
			if (!(building.room.Title != guid))
			{
				buildingRoom = building.room;
				return true;
			}
		}
		return false;
	}
}
