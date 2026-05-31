using System;
using System.Collections;
using System.Collections.Generic;
using XLua.LuaDLL;

namespace XLua;

public class LuaTable : LuaBase
{
	[Obsolete("use no boxing version: GetInPath/SetInPath Get/Set instead!")]
	public object this[string field]
	{
		get
		{
			return GetInPath<object>(field);
		}
		set
		{
			SetInPath(field, value);
		}
	}

	[Obsolete("use no boxing version: GetInPath/SetInPath Get/Set instead!")]
	public object this[object field]
	{
		get
		{
			return Get<object>(field);
		}
		set
		{
			Set(field, value);
		}
	}

	public int Length
	{
		get
		{
			IntPtr l = luaEnv.L;
			int newTop = Lua.lua_gettop(l);
			Lua.lua_getref(l, luaReference);
			int result = (int)Lua.xlua_objlen(l, -1);
			Lua.lua_settop(l, newTop);
			return result;
		}
	}

	public LuaTable(int reference, LuaEnv luaenv)
		: base(reference, luaenv)
	{
	}

	public void Get<TKey, TValue>(TKey key, out TValue value)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int newTop = Lua.lua_gettop(l);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, key);
		if (Lua.xlua_pgettable(l, -2) != 0)
		{
			string text = Lua.lua_tostring(l, -1);
			Lua.lua_settop(l, newTop);
			TKey val = key;
			throw new Exception("get field [" + val?.ToString() + "] error:" + text);
		}
		LuaTypes num = Lua.lua_type(l, -1);
		Type typeFromHandle = typeof(TValue);
		if (num == LuaTypes.LUA_TNIL && typeFromHandle.IsValueType())
		{
			throw new InvalidCastException("can not assign nil to " + typeFromHandle.GetFriendlyName());
		}
		try
		{
			translator.Get(l, -1, out value);
		}
		catch (Exception ex)
		{
			throw ex;
		}
		finally
		{
			Lua.lua_settop(l, newTop);
		}
	}

	public bool ContainsKey<TKey>(TKey key)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int newTop = Lua.lua_gettop(l);
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, key);
		if (Lua.xlua_pgettable(l, -2) != 0)
		{
			string text = Lua.lua_tostring(l, -1);
			Lua.lua_settop(l, newTop);
			TKey val = key;
			throw new Exception("get field [" + val?.ToString() + "] error:" + text);
		}
		bool result = Lua.lua_type(l, -1) != LuaTypes.LUA_TNIL;
		Lua.lua_settop(l, newTop);
		return result;
	}

	public void Set<TKey, TValue>(TKey key, TValue value)
	{
		IntPtr l = luaEnv.L;
		int num = Lua.lua_gettop(l);
		ObjectTranslator translator = luaEnv.translator;
		Lua.lua_getref(l, luaReference);
		translator.PushByType(l, key);
		translator.PushByType(l, value);
		if (Lua.xlua_psettable(l, -3) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		Lua.lua_settop(l, num);
	}

	public T GetInPath<T>(string path)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int num = Lua.lua_gettop(l);
		Lua.lua_getref(l, luaReference);
		if (Lua.xlua_pgettable_bypath(l, -1, path) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		if (Lua.lua_type(l, -1) == LuaTypes.LUA_TNIL && typeof(T).IsValueType())
		{
			throw new InvalidCastException("can not assign nil to " + typeof(T).GetFriendlyName());
		}
		try
		{
			translator.Get(l, -1, out T v);
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

	public void SetInPath<T>(string path, T val)
	{
		IntPtr l = luaEnv.L;
		int num = Lua.lua_gettop(l);
		Lua.lua_getref(l, luaReference);
		luaEnv.translator.PushByType(l, val);
		if (Lua.xlua_psettable_bypath(l, -2, path) != 0)
		{
			luaEnv.ThrowExceptionFromError(num);
		}
		Lua.lua_settop(l, num);
	}

	public void ForEach<TKey, TValue>(Action<TKey, TValue> action)
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int newTop = Lua.lua_gettop(l);
		try
		{
			Lua.lua_getref(l, luaReference);
			Lua.lua_pushnil(l);
			while (Lua.lua_next(l, -2) != 0)
			{
				if (translator.Assignable<TKey>(l, -2))
				{
					translator.Get(l, -2, out TKey v);
					translator.Get(l, -1, out TValue v2);
					action(v, v2);
				}
				Lua.lua_pop(l, 1);
			}
		}
		finally
		{
			Lua.lua_settop(l, newTop);
		}
	}

	public IEnumerable GetKeys()
	{
		IntPtr L = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int oldTop = Lua.lua_gettop(L);
		Lua.lua_getref(L, luaReference);
		Lua.lua_pushnil(L);
		while (Lua.lua_next(L, -2) != 0)
		{
			yield return translator.GetObject(L, -2);
			Lua.lua_pop(L, 1);
		}
		Lua.lua_settop(L, oldTop);
	}

	public IEnumerable<T> GetKeys<T>()
	{
		IntPtr L = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		int oldTop = Lua.lua_gettop(L);
		Lua.lua_getref(L, luaReference);
		Lua.lua_pushnil(L);
		while (Lua.lua_next(L, -2) != 0)
		{
			if (translator.Assignable<T>(L, -2))
			{
				translator.Get(L, -2, out T v);
				yield return v;
			}
			Lua.lua_pop(L, 1);
		}
		Lua.lua_settop(L, oldTop);
	}

	[Obsolete("use no boxing version: Get<TKey, TValue> !")]
	public T Get<T>(object key)
	{
		Get<object, T>(key, out var value);
		return value;
	}

	public TValue Get<TKey, TValue>(TKey key)
	{
		Get<TKey, TValue>(key, out var value);
		return value;
	}

	public TValue Get<TValue>(string key)
	{
		Get<string, TValue>(key, out var value);
		return value;
	}

	public void SetMetaTable(LuaTable metaTable)
	{
		push(luaEnv.L);
		metaTable.push(luaEnv.L);
		Lua.lua_setmetatable(luaEnv.L, -2);
		Lua.lua_pop(luaEnv.L, 1);
	}

	public T Cast<T>()
	{
		IntPtr l = luaEnv.L;
		ObjectTranslator translator = luaEnv.translator;
		push(l);
		T result = (T)translator.GetObject(l, -1, typeof(T));
		Lua.lua_pop(luaEnv.L, 1);
		return result;
	}

	internal override void push(IntPtr L)
	{
		Lua.lua_getref(L, luaReference);
	}

	public override string ToString()
	{
		return "table :" + luaReference;
	}
}
