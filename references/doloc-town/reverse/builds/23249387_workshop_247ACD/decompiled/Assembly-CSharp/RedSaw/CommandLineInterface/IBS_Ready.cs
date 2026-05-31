namespace RedSaw.CommandLineInterface;

internal class IBS_Ready : InputBehaviourStateWithInput
{
	public IBS_Ready(InputBehaviourStateMachine stateMachine)
		: base(stateMachine)
	{
	}

	public override InputBehaviourState StepForward(char c)
	{
		switch (c)
		{
		case '@':
			return Variable();
		case '"':
		case '\'':
			return new IBS_String(stateMachine, c);
		case '_':
			return Callable(c, shouldCollapse: true, base.AfterAssign);
		default:
			if (Lexer.IsWhiteSpace(c))
			{
				return Wait();
			}
			if (c == '_' || char.IsLetter(c))
			{
				return Callable(c);
			}
			return Unknown();
		}
	}

	public override bool StepBackward()
	{
		return false;
	}

	public override string ToString()
	{
		return "<Ready>";
	}
}
