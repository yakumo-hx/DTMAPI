using System;

public class DolocError : Exception
{
	public DolocError()
		: base("多洛可小镇未知异常")
	{
	}

	public DolocError(string message)
		: base(message)
	{
	}
}
