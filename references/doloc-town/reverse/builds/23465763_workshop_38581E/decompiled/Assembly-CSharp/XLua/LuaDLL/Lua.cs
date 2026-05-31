using System;
using System.Runtime.InteropServices;
using System.Text;

namespace XLua.LuaDLL;

public class Lua
{
	private const string LUADLL = "xlua";

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr lua_tothread(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_get_lib_version();

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int lua_gc(IntPtr L, LuaGCOptions what, int data);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr lua_getupvalue(IntPtr L, int funcindex, int n);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr lua_setupvalue(IntPtr L, int funcindex, int n);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int lua_pushthread(IntPtr L);

	public static bool lua_isfunction(IntPtr L, int stackPos)
	{
		return lua_type(L, stackPos) == LuaTypes.LUA_TFUNCTION;
	}

	public static bool lua_islightuserdata(IntPtr L, int stackPos)
	{
		return lua_type(L, stackPos) == LuaTypes.LUA_TLIGHTUSERDATA;
	}

	public static bool lua_istable(IntPtr L, int stackPos)
	{
		return lua_type(L, stackPos) == LuaTypes.LUA_TTABLE;
	}

	public static bool lua_isthread(IntPtr L, int stackPos)
	{
		return lua_type(L, stackPos) == LuaTypes.LUA_TTHREAD;
	}

	public static int luaL_error(IntPtr L, string message)
	{
		xlua_csharp_str_error(L, message);
		return 0;
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int lua_setfenv(IntPtr L, int stackPos);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr luaL_newstate();

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_close(IntPtr L);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void luaopen_xlua(IntPtr L);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void luaL_openlibs(IntPtr L);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint xlua_objlen(IntPtr L, int stackPos);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_createtable(IntPtr L, int narr, int nrec);

	public static void lua_newtable(IntPtr L)
	{
		lua_createtable(L, 0, 0);
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_getglobal(IntPtr L, string name);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_setglobal(IntPtr L, string name);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void xlua_getloaders(IntPtr L);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_settop(IntPtr L, int newTop);

	public static void lua_pop(IntPtr L, int amount)
	{
		lua_settop(L, -amount - 1);
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_insert(IntPtr L, int newTop);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_remove(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int lua_rawget(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_rawset(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int lua_setmetatable(IntPtr L, int objIndex);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int lua_rawequal(IntPtr L, int index1, int index2);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_pushvalue(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_pushcclosure(IntPtr L, IntPtr fn, int n);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_replace(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int lua_gettop(IntPtr L);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern LuaTypes lua_type(IntPtr L, int index);

	public static bool lua_isnil(IntPtr L, int index)
	{
		return lua_type(L, index) == LuaTypes.LUA_TNIL;
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool lua_isnumber(IntPtr L, int index);

	public static bool lua_isboolean(IntPtr L, int index)
	{
		return lua_type(L, index) == LuaTypes.LUA_TBOOLEAN;
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int luaL_ref(IntPtr L, int registryIndex);

	public static int luaL_ref(IntPtr L)
	{
		return luaL_ref(L, LuaIndexes.LUA_REGISTRYINDEX);
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void xlua_rawgeti(IntPtr L, int tableIndex, long index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void xlua_rawseti(IntPtr L, int tableIndex, long index);

	public static void lua_getref(IntPtr L, int reference)
	{
		xlua_rawgeti(L, LuaIndexes.LUA_REGISTRYINDEX, reference);
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int pcall_prepare(IntPtr L, int error_func_ref, int func_ref);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void luaL_unref(IntPtr L, int registryIndex, int reference);

	public static void lua_unref(IntPtr L, int reference)
	{
		luaL_unref(L, LuaIndexes.LUA_REGISTRYINDEX, reference);
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool lua_isstring(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool lua_isinteger(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_pushnil(IntPtr L);

	public static void lua_pushstdcallcfunction(IntPtr L, lua_CSFunction function, int n = 0)
	{
		IntPtr functionPointerForDelegate = Marshal.GetFunctionPointerForDelegate(function);
		xlua_push_csharp_function(L, functionPointerForDelegate, n);
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_upvalueindex(int n);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int lua_pcall(IntPtr L, int nArgs, int nResults, int errfunc);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern double lua_tonumber(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_tointeger(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint xlua_touint(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool lua_toboolean(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr lua_topointer(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr lua_tolstring(IntPtr L, int index, out IntPtr strLen);

	public static string lua_tostring(IntPtr L, int index)
	{
		IntPtr strLen;
		IntPtr intPtr = lua_tolstring(L, index, out strLen);
		if (intPtr != IntPtr.Zero)
		{
			string text = Marshal.PtrToStringAnsi(intPtr, strLen.ToInt32());
			if (text == null)
			{
				int num = strLen.ToInt32();
				byte[] array = new byte[num];
				Marshal.Copy(intPtr, array, 0, num);
				return Encoding.UTF8.GetString(array);
			}
			return text;
		}
		return null;
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr lua_atpanic(IntPtr L, lua_CSFunction panicf);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_pushnumber(IntPtr L, double number);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_pushboolean(IntPtr L, bool value);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void xlua_pushinteger(IntPtr L, int value);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void xlua_pushuint(IntPtr L, uint value);

	public static void lua_pushstring(IntPtr L, string str)
	{
		if (str == null)
		{
			lua_pushnil(L);
		}
		else if (Encoding.UTF8.GetByteCount(str) > InternalGlobals.strBuff.Length)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			xlua_pushlstring(L, bytes, bytes.Length);
		}
		else
		{
			int bytes2 = Encoding.UTF8.GetBytes(str, 0, str.Length, InternalGlobals.strBuff, 0);
			xlua_pushlstring(L, InternalGlobals.strBuff, bytes2);
		}
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void xlua_pushlstring(IntPtr L, byte[] str, int size);

	public static void xlua_pushasciistring(IntPtr L, string str)
	{
		if (str == null)
		{
			lua_pushnil(L);
			return;
		}
		int length = str.Length;
		if (InternalGlobals.strBuff.Length < length)
		{
			InternalGlobals.strBuff = new byte[length];
		}
		int bytes = Encoding.UTF8.GetBytes(str, 0, length, InternalGlobals.strBuff, 0);
		xlua_pushlstring(L, InternalGlobals.strBuff, bytes);
	}

	public static void lua_pushstring(IntPtr L, byte[] str)
	{
		if (str == null)
		{
			lua_pushnil(L);
		}
		else
		{
			xlua_pushlstring(L, str, str.Length);
		}
	}

	public static byte[] lua_tobytes(IntPtr L, int index)
	{
		if (lua_type(L, index) == LuaTypes.LUA_TSTRING)
		{
			IntPtr strLen;
			IntPtr intPtr = lua_tolstring(L, index, out strLen);
			if (intPtr != IntPtr.Zero)
			{
				int num = strLen.ToInt32();
				byte[] array = new byte[num];
				Marshal.Copy(intPtr, array, 0, num);
				return array;
			}
		}
		return null;
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int luaL_newmetatable(IntPtr L, string meta);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_pgettable(IntPtr L, int idx);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_psettable(IntPtr L, int idx);

	public static void luaL_getmetatable(IntPtr L, string meta)
	{
		xlua_pushasciistring(L, meta);
		lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xluaL_loadbuffer(IntPtr L, byte[] buff, int size, string name);

	public static int luaL_loadbuffer(IntPtr L, string buff, string name)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(buff);
		return xluaL_loadbuffer(L, bytes, bytes.Length, name);
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_tocsobj_safe(IntPtr L, int obj);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_tocsobj_fast(IntPtr L, int obj);

	public static int lua_error(IntPtr L)
	{
		xlua_csharp_error(L);
		return 0;
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool lua_checkstack(IntPtr L, int extra);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int lua_next(IntPtr L, int index);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_pushlightuserdata(IntPtr L, IntPtr udata);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr xlua_tag();

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void luaL_where(IntPtr L, int level);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_tryget_cachedud(IntPtr L, int key, int cache_ref);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void xlua_pushcsobj(IntPtr L, int key, int meta_ref, bool need_cache, int cache_ref);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int gen_obj_indexer(IntPtr L);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int gen_obj_newindexer(IntPtr L);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int gen_cls_indexer(IntPtr L);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int gen_cls_newindexer(IntPtr L);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int get_error_func_ref(IntPtr L);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int load_error_func(IntPtr L, int Ref);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int luaopen_i64lib(IntPtr L);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int luaopen_socket_core(IntPtr L);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_pushint64(IntPtr L, long n);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void lua_pushuint64(IntPtr L, ulong n);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool lua_isint64(IntPtr L, int idx);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool lua_isuint64(IntPtr L, int idx);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern long lua_toint64(IntPtr L, int idx);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern ulong lua_touint64(IntPtr L, int idx);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void xlua_push_csharp_function(IntPtr L, IntPtr fn, int n);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_csharp_str_error(IntPtr L, string message);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_csharp_error(IntPtr L);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_pack_int8_t(IntPtr buff, int offset, byte field);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_unpack_int8_t(IntPtr buff, int offset, out byte field);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_pack_int16_t(IntPtr buff, int offset, short field);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_unpack_int16_t(IntPtr buff, int offset, out short field);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_pack_int32_t(IntPtr buff, int offset, int field);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_unpack_int32_t(IntPtr buff, int offset, out int field);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_pack_int64_t(IntPtr buff, int offset, long field);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_unpack_int64_t(IntPtr buff, int offset, out long field);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_pack_float(IntPtr buff, int offset, float field);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_unpack_float(IntPtr buff, int offset, out float field);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_pack_double(IntPtr buff, int offset, double field);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_unpack_double(IntPtr buff, int offset, out double field);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr xlua_pushstruct(IntPtr L, uint size, int meta_ref);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern void xlua_pushcstable(IntPtr L, uint field_count, int meta_ref);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr lua_touserdata(IntPtr L, int idx);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_gettypeid(IntPtr L, int idx);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_get_registry_index();

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_pgettable_bypath(IntPtr L, int idx, string path);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern int xlua_psettable_bypath(IntPtr L, int idx, string path);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_pack_float2(IntPtr buff, int offset, float f1, float f2);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_unpack_float2(IntPtr buff, int offset, out float f1, out float f2);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_pack_float3(IntPtr buff, int offset, float f1, float f2, float f3);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_unpack_float3(IntPtr buff, int offset, out float f1, out float f2, out float f3);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_pack_float4(IntPtr buff, int offset, float f1, float f2, float f3, float f4);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_unpack_float4(IntPtr buff, int offset, out float f1, out float f2, out float f3, out float f4);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_pack_float5(IntPtr buff, int offset, float f1, float f2, float f3, float f4, float f5);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_unpack_float5(IntPtr buff, int offset, out float f1, out float f2, out float f3, out float f4, out float f5);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_pack_float6(IntPtr buff, int offset, float f1, float f2, float f3, float f4, float f5, float f6);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_unpack_float6(IntPtr buff, int offset, out float f1, out float f2, out float f3, out float f4, out float f5, out float f6);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_pack_decimal(IntPtr buff, int offset, ref decimal dec);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_unpack_decimal(IntPtr buff, int offset, out byte scale, out byte sign, out int hi32, out ulong lo64);

	public static bool xlua_is_eq_str(IntPtr L, int index, string str)
	{
		return xlua_is_eq_str(L, index, str, str.Length);
	}

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool xlua_is_eq_str(IntPtr L, int index, string str, int str_len);

	[DllImport("xlua", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr xlua_gl(IntPtr L);
}
