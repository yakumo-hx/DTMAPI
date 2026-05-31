namespace DolocTown;

public class DolocDeserializeAssert
{
	public static void IsTrue(bool condition)
	{
		if (condition)
		{
			return;
		}
		throw new DolocDeserializeError();
	}

	public static void IsTrue(bool condition, string message)
	{
		if (condition)
		{
			return;
		}
		throw new DolocDeserializeError(message);
	}

	public static void IsFalse(bool condition)
	{
		if (!condition)
		{
			return;
		}
		throw new DolocDeserializeError();
	}

	public static void IsFalse(bool condition, string message)
	{
		if (!condition)
		{
			return;
		}
		throw new DolocDeserializeError(message);
	}

	public static void IsNotNull<T>(T value)
	{
		if (value != null)
		{
			return;
		}
		throw new DolocDeserializeError();
	}

	public static void IsNotNull<T>(T value, string message)
	{
		if (value != null)
		{
			return;
		}
		throw new DolocDeserializeError(message);
	}
}
