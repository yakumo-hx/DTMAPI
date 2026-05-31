using System;
using XLua.LuaDLL;

namespace XLua;

public class LuaFunction : LuaBase
{
	public LuaFunction(int reference, LuaEnv luaenv)
		: base(reference, luaenv)
	{
	}

	public void Action<T>(T a)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, a);
		if (Lua.lua_pcall(l, 1, 0, errfunc) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		Lua.lua_settop(l, num);
	}

	public TResult Func<T, TResult>(T a)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, a);
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

	public void Action<T1, T2>(T1 a1, T2 a2)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, a1);
		translator.PushByType(l, a2);
		if (Lua.lua_pcall(l, 2, 0, errfunc) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		Lua.lua_settop(l, num);
	}

	public TResult Func<T1, T2, TResult>(T1 a1, T2 a2)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		int errfunc = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, a1);
		translator.PushByType(l, a2);
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

	public object[] Call(object[] args, Type[] returnTypes)
	{
		int nArgs = 0;
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int oldTop = Lua.lua_gettop(l);
		int num = Lua.load_error_func(l, luaEnv.errorFuncRef);
		Lua.lua_getref(l, luaReference);
		if (args != null)
		{
			nArgs = args.Length;
			for (int i = 0; i < args.Length; i++)
			{
				translator.PushAny(l, args[i]);
			}
		}
		if (Lua.lua_pcall(l, nArgs, -1, num) != 0)
		{
			luaEnv.ThrowExceptionFromError(oldTop);
		}
		Lua.lua_remove(l, num);
		if (returnTypes != null)
		{
			return translator.popValues(l, oldTop, returnTypes);
		}
		return translator.popValues(l, oldTop);
	}

	public object[] Call(params object[] args)
	{
		return Call(args, null);
	}

	public T Cast<T>()
	{
		if (!typeof(T).IsSubclassOf(typeof(Delegate)))
		{
			throw new InvalidOperationException(typeof(T).Name + " is not a delegate type");
		}
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		push(l);
		T result = (T)translator.GetObject(l, -1, typeof(T));
		Lua.lua_pop(luaEnv.L, 1);
		return result;
	}

	public void SetEnv(LuaTable env)
	{
		IntPtr l = luaEnv.L;
		int newTop = Lua.lua_gettop(l);
		push(l);
		env.push(l);
		Lua.lua_setfenv(l, -2);
		Lua.lua_settop(l, newTop);
	}

	internal override void push(IntPtr L)
	{
		Lua.lua_getref(L, luaReference);
	}

	public override string ToString()
	{
		return "function :" + luaReference;
	}
}
