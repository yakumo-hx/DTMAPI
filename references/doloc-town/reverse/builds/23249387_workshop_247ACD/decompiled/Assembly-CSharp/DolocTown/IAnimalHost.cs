using System.Linq;
using DolocTown.Config.Animal;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public interface IAnimalHost : IBaseHost
{
	AnimalSystem animalSystem { get; }

	[JsonProperty]
	AnimalManager DM_animal { get; }

	int TotalAnimalSpace => DM_animal.AllAnimals.Sum((Animal animal) => animal.proto.Space);

	int MaxAnimalSpace
	{
		get
		{
			if (!CurrentRoom.IsInHouse)
			{
				return 0;
			}
			Building building = ((TemplateRoomInHouse)CurrentRoom).Building;
			if (!building.proto.IsAnimalBuilding)
			{
				return 0;
			}
			return building.proto.AnimalSpace;
		}
	}

	bool IsClosed
	{
		get
		{
			if (CurrentRoom is TemplateRoomInHouse templateRoomInHouse)
			{
				return templateRoomInHouse.Building.isClosed;
			}
			return false;
		}
	}

	void SetClosed(bool value)
	{
		if (CurrentRoom is TemplateRoomInHouse templateRoomInHouse)
		{
			templateRoomInHouse.Building.SetClosed(value);
		}
	}

	bool CheckAnimalSpace(int requireSpace)
	{
		if (!CurrentRoom.IsInHouse)
		{
			return true;
		}
		Building building = ((TemplateRoomInHouse)CurrentRoom).Building;
		if (!building.proto.IsAnimalBuilding)
		{
			return false;
		}
		return TotalAnimalSpace + requireSpace <= building.proto.AnimalSpace;
	}

	bool AddAnimal(Animal animal, Vector2Int pos)
	{
		if (animal == null)
		{
			return false;
		}
		if (DM_animal.ContainsAnimal(animal))
		{
			return false;
		}
		if (animalSystem.ContainsAnimal(animal))
		{
			return false;
		}
		if (!CheckAnimalSpace(animal.proto.Space))
		{
			return false;
		}
		if (!AnimalUtils.IsPositionWalkable(CurrentRoom, pos, animal.width))
		{
			pos = AnimalUtils.GetNearestValidPosition(CurrentRoom, pos, animal.width);
		}
		DM_animal.AddAnimal(animal);
		animalSystem.AddAnimal(animal);
		animal.SetHomeRoom(CurrentRoom);
		animal.SetCurrentRoom(CurrentRoom, pos);
		animal.RefreshRenderer();
		return true;
	}

	Animal CreateAnimal(AnimalInfo proto, Vector2Int pos, bool shouldRender = true)
	{
		if (proto == null)
		{
			return null;
		}
		if (!CheckAnimalSpace(proto.Space))
		{
			return null;
		}
		if (!AnimalUtils.IsPositionWalkable(CurrentRoom, pos, proto.Size))
		{
			return null;
		}
		DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
		Animal animal = DM_animal.CreateAnimal(proto, dateNow);
		if (animal == null)
		{
			return null;
		}
		Debug.Log("添加小动物至全局系统");
		animalSystem.AddAnimal(animal);
		animal.SetHomeRoom(CurrentRoom);
		animal.SetCurrentRoom(CurrentRoom, pos);
		if (shouldRender)
		{
			animal.RefreshRenderer();
		}
		return animal;
	}

	bool RemoveAnimal(Animal animal)
	{
		if (animal == null)
		{
			return false;
		}
		if (!DM_animal.RemoveAnimal(animal))
		{
			Debug.LogError($"无法从小动物管理器中移除小动物\"{animal}");
			return false;
		}
		Debug.Log($"从系统中移除小动物\"{animal}\"");
		animal.controller.StopTask();
		animalSystem.RemoveAnimal(animal);
		if (animal.Renderer != null)
		{
			animal.Renderer.OnDisTouch();
			animal.Renderer = null;
		}
		return true;
	}

	int CountAnimal(string name, bool isAdult)
	{
		return DM_animal.AllAnimals.Count((Animal a) => a.IsAdult && a.protoName == name);
	}

	void __AfterLoadAnimals()
	{
		animalSystem.Rebuild(CurrentRoom.AllAnimals);
		foreach (Animal allAnimal in DM_animal.AllAnimals)
		{
			allAnimal.AfterLoadData();
		}
	}
}
