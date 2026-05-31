using System;

namespace RedSaw.CommandLineInterface;

public class StackFloat : StackValue
{
	public readonly float floatValue;

	public override Type ValueType => typeof(float);

	public StackFloat(float value)
		: base(value)
	{
		floatValue = value;
	}
}
