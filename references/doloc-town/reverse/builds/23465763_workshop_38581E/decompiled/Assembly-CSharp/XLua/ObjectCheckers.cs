using System;
using System.Collections.Generic;
using XLua.LuaDLL;

namespace XLua;

public class ObjectCheckers
{
	private Dictionary<Type, ObjectCheck> checkersMap = new Dictionary<Type, ObjectCheck>();

	private ObjectTranslator translator;

	public ObjectCheckers(ObjectTranslator translator)
	{
		this.translator = translator;
		checkersMap[typeof(sbyte)] = numberCheck;
		checkersMap[typeof(byte)] = numberCheck;
		checkersMap[typeof(short)] = numberCheck;
		checkersMap[typeof(ushort)] = numberCheck;
		checkersMap[typeof(int)] = numberCheck;
		checkersMap[typeof(uint)] = numberCheck;
		checkersMap[typeof(long)] = int64Check;
		checkersMap[typeof(ulong)] = uint64Check;
		checkersMap[typeof(double)] = numberCheck;
		checkersMap[typeof(char)] = numberCheck;
		checkersMap[typeof(float)] = numberCheck;
		checkersMap[typeof(decimal)] = decimalCheck;
		checkersMap[typeof(bool)] = boolCheck;
		checkersMap[typeof(string)] = strCheck;
		checkersMap[typeof(object)] = objectCheck;
		checkersMap[typeof(byte[])] = bytesCheck;
		checkersMap[typeof(IntPtr)] = intptrCheck;
		checkersMap[typeof(LuaTable)] = luaTableCheck;
		checkersMap[typeof(LuaFunction)] = luaFunctionCheck;
	}

	private static bool objectCheck(IntPtr L, int idx)
	{
		return true;
	}

	private bool luaTableCheck(IntPtr L, int idx)
	{
		if (!Lua.lua_isnil(L, idx) && !Lua.lua_istable(L, idx))
		{
			if (Lua.lua_type(L, idx) == LuaTypes.LUA_TUSERDATA)
			{
				return translator.SafeGetCSObj(L, idx) is LuaTable;
			}
			return false;
		}
		return true;
	}

	private bool numberCheck(IntPtr L, int idx)
	{
		return Lua.lua_type(L, idx) == LuaTypes.LUA_TNUMBER;
	}

	private bool decimalCheck(IntPtr L, int idx)
	{
		if (Lua.lua_type(L, idx) != LuaTypes.LUA_TNUMBER)
		{
			return translator.IsDecimal(L, idx);
		}
		return true;
	}

	private bool strCheck(IntPtr L, int idx)
	{
		if (Lua.lua_type(L, idx) != LuaTypes.LUA_TSTRING)
		{
			return Lua.lua_isnil(L, idx);
		}
		return true;
	}

	private bool bytesCheck(IntPtr L, int idx)
	{
		if (Lua.lua_type(L, idx) != LuaTypes.LUA_TSTRING && !Lua.lua_isnil(L, idx))
		{
			if (Lua.lua_type(L, idx) == LuaTypes.LUA_TUSERDATA)
			{
				return translator.SafeGetCSObj(L, idx) is byte[];
			}
			return false;
		}
		return true;
	}

	private bool boolCheck(IntPtr L, int idx)
	{
		return Lua.lua_type(L, idx) == LuaTypes.LUA_TBOOLEAN;
	}

	private bool int64Check(IntPtr L, int idx)
	{
		if (Lua.lua_type(L, idx) != LuaTypes.LUA_TNUMBER)
		{
			return Lua.lua_isint64(L, idx);
		}
		return true;
	}

	private bool uint64Check(IntPtr L, int idx)
	{
		if (Lua.lua_type(L, idx) != LuaTypes.LUA_TNUMBER)
		{
			return Lua.lua_isuint64(L, idx);
		}
		return true;
	}

	private bool luaFunctionCheck(IntPtr L, int idx)
	{
		if (!Lua.lua_isnil(L, idx) && !Lua.lua_isfunction(L, idx))
		{
			if (Lua.lua_type(L, idx) == LuaTypes.LUA_TUSERDATA)
			{
				return translator.SafeGetCSObj(L, idx) is LuaFunction;
			}
			return false;
		}
		return true;
	}

	private bool intptrCheck(IntPtr L, int idx)
	{
		return Lua.lua_type(L, idx) == LuaTypes.LUA_TLIGHTUSERDATA;
	}

	private ObjectCheck genChecker(Type type)
	{
		ObjectCheck fixTypeCheck = delegate(IntPtr L, int idx)
		{
			if (Lua.lua_type(L, idx) == LuaTypes.LUA_TUSERDATA)
			{
				object obj = translator.SafeGetCSObj(L, idx);
				if (obj != null)
				{
					return type.IsAssignableFrom(obj.GetType());
				}
				Type typeOf = translator.GetTypeOf(L, idx);
				if (typeOf != null)
				{
					return type.IsAssignableFrom(typeOf);
				}
			}
			return false;
		};
		if (!type.IsAbstract() && typeof(Delegate).IsAssignableFrom(type))
		{
			return (IntPtr L, int idx) => Lua.lua_isnil(L, idx) || Lua.lua_isfunction(L, idx) || fixTypeCheck(L, idx);
		}
		if (type.IsEnum())
		{
			return fixTypeCheck;
		}
		if (type.IsInterface())
		{
			return (IntPtr L, int idx) => Lua.lua_isnil(L, idx) || Lua.lua_istable(L, idx) || fixTypeCheck(L, idx);
		}
		if (type.IsClass() && type.GetConstructor(Type.EmptyTypes) != null)
		{
			return (IntPtr L, int idx) => Lua.lua_isnil(L, idx) || Lua.lua_istable(L, idx) || fixTypeCheck(L, idx);
		}
		if (type.IsValueType())
		{
			return (IntPtr L, int idx) => Lua.lua_istable(L, idx) || fixTypeCheck(L, idx);
		}
		if (type.IsArray)
		{
			return (IntPtr L, int idx) => Lua.lua_isnil(L, idx) || Lua.lua_istable(L, idx) || fixTypeCheck(L, idx);
		}
		return (IntPtr L, int idx) => Lua.lua_isnil(L, idx) || fixTypeCheck(L, idx);
	}

	public ObjectCheck genNullableChecker(ObjectCheck oc)
	{
		return (IntPtr L, int idx) => Lua.lua_isnil(L, idx) || oc(L, idx);
	}

	public void AddChecker(Type type, ObjectCheck oc)
	{
		checkersMap[type] = oc;
	}

	public ObjectCheck GetChecker(Type type)
	{
		if (type.IsByRef)
		{
			type = type.GetElementType();
		}
		Type underlyingType = Nullable.GetUnderlyingType(type);
		if (underlyingType != null)
		{
			return genNullableChecker(GetChecker(underlyingType));
		}
		if (!checkersMap.TryGetValue(type, out var value))
		{
			value = genChecker(type);
			checkersMap.Add(type, value);
		}
		return value;
	}
}
