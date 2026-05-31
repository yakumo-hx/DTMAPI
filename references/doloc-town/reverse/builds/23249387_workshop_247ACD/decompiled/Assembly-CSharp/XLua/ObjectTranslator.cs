using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using XLua.CSObjectWrap;
using XLua.LuaDLL;

namespace XLua;

public class ObjectTranslator
{
	private class IniterAdderUnityEngineVector2
	{
		static IniterAdderUnityEngineVector2()
		{
			LuaEnv.AddIniter(Init);
		}

		private static void Init(LuaEnv luaenv, ObjectTranslator translator)
		{
			translator.RegisterPushAndGetAndUpdate<Vector2>(translator.PushUnityEngineVector2, translator.Get, translator.UpdateUnityEngineVector2);
			translator.RegisterPushAndGetAndUpdate<Vector3>(translator.PushUnityEngineVector3, translator.Get, translator.UpdateUnityEngineVector3);
			translator.RegisterPushAndGetAndUpdate<Vector4>(translator.PushUnityEngineVector4, translator.Get, translator.UpdateUnityEngineVector4);
			translator.RegisterPushAndGetAndUpdate<Color>(translator.PushUnityEngineColor, translator.Get, translator.UpdateUnityEngineColor);
			translator.RegisterPushAndGetAndUpdate<Quaternion>(translator.PushUnityEngineQuaternion, translator.Get, translator.UpdateUnityEngineQuaternion);
			translator.RegisterPushAndGetAndUpdate<Ray>(translator.PushUnityEngineRay, translator.Get, translator.UpdateUnityEngineRay);
			translator.RegisterPushAndGetAndUpdate<Bounds>(translator.PushUnityEngineBounds, translator.Get, translator.UpdateUnityEngineBounds);
			translator.RegisterPushAndGetAndUpdate<Ray2D>(translator.PushUnityEngineRay2D, translator.Get, translator.UpdateUnityEngineRay2D);
		}
	}

	internal enum LOGLEVEL
	{
		NO,
		INFO,
		WARN,
		ERROR
	}

	public delegate void PushCSObject(IntPtr L, object obj);

	public delegate object GetCSObject(IntPtr L, int idx);

	public delegate void UpdateCSObject(IntPtr L, int idx, object obj);

	public delegate bool CheckFunc<T>(IntPtr L, int idx);

	public delegate void GetFunc<T>(IntPtr L, int idx, out T val);

	private static IniterAdderUnityEngineVector2 s_IniterAdderUnityEngineVector2_dumb_obj = new IniterAdderUnityEngineVector2();

	private int UnityEngineVector2_TypeID = -1;

	private int UnityEngineVector3_TypeID = -1;

	private int UnityEngineVector4_TypeID = -1;

	private int UnityEngineColor_TypeID = -1;

	private int UnityEngineQuaternion_TypeID = -1;

	private int UnityEngineRay_TypeID = -1;

	private int UnityEngineBounds_TypeID = -1;

	private int UnityEngineRay2D_TypeID = -1;

	private static XLua_Gen_Initer_Register__ s_gen_reg_dumb_obj = new XLua_Gen_Initer_Register__();

	internal MethodWrapsCache methodWrapsCache;

	internal ObjectCheckers objectCheckers;

	internal ObjectCasters objectCasters;

	internal readonly ObjectPool objects = new ObjectPool();

	internal readonly Dictionary<object, int> reverseMap = new Dictionary<object, int>(new ReferenceEqualsComparer());

	internal LuaEnv luaEnv;

	internal StaticLuaCallbacks metaFunctions;

	internal List<Assembly> assemblies;

	private lua_CSFunction importTypeFunction;

	private lua_CSFunction loadAssemblyFunction;

	private lua_CSFunction castFunction;

	private readonly Dictionary<Type, Action<IntPtr>> delayWrap = new Dictionary<Type, Action<IntPtr>>();

	private readonly Dictionary<Type, Func<int, LuaEnv, LuaBase>> interfaceBridgeCreators = new Dictionary<Type, Func<int, LuaEnv, LuaBase>>();

	private readonly Dictionary<Type, Type> aliasCfg = new Dictionary<Type, Type>();

	private Dictionary<Type, bool> loaded_types = new Dictionary<Type, bool>();

	public int cacheRef;

	private MethodInfo[] genericAction;

	private MethodInfo[] genericFunc;

	private Dictionary<Type, Func<DelegateBridgeBase, Delegate>> delegateCreatorCache = new Dictionary<Type, Func<DelegateBridgeBase, Delegate>>();

	private Dictionary<int, WeakReference> delegate_bridges = new Dictionary<int, WeakReference>();

	private int common_array_meta = -1;

	private int common_delegate_meta = -1;

	private int enumerable_pairs_func = -1;

	private Dictionary<Type, int> typeIdMap = new Dictionary<Type, int>();

	private Dictionary<int, Type> typeMap = new Dictionary<int, Type>();

	private HashSet<Type> privateAccessibleFlags = new HashSet<Type>();

	private Dictionary<object, int> enumMap = new Dictionary<object, int>();

	private List<lua_CSFunction> fix_cs_functions = new List<lua_CSFunction>();

	private Dictionary<Type, PushCSObject> custom_push_funcs = new Dictionary<Type, PushCSObject>();

	private Dictionary<Type, GetCSObject> custom_get_funcs = new Dictionary<Type, GetCSObject>();

	private Dictionary<Type, UpdateCSObject> custom_update_funcs = new Dictionary<Type, UpdateCSObject>();

	private Dictionary<Type, Delegate> push_func_with_type;

	private Dictionary<Type, Delegate> get_func_with_type;

	private int decimal_type_id = -1;

	private static IniterAdderUnityEngineVector2 IniterAdderUnityEngineVector2_dumb_obj => s_IniterAdderUnityEngineVector2_dumb_obj;

	private static XLua_Gen_Initer_Register__ gen_reg_dumb_obj => s_gen_reg_dumb_obj;

	public void PushUnityEngineVector2(IntPtr L, Vector2 val)
	{
		if (UnityEngineVector2_TypeID == -1)
		{
			UnityEngineVector2_TypeID = getTypeId(L, typeof(Vector2), out var _);
		}
		if (!CopyByValue.Pack(Lua.xlua_pushstruct(L, 8u, UnityEngineVector2_TypeID), 0, val))
		{
			Vector2 vector = val;
			throw new Exception("pack fail fail for UnityEngine.Vector2 ,value=" + vector.ToString());
		}
	}

	public void Get(IntPtr L, int index, out Vector2 val)
	{
		switch (Lua.lua_type(L, index))
		{
		case LuaTypes.LUA_TUSERDATA:
			if (Lua.xlua_gettypeid(L, index) != UnityEngineVector2_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Vector2");
			}
			if (!CopyByValue.UnPack(Lua.lua_touserdata(L, index), 0, out val))
			{
				throw new Exception("unpack fail for UnityEngine.Vector2");
			}
			break;
		case LuaTypes.LUA_TTABLE:
			CopyByValue.UnPack(this, L, index, out val);
			break;
		default:
			val = (Vector2)objectCasters.GetCaster(typeof(Vector2))(L, index, null);
			break;
		}
	}

	public void UpdateUnityEngineVector2(IntPtr L, int index, Vector2 val)
	{
		if (Lua.lua_type(L, index) == LuaTypes.LUA_TUSERDATA)
		{
			if (Lua.xlua_gettypeid(L, index) != UnityEngineVector2_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Vector2");
			}
			if (!CopyByValue.Pack(Lua.lua_touserdata(L, index), 0, val))
			{
				Vector2 vector = val;
				throw new Exception("pack fail for UnityEngine.Vector2 ,value=" + vector.ToString());
			}
			return;
		}
		throw new Exception("try to update a data with lua type:" + Lua.lua_type(L, index));
	}

