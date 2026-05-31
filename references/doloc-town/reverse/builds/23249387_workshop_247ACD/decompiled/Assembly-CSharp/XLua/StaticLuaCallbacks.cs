using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using XLua.LuaDLL;

namespace XLua;

public class StaticLuaCallbacks
{
	internal lua_CSFunction GcMeta;

	internal lua_CSFunction ToStringMeta;

	internal lua_CSFunction EnumAndMeta;

	internal lua_CSFunction EnumOrMeta;

	internal lua_CSFunction StaticCSFunctionWraper;

	internal lua_CSFunction FixCSFunctionWraper;

	internal lua_CSFunction DelegateCtor;

	internal static bool __tryArrayGet(Type type, IntPtr L, ObjectTranslator translator, object obj, int index)
	{
		if (type == typeof(Vector2[]))
		{
			Vector2[] array = obj as Vector2[];
			translator.PushUnityEngineVector2(L, array[index]);
			return true;
		}
		if (type == typeof(Vector3[]))
		{
			Vector3[] array2 = obj as Vector3[];
			translator.PushUnityEngineVector3(L, array2[index]);
			return true;
		}
		if (type == typeof(Vector4[]))
		{
			Vector4[] array3 = obj as Vector4[];
			translator.PushUnityEngineVector4(L, array3[index]);
			return true;
		}
		if (type == typeof(Color[]))
		{
			Color[] array4 = obj as Color[];
			translator.PushUnityEngineColor(L, array4[index]);
			return true;
		}
		if (type == typeof(Quaternion[]))
		{
			Quaternion[] array5 = obj as Quaternion[];
			translator.PushUnityEngineQuaternion(L, array5[index]);
			return true;
		}
		if (type == typeof(Ray[]))
		{
			Ray[] array6 = obj as Ray[];
			translator.PushUnityEngineRay(L, array6[index]);
			return true;
		}
		if (type == typeof(Bounds[]))
		{
			Bounds[] array7 = obj as Bounds[];
			translator.PushUnityEngineBounds(L, array7[index]);
			return true;
		}
		if (type == typeof(Ray2D[]))
		{
			Ray2D[] array8 = obj as Ray2D[];
			translator.PushUnityEngineRay2D(L, array8[index]);
			return true;
		}
		return false;
	}

	internal static bool __tryArraySet(Type type, IntPtr L, ObjectTranslator translator, object obj, int array_idx, int obj_idx)
	{
		if (type == typeof(Vector2[]))
		{
			Vector2[] array = obj as Vector2[];
			translator.Get(L, obj_idx, out array[array_idx]);
			return true;
		}
		if (type == typeof(Vector3[]))
		{
			Vector3[] array2 = obj as Vector3[];
			translator.Get(L, obj_idx, out array2[array_idx]);
			return true;
		}
		if (type == typeof(Vector4[]))
		{
			Vector4[] array3 = obj as Vector4[];
			translator.Get(L, obj_idx, out array3[array_idx]);
			return true;
		}
		if (type == typeof(Color[]))
		{
			Color[] array4 = obj as Color[];
			translator.Get(L, obj_idx, out array4[array_idx]);
			return true;
		}
		if (type == typeof(Quaternion[]))
		{
			Quaternion[] array5 = obj as Quaternion[];
			translator.Get(L, obj_idx, out array5[array_idx]);
			return true;
		}
		if (type == typeof(Ray[]))
		{
			Ray[] array6 = obj as Ray[];
			translator.Get(L, obj_idx, out array6[array_idx]);
			return true;
		}
		if (type == typeof(Bounds[]))
		{
			Bounds[] array7 = obj as Bounds[];
			translator.Get(L, obj_idx, out array7[array_idx]);
			return true;
		}
		if (type == typeof(Ray2D[]))
		{
			Ray2D[] array8 = obj as Ray2D[];
			translator.Get(L, obj_idx, out array8[array_idx]);
			return true;
		}
		return false;
	}

