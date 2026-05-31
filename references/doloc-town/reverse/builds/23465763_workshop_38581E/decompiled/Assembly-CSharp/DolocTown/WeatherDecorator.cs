using System;
using System.Collections.Generic;
using DolocTown.Config.Equipment;
using DolocTown.Config.Weather;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;

namespace DolocTown;

[DebugObject]
[JsonObject(MemberSerialization.OptIn)]
public abstract class WeatherDecorator
{
	private static readonly Dictionary<string, Type> DecoratorTypes = LoadDecoratorTypes();

	private WeatherDecoratorProto proto;

	protected Equipment.WeatherDecoratorManager parent;

	public abstract WeatherType WeatherType { get; }

	[DebugInfo("是否正在装饰设备", Color = "#ff4f4f")]
	public bool IsDecorating
	{
		get
		{
			if (parent != null)
			{
				return parent.CheckDecorator(this);
			}
			return false;
		}
	}

	public WeatherDecoratorProto Proto => proto;

	public bool IsValid => proto != null;

	protected Equipment Equipment => parent.Equipment;

	protected EquipmentRenderer EquipmentRenderer => parent.Equipment.Renderer;

	public virtual bool EndOnWeatherStop => true;

	public virtual bool CallOriginWhileNoOverride => false;

	private static Dictionary<string, Type> LoadDecoratorTypes()
	{
		Type[] subTypes = typeof(WeatherDecorator).GetSubTypes();
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		Type[] array = subTypes;
		foreach (Type type in array)
		{
			dictionary.Add(type.Name.Replace("WD", "WDP"), type);
		}
		return dictionary;
	}

	public static Type GetDecoratorProtoType(Type type)
	{
		string key = type.Name.Replace("WD", "WDP");
		return DecoratorTypes.GetValueOrDefault(key);
	}

	public static bool CreateDecorator(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto, out WeatherDecorator decorator)
	{
		if (!DecoratorTypes.TryGetValue(proto.GetType().Name, out var value))
		{
			decorator = null;
			return false;
		}
		try
		{
			decorator = (WeatherDecorator)Activator.CreateInstance(value, parent, proto);
		}
		catch (Exception)
		{
			decorator = null;
			return false;
		}
		return true;
	}

	public static bool CheckProtoInstance(string type)
	{
		return DecoratorTypes.ContainsKey(type);
	}

	public WeatherDecorator(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
	{
		this.parent = parent;
	}

	[JsonConstructor]
	protected WeatherDecorator()
	{
	}

	public virtual void RetrieveProto(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
	{
		this.parent = parent;
		this.proto = proto;
	}

	public virtual void AfterLoadData()
	{
	}

	public virtual void AfterResumeDecorator()
	{
	}

	public virtual void Begin()
	{
	}

	public virtual void BeginNoRender()
	{
	}

	public virtual void End()
	{
	}

	public virtual void EndNoRender()
	{
	}

	public virtual void Update(bool inProgress)
	{
		if (CallOriginWhileNoOverride)
		{
			parent.OriginUpdate();
		}
	}

	public virtual void UpdateNoRender(bool inProgress)
	{
		if (CallOriginWhileNoOverride)
		{
			parent.OriginUpdateNoRender();
		}
	}

	public virtual void OnRender()
	{
		if (CallOriginWhileNoOverride)
		{
			parent.OriginOnRender();
		}
	}

	public virtual void OnUnRender()
	{
		if (CallOriginWhileNoOverride)
		{
			parent.OriginOnUnRender();
		}
	}

	public virtual void OnTouch()
	{
		if (CallOriginWhileNoOverride)
		{
			parent.OriginOnTouch();
		}
	}

	public virtual void OnDisTouch()
	{
		if (CallOriginWhileNoOverride)
		{
			parent.OriginOnDisTouch();
		}
	}

	public virtual void OnInteract()
	{
		if (CallOriginWhileNoOverride)
		{
			parent.OriginOnInteract();
		}
	}
}
