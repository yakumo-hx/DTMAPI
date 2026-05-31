namespace RedSaw.CommandLineInterface;

public class CommandSyntaxException : CommandSystemException
{
	public CommandSyntaxException(string message)
		: base(message)
	{
	}
}
