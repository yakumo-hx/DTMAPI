using System;
using System.Collections.Generic;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using RedSaw.AI.LinearTask;
using RedSaw.CommandLineInterface;

namespace DolocTown;

[DebugObject]
public class AnimalController : DecisionMaker
{
	private class RoomSearcher
	{
		private readonly Queue<Room> unvisitedRooms = new Queue<Room>();

		public bool HasUnvisitedRooms => unvisitedRooms.Count > 0;

		public Room NextRoom
		{
			get
			{
				if (unvisitedRooms.Count <= 0)
				{
					return null;
				}
				return unvisitedRooms.Dequeue();
			}
		}

		public void ResetStatus(Animal animal, WeatherType weatherType)
		{
			unvisitedRooms.Clear();
			if (weatherType.IsMalignantWeather())
			{
				if (!animal.IsInHome)
				{
					unvisitedRooms.Enqueue(animal.homeRoom);
				}
				return;
			}
			Room anotherRoom = animal.AnotherRoom;
			if (anotherRoom != null)
			{
				unvisitedRooms.Enqueue(anotherRoom);
			}
		}
	}

	private readonly Animal animal;

	private readonly Dictionary<Type, RoomSearcher> _roomSearchers;

	private readonly AnimalAI animalAI;

	public string CurrentStateName => animalAI.currentState?.GetType().Name ?? "无状态";

	private static Dictionary<Type, RoomSearcher> InitRoomSearchers(Animal animal, WeatherType weatherType)
	{
		Dictionary<Type, RoomSearcher> dictionary = new Dictionary<Type, RoomSearcher>
		{
			{
				typeof(IFeeder),
				new RoomSearcher()
			},
			{
				typeof(IAnimalToilet),
				new RoomSearcher()
			},
			{
				typeof(IAnimalHoneyComb),
				new RoomSearcher()
			},
			{
				typeof(IAnimalLivestockNursery),
				new RoomSearcher()
			}
		};
		foreach (RoomSearcher value in dictionary.Values)
		{
			value.ResetStatus(animal, weatherType);
		}
		return dictionary;
	}

	public AnimalController(Animal animal)
	{
		this.animal = animal;
		animalAI = AnimalAI.Create(animal.proto.ScheduleId, animal);
		_roomSearchers = InitRoomSearchers(animal, DolocAPI.archiveHandle.CurrentWeatherType);
	}

	public bool TryGetAnotherRoom<T>(out Room room) where T : IAnimalInteractable
	{
		room = null;
		if (!_roomSearchers.TryGetValue(typeof(T), out var value))
		{
			return false;
		}
		if (!value.HasUnvisitedRooms)
		{
			return false;
		}
		room = value.NextRoom;
		return true;
	}

	public void ResetRoomSearcherStatus<T>() where T : IAnimalInteractable
	{
		if (_roomSearchers.TryGetValue(typeof(T), out var value))
		{
			value.ResetStatus(animal, DolocAPI.archiveHandle.CurrentWeatherType);
		}
	}

	protected override LinearTask MakeDecision()
	{
		if (animal.currentRoom == null)
		{
			return LinearTask.Defalut;
		}
		return _MakeDecision().Root.WrapAnimalTask(animal);
	}

	private LinearTask _MakeDecision()
	{
		return animalAI?.MakeDecision() ?? LinearTask.Defalut;
	}

	protected override void OnUpdate(float dt)
	{
		animalAI?.Update(dt);
	}
}