	public void PushUnityEngineVector3(IntPtr L, Vector3 val)
	{
		if (UnityEngineVector3_TypeID == -1)
		{
			UnityEngineVector3_TypeID = getTypeId(L, typeof(Vector3), out var _);
		}
		if (!CopyByValue.Pack(Lua.xlua_pushstruct(L, 12u, UnityEngineVector3_TypeID), 0, val))
		{
			Vector3 vector = val;
			throw new Exception("pack fail fail for UnityEngine.Vector3 ,value=" + vector.ToString());
		}
	}

	public void Get(IntPtr L, int index, out Vector3 val)
	{
		switch (Lua.lua_type(L, index))
		{
		case LuaTypes.LUA_TUSERDATA:
			if (Lua.xlua_gettypeid(L, index) != UnityEngineVector3_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Vector3");
			}
			if (!CopyByValue.UnPack(Lua.lua_touserdata(L, index), 0, out val))
			{
				throw new Exception("unpack fail for UnityEngine.Vector3");
			}
			break;
		case LuaTypes.LUA_TTABLE:
			CopyByValue.UnPack(this, L, index, out val);
			break;
		default:
			val = (Vector3)objectCasters.GetCaster(typeof(Vector3))(L, index, null);
			break;
		}
	}

	public void UpdateUnityEngineVector3(IntPtr L, int index, Vector3 val)
	{
		if (Lua.lua_type(L, index) == LuaTypes.LUA_TUSERDATA)
		{
			if (Lua.xlua_gettypeid(L, index) != UnityEngineVector3_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Vector3");
			}
			if (!CopyByValue.Pack(Lua.lua_touserdata(L, index), 0, val))
			{
				Vector3 vector = val;
				throw new Exception("pack fail for UnityEngine.Vector3 ,value=" + vector.ToString());
			}
			return;
		}
		throw new Exception("try to update a data with lua type:" + Lua.lua_type(L, index));
	}

	public void PushUnityEngineVector4(IntPtr L, Vector4 val)
	{
		if (UnityEngineVector4_TypeID == -1)
		{
			UnityEngineVector4_TypeID = getTypeId(L, typeof(Vector4), out var _);
		}
		if (!CopyByValue.Pack(Lua.xlua_pushstruct(L, 16u, UnityEngineVector4_TypeID), 0, val))
		{
			Vector4 vector = val;
			throw new Exception("pack fail fail for UnityEngine.Vector4 ,value=" + vector.ToString());
		}
	}

	public void Get(IntPtr L, int index, out Vector4 val)
	{
		switch (Lua.lua_type(L, index))
		{
		case LuaTypes.LUA_TUSERDATA:
			if (Lua.xlua_gettypeid(L, index) != UnityEngineVector4_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Vector4");
			}
			if (!CopyByValue.UnPack(Lua.lua_touserdata(L, index), 0, out val))
			{
				throw new Exception("unpack fail for UnityEngine.Vector4");
			}
			break;
		case LuaTypes.LUA_TTABLE:
			CopyByValue.UnPack(this, L, index, out val);
			break;
		default:
			val = (Vector4)objectCasters.GetCaster(typeof(Vector4))(L, index, null);
			break;
		}
	}

	public void UpdateUnityEngineVector4(IntPtr L, int index, Vector4 val)
	{
		if (Lua.lua_type(L, index) == LuaTypes.LUA_TUSERDATA)
		{
			if (Lua.xlua_gettypeid(L, index) != UnityEngineVector4_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Vector4");
			}
			if (!CopyByValue.Pack(Lua.lua_touserdata(L, index), 0, val))
			{
				Vector4 vector = val;
				throw new Exception("pack fail for UnityEngine.Vector4 ,value=" + vector.ToString());
			}
			return;
		}
		throw new Exception("try to update a data with lua type:" + Lua.lua_type(L, index));
	}

	public void PushUnityEngineColor(IntPtr L, Color val)
	{
		if (UnityEngineColor_TypeID == -1)
		{
			UnityEngineColor_TypeID = getTypeId(L, typeof(Color), out var _);
		}
		if (!CopyByValue.Pack(Lua.xlua_pushstruct(L, 16u, UnityEngineColor_TypeID), 0, val))
		{
			Color color = val;
			throw new Exception("pack fail fail for UnityEngine.Color ,value=" + color.ToString());
		}
	}

	public void Get(IntPtr L, int index, out Color val)
	{
		switch (Lua.lua_type(L, index))
		{
		case LuaTypes.LUA_TUSERDATA:
			if (Lua.xlua_gettypeid(L, index) != UnityEngineColor_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Color");
			}
			if (!CopyByValue.UnPack(Lua.lua_touserdata(L, index), 0, out val))
			{
				throw new Exception("unpack fail for UnityEngine.Color");
			}
			break;
		case LuaTypes.LUA_TTABLE:
			CopyByValue.UnPack(this, L, index, out val);
			break;
		default:
			val = (Color)objectCasters.GetCaster(typeof(Color))(L, index, null);
			break;
		}
	}

	public void UpdateUnityEngineColor(IntPtr L, int index, Color val)
	{
		if (Lua.lua_type(L, index) == LuaTypes.LUA_TUSERDATA)
		{
			if (Lua.xlua_gettypeid(L, index) != UnityEngineColor_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Color");
			}
			if (!CopyByValue.Pack(Lua.lua_touserdata(L, index), 0, val))
			{
				Color color = val;
				throw new Exception("pack fail for UnityEngine.Color ,value=" + color.ToString());
			}
			return;
		}
		throw new Exception("try to update a data with lua type:" + Lua.lua_type(L, index));
	}

	public void PushUnityEngineQuaternion(IntPtr L, Quaternion val)
	{
		if (UnityEngineQuaternion_TypeID == -1)
		{
			UnityEngineQuaternion_TypeID = getTypeId(L, typeof(Quaternion), out var _);
		}
		if (!CopyByValue.Pack(Lua.xlua_pushstruct(L, 16u, UnityEngineQuaternion_TypeID), 0, val))
		{
			Quaternion quaternion = val;
			throw new Exception("pack fail fail for UnityEngine.Quaternion ,value=" + quaternion.ToString());
		}
	}

	public void Get(IntPtr L, int index, out Quaternion val)
	{
		switch (Lua.lua_type(L, index))
		{
		case LuaTypes.LUA_TUSERDATA:
			if (Lua.xlua_gettypeid(L, index) != UnityEngineQuaternion_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Quaternion");
			}
			if (!CopyByValue.UnPack(Lua.lua_touserdata(L, index), 0, out val))
			{
				throw new Exception("unpack fail for UnityEngine.Quaternion");
			}
			break;
		case LuaTypes.LUA_TTABLE:
			CopyByValue.UnPack(this, L, index, out val);
			break;
		default:
			val = (Quaternion)objectCasters.GetCaster(typeof(Quaternion))(L, index, null);
			break;
		}
	}

	public void UpdateUnityEngineQuaternion(IntPtr L, int index, Quaternion val)
	{
		if (Lua.lua_type(L, index) == LuaTypes.LUA_TUSERDATA)
		{
			if (Lua.xlua_gettypeid(L, index) != UnityEngineQuaternion_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Quaternion");
			}
			if (!CopyByValue.Pack(Lua.lua_touserdata(L, index), 0, val))
			{
				Quaternion quaternion = val;
				throw new Exception("pack fail for UnityEngine.Quaternion ,value=" + quaternion.ToString());
			}
			return;
		}
		throw new Exception("try to update a data with lua type:" + Lua.lua_type(L, index));
	}

	public void PushUnityEngineRay(IntPtr L, Ray val)
	{
		if (UnityEngineRay_TypeID == -1)
		{
			UnityEngineRay_TypeID = getTypeId(L, typeof(Ray), out var _);
		}
		if (!CopyByValue.Pack(Lua.xlua_pushstruct(L, 24u, UnityEngineRay_TypeID), 0, val))
		{
			Ray ray = val;
			throw new Exception("pack fail fail for UnityEngine.Ray ,value=" + ray.ToString());
		}
	}

