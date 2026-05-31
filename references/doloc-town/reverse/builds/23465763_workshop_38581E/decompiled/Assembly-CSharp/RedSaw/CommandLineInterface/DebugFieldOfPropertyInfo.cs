using System;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

public class DebugFieldOfPropertyInfo : DebugField
{
	public readonly PropertyInfo _property;

	public override Type FieldType => _property.PropertyType;

	public override bool IsReadonly => !_property.CanWrite;

	public override object Value
	{
		get
		{
			if (instance == null)
			{
				return null;
			}
			if (_property.GetIndexParameters().Length != 0)
			{
				return null;
			}
			return _property.GetValue(instance);
		}
	}

	public DebugFieldOfPropertyInfo(object instance, PropertyInfo property)
		: base(instance, property)
	{
		_property = property;
	}

	public override bool TryGetValue<T>(out T value)
	{
		value = default(T);
		if (instance == null)
		{
			return false;
		}
		if (typeof(T) == typeof(Enum))
		{
			if (_property.PropertyType.IsEnum)
			{
				value = (T)_property.GetValue(instance);
				return true;
			}
			return false;
		}
		if (typeof(T).IsAssignableFrom(_property.PropertyType))
		{
			value = (T)_property.GetValue(instance);
			return true;
		}
		return false;
	}

	public override void TrySetValue<T>(T value)
	{
		if (instance == null)
		{
			return;
		}
		if (_property.PropertyType.IsEnum)
		{
			_property.SetEnumValueFromInt(instance, value.GetHashCode());
			return;
		}
		if (!_property.PropertyType.IsAssignableFrom(typeof(T)))
		{
			throw new InvalidCastException($"Cannot assign value of type {typeof(T)} to property of type {_property.PropertyType}");
		}
		if (_property.CanWrite)
		{
			_property.SetValue(instance, value);
			OnValueChanged(value);
			return;
		}
		throw new InvalidOperationException("Property " + _property.Name + " is read-only.");
	}
}
