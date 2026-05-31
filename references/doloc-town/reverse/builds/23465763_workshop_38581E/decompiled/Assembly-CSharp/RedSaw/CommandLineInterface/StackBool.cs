using System;

namespace RedSaw.CommandLineInterface;

public class StackBool : StackValue
{
	public readonly bool boolValue;

	public static StackBool True => new StackBool(value: true);

	public static StackBool False => new StackBool(value: false);

	public override Type ValueType => typeof(bool);

	public StackBool(bool value)
		: base(value)
	{
		boolValue = value;
	}
}
