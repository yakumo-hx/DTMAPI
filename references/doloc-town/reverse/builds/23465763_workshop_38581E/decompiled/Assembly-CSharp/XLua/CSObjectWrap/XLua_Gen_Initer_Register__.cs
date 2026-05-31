namespace XLua.CSObjectWrap;

public class XLua_Gen_Initer_Register__
{
	private static void Init(LuaEnv luaenv, ObjectTranslator translator)
	{
	}

	static XLua_Gen_Initer_Register__()
	{
		LuaEnv.AddIniter(Init);
	}
}
