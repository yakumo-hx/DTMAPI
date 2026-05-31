using DolocTown.Config.Weather;
using DolocTown.GameData;
using DolocTown.Utils;
using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

[Name("进入房间", 0)]
public class AnimalWork_EnterRoom : AnimalWork
{
	[SerializeField]
	private AnimalRoomState _roomState = AnimalRoomState.Farm;

	[SerializeField]
	private bool force;

	[SerializeField]
	private bool requireSunny;

	[SerializeField]
	private bool requireNormalWeather;

	public override string Title
	{
		get
		{
			string text = (force ? "<color=grey>强制</color>" : "");
			return text + "进入" + _roomState switch
			{
				AnimalRoomState.Home => "<b>家园</b>", 
				AnimalRoomState.Farm => "<b>农场</b>", 
				AnimalRoomState.AnotherRoom => "<b>另外房间</b>", 
				_ => "未知房间", 
			};
		}
	}

	private bool RoomCondition(Room room)
	{
		if (_roomState != AnimalRoomState.AnotherRoom)
		{
			return true;
		}
		if (room.IsInHouse)
		{
			return true;
		}
		WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
		if (requireSunny && currentWeatherType.IsRainyWeather())
		{
			return false;
		}
		if (requireNormalWeather && currentWeatherType.IsMalignantWeather())
		{
			return false;
		}
		return true;
	}

	public override bool GenTask(Animal animal, out LinearTask task)
	{
		task = null;
		if (_roomState == AnimalRoomState.Home && animal.IsInHome)
		{
			return false;
		}
		return animal.GenTask_JourneyToRoom(_roomState, force, out task, RoomCondition);
	}
}
