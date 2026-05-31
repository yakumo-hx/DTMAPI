namespace RedSaw.CommandLineInterface;

public class StackSourceInput : StackObject
{
	public readonly string inputStr;

	public StackSourceInput(string inputStr)
		: base(StackObjectType.Any)
	{
		this.inputStr = inputStr;
	}

	public object GetValue()
	{
		return inputStr;
	}
}