	public StaticLuaCallbacks()
	{
		GcMeta = LuaGC;
		ToStringMeta = ToString;
		EnumAndMeta = EnumAnd;
		EnumOrMeta = EnumOr;
		StaticCSFunctionWraper = StaticCSFunction;
		FixCSFunctionWraper = FixCSFunction;
		DelegateCtor = DelegateConstructor;
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int EnumAnd(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			object obj = objectTranslator.FastGetCSObj(L, 1);
			object obj2 = objectTranslator.FastGetCSObj(L, 2);
			Type type = obj.GetType();
			if (!type.IsEnum() || type != obj2.GetType())
			{
				return Lua.luaL_error(L, "invalid argument for Enum BitwiseAnd");
			}
			objectTranslator.PushAny(L, Enum.ToObject(type, Convert.ToInt64(obj) & Convert.ToInt64(obj2)));
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in Enum BitwiseAnd:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int EnumOr(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			object obj = objectTranslator.FastGetCSObj(L, 1);
			object obj2 = objectTranslator.FastGetCSObj(L, 2);
			Type type = obj.GetType();
			if (!type.IsEnum() || type != obj2.GetType())
			{
				return Lua.luaL_error(L, "invalid argument for Enum BitwiseOr");
			}
			objectTranslator.PushAny(L, Enum.ToObject(type, Convert.ToInt64(obj) | Convert.ToInt64(obj2)));
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in Enum BitwiseOr:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	private static int StaticCSFunction(IntPtr L)
	{
		try
		{
			return ((lua_CSFunction)ObjectTranslatorPool.Instance.Find(L).FastGetCSObj(L, Lua.xlua_upvalueindex(1)))(L);
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in StaticCSFunction:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	private static int FixCSFunction(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			int index = Lua.xlua_tointeger(L, Lua.xlua_upvalueindex(1));
			return objectTranslator.GetFixCSFunction(index)(L);
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in FixCSFunction:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int DelegateCall(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			object obj = objectTranslator.FastGetCSObj(L, 1);
			if (obj == null || !(obj is Delegate))
			{
				return Lua.luaL_error(L, "trying to invoke a value that is not delegate nor callable");
			}
			return objectTranslator.methodWrapsCache.GetDelegateWrap(obj.GetType())(L);
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in DelegateCall:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int LuaGC(IntPtr L)
	{
		try
		{
			int num = Lua.xlua_tocsobj_safe(L, 1);
			if (num != -1)
			{
				ObjectTranslatorPool.Instance.Find(L)?.collectObject(num);
			}
			return 0;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in LuaGC:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int ToString(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			object obj = objectTranslator.FastGetCSObj(L, 1);
			objectTranslator.PushAny(L, (obj != null) ? (obj.ToString() + ": " + obj.GetHashCode()) : "<invalid c# object>");
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in ToString:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int DelegateCombine(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			Type type = objectTranslator.FastGetCSObj(L, (Lua.lua_type(L, 1) == LuaTypes.LUA_TUSERDATA) ? 1 : 2).GetType();
			Delegate @delegate = objectTranslator.GetObject(L, 1, type) as Delegate;
			Delegate delegate2 = objectTranslator.GetObject(L, 2, type) as Delegate;
			if ((object)@delegate == null || (object)delegate2 == null)
			{
				return Lua.luaL_error(L, "one parameter must be a delegate, other one must be delegate or function");
			}
			objectTranslator.PushAny(L, Delegate.Combine(@delegate, delegate2));
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in DelegateCombine:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int DelegateRemove(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			if (!(objectTranslator.FastGetCSObj(L, 1) is Delegate @delegate))
			{
				return Lua.luaL_error(L, "#1 parameter must be a delegate");
			}
			if (!(objectTranslator.GetObject(L, 2, @delegate.GetType()) is Delegate value))
			{
				return Lua.luaL_error(L, "#2 parameter must be a delegate or a function ");
			}
			objectTranslator.PushAny(L, Delegate.Remove(@delegate, value));
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in DelegateRemove:" + ex);
		}
	}

	private static bool tryPrimitiveArrayGet(Type type, IntPtr L, object obj, int index)
	{
		bool result = true;
		if (type == typeof(int[]))
		{
			int[] array = obj as int[];
			Lua.xlua_pushinteger(L, array[index]);
		}
		else if (type == typeof(float[]))
		{
			float[] array2 = obj as float[];
			Lua.lua_pushnumber(L, array2[index]);
		}
		else if (type == typeof(double[]))
		{
			double[] array3 = obj as double[];
			Lua.lua_pushnumber(L, array3[index]);
		}
		else if (type == typeof(bool[]))
		{
			bool[] array4 = obj as bool[];
			Lua.lua_pushboolean(L, array4[index]);
		}
		else if (type == typeof(long[]))
		{
			long[] array5 = obj as long[];
			Lua.lua_pushint64(L, array5[index]);
		}
		else if (type == typeof(ulong[]))
		{
			ulong[] array6 = obj as ulong[];
			Lua.lua_pushuint64(L, array6[index]);
		}
		else if (type == typeof(sbyte[]))
		{
			sbyte[] array7 = obj as sbyte[];
			Lua.xlua_pushinteger(L, array7[index]);
		}
		else if (type == typeof(short[]))
		{
			short[] array8 = obj as short[];
			Lua.xlua_pushinteger(L, array8[index]);
		}
		else if (type == typeof(ushort[]))
		{
			ushort[] array9 = obj as ushort[];
			Lua.xlua_pushinteger(L, array9[index]);
		}
		else if (type == typeof(char[]))
		{
			char[] array10 = obj as char[];
			Lua.xlua_pushinteger(L, array10[index]);
		}
		else if (type == typeof(uint[]))
		{
			uint[] array11 = obj as uint[];
			Lua.xlua_pushuint(L, array11[index]);
		}
		else if (type == typeof(IntPtr[]))
		{
			IntPtr[] array12 = obj as IntPtr[];
			Lua.lua_pushlightuserdata(L, array12[index]);
		}
		else if (type == typeof(decimal[]))
		{
			decimal[] array13 = obj as decimal[];
			ObjectTranslatorPool.Instance.Find(L).PushDecimal(L, array13[index]);
		}
		else if (type == typeof(string[]))
		{
			string[] array14 = obj as string[];
			Lua.lua_pushstring(L, array14[index]);
		}
		else
		{
			result = false;
		}
		return result;
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int ArrayIndexer(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			Array array = (Array)objectTranslator.FastGetCSObj(L, 1);
			if (array == null)
			{
				return Lua.luaL_error(L, "#1 parameter is not a array!");
			}
			int num = Lua.xlua_tointeger(L, 2);
			if (num >= array.Length)
			{
				return Lua.luaL_error(L, "index out of range! i =" + num + ", array.Length=" + array.Length);
			}
			Type type = array.GetType();
			if (tryPrimitiveArrayGet(type, L, array, num))
			{
				return 1;
			}
			if (InternalGlobals.genTryArrayGetPtr != null)
			{
				try
				{
					if (InternalGlobals.genTryArrayGetPtr(type, L, objectTranslator, array, num))
					{
						return 1;
					}
				}
				catch (Exception ex)
				{
					return Lua.luaL_error(L, "c# exception:" + ex.Message + ",stack:" + ex.StackTrace);
				}
			}
			object value = array.GetValue(num);
			objectTranslator.PushAny(L, value);
			return 1;
		}
		catch (Exception ex2)
		{
			return Lua.luaL_error(L, "c# exception in ArrayIndexer:" + ex2);
		}
	}

	public static bool TryPrimitiveArraySet(Type type, IntPtr L, object obj, int array_idx, int obj_idx)
	{
		bool result = true;
		LuaTypes luaTypes = Lua.lua_type(L, obj_idx);
		if (type == typeof(int[]) && luaTypes == LuaTypes.LUA_TNUMBER)
		{
			(obj as int[])[array_idx] = Lua.xlua_tointeger(L, obj_idx);
		}
		else if (type == typeof(float[]) && luaTypes == LuaTypes.LUA_TNUMBER)
		{
			(obj as float[])[array_idx] = (float)Lua.lua_tonumber(L, obj_idx);
		}
		else if (type == typeof(double[]) && luaTypes == LuaTypes.LUA_TNUMBER)
		{
			(obj as double[])[array_idx] = Lua.lua_tonumber(L, obj_idx);
		}
		else if (type == typeof(bool[]) && luaTypes == LuaTypes.LUA_TBOOLEAN)
		{
			(obj as bool[])[array_idx] = Lua.lua_toboolean(L, obj_idx);
		}
		else if (type == typeof(long[]) && Lua.lua_isint64(L, obj_idx))
		{
			(obj as long[])[array_idx] = Lua.lua_toint64(L, obj_idx);
		}
		else if (type == typeof(ulong[]) && Lua.lua_isuint64(L, obj_idx))
		{
			(obj as ulong[])[array_idx] = Lua.lua_touint64(L, obj_idx);
		}
		else if (type == typeof(sbyte[]) && luaTypes == LuaTypes.LUA_TNUMBER)
		{
			(obj as sbyte[])[array_idx] = (sbyte)Lua.xlua_tointeger(L, obj_idx);
		}
		else if (type == typeof(short[]) && luaTypes == LuaTypes.LUA_TNUMBER)
		{
			(obj as short[])[array_idx] = (short)Lua.xlua_tointeger(L, obj_idx);
		}
		else if (type == typeof(ushort[]) && luaTypes == LuaTypes.LUA_TNUMBER)
		{
			(obj as ushort[])[array_idx] = (ushort)Lua.xlua_tointeger(L, obj_idx);
		}
		else if (type == typeof(char[]) && luaTypes == LuaTypes.LUA_TNUMBER)
		{
			(obj as char[])[array_idx] = (char)Lua.xlua_tointeger(L, obj_idx);
		}
		else if (type == typeof(uint[]) && luaTypes == LuaTypes.LUA_TNUMBER)
		{
			(obj as uint[])[array_idx] = Lua.xlua_touint(L, obj_idx);
		}
		else if (type == typeof(IntPtr[]) && luaTypes == LuaTypes.LUA_TLIGHTUSERDATA)
		{
			(obj as IntPtr[])[array_idx] = Lua.lua_touserdata(L, obj_idx);
		}
		else if (type == typeof(decimal[]))
		{
			decimal[] array = obj as decimal[];
			if (luaTypes == LuaTypes.LUA_TNUMBER)
			{
				array[array_idx] = (decimal)Lua.lua_tonumber(L, obj_idx);
			}
			if (luaTypes == LuaTypes.LUA_TUSERDATA)
			{
				ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
				if (objectTranslator.IsDecimal(L, obj_idx))
				{
					objectTranslator.Get(L, obj_idx, out array[array_idx]);
				}
				else
				{
					result = false;
				}
			}
			else
			{
				result = false;
			}
		}
		else if (type == typeof(string[]) && luaTypes == LuaTypes.LUA_TSTRING)
		{
			(obj as string[])[array_idx] = Lua.lua_tostring(L, obj_idx);
		}
		else
		{
			result = false;
		}
		return result;
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int ArrayNewIndexer(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			Array array = (Array)objectTranslator.FastGetCSObj(L, 1);
			if (array == null)
			{
				return Lua.luaL_error(L, "#1 parameter is not a array!");
			}
			int num = Lua.xlua_tointeger(L, 2);
			if (num >= array.Length)
			{
				return Lua.luaL_error(L, "index out of range! i =" + num + ", array.Length=" + array.Length);
			}
			Type type = array.GetType();
			if (TryPrimitiveArraySet(type, L, array, num, 3))
			{
				return 0;
			}
			if (InternalGlobals.genTryArraySetPtr != null)
			{
				try
				{
					if (InternalGlobals.genTryArraySetPtr(type, L, objectTranslator, array, num, 3))
					{
						return 0;
					}
				}
				catch (Exception ex)
				{
					return Lua.luaL_error(L, "c# exception:" + ex.Message + ",stack:" + ex.StackTrace);
				}
			}
			object @object = objectTranslator.GetObject(L, 3, type.GetElementType());
			array.SetValue(@object, num);
			return 0;
		}
		catch (Exception ex2)
		{
			return Lua.luaL_error(L, "c# exception in ArrayNewIndexer:" + ex2);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int ArrayLength(IntPtr L)
	{
		try
		{
			Array array = (Array)ObjectTranslatorPool.Instance.Find(L).FastGetCSObj(L, 1);
			Lua.xlua_pushinteger(L, array.Length);
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in ArrayLength:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int MetaFuncIndex(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			Type type = objectTranslator.FastGetCSObj(L, 2) as Type;
			if (type == null)
			{
				return Lua.luaL_error(L, "#2 param need a System.Type!");
			}
			objectTranslator.GetTypeId(L, type);
			Lua.lua_pushvalue(L, 2);
			Lua.lua_rawget(L, 1);
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in MetaFuncIndex:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	internal static int Panic(IntPtr L)
	{
		throw new LuaException($"unprotected error in call to Lua API ({Lua.lua_tostring(L, -1)})");
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	internal static int Print(IntPtr L)
	{
		try
		{
			int num = Lua.lua_gettop(L);
			string text = string.Empty;
			if (Lua.xlua_getglobal(L, "tostring") != 0)
			{
				return Lua.luaL_error(L, "can not get tostring in print:");
			}
			for (int i = 1; i <= num; i++)
			{
				Lua.lua_pushvalue(L, -1);
				Lua.lua_pushvalue(L, i);
				if (Lua.lua_pcall(L, 1, 1, 0) != 0)
				{
					return Lua.lua_error(L);
				}
				text += Lua.lua_tostring(L, -1);
				if (i != num)
				{
					text += "\t";
				}
				Lua.lua_pop(L, 1);
			}
			Debug.Log("LUA: " + text);
			return 0;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in print:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	internal static int LoadSocketCore(IntPtr L)
	{
		return Lua.luaopen_socket_core(L);
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	internal static int LoadCS(IntPtr L)
	{
		Lua.xlua_pushasciistring(L, "xlua_csharp_namespace");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		return 1;
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	internal static int LoadBuiltinLib(IntPtr L)
	{
		try
		{
			string text = Lua.lua_tostring(L, 1);
			if (ObjectTranslatorPool.Instance.Find(L).luaEnv.buildin_initer.TryGetValue(text, out var value))
			{
				Lua.lua_pushstdcallcfunction(L, value);
			}
			else
			{
				Lua.lua_pushstring(L, $"\n\tno such builtin lib '{text}'");
			}
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in LoadBuiltinLib:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	internal static int LoadFromResource(IntPtr L)
	{
		try
		{
			string text = Lua.lua_tostring(L, 1).Replace('.', '/') + ".lua";
			TextAsset textAsset = (TextAsset)Resources.Load(text);
			if (textAsset == null)
			{
				Lua.lua_pushstring(L, $"\n\tno such resource '{text}'");
			}
			else if (Lua.xluaL_loadbuffer(L, textAsset.bytes, textAsset.bytes.Length, "@" + text) != 0)
			{
				return Lua.luaL_error(L, $"error loading module {Lua.lua_tostring(L, 1)} from resource, {Lua.lua_tostring(L, -1)}");
			}
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in LoadFromResource:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	internal static int LoadFromStreamingAssetsPath(IntPtr L)
	{
		try
		{
			string text = Lua.lua_tostring(L, 1).Replace('.', '/') + ".lua";
			string path = Application.streamingAssetsPath + "/" + text;
			if (File.Exists(path))
			{
				byte[] array = File.ReadAllBytes(path);
				Debug.LogWarning("load lua file from StreamingAssets is obsolete, filename:" + text);
				if (Lua.xluaL_loadbuffer(L, array, array.Length, "@" + text) != 0)
				{
					return Lua.luaL_error(L, $"error loading module {Lua.lua_tostring(L, 1)} from streamingAssetsPath, {Lua.lua_tostring(L, -1)}");
				}
			}
			else
			{
				Lua.lua_pushstring(L, $"\n\tno such file '{text}' in streamingAssetsPath!");
			}
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in LoadFromStreamingAssetsPath:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	internal static int LoadFromCustomLoaders(IntPtr L)
	{
		try
		{
			string text = Lua.lua_tostring(L, 1);
			foreach (LuaEnv.CustomLoader customLoader in ObjectTranslatorPool.Instance.Find(L).luaEnv.customLoaders)
			{
				string filepath = text;
				byte[] array = customLoader(ref filepath);
				if (array != null)
				{
					if (Lua.xluaL_loadbuffer(L, array, array.Length, "@" + filepath) != 0)
					{
						return Lua.luaL_error(L, $"error loading module {Lua.lua_tostring(L, 1)} from CustomLoader, {Lua.lua_tostring(L, -1)}");
					}
					return 1;
				}
			}
			Lua.lua_pushstring(L, $"\n\tno such file '{text}' in CustomLoaders!");
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in LoadFromCustomLoaders:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int LoadAssembly(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			string text = Lua.lua_tostring(L, 1);
			Assembly assembly = null;
			try
			{
				assembly = Assembly.Load(text);
			}
			catch (BadImageFormatException)
			{
			}
			if (assembly == null)
			{
				assembly = Assembly.Load(AssemblyName.GetAssemblyName(text));
			}
			if (assembly != null && !objectTranslator.assemblies.Contains(assembly))
			{
				objectTranslator.assemblies.Add(assembly);
			}
			return 0;
		}
		catch (Exception ex2)
		{
			return Lua.luaL_error(L, "c# exception in xlua.load_assembly:" + ex2);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int ImportType(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			string className = Lua.lua_tostring(L, 1);
			Type type = objectTranslator.FindType(className);
			if (type != null)
			{
				if (objectTranslator.GetTypeId(L, type) < 0)
				{
					return Lua.luaL_error(L, "can not load type " + type);
				}
				Lua.lua_pushboolean(L, value: true);
			}
			else
			{
				Lua.lua_pushnil(L);
			}
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in xlua.import_type:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int ImportGenericType(IntPtr L)
	{
		try
		{
			int num = Lua.lua_gettop(L);
			if (num < 2)
			{
				return Lua.luaL_error(L, "import generic type need at lease 2 arguments");
			}
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			string text = Lua.lua_tostring(L, 1);
			if (text.EndsWith("<>"))
			{
				text = text.Substring(0, text.Length - 2);
			}
			Type type = objectTranslator.FindType(text + "`" + (num - 1));
			if (type == null || !type.IsGenericTypeDefinition())
			{
				Lua.lua_pushnil(L);
			}
			else
			{
				Type[] array = new Type[num - 1];
				for (int i = 2; i <= num; i++)
				{
					array[i - 2] = getType(L, objectTranslator, i);
					if (array[i - 2] == null)
					{
						return Lua.luaL_error(L, "param need a type");
					}
				}
				Type type2 = type.MakeGenericType(array);
				objectTranslator.GetTypeId(L, type2);
				objectTranslator.PushAny(L, type2);
			}
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in xlua.import_type:" + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int Cast(IntPtr L)
	{
		try
		{
			ObjectTranslatorPool.Instance.Find(L).Get(L, 2, out Type v);
			if (v == null)
			{
				return Lua.luaL_error(L, "#2 param[" + Lua.lua_tostring(L, 2) + "]is not valid type indicator");
			}
			Lua.luaL_getmetatable(L, v.FullName);
			if (Lua.lua_isnil(L, -1))
			{
				return Lua.luaL_error(L, "no gen code for " + Lua.lua_tostring(L, 2));
			}
			Lua.lua_setmetatable(L, 1);
			return 0;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in xlua.cast:" + ex);
		}
	}

	private static Type getType(IntPtr L, ObjectTranslator translator, int idx)
	{
		if (Lua.lua_type(L, idx) == LuaTypes.LUA_TTABLE)
		{
			translator.Get(L, idx, out LuaTable v);
			return v.Get<Type>("UnderlyingSystemType");
		}
		if (Lua.lua_type(L, idx) == LuaTypes.LUA_TSTRING)
		{
			string className = Lua.lua_tostring(L, idx);
			return translator.FindType(className);
		}
		if (translator.GetObject(L, idx) is Type)
		{
			return translator.GetObject(L, idx) as Type;
		}
		return null;
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int XLuaAccess(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			Type type = getType(L, objectTranslator, 1);
			object obj = null;
			if (type == null && Lua.lua_type(L, 1) == LuaTypes.LUA_TUSERDATA)
			{
				obj = objectTranslator.SafeGetCSObj(L, 1);
				if (obj == null)
				{
					return Lua.luaL_error(L, "xlua.access, #1 parameter must a type/c# object/string");
				}
				type = obj.GetType();
			}
			if (type == null)
			{
				return Lua.luaL_error(L, "xlua.access, can not find c# type");
			}
			string text = Lua.lua_tostring(L, 2);
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			if (Lua.lua_gettop(L) > 2)
			{
				FieldInfo field = type.GetField(text, bindingAttr);
				if (field != null)
				{
					field.SetValue(obj, objectTranslator.GetObject(L, 3, field.FieldType));
					return 0;
				}
				PropertyInfo property = type.GetProperty(text, bindingAttr);
				if (property != null)
				{
					property.SetValue(obj, objectTranslator.GetObject(L, 3, property.PropertyType), null);
					return 0;
				}
			}
			else
			{
				FieldInfo field2 = type.GetField(text, bindingAttr);
				if (field2 != null)
				{
					objectTranslator.PushAny(L, field2.GetValue(obj));
					return 1;
				}
				PropertyInfo property2 = type.GetProperty(text, bindingAttr);
				if (property2 != null)
				{
					objectTranslator.PushAny(L, property2.GetValue(obj, null));
					return 1;
				}
			}
			return Lua.luaL_error(L, "xlua.access, no field " + text);
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in xlua.access: " + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int XLuaPrivateAccessible(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			Type type = getType(L, objectTranslator, 1);
			if (type == null)
			{
				return Lua.luaL_error(L, "xlua.private_accessible, can not find c# type");
			}
			while (type != null)
			{
				objectTranslator.PrivateAccessible(L, type);
				type = type.BaseType();
			}
			return 0;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in xlua.private_accessible: " + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int XLuaMetatableOperation(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			Type type = getType(L, objectTranslator, 1);
			if (type == null)
			{
				return Lua.luaL_error(L, "xlua.metatable_operation, can not find c# type");
			}
			bool is_first = false;
			int typeId = objectTranslator.getTypeId(L, type, out is_first);
			int num = Lua.lua_gettop(L);
			switch (num)
			{
			case 1:
				Lua.xlua_rawgeti(L, LuaIndexes.LUA_REGISTRYINDEX, typeId);
				return 1;
			case 2:
				if (Lua.lua_type(L, 2) != LuaTypes.LUA_TTABLE)
				{
					return Lua.luaL_error(L, "argument #2 must be a table");
				}
				Lua.lua_pushnumber(L, typeId);
				Lua.xlua_rawseti(L, 2, 1L);
				Lua.xlua_rawseti(L, LuaIndexes.LUA_REGISTRYINDEX, typeId);
				return 0;
			default:
				return Lua.luaL_error(L, "invalid argument num for xlua.metatable_operation: " + num);
			}
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in xlua.metatable_operation: " + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int DelegateConstructor(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			Type type = getType(L, objectTranslator, 1);
			if (type == null || !typeof(Delegate).IsAssignableFrom(type))
			{
				return Lua.luaL_error(L, "delegate constructor: #1 argument must be a Delegate's type");
			}
			objectTranslator.PushAny(L, objectTranslator.GetObject(L, 2, type));
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in delegate constructor: " + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int ToFunction(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			objectTranslator.Get(L, 1, out MethodBase v);
			if (v == null)
			{
				return Lua.luaL_error(L, "ToFunction: #1 argument must be a MethodBase");
			}
			objectTranslator.PushFixCSFunction(L, objectTranslator.methodWrapsCache._GenMethodWrap(v.DeclaringType, v.Name, new MethodBase[1] { v }).Call);
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in ToFunction: " + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int GenericMethodWraper(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			objectTranslator.Get(L, Lua.xlua_upvalueindex(1), out MethodInfo v);
			int num = Lua.lua_gettop(L);
			Type[] array = new Type[num];
			for (int i = 0; i < num; i++)
			{
				Type type = getType(L, objectTranslator, i + 1);
				if (type == null)
				{
					return Lua.luaL_error(L, "param #" + (i + 1) + " is not a type");
				}
				array[i] = type;
			}
			MethodInfo methodInfo = v.MakeGenericMethod(array);
			objectTranslator.PushFixCSFunction(L, objectTranslator.methodWrapsCache._GenMethodWrap(methodInfo.DeclaringType, methodInfo.Name, new MethodBase[1] { methodInfo }).Call);
			return 1;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in GenericMethodWraper: " + ex);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int GetGenericMethod(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			Type type = getType(L, objectTranslator, 1);
			if (type == null)
			{
				return Lua.luaL_error(L, "xlua.get_generic_method, can not find c# type");
			}
			string text = Lua.lua_tostring(L, 2);
			if (string.IsNullOrEmpty(text))
			{
				return Lua.luaL_error(L, "xlua.get_generic_method, #2 param need a string");
			}
			List<MethodInfo> list = new List<MethodInfo>();
			MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.Name == text && methodInfo.IsGenericMethodDefinition)
				{
					list.Add(methodInfo);
				}
			}
			int index = 0;
			if (list.Count == 0)
			{
				Lua.lua_pushnil(L);
			}
			else
			{
				if (Lua.lua_isinteger(L, 3))
				{
					index = Lua.xlua_tointeger(L, 3);
				}
				objectTranslator.PushAny(L, list[index]);
				Lua.lua_pushstdcallcfunction(L, GenericMethodWraper, 1);
			}
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in xlua.get_generic_method: " + ex);
		}
		return 1;
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int ReleaseCsObject(IntPtr L)
	{
		try
		{
			ObjectTranslatorPool.Instance.Find(L).ReleaseCSObj(L, 1);
			return 0;
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in ReleaseCsObject: " + ex);
		}
	}
}
