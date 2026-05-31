namespace DolocTown;

public static class GameEventArgsExtension
{
	public static bool TryGetBool(this GameEventArgs<object> args, out bool value)
	{
		if (args.value is bool flag)
		{
			value = flag;
			return true;
		}
		value = false;
		return false;
	}

	public static bool TryGetInt(this GameEventArgs<object> args, out int value)
	{
		if (args.value is int num)
		{
			value = num;
			return true;
		}
		if (args.value is long num2)
		{
			value = (int)num2;
			return true;
		}
		value = -1;
		return false;
	}
}
