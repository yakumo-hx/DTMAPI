using System;
using UnityEngine.Events;

public class BindableValue<T> where T : IEquatable<T>
{
	private T _value;

	public UnityEvent<T> OnValueChanged = new UnityEvent<T>();

	public T Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (!object.Equals(value, _value))
			{
				_value = value;
				OnValueChanged?.Invoke(value);
			}
		}
	}

	public BindableValue(T value)
	{
		_value = value;
	}

	public BindableValue()
	{
		_value = default(T);
	}
}
