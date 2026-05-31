using System;

namespace RedSaw.CommandLineInterface;

public class StackString : StackValue
{
	public readonly string strValue;

	public override Type ValueType => typeof(string);

	public StackString(string value)
		: base(value)
	{
		strValue = value;
	}
}
