using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Weather;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class AnimalSystem
{
	private readonly List<Animal> _animals = new List<Animal>();

	private readonly Queue<(Animal, bool)> _changeList = new Queue<(Animal, bool)>();

	private bool _isLocked;

	public int TotalCount => _animals.Count;

	public IEnumerable<Animal> Animals => _animals;

	[Command("debug_current_animal_system_infos")]
	public static void OutputCurrentAnimalSystemInfos()
	{
		Room currentRoom = DolocAPI.CurrentRoom;
		Debug.Log($"当前动物系统管理器中的动物数量:{currentRoom.CurrentRoom.animalSystem.TotalCount}");
		foreach (Animal animal in currentRoom.animalSystem.Animals)
		{
			Debug.Log(animal.ToString());
		}
	}

	public void SetCurrentRoom(Room room)
	{
		if (room == null)
		{
			foreach (Animal item in _animals.Where((Animal x) => x.IsRender))
			{
				item.Renderer = null;
			}
			return;
		}
		foreach (Animal animal in _animals)
		{
			animal.RefreshRenderer();
		}
	}

	public void BeforePassTime()
	{
		foreach (Animal animal in _animals)
		{
			animal.Renderer = null;
			animal.isPassingTime = true;
		}
	}

	public void AfterPassTime()
	{
		foreach (Animal animal in _animals)
		{
			animal.isPassingTime = false;
			animal.RefreshRenderer();
		}
	}

	public bool ContainsAnimal(Animal animal)
	{
		return _animals.Contains(animal);
	}

	public void AddAnimal(Animal animal)
	{
		if (animal != null && !_animals.Contains(animal))
		{
			if (_isLocked)
			{
				_changeList.Enqueue((animal, true));
			}
			else
			{
				_animals.Add(animal);
			}
		}
	}

	public void RemoveAnimal(Animal animal)
	{
		if (animal != null && _animals.Contains(animal))
		{
			if (_isLocked)
			{
				_changeList.Enqueue((animal, false));
			}
			else
			{
				_animals.Remove(animal);
			}
		}
	}

	public void Rebuild(IEnumerable<Animal> animals)
	{
		foreach (Animal animal in animals)
		{
			_animals.Add(animal);
		}
	}

	private void _CommitChangeList()
	{
		if (_isLocked)
		{
			return;
		}
		while (_changeList.Count > 0)
		{
			(Animal, bool) tuple = _changeList.Dequeue();
			var (item, _) = tuple;
			if (tuple.Item2)
			{
				if (!_animals.Contains(item))
				{
					_animals.Add(item);
				}
			}
			else if (_animals.Contains(item))
			{
				_animals.Remove(item);
			}
		}
	}

	public void Update()
	{
		if (_animals.Count == 0)
		{
			return;
		}
		_isLocked = true;
		foreach (Animal animal in _animals)
		{
			animal.Update();
		}
		_isLocked = false;
		_CommitChangeList();
	}

	public void UpdateNoRender()
	{
		if (_animals.Count == 0)
		{
			return;
		}
		_isLocked = true;
		foreach (Animal animal in _animals)
		{
			animal.UpdateNoRender();
		}
		_isLocked = false;
		_CommitChangeList();
	}

	public void OnDayChanged()
	{
		Queue<Animal> queue = new Queue<Animal>();
		foreach (Animal animal2 in _animals)
		{
			animal2.__OnDayChanged();
			if (animal2.isEscaped)
			{
				queue.Enqueue(animal2);
			}
		}
		bool flag = queue.Count > 0;
		int count = queue.Count;
		while (queue.Count > 0)
		{
			Animal animal = queue.Dequeue();
			((IAnimalHost)animal.homeRoom).RemoveAnimal(animal);
		}
		if (flag)
		{
			DolocAPI.ShowMessageBoxSmallErr(string.Format(DolocConfig.StaticTexts.AnimalHasEscaped, count));
		}
	}

	public void SendMessage(AnimalEvent evt)
	{
		foreach (Animal animal in _animals)
		{
			animal.OnReceiveEvent(evt);
		}
	}

	public void OnWeatherChanged(WeatherType weatherType)
	{
		foreach (Animal animal in _animals)
		{
			animal.__OnWeatherChanged(weatherType);
		}
	}

	public void RefreshEnv(Room currentRoom)
	{
		AnimalUtils.PathFinderManager.OnEnvChanged(currentRoom);
		if (!currentRoom.IsInHouse)
		{
			AnimalUtils.AnimalMapForBuilding.Refresh();
		}
		foreach (Animal animal in _animals)
		{
			animal.RefreshCurrentEnv();
		}
	}

	public void OnFenceChanged(Room room)
	{
		AnimalUtils.PathFinderManager.OnEnvChanged(room);
		foreach (Animal animal in _animals)
		{
			animal.__OnFenceChanged(room);
		}
	}

	public void OnPlatformChanged(Room room)
	{
		AnimalUtils.PathFinderManager.OnEnvChanged(room);
		foreach (Animal animal in _animals)
		{
			animal.__OnPlatformChanged(room);
		}
	}

	public void OnTerrainExtent(Room room, Vector2Int offset)
	{
		Debug.Log("扩展农场：重构小动物活动区域");
		AnimalUtils.PathFinderManager.OnEnvChanged(room);
		AnimalUtils.AnimalMapForBuilding.Refresh();
		foreach (Animal animal in _animals)
		{
			animal.__OnTerrainExtent(room, offset);
		}
	}

	public void OnBuildingChanged(Room room)
	{
		AnimalUtils.PathFinderManager.OnEnvChanged(room);
		AnimalUtils.AnimalMapForBuilding.Refresh();
		foreach (Animal animal in _animals)
		{
			animal.__OnBuildingChanged(room);
		}
	}
}
