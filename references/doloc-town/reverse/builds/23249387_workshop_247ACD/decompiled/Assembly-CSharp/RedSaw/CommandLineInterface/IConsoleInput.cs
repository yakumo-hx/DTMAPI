namespace RedSaw.CommandLineInterface;

public interface IConsoleInput
{
	bool MoveUp { get; }

	bool MoveUpC { get; }

	bool MoveDown { get; }

	bool MoveDownC { get; }

	bool Focus { get; }

	bool QuitFocus { get; }

	bool ShowOrHide { get; }
}
