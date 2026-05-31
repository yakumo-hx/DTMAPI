using System;

namespace RedSaw.CommandLineInterface;

public class StackInt : StackValue
{
	public readonly int intValue;

	public override Type ValueType => typeof(int);

	public StackInt(int value)
		: base(value)
	{
		intValue = value;
	}
}