	public void Get(IntPtr L, int index, out Ray val)
	{
		switch (Lua.lua_type(L, index))
		{
		case LuaTypes.LUA_TUSERDATA:
			if (Lua.xlua_gettypeid(L, index) != UnityEngineRay_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Ray");
			}
			if (!CopyByValue.UnPack(Lua.lua_touserdata(L, index), 0, out val))
			{
				throw new Exception("unpack fail for UnityEngine.Ray");
			}
			break;
		case LuaTypes.LUA_TTABLE:
			CopyByValue.UnPack(this, L, index, out val);
			break;
		default:
			val = (Ray)objectCasters.GetCaster(typeof(Ray))(L, index, null);
			break;
		}
	}

	public void UpdateUnityEngineRay(IntPtr L, int index, Ray val)
	{
		if (Lua.lua_type(L, index) == LuaTypes.LUA_TUSERDATA)
		{
			if (Lua.xlua_gettypeid(L, index) != UnityEngineRay_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Ray");
			}
			if (!CopyByValue.Pack(Lua.lua_touserdata(L, index), 0, val))
			{
				Ray ray = val;
				throw new Exception("pack fail for UnityEngine.Ray ,value=" + ray.ToString());
			}
			return;
		}
		throw new Exception("try to update a data with lua type:" + Lua.lua_type(L, index));
	}

	public void PushUnityEngineBounds(IntPtr L, Bounds val)
	{
		if (UnityEngineBounds_TypeID == -1)
		{
			UnityEngineBounds_TypeID = getTypeId(L, typeof(Bounds), out var _);
		}
		if (!CopyByValue.Pack(Lua.xlua_pushstruct(L, 24u, UnityEngineBounds_TypeID), 0, val))
		{
			Bounds bounds = val;
			throw new Exception("pack fail fail for UnityEngine.Bounds ,value=" + bounds.ToString());
		}
	}

	public void Get(IntPtr L, int index, out Bounds val)
	{
		switch (Lua.lua_type(L, index))
		{
		case LuaTypes.LUA_TUSERDATA:
			if (Lua.xlua_gettypeid(L, index) != UnityEngineBounds_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Bounds");
			}
			if (!CopyByValue.UnPack(Lua.lua_touserdata(L, index), 0, out val))
			{
				throw new Exception("unpack fail for UnityEngine.Bounds");
			}
			break;
		case LuaTypes.LUA_TTABLE:
			CopyByValue.UnPack(this, L, index, out val);
			break;
		default:
			val = (Bounds)objectCasters.GetCaster(typeof(Bounds))(L, index, null);
			break;
		}
	}

	public void UpdateUnityEngineBounds(IntPtr L, int index, Bounds val)
	{
		if (Lua.lua_type(L, index) == LuaTypes.LUA_TUSERDATA)
		{
			if (Lua.xlua_gettypeid(L, index) != UnityEngineBounds_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Bounds");
			}
			if (!CopyByValue.Pack(Lua.lua_touserdata(L, index), 0, val))
			{
				Bounds bounds = val;
				throw new Exception("pack fail for UnityEngine.Bounds ,value=" + bounds.ToString());
			}
			return;
		}
		throw new Exception("try to update a data with lua type:" + Lua.lua_type(L, index));
	}

	public void PushUnityEngineRay2D(IntPtr L, Ray2D val)
	{
		if (UnityEngineRay2D_TypeID == -1)
		{
			UnityEngineRay2D_TypeID = getTypeId(L, typeof(Ray2D), out var _);
		}
		if (!CopyByValue.Pack(Lua.xlua_pushstruct(L, 16u, UnityEngineRay2D_TypeID), 0, val))
		{
			Ray2D ray2D = val;
			throw new Exception("pack fail fail for UnityEngine.Ray2D ,value=" + ray2D.ToString());
		}
	}

	public void Get(IntPtr L, int index, out Ray2D val)
	{
		switch (Lua.lua_type(L, index))
		{
		case LuaTypes.LUA_TUSERDATA:
			if (Lua.xlua_gettypeid(L, index) != UnityEngineRay2D_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Ray2D");
			}
			if (!CopyByValue.UnPack(Lua.lua_touserdata(L, index), 0, out val))
			{
				throw new Exception("unpack fail for UnityEngine.Ray2D");
			}
			break;
		case LuaTypes.LUA_TTABLE:
			CopyByValue.UnPack(this, L, index, out val);
			break;
		default:
			val = (Ray2D)objectCasters.GetCaster(typeof(Ray2D))(L, index, null);
			break;
		}
	}

	public void UpdateUnityEngineRay2D(IntPtr L, int index, Ray2D val)
	{
		if (Lua.lua_type(L, index) == LuaTypes.LUA_TUSERDATA)
		{
			if (Lua.xlua_gettypeid(L, index) != UnityEngineRay2D_TypeID)
			{
				throw new Exception("invalid userdata for UnityEngine.Ray2D");
			}
			if (!CopyByValue.Pack(Lua.lua_touserdata(L, index), 0, val))
			{
				Ray2D ray2D = val;
				throw new Exception("pack fail for UnityEngine.Ray2D ,value=" + ray2D.ToString());
			}
			return;
		}
		throw new Exception("try to update a data with lua type:" + Lua.lua_type(L, index));
	}

	public void DelayWrapLoader(Type type, Action<IntPtr> loader)
	{
		delayWrap[type] = loader;
	}

	public void AddInterfaceBridgeCreator(Type type, Func<int, LuaEnv, LuaBase> creator)
	{
		interfaceBridgeCreators.Add(type, creator);
	}

	public bool TryDelayWrapLoader(IntPtr L, Type type)
	{
		if (loaded_types.ContainsKey(type))
		{
			return true;
		}
		loaded_types.Add(type, value: true);
		Lua.luaL_newmetatable(L, type.FullName);
		Lua.lua_pop(L, 1);
		int num = Lua.lua_gettop(L);
		if (delayWrap.TryGetValue(type, out var value))
		{
			delayWrap.Remove(type);
			value(L);
		}
		else
		{
			Utils.ReflectionWrap(L, type, privateAccessibleFlags.Contains(type));
		}
		if (num != Lua.lua_gettop(L))
		{
			throw new Exception("top change, before:" + num + ", after:" + Lua.lua_gettop(L));
		}
		Type[] nestedTypes = type.GetNestedTypes(BindingFlags.Public);
		foreach (Type type2 in nestedTypes)
		{
			if (!type2.IsGenericTypeDefinition())
			{
				GetTypeId(L, type2);
			}
		}
		return true;
	}

	public void Alias(Type type, string alias)
	{
		Type type2 = FindType(alias);
		if (type2 == null)
		{
			throw new ArgumentException("Can not find " + alias);
		}
		aliasCfg[type2] = type;
	}

	private void addAssemblieByName(IEnumerable<Assembly> assemblies_usorted, string name)
	{
		foreach (Assembly item in assemblies_usorted)
		{
			if (item.FullName.StartsWith(name) && !assemblies.Contains(item))
			{
				assemblies.Add(item);
				break;
			}
		}
	}

