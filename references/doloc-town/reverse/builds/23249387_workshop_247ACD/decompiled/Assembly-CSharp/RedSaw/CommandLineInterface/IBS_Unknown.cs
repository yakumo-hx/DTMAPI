namespace RedSaw.CommandLineInterface;

internal class IBS_Unknown : InputBehaviourState
{
	private int count;

	public IBS_Unknown(InputBehaviourStateMachine stateMachine)
		: base(stateMachine)
	{
		count = 0;
	}

	public override InputBehaviourState StepForward(char c)
	{
		count++;
		return this;
	}

	public override bool StepBackward()
	{
		return --count < 0;
	}

	public override string ToString()
	{
		return "<Unknown>";
	}
}
