public class DolocLuaScriptError : DolocError
{
	public DolocLuaScriptError()
		: base("LuaScript Error")
	{
	}

	public DolocLuaScriptError(string value)
		: base(value)
	{
	}
}
