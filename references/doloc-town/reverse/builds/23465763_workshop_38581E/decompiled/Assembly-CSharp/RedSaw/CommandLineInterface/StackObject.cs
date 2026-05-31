namespace RedSaw.CommandLineInterface;

public abstract class StackObject
{
	public readonly StackObjectType stackType;

	public StackObject(StackObjectType stackType)
	{
		this.stackType = stackType;
	}
}
