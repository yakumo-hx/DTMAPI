using System;

namespace RedSaw.CommandLineInterface;

public class StackNull : StackValue
{
	public override Type ValueType => typeof(void);

	public static StackNull Default => new StackNull();

	public StackNull()
		: base(null)
	{
	}
}
