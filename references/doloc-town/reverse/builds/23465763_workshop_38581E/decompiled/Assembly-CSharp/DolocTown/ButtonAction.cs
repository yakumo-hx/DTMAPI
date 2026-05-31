using System;

namespace DolocTown;

public class ButtonAction
{
	private Func<string> textGetter;

	public Action action;

	public int index { get; private set; }

	public string text => textGetter?.Invoke() ?? string.Empty;

	public ButtonAction(int index, Func<string> textGetter, Action action)
	{
		this.index = index;
		this.textGetter = textGetter;
		this.action = action;
	}
}
