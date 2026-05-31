using System;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

public class DebugFieldOfFieldInfo : DebugField
{
	public readonly FieldInfo _field;

	public override Type FieldType => _field.FieldType;

	public override bool IsReadonly => false;

	public override object Value
	{
		get
		{
			if (instance == null)
			{
				return null;
			}
			if (_field.IsStatic)
			{
				return _field.GetValue(null);
			}
			return _field.GetValue(instance);
		}
	}

	public DebugFieldOfFieldInfo(object instance, FieldInfo field)
		: base(instance, field)
	{
		_field = field;
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
			if (_field.FieldType.IsEnum)
			{
				value = (T)_field.GetValue(instance);
				return true;
			}
			return false;
		}
		if (typeof(T).IsAssignableFrom(_field.FieldType))
		{
			value = (T)_field.GetValue(instance);
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
		if (_field.FieldType.IsEnum)
		{
			_field.SetEnumValueFromInt(instance, value.GetHashCode());
			return;
		}
		if (!_field.FieldType.IsAssignableFrom(typeof(T)))
		{
			throw new InvalidCastException($"Cannot assign value of type {typeof(T)} to field of type {_field.FieldType}");
		}
		_field.SetValue(_field.IsStatic ? null : instance, value);
		OnValueChanged(value);
	}
}
