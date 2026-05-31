public class DolocInitError : DolocError
{
	public DolocInitError()
		: base("多洛可小镇初始化异常")
	{
	}

	public DolocInitError(string message)
		: base("多洛可小镇初始化异常:" + message)
	{
	}
}
