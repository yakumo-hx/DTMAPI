namespace RedSaw.CommandLineInterface;

internal class IBS_Variable : InputBehaviourStateWithInput
{
	public IBS_Variable(InputBehaviourStateMachine stateMachine, string input = "")
		: base(stateMachine, input)
	{
	}

	public override InputBehaviourState StepForward(char c)
	{
		if (base.CurrentInput.Length == 0)
		{
			if (char.IsLetter(c) || c == '_')
			{
				base.CurrentInput += c;
				return this;
			}
			return Unknown();
		}
		if (!char.IsLetterOrDigit(c))
		{
			switch (c)
			{
			case '_':
				break;
			case '.':
				return VariableMember(base.CurrentInput);
			default:
				return Unknown();
			}
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

	public override SuggestionQuery GetSuggestion()
	{
		return new SuggestionQuery(SuggestionType.Variable, base.CurrentInput);
	}

	public override string ToString()
	{
		return "<Variable: " + base.CurrentInput + ">";
	}
}
