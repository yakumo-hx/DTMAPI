using System;
using DolocTown.GameData;
using DolocTown.Utils;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public static class AnimalTaskHelper
{
	public static LinearTask WrapAnimalTask(this LinearTask linearTask, Animal entity)
	{
		linearTask.ForEach(delegate(LinearTask task)
		{
			if (task is AnimalTask animalTask)
			{
				animalTask.SetAnimal(entity);
			}
		});
		return linearTask;
	}

	private static Room GetRoom(this Animal animal, AnimalRoomState roomState)
	{
		return roomState switch
		{
			AnimalRoomState.Home => animal.homeRoom, 
			AnimalRoomState.Farm => DolocAPI.archiveHandle.MainFarm, 
			AnimalRoomState.AnotherRoom => animal.AnotherRoom, 
			_ => null, 
		};
	}

	private static bool _IsRoomClosed(Room currentRoom)
	{
		if (currentRoom is TemplateRoomInHouse templateRoomInHouse)
		{
			return templateRoomInHouse.Building.isClosed;
		}
		return false;
	}

	public static bool GenTask_JourneyToPosition(this Animal animal, Vector2Int from, Vector2Int to, out LinearTask task)
	{
		task = null;
		if (from == to)
		{
			task = LinearTask.WaitFrames(1);
			return true;
		}
		if (!AnimalUtils.CanArrive(animal.currentRoom, from, to, animal.width, out var path))
		{
			return false;
		}
		task = LinearTask.StartWith.AnimalMoveTaskSequence(path);
		return true;
	}

	public static bool GenTask_JourneyToPosition(this Animal animal, Vector2Int targetPosition, out LinearTask task)
	{
		task = null;
		if (animal.positionCell == targetPosition)
		{
			task = LinearTask.WaitFrames(1);
			return true;
		}
		if (!AnimalUtils.CanArrive(animal.currentRoom, animal.positionCell, targetPosition, animal.width, out var path))
		{
			return false;
		}
		task = LinearTask.StartWith.AnimalMoveTaskSequence(path);
		return true;
	}

	public static bool GenTask_JourneyToRoom(this Animal animal, Room nextRoom, bool force, out LinearTask task)
	{
		task = null;
		if (_IsRoomClosed(animal.currentRoom))
		{
			return false;
		}
		if (!AnimalUtils.GetTransitionPoint(animal.currentRoom, nextRoom, out var pt))
		{
			task = (force ? new AnimalEnterRoom(nextRoom) : null);
			return task != null;
		}
		if (!AnimalUtils.CanArrive(animal.currentRoom, animal.positionCell, pt, animal.width, out var path))
		{
			task = (force ? new AnimalEnterRoom(nextRoom) : null);
			return task != null;
		}
		task = LinearTask.StartWith.AnimalMoveTaskSequence(path).AnimalEnterRoom(nextRoom);
		return true;
	}

	public static bool GenTask_JourneyToRoom(this Animal animal, AnimalRoomState roomState, bool force, out LinearTask task, Func<Room, bool> condition = null)
	{
		task = null;
		Room room = animal.GetRoom(roomState);
		if (room == null || animal.currentRoom == room || (condition != null && !condition(room)))
		{
			return false;
		}
		return animal.GenTask_JourneyToRoom(room, force, out task);
	}

	public static bool GenTask_JourneyToHomeRoom(this Animal animal, bool force, out LinearTask task)
	{
		return animal.GenTask_JourneyToRoom(AnimalRoomState.Home, force, out task);
	}

	public static LinearTask AnimalMoveTaskSequence(this LinearTask task, Vector2Int[] path)
	{
		if (path == null || path.Length <= 1)
		{
			return task;
		}
		Vector2Int vector2Int = path[0];
		for (int i = 1; i < path.Length; i++)
		{
			Vector2Int vector2Int2 = path[i];
			task = ((vector2Int2.y == vector2Int.y) ? task.Then(new AnimalMove(vector2Int2)) : task.Then(new AnimalJump(vector2Int2)));
			vector2Int = vector2Int2;
		}
		return task;
	}

	public static LinearTask AnimalEat(this LinearTask task, IFeeder feeder, bool playAnimation = true)
	{
		return task.Then(new AnimalEat(feeder, playAnimation));
	}

	public static LinearTask AnimalEmotion(this LinearTask task, EmotionName name)
	{
		return task.Then(new AnimalEmotion(name));
	}

	public static LinearTask Wink(this LinearTask task, int duration)
	{
		return task.Then(new AnimalWink(Mathf.Max(1, duration)));
	}

	public static LinearTask AnimalEnterRoom(this LinearTask task, Room room)
	{
		return task.Then(new AnimalEnterRoom(room));
	}

	public static LinearTask AnimalExcrete(this LinearTask task, IAnimalToilet toilet)
	{
		return task.Then(new AnimalExcrete(toilet));
	}
}
