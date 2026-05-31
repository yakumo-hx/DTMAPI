namespace RedSaw.CommandLineInterface;

public class CommandExecuteException : CommandSystemException
{
	public CommandExecuteException(string message)
		: base(message)
	{
	}
}
