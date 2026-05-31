namespace RedSaw.CommandLineInterface;

internal class IBS_Wait : InputBehaviourStateWithInput
{
	public IBS_Wait(InputBehaviourStateMachine stateMachine, string input = "")
		: base(stateMachine, input)
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
		default:
			if (Lexer.IsWhiteSpace(c))
			{
				return Wait();
			}
			if (c == '=')
			{
				return Wait();
			}
			if (char.IsLetter(c) || c == '_')
			{
				return Callable(c, shouldCollapse: true, base.AfterAssign);
			}
			return Unknown();
		}
	}

	public override bool StepBackward()
	{
		return true;
	}

	public override string ToString()
	{
		return "<Wait>";
	}
}
