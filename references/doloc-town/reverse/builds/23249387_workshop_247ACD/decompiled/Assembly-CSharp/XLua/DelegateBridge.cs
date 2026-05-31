using System;
using XLua.LuaDLL;

namespace XLua;

public class DelegateBridge : DelegateBridgeBase
{
	internal static DelegateBridge[] DelegateBridgeList;

	public static bool Gen_Flag;

	static DelegateBridge()
	{
		DelegateBridgeList = new DelegateBridge[0];
		Gen_Flag = false;
		Gen_Flag = true;
	}

	public override Delegate GetDelegateByType(Type type)
	{
		return null;
	}

	public DelegateBridge(int reference, LuaEnv luaenv)
		: base(reference, luaenv)
	{
	}

	public void PCall(IntPtr L, int nArgs, int nResults, int errFunc)
	{
		if (Lua.lua_pcall(L, nArgs, nResults, errFunc) != 0)
		{
			luaEnv.ThrowExceptionFromError(errFunc - 1);
		}
	}

	public void Action()
	{
		IntPtr l = luaEnv.L;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		if (Lua.lua_pcall(l, 0, 0, errfunc) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		Lua.lua_settop(l, num);
	}

	public void Action<T1>(T1 p1)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, p1);
		if (Lua.lua_pcall(l, 1, 0, errfunc) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		Lua.lua_settop(l, num);
	}

	public void Action<T1, T2>(T1 p1, T2 p2)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, p1);
		translator.PushByType(l, p2);
		if (Lua.lua_pcall(l, 2, 0, errfunc) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		Lua.lua_settop(l, num);
	}

	public void Action<T1, T2, T3>(T1 p1, T2 p2, T3 p3)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, p1);
		translator.PushByType(l, p2);
		translator.PushByType(l, p3);
		if (Lua.lua_pcall(l, 3, 0, errfunc) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		Lua.lua_settop(l, num);
	}

	public void Action<T1, T2, T3, T4>(T1 p1, T2 p2, T3 p3, T4 p4)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, p1);
		translator.PushByType(l, p2);
		translator.PushByType(l, p3);
		translator.PushByType(l, p4);
		if (Lua.lua_pcall(l, 4, 0, errfunc) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		Lua.lua_settop(l, num);
	}

	public TResult Func<TResult>()
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		if (Lua.lua_pcall(l, 0, 1, errfunc) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		try
		{
			translator.Get(l, -1, out TResult v);
			return v;
		}
		catch (Exception ex)
		{
			throw ex;
		}
		finally
		{
			Lua.lua_settop(l, num);
		}
	}

	public TResult Func<T1, TResult>(T1 p1)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, p1);
		if (Lua.lua_pcall(l, 1, 1, errfunc) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		try
		{
			translator.Get(l, -1, out TResult v);
			return v;
		}
		catch (Exception ex)
		{
			throw ex;
		}
		finally
		{
			Lua.lua_settop(l, num);
		}
	}

	public TResult Func<T1, T2, TResult>(T1 p1, T2 p2)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, p1);
		translator.PushByType(l, p2);
		if (Lua.lua_pcall(l, 2, 1, errfunc) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		try
		{
			translator.Get(l, -1, out TResult v);
			return v;
		}
		catch (Exception ex)
		{
			throw ex;
		}
		finally
		{
			Lua.lua_settop(l, num);
		}
	}

	public TResult Func<T1, T2, T3, TResult>(T1 p1, T2 p2, T3 p3)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, p1);
		translator.PushByType(l, p2);
		translator.PushByType(l, p3);
		if (Lua.lua_pcall(l, 3, 1, errfunc) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		try
		{
			translator.Get(l, -1, out TResult v);
			return v;
		}
		catch (Exception ex)
		{
			throw ex;
		}
		finally
		{
			Lua.lua_settop(l, num);
		}
	}

	public TResult Func<T1, T2, T3, T4, TResult>(T1 p1, T2 p2, T3 p3, T4 p4)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, p1);
		translator.PushByType(l, p2);
		translator.PushByType(l, p3);
		translator.PushByType(l, p4);
		if (Lua.lua_pcall(l, 4, 1, errfunc) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		try
		{
			translator.Get(l, -1, out TResult v);
			return v;
		}
		catch (Exception ex)
		{
			throw ex;
		}
		finally
		{
			Lua.lua_settop(l, num);
		}
	}
}
