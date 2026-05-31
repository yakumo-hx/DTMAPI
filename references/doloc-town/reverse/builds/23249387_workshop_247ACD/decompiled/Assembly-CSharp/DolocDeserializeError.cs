public class DolocDeserializeError : DolocError
{
	public DolocDeserializeError()
		: base("多洛可小镇反序列化异常")
	{
	}

	public DolocDeserializeError(string message)
		: base("多洛可小镇反序列化异常:" + message)
	{
	}
}
