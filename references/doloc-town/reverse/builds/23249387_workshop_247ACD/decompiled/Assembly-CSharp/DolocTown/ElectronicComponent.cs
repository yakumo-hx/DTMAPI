using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public abstract class ElectronicComponent : IElectronicComponent
{
	private static readonly Dictionary<string, Type> ProtoToEntityMap = LoadProtoToEntityMap();

	protected Equipment equipment;

	public static ElectronicComponent CreateComponent(Equipment equipment)
	{
		if (!equipment.proto.isElectrical)
		{
			return null;
		}
		ElectronicComponentProto electronicComponent = equipment.proto.ElectronicComponent;
		if (!ProtoToEntityMap.TryGetValue(electronicComponent.GetType().Name, out var value))
		{
			return null;
		}
		return (ElectronicComponent)Activator.CreateInstance(value, equipment);
	}

	public static ElectronicComponent ValidateComponent(ElectronicComponent old, Equipment equipment)
	{
		if (!equipment.IsElectric)
		{
			return null;
		}
		if (old == null)
		{
			return CreateComponent(equipment);
		}
		ElectronicComponentProto electronicComponent = equipment.proto.ElectronicComponent;
		if (ProtoToEntityMap.TryGetValue(electronicComponent.GetType().Name, out var value) && old.GetType() == value)
		{
			old.SetEquipment(equipment);
			return old;
		}
		Debug.Log("设备\"" + equipment.Name + "\"的电力元件原型发生了变化，将重新创建电力元件类型\"" + equipment.proto.ElectronicComponent.GetType().Name + "\"");
		return CreateComponent(equipment);
	}

	private static Dictionary<string, Type> LoadProtoToEntityMap()
	{
		Type baseClass = typeof(ElectronicComponent);
		IEnumerable<Type> enumerable = from x in Assembly.GetExecutingAssembly().GetTypes()
			where x.IsSubclassOf(baseClass)
			where !x.IsAbstract
			select x;
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		foreach (Type item in enumerable)
		{
			dictionary.Add(item.Name.Replace("ElectronicComponent", "EComProto"), item);
		}
		return dictionary;
	}

	public static bool CheckProtoInstance(string type)
	{
		return ProtoToEntityMap.ContainsKey(type);
	}

	protected ElectronicComponent(Equipment equipment)
	{
		this.equipment = equipment;
	}

	[JsonConstructor]
	protected ElectronicComponent()
	{
		equipment = null;
	}

	public virtual void SetEquipment(Equipment equipment)
	{
		this.equipment = equipment;
	}

	public virtual void AfterLoadElectricComponent()
	{
	}

	public virtual void OnHostChanged()
	{
	}
}
