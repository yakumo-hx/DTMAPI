public class DolocGameError : DolocError
{
	public DolocGameError()
		: base("多洛可小镇游戏异常")
	{
	}

	public DolocGameError(string message)
		: base("多洛可小镇游戏异常:" + message)
	{
	}
}
