using System;

namespace RedSaw.CommandLineInterface;

internal class IBS_Member : InputBehaviourStateWithInput
{
	private readonly Type queryType;

	public IBS_Member(InputBehaviourStateMachine stateMachine, Type type)
		: base(stateMachine, string.Empty)
	{
		queryType = type;
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
				return Member(queryType, base.CurrentInput);
			default:
				return Callable(c);
			}
		}
		base.CurrentInput += c;
		return this;
	}

	public override SuggestionQuery GetSuggestion()
	{
		return new SuggestionQuery(SuggestionType.Member, base.CurrentInput, queryType);
	}

	public override string ToString()
	{
		return "<Member: " + base.CurrentInput + ">";
	}
}