	public ObjectTranslator(LuaEnv luaenv, IntPtr L)
	{
		assemblies = new List<Assembly>();
		assemblies.Add(Assembly.GetExecutingAssembly());
		Assembly[] array = AppDomain.CurrentDomain.GetAssemblies();
		addAssemblieByName(array, "mscorlib,");
		addAssemblieByName(array, "System,");
		addAssemblieByName(array, "System.Core,");
		Assembly[] array2 = array;
		foreach (Assembly item in array2)
		{
			if (!assemblies.Contains(item))
			{
				assemblies.Add(item);
			}
		}
		luaEnv = luaenv;
		objectCasters = new ObjectCasters(this);
		objectCheckers = new ObjectCheckers(this);
		methodWrapsCache = new MethodWrapsCache(this, objectCheckers, objectCasters);
		metaFunctions = new StaticLuaCallbacks();
		importTypeFunction = StaticLuaCallbacks.ImportType;
		loadAssemblyFunction = StaticLuaCallbacks.LoadAssembly;
		castFunction = StaticLuaCallbacks.Cast;
		Lua.lua_newtable(L);
		Lua.lua_newtable(L);
		Lua.xlua_pushasciistring(L, "__mode");
		Lua.xlua_pushasciistring(L, "v");
		Lua.lua_rawset(L, -3);
		Lua.lua_setmetatable(L, -2);
		cacheRef = Lua.luaL_ref(L, LuaIndexes.LUA_REGISTRYINDEX);
		initCSharpCallLua();
	}

	private void initCSharpCallLua()
	{
	}

	private Func<DelegateBridgeBase, Delegate> getCreatorUsingGeneric(DelegateBridgeBase bridge, Type delegateType, MethodInfo delegateMethod)
	{
		Func<DelegateBridgeBase, Delegate> func = null;
		if (genericAction == null)
		{
			MethodInfo[] methods = typeof(DelegateBridge).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
			genericAction = (from m in methods
				where m.Name == "Action"
				orderby m.GetParameters().Length
				select m).ToArray();
			genericFunc = (from m in methods
				where m.Name == "Func"
				orderby m.GetParameters().Length
				select m).ToArray();
		}
		if (genericAction.Length != 5 || genericFunc.Length != 5)
		{
			return null;
		}
		ParameterInfo[] parameters = delegateMethod.GetParameters();
		if ((delegateMethod.ReturnType.IsValueType() && delegateMethod.ReturnType != typeof(void)) || parameters.Length > 4)
		{
			func = (DelegateBridgeBase x) => (Delegate)null;
		}
		else
		{
			ParameterInfo[] array = parameters;
			foreach (ParameterInfo parameterInfo in array)
			{
				if (parameterInfo.ParameterType.IsValueType() || parameterInfo.IsOut || parameterInfo.ParameterType.IsByRef)
				{
					func = (DelegateBridgeBase x) => (Delegate)null;
					break;
				}
			}
			if (func == null)
			{
				IEnumerable<Type> enumerable = parameters.Select((ParameterInfo pinfo) => pinfo.ParameterType);
				MethodInfo genericMethodInfo = null;
				if (delegateMethod.ReturnType == typeof(void))
				{
					genericMethodInfo = genericAction[parameters.Length];
				}
				else
				{
					genericMethodInfo = genericFunc[parameters.Length];
					enumerable = enumerable.Concat(new Type[1] { delegateMethod.ReturnType });
				}
				if (genericMethodInfo.IsGenericMethodDefinition)
				{
					MethodInfo methodInfo = genericMethodInfo.MakeGenericMethod(enumerable.ToArray());
					func = (DelegateBridgeBase o) => Delegate.CreateDelegate(delegateType, o, methodInfo);
				}
				else
				{
					func = (DelegateBridgeBase o) => Delegate.CreateDelegate(delegateType, o, genericMethodInfo);
				}
			}
		}
		return func;
	}

