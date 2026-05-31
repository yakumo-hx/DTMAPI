using System;

namespace RedSaw.CommandLineInterface;

public class CommandSystemException : Exception
{
	public CommandSystemException(string message)
		: base(message)
	{
	}
}
