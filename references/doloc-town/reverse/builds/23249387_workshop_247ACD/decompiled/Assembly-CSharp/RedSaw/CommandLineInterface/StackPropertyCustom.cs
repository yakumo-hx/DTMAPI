using System;

namespace RedSaw.CommandLineInterface;

public class StackPropertyCustom : StackProperty
{
	private object value;

	public override Type ValueType
	{
		get
		{
			if (value == null)
			{
				return typeof(void);
			}
			return value.GetType();
		}
	}

	public StackPropertyCustom(string name)
		: base(name)
	{
		value = null;
	}

	public StackPropertyCustom(string name, object initValue)
		: base(name)
	{
		value = initValue;
	}

	public override object GetValue()
	{
		return value;
	}

	public override void SetValue(object value)
	{
		this.value = value;
	}
}
