namespace XLua;

internal sealed class LuaIndexes
{
	public static int LUA_REGISTRYINDEX
	{
		get
		{
			return InternalGlobals.LUA_REGISTRYINDEX;
		}
		set
		{
			InternalGlobals.LUA_REGISTRYINDEX = value;
		}
	}
}
