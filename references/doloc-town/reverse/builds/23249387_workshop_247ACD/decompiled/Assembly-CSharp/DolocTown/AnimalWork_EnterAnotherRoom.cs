using DolocTown.Config.Weather;
using DolocTown.GameData;
using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

[Name("进入另外的房间", 0)]
[Description("为了独特的目的进入另外的房间")]
public class AnimalWork_EnterAnotherRoom : AnimalWork
{
	private enum AnotherRoomType
	{
		None,
		IFeeder,
		IToilet,
		ILivestockNursery,
		IHoneyComb
	}

	[SerializeField]
	private AnotherRoomType _anotherRoomType;

	[SerializeField]
	private bool requireSunny;

	[SerializeField]
	private bool requireNormalWeather;

	public override string Title => "进入另外房间" + _anotherRoomType switch
	{
		AnotherRoomType.None => "<b>(无约束)</b>", 
		AnotherRoomType.IFeeder => "<b>(饲料槽)</b>", 
		AnotherRoomType.IToilet => "<b>(厕所)</b>", 
		AnotherRoomType.ILivestockNursery => "<b>(繁育室)</b>", 
		_ => "(未知)", 
	};

	private Room GetAnotherRoom(Animal animal)
	{
		Room room;
		Room room2;
		Room room3;
		Room room4;
		return _anotherRoomType switch
		{
			AnotherRoomType.None => animal.AnotherRoom, 
			AnotherRoomType.IFeeder => animal.controller.TryGetAnotherRoom<IFeeder>(out room) ? room : null, 
			AnotherRoomType.IToilet => animal.controller.TryGetAnotherRoom<IAnimalToilet>(out room2) ? room2 : null, 
			AnotherRoomType.ILivestockNursery => animal.controller.TryGetAnotherRoom<IAnimalLivestockNursery>(out room3) ? room3 : null, 
			AnotherRoomType.IHoneyComb => animal.controller.TryGetAnotherRoom<IAnimalHoneyComb>(out room4) ? room4 : null, 
			_ => null, 
		};
	}

	private bool RoomCondition(Room room)
	{
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
		Room anotherRoom = GetAnotherRoom(animal);
		if (anotherRoom == null || animal.currentRoom == anotherRoom)
		{
			return false;
		}
		if (RoomCondition(anotherRoom))
		{
			return animal.GenTask_JourneyToRoom(anotherRoom, force: true, out task);
		}
		return false;
	}
}
