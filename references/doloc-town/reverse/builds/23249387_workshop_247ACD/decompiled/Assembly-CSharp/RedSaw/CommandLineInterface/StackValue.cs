using System;

namespace RedSaw.CommandLineInterface;

public class StackValue : StackObject, IValueGetter
{
	public readonly object value;

	public virtual Type ValueType => value?.GetType() ?? typeof(void);

	public static StackValue Wrap(object value)
	{
		if (value == null)
		{
			return StackNull.Default;
		}
		if (!(value is int num))
		{
			if (!(value is float num2))
			{
				if (!(value is string text))
				{
					if (value is bool)
					{
						return ((bool)value) ? StackBool.True : StackBool.False;
					}
					return new StackValue(value);
				}
				return new StackString(text);
			}
			return new StackFloat(num2);
		}
		return new StackInt(num);
	}

	public StackValue(object value)
		: base(StackObjectType.ValueGetter)
	{
		this.value = value;
	}

	public object GetValue()
	{
		return value;
	}
}