	private Delegate getDelegate(DelegateBridgeBase bridge, Type delegateType)
	{
		Delegate delegateByType = bridge.GetDelegateByType(delegateType);
		if ((object)delegateByType != null)
		{
			return delegateByType;
		}
		if (delegateType == typeof(Delegate) || delegateType == typeof(MulticastDelegate))
		{
			return null;
		}
		if (!delegateCreatorCache.TryGetValue(delegateType, out var value))
		{
			MethodInfo method = delegateType.GetMethod("JumpTo");
			MethodInfo[] array = (from m in bridge.GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
				where !m.IsGenericMethodDefinition && (m.Name.StartsWith("__Gen_Delegate_Imp") || m.Name == "Action")
				select m).ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				if (!array[i].IsConstructor && Utils.IsParamsMatch(method, array[i]))
				{
					MethodInfo foundMethod = array[i];
					value = (DelegateBridgeBase o) => Delegate.CreateDelegate(delegateType, o, foundMethod);
					break;
				}
			}
			if (value == null)
			{
				value = getCreatorUsingGeneric(bridge, delegateType, method);
			}
			delegateCreatorCache.Add(delegateType, value);
		}
		delegateByType = value(bridge);
		if ((object)delegateByType != null)
		{
			return delegateByType;
		}
		throw new InvalidCastException("This type must add to CSharpCallLua: " + delegateType.GetFriendlyName());
	}

	public object CreateDelegateBridge(IntPtr L, Type delegateType, int idx)
	{
		Lua.lua_pushvalue(L, idx);
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		if (!Lua.lua_isnil(L, -1))
		{
			int key = Lua.xlua_tointeger(L, -1);
			Lua.lua_pop(L, 1);
			if (delegate_bridges[key].IsAlive)
			{
				if (delegateType == null)
				{
					return delegate_bridges[key].Target;
				}
				DelegateBridgeBase delegateBridgeBase = delegate_bridges[key].Target as DelegateBridgeBase;
				if (delegateBridgeBase.TryGetDelegate(delegateType, out var value))
				{
					return value;
				}
				value = getDelegate(delegateBridgeBase, delegateType);
				delegateBridgeBase.AddDelegate(delegateType, value);
				return value;
			}
		}
		else
		{
			Lua.lua_pop(L, 1);
		}
		Lua.lua_pushvalue(L, idx);
		int num = Lua.luaL_ref(L);
		Lua.lua_pushvalue(L, idx);
		Lua.lua_pushnumber(L, num);
		Lua.lua_rawset(L, LuaIndexes.LUA_REGISTRYINDEX);
		DelegateBridgeBase delegateBridgeBase2;
		try
		{
			delegateBridgeBase2 = new DelegateBridge(num, luaEnv);
		}
		catch (Exception ex)
		{
			Lua.lua_pushvalue(L, idx);
			Lua.lua_pushnil(L);
			Lua.lua_rawset(L, LuaIndexes.LUA_REGISTRYINDEX);
			Lua.lua_pushnil(L);
			Lua.xlua_rawseti(L, LuaIndexes.LUA_REGISTRYINDEX, num);
			throw ex;
		}
		if (delegateType == null)
		{
			delegate_bridges[num] = new WeakReference(delegateBridgeBase2);
			return delegateBridgeBase2;
		}
		try
		{
			Delegate @delegate = getDelegate(delegateBridgeBase2, delegateType);
			delegateBridgeBase2.AddDelegate(delegateType, @delegate);
			delegate_bridges[num] = new WeakReference(delegateBridgeBase2);
			return @delegate;
		}
		catch (Exception ex2)
		{
			delegateBridgeBase2.Dispose();
			throw ex2;
		}
	}

	public bool AllDelegateBridgeReleased()
	{
		foreach (KeyValuePair<int, WeakReference> delegate_bridge in delegate_bridges)
		{
			if (delegate_bridge.Value.IsAlive)
			{
				return false;
			}
		}
		return true;
	}

	public void ReleaseLuaBase(IntPtr L, int reference, bool is_delegate)
	{
		if (is_delegate)
		{
			Lua.xlua_rawgeti(L, LuaIndexes.LUA_REGISTRYINDEX, reference);
			if (Lua.lua_isnil(L, -1))
			{
				Lua.lua_pop(L, 1);
			}
			else
			{
				Lua.lua_pushvalue(L, -1);
				Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
				if (Lua.lua_type(L, -1) == LuaTypes.LUA_TNUMBER && Lua.xlua_tointeger(L, -1) == reference)
				{
					Lua.lua_pop(L, 1);
					Lua.lua_pushnil(L);
					Lua.lua_rawset(L, LuaIndexes.LUA_REGISTRYINDEX);
				}
				else
				{
					Lua.lua_pop(L, 2);
				}
			}
			Lua.lua_unref(L, reference);
			delegate_bridges.Remove(reference);
		}
		else
		{
			Lua.lua_unref(L, reference);
		}
	}

	public object CreateInterfaceBridge(IntPtr L, Type interfaceType, int idx)
	{
		if (!interfaceBridgeCreators.TryGetValue(interfaceType, out var value))
		{
			throw new InvalidCastException("This type must add to CSharpCallLua: " + interfaceType);
		}
		Lua.lua_pushvalue(L, idx);
		return value(Lua.luaL_ref(L), luaEnv);
	}

	public void CreateArrayMetatable(IntPtr L)
	{
		Utils.BeginObjectRegister(null, L, this, 0, 0, 1, 0, common_array_meta);
		Utils.RegisterFunc(L, -2, "Length", StaticLuaCallbacks.ArrayLength);
		Utils.EndObjectRegister(null, L, this, null, null, typeof(Array), StaticLuaCallbacks.ArrayIndexer, StaticLuaCallbacks.ArrayNewIndexer);
	}

	public void CreateDelegateMetatable(IntPtr L)
	{
		Utils.BeginObjectRegister(null, L, this, 3, 0, 0, 0, common_delegate_meta);
		Utils.RegisterFunc(L, -4, "__call", StaticLuaCallbacks.DelegateCall);
		Utils.RegisterFunc(L, -4, "__add", StaticLuaCallbacks.DelegateCombine);
		Utils.RegisterFunc(L, -4, "__sub", StaticLuaCallbacks.DelegateRemove);
		Utils.EndObjectRegister(null, L, this, null, null, typeof(MulticastDelegate), null, null);
	}

	internal void CreateEnumerablePairs(IntPtr L)
	{
		LuaFunction obj = luaEnv.DoString("\r\n                return function(obj)\r\n                    local isKeyValuePair\r\n                    local function lua_iter(cs_iter, k)\r\n                        if cs_iter:MoveNext() then\r\n                            local current = cs_iter.Current\r\n                            if isKeyValuePair == nil then\r\n                                if type(current) == 'userdata' then\r\n                                    local t = current:GetType()\r\n                                    isKeyValuePair = t.Name == 'KeyValuePair`2' and t.Namespace == 'System.Collections.Generic'\r\n                                 else\r\n                                    isKeyValuePair = false\r\n                                 end\r\n                                 --print(current, isKeyValuePair)\r\n                            end\r\n                            if isKeyValuePair then\r\n                                return current.Key, current.Value\r\n                            else\r\n                                return k + 1, current\r\n                            end\r\n                        end\r\n                    end\r\n                    return lua_iter, obj:GetEnumerator(), -1\r\n                end\r\n            ")[0] as LuaFunction;
		obj.push(L);
		enumerable_pairs_func = Lua.luaL_ref(L, LuaIndexes.LUA_REGISTRYINDEX);
		obj.Dispose();
	}

	public void OpenLib(IntPtr L)
	{
		if (Lua.xlua_getglobal(L, "xlua") != 0)
		{
			throw new Exception("call xlua_getglobal fail!" + Lua.lua_tostring(L, -1));
		}
		Lua.xlua_pushasciistring(L, "import_type");
		Lua.lua_pushstdcallcfunction(L, importTypeFunction);
		Lua.lua_rawset(L, -3);
		Lua.xlua_pushasciistring(L, "import_generic_type");
		Lua.lua_pushstdcallcfunction(L, StaticLuaCallbacks.ImportGenericType);
		Lua.lua_rawset(L, -3);
		Lua.xlua_pushasciistring(L, "cast");
		Lua.lua_pushstdcallcfunction(L, castFunction);
		Lua.lua_rawset(L, -3);
		Lua.xlua_pushasciistring(L, "load_assembly");
		Lua.lua_pushstdcallcfunction(L, loadAssemblyFunction);
		Lua.lua_rawset(L, -3);
		Lua.xlua_pushasciistring(L, "access");
		Lua.lua_pushstdcallcfunction(L, StaticLuaCallbacks.XLuaAccess);
		Lua.lua_rawset(L, -3);
		Lua.xlua_pushasciistring(L, "private_accessible");
		Lua.lua_pushstdcallcfunction(L, StaticLuaCallbacks.XLuaPrivateAccessible);
		Lua.lua_rawset(L, -3);
		Lua.xlua_pushasciistring(L, "metatable_operation");
		Lua.lua_pushstdcallcfunction(L, StaticLuaCallbacks.XLuaMetatableOperation);
		Lua.lua_rawset(L, -3);
		Lua.xlua_pushasciistring(L, "tofunction");
		Lua.lua_pushstdcallcfunction(L, StaticLuaCallbacks.ToFunction);
		Lua.lua_rawset(L, -3);
		Lua.xlua_pushasciistring(L, "get_generic_method");
		Lua.lua_pushstdcallcfunction(L, StaticLuaCallbacks.GetGenericMethod);
		Lua.lua_rawset(L, -3);
		Lua.xlua_pushasciistring(L, "release");
		Lua.lua_pushstdcallcfunction(L, StaticLuaCallbacks.ReleaseCsObject);
		Lua.lua_rawset(L, -3);
		Lua.lua_pop(L, 1);
		Lua.lua_createtable(L, 1, 4);
		common_array_meta = Lua.luaL_ref(L, LuaIndexes.LUA_REGISTRYINDEX);
		Lua.lua_createtable(L, 1, 4);
		common_delegate_meta = Lua.luaL_ref(L, LuaIndexes.LUA_REGISTRYINDEX);
	}

	internal void createFunctionMetatable(IntPtr L)
	{
		Lua.lua_newtable(L);
		Lua.xlua_pushasciistring(L, "__gc");
		Lua.lua_pushstdcallcfunction(L, metaFunctions.GcMeta);
		Lua.lua_rawset(L, -3);
		Lua.lua_pushlightuserdata(L, Lua.xlua_tag());
		Lua.lua_pushnumber(L, 1.0);
		Lua.lua_rawset(L, -3);
		Lua.lua_pushvalue(L, -1);
		int num = Lua.luaL_ref(L, LuaIndexes.LUA_REGISTRYINDEX);
		Lua.lua_pushnumber(L, num);
		Lua.xlua_rawseti(L, -2, 1L);
		Lua.lua_pop(L, 1);
		typeIdMap.Add(typeof(lua_CSFunction), num);
	}

	internal Type FindType(string className, bool isQualifiedName = false)
	{
		foreach (Assembly assembly in assemblies)
		{
			Type type = assembly.GetType(className);
			if (type != null)
			{
				return type;
			}
		}
		int num = className.IndexOf('[');
		if (num > 0 && !isQualifiedName)
		{
			string text = className.Substring(0, num + 1);
			string[] array = className.Substring(num + 1, className.Length - text.Length - 1).Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				Type type2 = FindType(array[i].Trim());
				if (type2 == null)
				{
					return null;
				}
				if (i != 0)
				{
					text += ", ";
				}
				text = text + "[" + type2.AssemblyQualifiedName + "]";
			}
			text += "]";
			return FindType(text, isQualifiedName: true);
		}
		return null;
	}

	private bool hasMethod(Type type, string methodName)
	{
		MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		for (int i = 0; i < methods.Length; i++)
		{
			if (methods[i].Name == methodName)
			{
				return true;
			}
		}
		return false;
	}

	internal void collectObject(int obj_index_to_collect)
	{
		if (!objects.TryGetValue(obj_index_to_collect, out var obj))
		{
			return;
		}
		objects.Remove(obj_index_to_collect);
		if (obj == null)
		{
			return;
		}
		bool flag = obj.GetType().IsEnum();
		if ((flag ? enumMap.TryGetValue(obj, out var value) : reverseMap.TryGetValue(obj, out value)) && value == obj_index_to_collect)
		{
			if (flag)
			{
				enumMap.Remove(obj);
			}
			else
			{
				reverseMap.Remove(obj);
			}
		}
	}

	private int addObject(object obj, bool is_valuetype, bool is_enum)
	{
		int num = objects.Add(obj);
		if (is_enum)
		{
			enumMap[obj] = num;
		}
		else if (!is_valuetype)
		{
			reverseMap[obj] = num;
		}
		return num;
	}

	internal object GetObject(IntPtr L, int index)
	{
		return objectCasters.GetCaster(typeof(object))(L, index, null);
	}

	public Type GetTypeOf(IntPtr L, int idx)
	{
		Type value = null;
		int num = Lua.xlua_gettypeid(L, idx);
		if (num != -1)
		{
			typeMap.TryGetValue(num, out value);
		}
		return value;
	}

	public bool Assignable<T>(IntPtr L, int index)
	{
		return Assignable(L, index, typeof(T));
	}

	public bool Assignable(IntPtr L, int index, Type type)
	{
		if (Lua.lua_type(L, index) == LuaTypes.LUA_TUSERDATA)
		{
			int num = Lua.xlua_tocsobj_safe(L, index);
			if (num != -1 && objects.TryGetValue(num, out var obj))
			{
				if (obj is RawObject rawObject)
				{
					obj = rawObject.Target;
				}
				if (obj == null)
				{
					return !type.IsValueType();
				}
				return type.IsAssignableFrom(obj.GetType());
			}
			int num2 = Lua.xlua_gettypeid(L, index);
			if (num2 != -1 && typeMap.TryGetValue(num2, out var value))
			{
				return type.IsAssignableFrom(value);
			}
		}
		return objectCheckers.GetChecker(type)(L, index);
	}

	public object GetObject(IntPtr L, int index, Type type)
	{
		int num = Lua.xlua_tocsobj_safe(L, index);
		if (num != -1)
		{
			object obj = objects.Get(num);
			if (obj is RawObject rawObject)
			{
				return rawObject.Target;
			}
			return obj;
		}
		if (Lua.lua_type(L, index) == LuaTypes.LUA_TUSERDATA)
		{
			int num2 = Lua.xlua_gettypeid(L, index);
			if (num2 != -1 && num2 == decimal_type_id)
			{
				Get(L, index, out decimal val);
				return val;
			}
			if (num2 != -1 && typeMap.TryGetValue(num2, out var value) && type.IsAssignableFrom(value) && custom_get_funcs.TryGetValue(type, out var value2))
			{
				return value2(L, index);
			}
		}
		return objectCasters.GetCaster(type)(L, index, null);
	}

	public void Get<T>(IntPtr L, int index, out T v)
	{
		if (tryGetGetFuncByType<Func<IntPtr, int, T>>(typeof(T), out var func))
		{
			v = func(L, index);
		}
		else
		{
			v = (T)GetObject(L, index, typeof(T));
		}
	}

	public void PushByType<T>(IntPtr L, T v)
	{
		if (tryGetPushFuncByType<Action<IntPtr, T>>(typeof(T), out var func))
		{
			func(L, v);
		}
		else
		{
			PushAny(L, v);
		}
	}

	public T[] GetParams<T>(IntPtr L, int index)
	{
		T[] array = new T[Math.Max(Lua.lua_gettop(L) - index + 1, 0)];
		for (int i = 0; i < array.Length; i++)
		{
			Get(L, index + i, out array[i]);
		}
		return array;
	}

	public Array GetParams(IntPtr L, int index, Type type)
	{
		Array array = Array.CreateInstance(type, Math.Max(Lua.lua_gettop(L) - index + 1, 0));
		for (int i = 0; i < array.Length; i++)
		{
			array.SetValue(GetObject(L, index + i, type), i);
		}
		return array;
	}

	public T GetDelegate<T>(IntPtr L, int index) where T : class
	{
		if (Lua.lua_isfunction(L, index))
		{
			return CreateDelegateBridge(L, typeof(T), index) as T;
		}
		if (Lua.lua_type(L, index) == LuaTypes.LUA_TUSERDATA)
		{
			return (T)SafeGetCSObj(L, index);
		}
		return null;
	}

	public int GetTypeId(IntPtr L, Type type)
	{
		bool is_first;
		return getTypeId(L, type, out is_first);
	}

	public void PrivateAccessible(IntPtr L, Type type)
	{
		if (!privateAccessibleFlags.Contains(type))
		{
			privateAccessibleFlags.Add(type);
			if (typeIdMap.ContainsKey(type))
			{
				Utils.MakePrivateAccessible(L, type);
			}
		}
	}

	internal int getTypeId(IntPtr L, Type type, out bool is_first, LOGLEVEL log_level = LOGLEVEL.WARN)
	{
		is_first = false;
		if (!typeIdMap.TryGetValue(type, out var value))
		{
			if (type.IsArray)
			{
				if (common_array_meta == -1)
				{
					throw new Exception("Fatal Exception! Array Metatable not inited!");
				}
				return common_array_meta;
			}
			if (typeof(MulticastDelegate).IsAssignableFrom(type))
			{
				if (common_delegate_meta == -1)
				{
					throw new Exception("Fatal Exception! Delegate Metatable not inited!");
				}
				TryDelayWrapLoader(L, type);
				return common_delegate_meta;
			}
			is_first = true;
			Type value2 = null;
			aliasCfg.TryGetValue(type, out value2);
			Lua.luaL_getmetatable(L, (value2 == null) ? type.FullName : value2.FullName);
			if (Lua.lua_isnil(L, -1))
			{
				Lua.lua_pop(L, 1);
				if (!TryDelayWrapLoader(L, (value2 == null) ? type : value2))
				{
					throw new Exception("Fatal: can not load metatable of type:" + type);
				}
				Lua.luaL_getmetatable(L, (value2 == null) ? type.FullName : value2.FullName);
			}
			if (typeIdMap.TryGetValue(type, out value))
			{
				Lua.lua_pop(L, 1);
			}
			else
			{
				if (type.IsEnum())
				{
					Lua.xlua_pushasciistring(L, "__band");
					Lua.lua_pushstdcallcfunction(L, metaFunctions.EnumAndMeta);
					Lua.lua_rawset(L, -3);
					Lua.xlua_pushasciistring(L, "__bor");
					Lua.lua_pushstdcallcfunction(L, metaFunctions.EnumOrMeta);
					Lua.lua_rawset(L, -3);
				}
				if (typeof(IEnumerable).IsAssignableFrom(type))
				{
					Lua.xlua_pushasciistring(L, "__pairs");
					Lua.lua_getref(L, enumerable_pairs_func);
					Lua.lua_rawset(L, -3);
				}
				Lua.lua_pushvalue(L, -1);
				value = Lua.luaL_ref(L, LuaIndexes.LUA_REGISTRYINDEX);
				Lua.lua_pushnumber(L, value);
				Lua.xlua_rawseti(L, -2, 1L);
				Lua.lua_pop(L, 1);
				if (type.IsValueType())
				{
					typeMap.Add(value, type);
				}
				typeIdMap.Add(type, value);
			}
		}
		return value;
	}

	private void pushPrimitive(IntPtr L, object o)
	{
		if (o is sbyte || o is byte || o is short || o is ushort || o is int)
		{
			int value = Convert.ToInt32(o);
			Lua.xlua_pushinteger(L, value);
			return;
		}
		if (o is uint)
		{
			Lua.xlua_pushuint(L, (uint)o);
			return;
		}
		if (o is float || o is double)
		{
			double number = Convert.ToDouble(o);
			Lua.lua_pushnumber(L, number);
			return;
		}
		if (o is IntPtr)
		{
			Lua.lua_pushlightuserdata(L, (IntPtr)o);
			return;
		}
		if (o is char)
		{
			Lua.xlua_pushinteger(L, (char)o);
			return;
		}
		if (o is long)
		{
			Lua.lua_pushint64(L, Convert.ToInt64(o));
			return;
		}
		if (o is ulong)
		{
			Lua.lua_pushuint64(L, Convert.ToUInt64(o));
			return;
		}
		if (o is bool value2)
		{
			Lua.lua_pushboolean(L, value2);
			return;
		}
		throw new Exception("No support type " + o.GetType());
	}

	public void PushAny(IntPtr L, object o)
	{
		if (o == null)
		{
			Lua.lua_pushnil(L);
			return;
		}
		Type type = o.GetType();
		if (type.IsPrimitive())
		{
			pushPrimitive(L, o);
		}
		else if (o is string)
		{
			Lua.lua_pushstring(L, o as string);
		}
		else if (type == typeof(byte[]))
		{
			Lua.lua_pushstring(L, o as byte[]);
		}
		else if (o is decimal)
		{
			PushDecimal(L, (decimal)o);
		}
		else if (o is LuaBase)
		{
			((LuaBase)o).push(L);
		}
		else if (o is lua_CSFunction)
		{
			Push(L, o as lua_CSFunction);
		}
		else if (o is ValueType)
		{
			if (custom_push_funcs.TryGetValue(o.GetType(), out var value))
			{
				value(L, o);
			}
			else
			{
				Push(L, o);
			}
		}
		else
		{
			Push(L, o);
		}
	}

	public int TranslateToEnumToTop(IntPtr L, Type type, int idx)
	{
		object obj = null;
		switch (Lua.lua_type(L, idx))
		{
		case LuaTypes.LUA_TNUMBER:
		{
			int value2 = (int)Lua.lua_tonumber(L, idx);
			obj = Enum.ToObject(type, value2);
			break;
		}
		case LuaTypes.LUA_TSTRING:
		{
			string value = Lua.lua_tostring(L, idx);
			obj = Enum.Parse(type, value);
			break;
		}
		default:
			return Lua.luaL_error(L, "#1 argument must be a integer or a string");
		}
		PushAny(L, obj);
		return 1;
	}

	public void Push(IntPtr L, lua_CSFunction o)
	{
		if (Utils.IsStaticPInvokeCSFunction(o))
		{
			Lua.lua_pushstdcallcfunction(L, o);
			return;
		}
		Push(L, (object)o);
		Lua.lua_pushstdcallcfunction(L, metaFunctions.StaticCSFunctionWraper, 1);
	}

	public void Push(IntPtr L, LuaBase o)
	{
		if (o == null)
		{
			Lua.lua_pushnil(L);
		}
		else
		{
			o.push(L);
		}
	}

	public void Push(IntPtr L, object o)
	{
		if (o == null)
		{
			Lua.lua_pushnil(L);
			return;
		}
		int value = -1;
		Type type = o.GetType();
		bool isEnum = type.IsEnum;
		bool isValueType = type.IsValueType;
		bool flag = !isValueType || isEnum;
		if (!flag || !(isEnum ? enumMap.TryGetValue(o, out value) : reverseMap.TryGetValue(o, out value)) || Lua.xlua_tryget_cachedud(L, value, cacheRef) != 1)
		{
			bool is_first;
			int typeId = getTypeId(L, type, out is_first);
			if (!(is_first && flag) || !(isEnum ? enumMap.TryGetValue(o, out value) : reverseMap.TryGetValue(o, out value)) || Lua.xlua_tryget_cachedud(L, value, cacheRef) != 1)
			{
				value = addObject(o, isValueType, isEnum);
				Lua.xlua_pushcsobj(L, value, typeId, flag, cacheRef);
			}
		}
	}

	public void PushObject(IntPtr L, object o, int type_id)
	{
		if (o == null)
		{
			Lua.lua_pushnil(L);
			return;
		}
		int value = -1;
		if (!reverseMap.TryGetValue(o, out value) || Lua.xlua_tryget_cachedud(L, value, cacheRef) != 1)
		{
			value = addObject(o, is_valuetype: false, is_enum: false);
			Lua.xlua_pushcsobj(L, value, type_id, need_cache: true, cacheRef);
		}
	}

	public void Update(IntPtr L, int index, object obj)
	{
		int num = Lua.xlua_tocsobj_fast(L, index);
		if (num != -1)
		{
			objects.Replace(num, obj);
			return;
		}
		if (custom_update_funcs.TryGetValue(obj.GetType(), out var value))
		{
			value(L, index, obj);
			return;
		}
		throw new Exception("can not update [" + obj?.ToString() + "]");
	}

	private object getCsObj(IntPtr L, int index, int udata)
	{
		if (udata == -1)
		{
			if (Lua.lua_type(L, index) != LuaTypes.LUA_TUSERDATA)
			{
				return null;
			}
			Type typeOf = GetTypeOf(L, index);
			if (typeOf == typeof(decimal))
			{
				Get(L, index, out decimal val);
				return val;
			}
			if (typeOf != null && custom_get_funcs.TryGetValue(typeOf, out var value))
			{
				return value(L, index);
			}
			return null;
		}
		if (objects.TryGetValue(udata, out var obj))
		{
			return obj;
		}
		return null;
	}

	internal object SafeGetCSObj(IntPtr L, int index)
	{
		return getCsObj(L, index, Lua.xlua_tocsobj_safe(L, index));
	}

	internal object FastGetCSObj(IntPtr L, int index)
	{
		return getCsObj(L, index, Lua.xlua_tocsobj_fast(L, index));
	}

	internal void ReleaseCSObj(IntPtr L, int index)
	{
		int num = Lua.xlua_tocsobj_safe(L, index);
		if (num != -1)
		{
			object obj = objects.Replace(num, null);
			if (obj != null && reverseMap.ContainsKey(obj))
			{
				reverseMap.Remove(obj);
			}
		}
	}

	internal lua_CSFunction GetFixCSFunction(int index)
	{
		return fix_cs_functions[index];
	}

	internal void PushFixCSFunction(IntPtr L, lua_CSFunction func)
	{
		if (func == null)
		{
			Lua.lua_pushnil(L);
			return;
		}
		Lua.xlua_pushinteger(L, fix_cs_functions.Count);
		fix_cs_functions.Add(func);
		Lua.lua_pushstdcallcfunction(L, metaFunctions.FixCSFunctionWraper, 1);
	}

	internal object[] popValues(IntPtr L, int oldTop)
	{
		int num = Lua.lua_gettop(L);
		if (oldTop == num)
		{
			return null;
		}
		ArrayList arrayList = new ArrayList();
		for (int i = oldTop + 1; i <= num; i++)
		{
			arrayList.Add(GetObject(L, i));
		}
		Lua.lua_settop(L, oldTop);
		return arrayList.ToArray();
	}

	internal object[] popValues(IntPtr L, int oldTop, Type[] popTypes)
	{
		int num = Lua.lua_gettop(L);
		if (oldTop == num)
		{
			return null;
		}
		ArrayList arrayList = new ArrayList();
		int num2 = ((popTypes[0] == typeof(void)) ? 1 : 0);
		for (int i = oldTop + 1; i <= num; i++)
		{
			arrayList.Add(GetObject(L, i, popTypes[num2]));
			num2++;
		}
		Lua.lua_settop(L, oldTop);
		return arrayList.ToArray();
	}

	private void registerCustomOp(Type type, PushCSObject push, GetCSObject get, UpdateCSObject update)
	{
		if (push != null)
		{
			custom_push_funcs.Add(type, push);
		}
		if (get != null)
		{
			custom_get_funcs.Add(type, get);
		}
		if (update != null)
		{
			custom_update_funcs.Add(type, update);
		}
	}

	public bool HasCustomOp(Type type)
	{
		return custom_push_funcs.ContainsKey(type);
	}

	private bool tryGetPushFuncByType<T>(Type type, out T func) where T : class
	{
		if (push_func_with_type == null)
		{
			push_func_with_type = new Dictionary<Type, Delegate>
			{
				{
					typeof(int),
					new Action<IntPtr, int>(Lua.xlua_pushinteger)
				},
				{
					typeof(double),
					new Action<IntPtr, double>(Lua.lua_pushnumber)
				},
				{
					typeof(string),
					new Action<IntPtr, string>(Lua.lua_pushstring)
				},
				{
					typeof(byte[]),
					new Action<IntPtr, byte[]>(Lua.lua_pushstring)
				},
				{
					typeof(bool),
					new Action<IntPtr, bool>(Lua.lua_pushboolean)
				},
				{
					typeof(long),
					new Action<IntPtr, long>(Lua.lua_pushint64)
				},
				{
					typeof(ulong),
					new Action<IntPtr, ulong>(Lua.lua_pushuint64)
				},
				{
					typeof(IntPtr),
					new Action<IntPtr, IntPtr>(Lua.lua_pushlightuserdata)
				},
				{
					typeof(decimal),
					new Action<IntPtr, decimal>(PushDecimal)
				},
				{
					typeof(byte),
					(Action<IntPtr, byte>)delegate(IntPtr L, byte v)
					{
						Lua.xlua_pushinteger(L, v);
					}
				},
				{
					typeof(sbyte),
					(Action<IntPtr, sbyte>)delegate(IntPtr L, sbyte v)
					{
						Lua.xlua_pushinteger(L, v);
					}
				},
				{
					typeof(char),
					(Action<IntPtr, char>)delegate(IntPtr L, char v)
					{
						Lua.xlua_pushinteger(L, v);
					}
				},
				{
					typeof(short),
					(Action<IntPtr, short>)delegate(IntPtr L, short v)
					{
						Lua.xlua_pushinteger(L, v);
					}
				},
				{
					typeof(ushort),
					(Action<IntPtr, ushort>)delegate(IntPtr L, ushort v)
					{
						Lua.xlua_pushinteger(L, v);
					}
				},
				{
					typeof(uint),
					new Action<IntPtr, uint>(Lua.xlua_pushuint)
				},
				{
					typeof(float),
					(Action<IntPtr, float>)delegate(IntPtr L, float v)
					{
						Lua.lua_pushnumber(L, v);
					}
				}
			};
		}
		if (push_func_with_type.TryGetValue(type, out var value))
		{
			func = value as T;
			return true;
		}
		func = null;
		return false;
	}

	private bool tryGetGetFuncByType<T>(Type type, out T func) where T : class
	{
		if (get_func_with_type == null)
		{
			get_func_with_type = new Dictionary<Type, Delegate>
			{
				{
					typeof(int),
					new Func<IntPtr, int, int>(Lua.xlua_tointeger)
				},
				{
					typeof(double),
					new Func<IntPtr, int, double>(Lua.lua_tonumber)
				},
				{
					typeof(string),
					new Func<IntPtr, int, string>(Lua.lua_tostring)
				},
				{
					typeof(byte[]),
					new Func<IntPtr, int, byte[]>(Lua.lua_tobytes)
				},
				{
					typeof(bool),
					new Func<IntPtr, int, bool>(Lua.lua_toboolean)
				},
				{
					typeof(long),
					new Func<IntPtr, int, long>(Lua.lua_toint64)
				},
				{
					typeof(ulong),
					new Func<IntPtr, int, ulong>(Lua.lua_touint64)
				},
				{
					typeof(IntPtr),
					new Func<IntPtr, int, IntPtr>(Lua.lua_touserdata)
				},
				{
					typeof(decimal),
					(Func<IntPtr, int, decimal>)delegate(IntPtr L, int idx)
					{
						Get(L, idx, out decimal val);
						return val;
					}
				},
				{
					typeof(byte),
					(Func<IntPtr, int, byte>)((IntPtr L, int idx) => (byte)Lua.xlua_tointeger(L, idx))
				},
				{
					typeof(sbyte),
					(Func<IntPtr, int, sbyte>)((IntPtr L, int idx) => (sbyte)Lua.xlua_tointeger(L, idx))
				},
				{
					typeof(char),
					(Func<IntPtr, int, char>)((IntPtr L, int idx) => (char)Lua.xlua_tointeger(L, idx))
				},
				{
					typeof(short),
					(Func<IntPtr, int, short>)((IntPtr L, int idx) => (short)Lua.xlua_tointeger(L, idx))
				},
				{
					typeof(ushort),
					(Func<IntPtr, int, ushort>)((IntPtr L, int idx) => (ushort)Lua.xlua_tointeger(L, idx))
				},
				{
					typeof(uint),
					new Func<IntPtr, int, uint>(Lua.xlua_touint)
				},
				{
					typeof(float),
					(Func<IntPtr, int, float>)((IntPtr L, int idx) => (float)Lua.lua_tonumber(L, idx))
				}
			};
		}
		if (get_func_with_type.TryGetValue(type, out var value))
		{
			func = value as T;
			return true;
		}
		func = null;
		return false;
	}

	public void RegisterPushAndGetAndUpdate<T>(Action<IntPtr, T> push, GetFunc<T> get, Action<IntPtr, int, T> update)
	{
		Type typeFromHandle = typeof(T);
		if (tryGetPushFuncByType<Action<IntPtr, T>>(typeFromHandle, out var _) || tryGetGetFuncByType<Func<IntPtr, int, T>>(typeFromHandle, out var _))
		{
			throw new InvalidOperationException("push or get of " + typeFromHandle?.ToString() + " has register!");
		}
		push_func_with_type.Add(typeFromHandle, push);
		get_func_with_type.Add(typeFromHandle, (Func<IntPtr, int, T>)delegate(IntPtr L, int idx)
		{
			get(L, idx, out var val2);
			return val2;
		});
		registerCustomOp(typeFromHandle, delegate(IntPtr L, object obj)
		{
			push(L, (T)obj);
		}, delegate(IntPtr L, int idx)
		{
			get(L, idx, out var val);
			return val;
		}, delegate(IntPtr L, int idx, object obj)
		{
			update(L, idx, (T)obj);
		});
	}

	public void RegisterChecker<T>(CheckFunc<T> check)
	{
		objectCheckers.AddChecker(typeof(T), (IntPtr L, int idx) => check(L, idx));
	}

	public void RegisterCaster<T>(GetFunc<T> get)
	{
		objectCasters.AddCaster(typeof(T), delegate(IntPtr L, int idx, object o)
		{
			get(L, idx, out var val);
			return val;
		});
	}

	public void PushDecimal(IntPtr L, decimal val)
	{
		if (decimal_type_id == -1)
		{
			decimal_type_id = getTypeId(L, typeof(decimal), out var _);
		}
		if (!CopyByValue.Pack(Lua.xlua_pushstruct(L, 16u, decimal_type_id), 0, val))
		{
			throw new Exception("pack fail for decimal ,value=" + val);
		}
	}

	public bool IsDecimal(IntPtr L, int index)
	{
		if (decimal_type_id == -1)
		{
			return false;
		}
		return Lua.xlua_gettypeid(L, index) == decimal_type_id;
	}

	public decimal GetDecimal(IntPtr L, int index)
	{
		Get(L, index, out decimal val);
		return val;
	}

	public void Get(IntPtr L, int index, out decimal val)
	{
		LuaTypes luaTypes = Lua.lua_type(L, index);
		switch (luaTypes)
		{
		case LuaTypes.LUA_TUSERDATA:
			if (Lua.xlua_gettypeid(L, index) != decimal_type_id)
			{
				throw new Exception("invalid userdata for decimal!");
			}
			if (!CopyByValue.UnPack(Lua.lua_touserdata(L, index), 0, out val))
			{
				throw new Exception("unpack decimal fail!");
			}
			break;
		case LuaTypes.LUA_TNUMBER:
			if (Lua.lua_isint64(L, index))
			{
				val = Lua.lua_toint64(L, index);
			}
			else
			{
				val = (decimal)Lua.lua_tonumber(L, index);
			}
			break;
		default:
			throw new Exception("invalid lua value for decimal, LuaType=" + luaTypes);
		}
	}
}
