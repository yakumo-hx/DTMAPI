namespace RedSaw.CommandLineInterface;

internal class IBS_String : InputBehaviourStateWithInput
{
	private readonly char quote;

	public IBS_String(InputBehaviourStateMachine stateMachine, char quote, string input = "")
		: base(stateMachine, input)
	{
		this.quote = quote;
	}

	public override InputBehaviourState StepForward(char c)
	{
		if (c == quote)
		{
			return Wait();
		}
		base.CurrentInput += c;
		return this;
	}

	public override bool StepBackward()
	{
		if (base.CurrentInput.Length == 0)
		{
			return true;
		}
		base.CurrentInput = base.CurrentInput[..^1];
		return false;
	}

	public override string ToString()
	{
		return "<String: " + base.CurrentInput + ">";
	}
}
