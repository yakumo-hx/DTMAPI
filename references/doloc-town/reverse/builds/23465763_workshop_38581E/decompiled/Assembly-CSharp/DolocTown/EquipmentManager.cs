using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class EquipmentManager
{
	private static readonly Dictionary<string, Type> typeLut = EquipmentUtils.LoadEquipmentTypes();

	[JsonProperty]
	private readonly IndexList<Equipment> equipments = new IndexList<Equipment>();

	private readonly Dictionary<Type, List<Equipment>> searchListFuncType = new Dictionary<Type, List<Equipment>>();

	private readonly List<Equipment> removeBuffer = new List<Equipment>();

	public bool IsUpdating { get; private set; }

	public IEnumerable<Equipment> AllEquipments => equipments;

	public int Count => equipments.Count;

	public EquipmentManager()
	{
	}

	[JsonConstructor]
	protected EquipmentManager(IndexList<Equipment> equipments)
	{
		this.equipments = equipments;
		if (equipments == null)
		{
			return;
		}
		foreach (Equipment equipment in equipments)
		{
			Type type = equipment.GetType();
			searchListFuncType.TryAdd(type, new List<Equipment>());
			searchListFuncType[type].Add(equipment);
		}
	}

	public void Update()
	{
		IsUpdating = true;
		foreach (Equipment equipment in equipments)
		{
			equipment.DecoratedUpdate();
		}
		IsUpdating = false;
		FlushRemoveBuffer();
	}

	public void UpdateNoRender()
	{
		IsUpdating = true;
		foreach (Equipment equipment in equipments)
		{
			equipment.DecoratedUpdateNoRender();
		}
		IsUpdating = false;
		FlushRemoveBuffer();
	}

	public void FlushRemoveBuffer()
	{
		if (removeBuffer.Count <= 0)
		{
			return;
		}
		foreach (Equipment item in removeBuffer)
		{
			item.Host.RemoveEquipment(item);
		}
		removeBuffer.Clear();
	}

	public void AddRemoveBuffer(Equipment equipment)
	{
		if (equipment != null && !removeBuffer.Contains(equipment))
		{
			Debug.Log("设备加入移除缓冲区\"" + equipment.Name + "\"");
			removeBuffer.Add(equipment);
		}
	}

	public Equipment CreateEquipmentFromDirty(IEquipmentHost host, Vector3 wp, Vector2Int anchor, Equipment dirty, bool turn)
	{
		Type type = dirty.GetType();
		dirty.MoveTerrainContent(anchor, wp);
		if (!searchListFuncType.ContainsKey(type))
		{
			searchListFuncType.Add(type, new List<Equipment>());
		}
		dirty.SetHost(host);
		dirty.Turn = turn;
		searchListFuncType[type].Add(dirty);
		equipments.Add(dirty);
		return dirty;
	}

	public Equipment CreateEquipment(IEquipmentHost host, Vector3 wp, Vector2Int anchor, EquipmentInfo proto, bool turn)
	{
		string name = proto.Function.GetType().Name;
		if (!typeLut.TryGetValue(name, out var value))
		{
			DolocAPI.outputError("未知的设备类型:" + name);
			return null;
		}
		Equipment equipment = (Equipment)Activator.CreateInstance(value, host, -1, proto, wp, anchor, turn);
		if (!searchListFuncType.ContainsKey(value))
		{
			searchListFuncType.Add(value, new List<Equipment>());
		}
		searchListFuncType[value].Add(equipment);
		equipments.Add(equipment);
		return equipment;
	}

	public bool ValidateEquipmentType(Equipment equipment)
	{
		if (equipment == null)
		{
			return false;
		}
		string name = equipment.proto.Function.GetType().Name;
		if (!typeLut.TryGetValue(name, out var value))
		{
			return false;
		}
		return equipment.GetType() == value;
	}

	public static Equipment CreateDisposeEquipment(IEquipmentHost host, EquipmentInfo proto, bool turn)
	{
		string name = proto.Function.GetType().Name;
		if (!typeLut.TryGetValue(name, out var value))
		{
			DolocAPI.outputError("未知的设备类型:" + name);
			return null;
		}
		return (Equipment)Activator.CreateInstance(value, host, -1, proto, Vector3.zero, Vector2Int.zero, turn);
	}

	public void RemoveEquipment(Equipment equipment)
	{
		equipments.Remove(equipment);
		searchListFuncType[equipment.GetType()].Remove(equipment);
	}

	public IEnumerable<T> ForEachEquipments<T>() where T : Equipment
	{
		if (!searchListFuncType.TryGetValue(typeof(T), out var value))
		{
			yield break;
		}
		foreach (Equipment item in value)
		{
			yield return (T)item;
		}
	}

	public Equipment GetEquipment(int id)
	{
		return equipments[id];
	}

	public T[] GetEquipments<T>() where T : Equipment
	{
		List<T> list = new List<T>();
		Type targetType = typeof(T);
		foreach (Type item in searchListFuncType.Keys.Where((Type type) => targetType.IsAssignableFrom(type)))
		{
			list.AddRange(searchListFuncType[item].Cast<T>());
		}
		return list.ToArray();
	}

	public int CountEquipment<T>() where T : Equipment
	{
		if (!searchListFuncType.TryGetValue(typeof(T), out var value))
		{
			return 0;
		}
		return value.Count;
	}

	public int CountEquipment(string name)
	{
		return equipments.Count((Equipment x) => x.Name == name);
	}

	public T GetEquipment<T>() where T : Equipment
	{
		if (searchListFuncType.TryGetValue(typeof(T), out var value) && value.Count > 0)
		{
			return (T)value[0];
		}
		return null;
	}

	public T GetEquipment<T>(Func<T, bool> condition) where T : Equipment
	{
		if (condition == null)
		{
			return GetEquipment<T>();
		}
		if (searchListFuncType.TryGetValue(typeof(T), out var value))
		{
			foreach (T item in value.Cast<T>())
			{
				if (condition(item))
				{
					return item;
				}
			}
		}
		return null;
	}

	public Equipment GetRandomEquipment()
	{
		return equipments.RandomSample();
	}

	public int GetEquipmentCount(string id)
	{
		if (id.IsNullOrEmpty())
		{
			return 0;
		}
		return equipments.Count((Equipment x) => x.Name == id);
	}

	public void Clear()
	{
		equipments.Clear();
		searchListFuncType.Clear();
	}
}
