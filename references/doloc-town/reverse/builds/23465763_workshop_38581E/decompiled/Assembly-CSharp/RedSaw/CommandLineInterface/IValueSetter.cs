using System;

namespace RedSaw.CommandLineInterface;

public interface IValueSetter
{
	string Name { get; }

	Type ValueType { get; }

	void SetValue(object value);
}
