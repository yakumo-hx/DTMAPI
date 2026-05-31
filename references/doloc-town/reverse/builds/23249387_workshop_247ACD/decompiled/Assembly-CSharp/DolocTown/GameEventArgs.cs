using System;

namespace DolocTown;

public class GameEventArgs : EventArgs
{
	public static readonly GameEventArgs None = new GameEventArgs();

	public override string ToString()
	{
		return "None";
	}
}
public class GameEventArgs<T> : GameEventArgs
{
	public readonly T value;

	public GameEventArgs(T value)
	{
		this.value = value;
	}
}
