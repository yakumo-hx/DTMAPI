namespace RedSaw.CommandLineInterface;

internal class IBS_Command : InputBehaviourStateWithInput
{
	public IBS_Command(InputBehaviourStateMachine stateMachine, char firstChar, bool shouldCollapse = false, bool afterAssign = false)
		: base(stateMachine, firstChar.ToString(), afterAssign)
	{
		base.CurrentInput = firstChar.ToString();
		base.ShouldCollapse = shouldCollapse;
	}

	public override InputBehaviourState StepForward(char c)
	{
		if (Lexer.IsWhiteSpace(c))
		{
			return Wait();
		}
		switch (c)
		{
		case '.':
			return CommandMember(base.CurrentInput);
		default:
			if (!char.IsLetterOrDigit(c))
			{
				return Unknown();
			}
			goto case '_';
		case '_':
			base.CurrentInput += c;
			return this;
		}
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

	public override SuggestionQuery GetSuggestion()
	{
		return new SuggestionQuery(SuggestionType.Command, base.CurrentInput);
	}

	public override string ToString()
	{
		return "<Command: " + base.CurrentInput + ">";
	}
}
