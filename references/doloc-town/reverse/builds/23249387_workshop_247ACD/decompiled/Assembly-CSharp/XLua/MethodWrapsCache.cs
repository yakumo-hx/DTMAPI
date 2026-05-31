using System;
using System.Collections.Generic;
using System.Reflection;
using XLua.LuaDLL;

namespace XLua;

public class MethodWrapsCache
{
	private ObjectTranslator translator;

	private ObjectCheckers objCheckers;

	private ObjectCasters objCasters;

	private Dictionary<Type, lua_CSFunction> constructorCache = new Dictionary<Type, lua_CSFunction>();

	private Dictionary<Type, Dictionary<string, lua_CSFunction>> methodsCache = new Dictionary<Type, Dictionary<string, lua_CSFunction>>();

	private Dictionary<Type, lua_CSFunction> delegateCache = new Dictionary<Type, lua_CSFunction>();

	public MethodWrapsCache(ObjectTranslator translator, ObjectCheckers objCheckers, ObjectCasters objCasters)
	{
		this.translator = translator;
		this.objCheckers = objCheckers;
		this.objCasters = objCasters;
	}

	public lua_CSFunction GetConstructorWrap(Type type)
	{
		if (!constructorCache.ContainsKey(type))
		{
			ConstructorInfo[] constructors = type.GetConstructors();
			if (type.IsAbstract() || constructors == null || constructors.Length == 0)
			{
				if (!type.IsValueType())
				{
					return null;
				}
				constructorCache[type] = delegate(IntPtr L)
				{
					translator.PushAny(L, Activator.CreateInstance(type));
					return 1;
				};
			}
			else
			{
				lua_CSFunction ctor = _GenMethodWrap(type, ".ctor", constructors, forceCheck: true).Call;
				if (type.IsValueType())
				{
					bool flag = false;
					for (int i = 0; i < constructors.Length; i++)
					{
						if (constructors[i].GetParameters().Length == 0)
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						constructorCache[type] = ctor;
					}
					else
					{
						constructorCache[type] = delegate(IntPtr L)
						{
							if (Lua.lua_gettop(L) == 1)
							{
								translator.PushAny(L, Activator.CreateInstance(type));
								return 1;
							}
							return ctor(L);
						};
					}
				}
				else
				{
					constructorCache[type] = ctor;
				}
			}
		}
		return constructorCache[type];
	}

	public lua_CSFunction GetMethodWrap(Type type, string methodName)
	{
		if (!methodsCache.ContainsKey(type))
		{
			methodsCache[type] = new Dictionary<string, lua_CSFunction>();
		}
		Dictionary<string, lua_CSFunction> dictionary = methodsCache[type];
		if (!dictionary.ContainsKey(methodName))
		{
			MemberInfo[] member = type.GetMember(methodName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			if (member == null || member.Length == 0 || member[0].MemberType != MemberTypes.Method)
			{
				return null;
			}
			dictionary[methodName] = _GenMethodWrap(type, methodName, member).Call;
		}
		return dictionary[methodName];
	}

	public lua_CSFunction GetMethodWrapInCache(Type type, string methodName)
	{
		if (!methodsCache.ContainsKey(type))
		{
			methodsCache[type] = new Dictionary<string, lua_CSFunction>();
		}
		Dictionary<string, lua_CSFunction> dictionary = methodsCache[type];
		if (!dictionary.ContainsKey(methodName))
		{
			return null;
		}
		return dictionary[methodName];
	}

	public lua_CSFunction GetDelegateWrap(Type type)
	{
		if (!typeof(Delegate).IsAssignableFrom(type))
		{
			return null;
		}
		if (!delegateCache.ContainsKey(type))
		{
			delegateCache[type] = _GenMethodWrap(type, type.ToString(), new MethodBase[1] { type.GetMethod("JumpTo") }).Call;
		}
		return delegateCache[type];
	}

	public lua_CSFunction GetEventWrap(Type type, string eventName)
	{
		if (!methodsCache.ContainsKey(type))
		{
			methodsCache[type] = new Dictionary<string, lua_CSFunction>();
		}
		Dictionary<string, lua_CSFunction> dictionary = methodsCache[type];
		if (!dictionary.ContainsKey(eventName))
		{
			EventInfo eventInfo = type.GetEvent(eventName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (eventInfo == null)
			{
				throw new Exception(type.Name + " has no event named: " + eventName);
			}
			int start_idx = 0;
			MethodInfo add = eventInfo.GetAddMethod(nonPublic: true);
			MethodInfo remove = eventInfo.GetRemoveMethod(nonPublic: true);
			if (add == null && remove == null)
			{
				throw new Exception(type.Name + "'s " + eventName + " has either add nor remove");
			}
			bool is_static = ((add != null) ? add.IsStatic : remove.IsStatic);
			if (!is_static)
			{
				start_idx = 1;
			}
			dictionary[eventName] = delegate(IntPtr L)
			{
				object obj = null;
				if (!is_static)
				{
					obj = translator.GetObject(L, 1, type);
					if (obj == null)
					{
						return Lua.luaL_error(L, "invalid #1, needed:" + type);
					}
				}
				try
				{
					object obj2 = translator.CreateDelegateBridge(L, eventInfo.EventHandlerType, start_idx + 2);
					if (obj2 == null)
					{
						return Lua.luaL_error(L, "invalid #" + (start_idx + 2) + ", needed:" + eventInfo.EventHandlerType);
					}
					string text = Lua.lua_tostring(L, start_idx + 1);
					if (!(text == "+"))
					{
						if (!(text == "-"))
						{
							return Lua.luaL_error(L, "invalid #" + (start_idx + 1) + ", needed: '+' or '-'" + eventInfo.EventHandlerType);
						}
						if (remove == null)
						{
							return Lua.luaL_error(L, "no remove for event " + eventName);
						}
						remove.Invoke(obj, new object[1] { obj2 });
					}
					else
					{
						if (add == null)
						{
							return Lua.luaL_error(L, "no add for event " + eventName);
						}
						add.Invoke(obj, new object[1] { obj2 });
					}
				}
				catch (Exception ex)
				{
					return Lua.luaL_error(L, "c# exception:" + ex?.ToString() + ",stack:" + ex.StackTrace);
				}
				return 0;
			};
		}
		return dictionary[eventName];
	}

	public MethodWrap _GenMethodWrap(Type type, string methodName, IEnumerable<MemberInfo> methodBases, bool forceCheck = false)
	{
		List<OverloadMethodWrap> list = new List<OverloadMethodWrap>();
		foreach (MemberInfo methodBasis in methodBases)
		{
			MethodBase method = methodBasis as MethodBase;
			if (!(method == null) && (!method.IsGenericMethodDefinition || tryMakeGenericMethod(ref method)))
			{
				OverloadMethodWrap overloadMethodWrap = new OverloadMethodWrap(translator, type, method);
				overloadMethodWrap.Init(objCheckers, objCasters);
				list.Add(overloadMethodWrap);
			}
		}
		return new MethodWrap(methodName, list, forceCheck);
	}

	private static bool tryMakeGenericMethod(ref MethodBase method)
	{
		try
		{
			if (!(method is MethodInfo) || !Utils.IsSupportedMethod(method as MethodInfo))
			{
				return false;
			}
			Type[] genericArguments = method.GetGenericArguments();
			Type[] array = new Type[genericArguments.Length];
			for (int i = 0; i < genericArguments.Length; i++)
			{
				Type[] genericParameterConstraints = genericArguments[i].GetGenericParameterConstraints();
				array[i] = genericParameterConstraints[0];
			}
			method = ((MethodInfo)method).MakeGenericMethod(array);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
