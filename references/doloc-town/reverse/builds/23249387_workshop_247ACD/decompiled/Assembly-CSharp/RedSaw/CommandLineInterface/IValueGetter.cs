using System;

namespace RedSaw.CommandLineInterface;

public interface IValueGetter
{
	Type ValueType { get; }

	object GetValue();
}
