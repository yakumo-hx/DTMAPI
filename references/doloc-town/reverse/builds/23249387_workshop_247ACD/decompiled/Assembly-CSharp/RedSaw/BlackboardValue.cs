using System;

namespace RedSaw;

public class BlackboardValue
{
	public readonly Type type;

	public object Value { get; protected set; }

	public void Write<T>(T value)
	{
		if (type.IsAssignableFrom(typeof(T)))
		{
			Value = value;
		}
	}

	public void Write(object value)
	{
		if (type.IsAssignableFrom(value.GetType()))
		{
			Value = value;
		}
	}

	public BlackboardValue(Type type, object value)
	{
		this.type = type;
		Value = value;
	}

	public BlackboardValue(Type type)
		: this(type, null)
	{
	}
}
