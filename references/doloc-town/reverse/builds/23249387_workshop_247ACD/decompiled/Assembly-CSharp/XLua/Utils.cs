using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using XLua.LuaDLL;

namespace XLua;

public static class Utils
{
	private struct MethodKey
	{
		public string Name;

		public bool IsStatic;
	}

	public const int OBJ_META_IDX = -4;

	public const int METHOD_IDX = -3;

	public const int GETTER_IDX = -2;

	public const int SETTER_IDX = -1;

	public const int CLS_IDX = -4;

	public const int CLS_META_IDX = -3;

	public const int CLS_GETTER_IDX = -2;

	public const int CLS_SETTER_IDX = -1;

	public const string LuaIndexsFieldName = "LuaIndexs";

	public const string LuaNewIndexsFieldName = "LuaNewIndexs";

	public const string LuaClassIndexsFieldName = "LuaClassIndexs";

	public const string LuaClassNewIndexsFieldName = "LuaClassNewIndexs";

	public static bool LoadField(IntPtr L, int idx, string field_name)
	{
		idx = ((idx > 0) ? idx : (Lua.lua_gettop(L) + idx + 1));
		Lua.xlua_pushasciistring(L, field_name);
		Lua.lua_rawget(L, idx);
		return !Lua.lua_isnil(L, -1);
	}

	public static IntPtr GetMainState(IntPtr L)
	{
		IntPtr result = default(IntPtr);
		Lua.xlua_pushasciistring(L, "xlua_main_thread");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		if (Lua.lua_isthread(L, -1))
		{
			result = Lua.lua_tothread(L, -1);
		}
		Lua.lua_pop(L, 1);
		return result;
	}

	public static List<Type> GetAllTypes(bool exclude_generic_definition = true)
	{
		List<Type> list = new List<Type>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			try
			{
				list.AddRange(from type in assemblies[i].GetTypes()
					where !exclude_generic_definition || !type.IsGenericTypeDefinition()
					select type);
			}
			catch (Exception)
			{
			}
		}
		return list;
	}

	private static lua_CSFunction genFieldGetter(Type type, FieldInfo field)
	{
		if (field.IsStatic)
		{
			return delegate(IntPtr L)
			{
				ObjectTranslatorPool.Instance.Find(L).PushAny(L, field.GetValue(null));
				return 1;
			};
		}
		return delegate(IntPtr L)
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			object obj = objectTranslator.FastGetCSObj(L, 1);
			if (obj == null || !type.IsInstanceOfType(obj))
			{
				return Lua.luaL_error(L, "Expected type " + type?.ToString() + ", but got " + ((obj == null) ? "null" : obj.GetType().ToString()) + ", while get field " + field);
			}
			objectTranslator.PushAny(L, field.GetValue(obj));
			return 1;
		};
	}

	private static lua_CSFunction genFieldSetter(Type type, FieldInfo field)
	{
		if (field.IsStatic)
		{
			return delegate(IntPtr L)
			{
				object object2 = ObjectTranslatorPool.Instance.Find(L).GetObject(L, 1, field.FieldType);
				if (field.FieldType.IsValueType() && Nullable.GetUnderlyingType(field.FieldType) == null && object2 == null)
				{
					return Lua.luaL_error(L, type.Name + "." + field.Name + " Expected type " + field.FieldType);
				}
				field.SetValue(null, object2);
				return 0;
			};
		}
		return delegate(IntPtr L)
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			object obj = objectTranslator.FastGetCSObj(L, 1);
			if (obj == null || !type.IsInstanceOfType(obj))
			{
				return Lua.luaL_error(L, "Expected type " + type?.ToString() + ", but got " + ((obj == null) ? "null" : obj.GetType().ToString()) + ", while set field " + field);
			}
			object @object = objectTranslator.GetObject(L, 2, field.FieldType);
			if (field.FieldType.IsValueType() && Nullable.GetUnderlyingType(field.FieldType) == null && @object == null)
			{
				return Lua.luaL_error(L, type.Name + "." + field.Name + " Expected type " + field.FieldType);
			}
			field.SetValue(obj, @object);
			if (type.IsValueType())
			{
				objectTranslator.Update(L, 1, obj);
			}
			return 0;
		};
	}

	private static lua_CSFunction genItemGetter(Type type, PropertyInfo[] props)
	{
		props = props.Where((PropertyInfo prop) => !prop.GetIndexParameters()[0].ParameterType.IsAssignableFrom(typeof(string))).ToArray();
		if (props.Length == 0)
		{
			return null;
		}
		Type[] params_type = new Type[props.Length];
		for (int i = 0; i < props.Length; i++)
		{
			params_type[i] = props[i].GetIndexParameters()[0].ParameterType;
		}
		object[] arg = new object[1];
		return delegate(IntPtr L)
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			object obj = objectTranslator.FastGetCSObj(L, 1);
			if (obj == null || !type.IsInstanceOfType(obj))
			{
				return Lua.luaL_error(L, "Expected type " + type?.ToString() + ", but got " + ((obj == null) ? "null" : obj.GetType().ToString()) + ", while get prop " + props[0].Name);
			}
			for (int j = 0; j < props.Length; j++)
			{
				if (objectTranslator.Assignable(L, 2, params_type[j]))
				{
					PropertyInfo propertyInfo = props[j];
					try
					{
						object @object = objectTranslator.GetObject(L, 2, params_type[j]);
						arg[0] = @object;
						object value = propertyInfo.GetValue(obj, arg);
						Lua.lua_pushboolean(L, value: true);
						objectTranslator.PushAny(L, value);
						return 2;
					}
					catch (Exception ex)
					{
						return Lua.luaL_error(L, "try to get " + type?.ToString() + "." + propertyInfo.Name + " throw a exception:" + ex?.ToString() + ",stack:" + ex.StackTrace);
					}
				}
			}
			Lua.lua_pushboolean(L, value: false);
			return 1;
		};
	}

	private static lua_CSFunction genItemSetter(Type type, PropertyInfo[] props)
	{
		props = props.Where((PropertyInfo prop) => !prop.GetIndexParameters()[0].ParameterType.IsAssignableFrom(typeof(string))).ToArray();
		if (props.Length == 0)
		{
			return null;
		}
		Type[] params_type = new Type[props.Length];
		for (int i = 0; i < props.Length; i++)
		{
			params_type[i] = props[i].GetIndexParameters()[0].ParameterType;
		}
		object[] arg = new object[1];
		return delegate(IntPtr L)
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			object obj = objectTranslator.FastGetCSObj(L, 1);
			if (obj == null || !type.IsInstanceOfType(obj))
			{
				return Lua.luaL_error(L, "Expected type " + type?.ToString() + ", but got " + ((obj == null) ? "null" : obj.GetType().ToString()) + ", while set prop " + props[0].Name);
			}
			for (int j = 0; j < props.Length; j++)
			{
				if (objectTranslator.Assignable(L, 2, params_type[j]))
				{
					PropertyInfo propertyInfo = props[j];
					try
					{
						arg[0] = objectTranslator.GetObject(L, 2, params_type[j]);
						object @object = objectTranslator.GetObject(L, 3, propertyInfo.PropertyType);
						if (@object == null)
						{
							return Lua.luaL_error(L, type.Name + "." + propertyInfo.Name + " Expected type " + propertyInfo.PropertyType);
						}
						propertyInfo.SetValue(obj, @object, arg);
						Lua.lua_pushboolean(L, value: true);
						return 1;
					}
					catch (Exception ex)
					{
						return Lua.luaL_error(L, "try to set " + type?.ToString() + "." + propertyInfo.Name + " throw a exception:" + ex?.ToString() + ",stack:" + ex.StackTrace);
					}
				}
			}
			Lua.lua_pushboolean(L, value: false);
			return 1;
		};
	}

	private static lua_CSFunction genEnumCastFrom(Type type)
	{
		return delegate(IntPtr L)
		{
			try
			{
				return ObjectTranslatorPool.Instance.Find(L).TranslateToEnumToTop(L, type, 1);
			}
			catch (Exception ex)
			{
				return Lua.luaL_error(L, "cast to " + type?.ToString() + " exception:" + ex);
			}
		};
	}

	internal static IEnumerable<MethodInfo> GetExtensionMethodsOf(Type type_to_be_extend)
	{
		if (InternalGlobals.extensionMethodMap == null)
		{
			List<Type> list = new List<Type>();
			IEnumerator<Type> enumerator = GetAllTypes().GetEnumerator();
			while (enumerator.MoveNext())
			{
				Type current = enumerator.Current;
				if (current.IsDefined(typeof(ExtensionAttribute), inherit: false) && current.IsDefined(typeof(ReflectionUseAttribute), inherit: false))
				{
					list.Add(current);
				}
				if (!current.IsAbstract() || !current.IsSealed())
				{
					continue;
				}
				FieldInfo[] fields = current.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (FieldInfo fieldInfo in fields)
				{
					if (fieldInfo.IsDefined(typeof(ReflectionUseAttribute), inherit: false) && typeof(IEnumerable<Type>).IsAssignableFrom(fieldInfo.FieldType))
					{
						list.AddRange((fieldInfo.GetValue(null) as IEnumerable<Type>).Where((Type t) => t.IsDefined(typeof(ExtensionAttribute), inherit: false)));
					}
				}
				PropertyInfo[] properties = current.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (PropertyInfo propertyInfo in properties)
				{
					if (propertyInfo.IsDefined(typeof(ReflectionUseAttribute), inherit: false) && typeof(IEnumerable<Type>).IsAssignableFrom(propertyInfo.PropertyType))
					{
						list.AddRange((propertyInfo.GetValue(null, null) as IEnumerable<Type>).Where((Type t) => t.IsDefined(typeof(ExtensionAttribute), inherit: false)));
					}
				}
			}
			enumerator.Dispose();
			InternalGlobals.extensionMethodMap = (from type in list
				from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
				where method.IsDefined(typeof(ExtensionAttribute), inherit: false) && IsSupportedMethod(method)
				group method by getExtendedType(method)).ToDictionary((Func<IGrouping<Type, MethodInfo>, Type>)((IGrouping<Type, MethodInfo> g) => g.Key), (Func<IGrouping<Type, MethodInfo>, IEnumerable<MethodInfo>>)((IGrouping<Type, MethodInfo> g) => g));
		}
		IEnumerable<MethodInfo> value = null;
		InternalGlobals.extensionMethodMap.TryGetValue(type_to_be_extend, out value);
		return value;
	}

	private static void makeReflectionWrap(IntPtr L, Type type, int cls_field, int cls_getter, int cls_setter, int obj_field, int obj_getter, int obj_setter, int obj_meta, out lua_CSFunction item_getter, out lua_CSFunction item_setter, BindingFlags access)
	{
		ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
		BindingFlags bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | access;
		FieldInfo[] fields = type.GetFields(bindingFlags);
		EventInfo[] events = type.GetEvents(bindingFlags | BindingFlags.Public | BindingFlags.NonPublic);
		Lua.lua_checkstack(L, 2);
		foreach (FieldInfo fieldInfo in fields)
		{
			string fieldName = fieldInfo.Name;
			if (!fieldInfo.IsStatic || (!fieldInfo.Name.StartsWith("__Hotfix") && !fieldInfo.Name.StartsWith("_c__Hotfix")) || !typeof(Delegate).IsAssignableFrom(fieldInfo.FieldType))
			{
				if (events.Any((EventInfo e) => e.Name == fieldName))
				{
					fieldName = "&" + fieldName;
				}
				if (fieldInfo.IsStatic && (fieldInfo.IsInitOnly || fieldInfo.IsLiteral))
				{
					Lua.xlua_pushasciistring(L, fieldName);
					objectTranslator.PushAny(L, fieldInfo.GetValue(null));
					Lua.lua_rawset(L, cls_field);
					continue;
				}
				Lua.xlua_pushasciistring(L, fieldName);
				objectTranslator.PushFixCSFunction(L, genFieldGetter(type, fieldInfo));
				Lua.lua_rawset(L, fieldInfo.IsStatic ? cls_getter : obj_getter);
				Lua.xlua_pushasciistring(L, fieldName);
				objectTranslator.PushFixCSFunction(L, genFieldSetter(type, fieldInfo));
				Lua.lua_rawset(L, fieldInfo.IsStatic ? cls_setter : obj_setter);
			}
		}
		EventInfo[] events2 = type.GetEvents(bindingFlags);
		foreach (EventInfo eventInfo in events2)
		{
			Lua.xlua_pushasciistring(L, eventInfo.Name);
			objectTranslator.PushFixCSFunction(L, objectTranslator.methodWrapsCache.GetEventWrap(type, eventInfo.Name));
			bool flag = ((eventInfo.GetAddMethod(nonPublic: true) != null) ? eventInfo.GetAddMethod(nonPublic: true).IsStatic : eventInfo.GetRemoveMethod(nonPublic: true).IsStatic);
			Lua.lua_rawset(L, flag ? cls_field : obj_field);
		}
		List<PropertyInfo> list = new List<PropertyInfo>();
		PropertyInfo[] properties = type.GetProperties(bindingFlags);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.GetIndexParameters().Length != 0)
			{
				list.Add(propertyInfo);
			}
		}
		PropertyInfo[] array = list.ToArray();
		item_getter = ((array.Length != 0) ? genItemGetter(type, array) : null);
		item_setter = ((array.Length != 0) ? genItemSetter(type, array) : null);
		MethodInfo[] array2 = type.GetMethods(bindingFlags);
		if (access == BindingFlags.NonPublic)
		{
			array2 = (from p in type.GetMethods(bindingFlags | BindingFlags.Public)
				join q in array2 on p.Name equals q.Name
				select p).ToArray();
		}
		Dictionary<MethodKey, List<MemberInfo>> dictionary = new Dictionary<MethodKey, List<MemberInfo>>();
		foreach (MethodInfo methodInfo in array2)
		{
			string name = methodInfo.Name;
			MethodKey methodKey = default(MethodKey);
			methodKey.Name = name;
			methodKey.IsStatic = methodInfo.IsStatic;
			MethodKey key = methodKey;
			if (dictionary.TryGetValue(key, out var value))
			{
				value.Add(methodInfo);
			}
			else
			{
				if ((methodInfo.IsSpecialName && ((methodInfo.Name == "get_Item" && methodInfo.GetParameters().Length == 1) || (methodInfo.Name == "set_Item" && methodInfo.GetParameters().Length == 2)) && !methodInfo.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(string))) || ((name.StartsWith("add_") || name.StartsWith("remove_")) && methodInfo.IsSpecialName))
				{
					continue;
				}
				if (name.StartsWith("op_") && methodInfo.IsSpecialName)
				{
					if (InternalGlobals.supportOp.ContainsKey(name))
					{
						if (value == null)
						{
							value = new List<MemberInfo>();
							dictionary.Add(key, value);
						}
						value.Add(methodInfo);
					}
				}
				else if (name.StartsWith("get_") && methodInfo.IsSpecialName && methodInfo.GetParameters().Length != 1)
				{
					string text = methodInfo.Name.Substring(4);
					Lua.xlua_pushasciistring(L, text);
					objectTranslator.PushFixCSFunction(L, objectTranslator.methodWrapsCache._GenMethodWrap(methodInfo.DeclaringType, text, new MethodBase[1] { methodInfo }).Call);
					Lua.lua_rawset(L, methodInfo.IsStatic ? cls_getter : obj_getter);
				}
				else if (name.StartsWith("set_") && methodInfo.IsSpecialName && methodInfo.GetParameters().Length != 2)
				{
					string text2 = methodInfo.Name.Substring(4);
					Lua.xlua_pushasciistring(L, text2);
					objectTranslator.PushFixCSFunction(L, objectTranslator.methodWrapsCache._GenMethodWrap(methodInfo.DeclaringType, text2, new MethodBase[1] { methodInfo }).Call);
					Lua.lua_rawset(L, methodInfo.IsStatic ? cls_setter : obj_setter);
				}
				else if (!(name == ".ctor") || !methodInfo.IsConstructor)
				{
					if (value == null)
					{
						value = new List<MemberInfo>();
						dictionary.Add(key, value);
					}
					value.Add(methodInfo);
				}
			}
		}
		IEnumerable<MethodInfo> extensionMethodsOf = GetExtensionMethodsOf(type);
		if (extensionMethodsOf != null)
		{
			foreach (MethodInfo item in extensionMethodsOf)
			{
				MethodKey methodKey = default(MethodKey);
				methodKey.Name = item.Name;
				methodKey.IsStatic = false;
				MethodKey key2 = methodKey;
				if (dictionary.TryGetValue(key2, out var value2))
				{
					value2.Add(item);
					continue;
				}
				value2 = new List<MemberInfo> { item };
				dictionary.Add(key2, value2);
			}
		}
		foreach (KeyValuePair<MethodKey, List<MemberInfo>> item2 in dictionary)
		{
			if (item2.Key.Name.StartsWith("op_"))
			{
				Lua.xlua_pushasciistring(L, InternalGlobals.supportOp[item2.Key.Name]);
				objectTranslator.PushFixCSFunction(L, objectTranslator.methodWrapsCache._GenMethodWrap(type, item2.Key.Name, item2.Value.ToArray()).Call);
				Lua.lua_rawset(L, obj_meta);
			}
			else
			{
				Lua.xlua_pushasciistring(L, item2.Key.Name);
				objectTranslator.PushFixCSFunction(L, objectTranslator.methodWrapsCache._GenMethodWrap(type, item2.Key.Name, item2.Value.ToArray()).Call);
				Lua.lua_rawset(L, item2.Key.IsStatic ? cls_field : obj_field);
			}
		}
	}

	public static void loadUpvalue(IntPtr L, Type type, string metafunc, int index)
	{
		ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
		Lua.xlua_pushasciistring(L, metafunc);
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		objectTranslator.Push(L, type);
		Lua.lua_rawget(L, -2);
		Lua.lua_remove(L, -2);
		for (int i = 1; i <= index; i++)
		{
			Lua.lua_getupvalue(L, -i, i);
			if (Lua.lua_isnil(L, -1))
			{
				Lua.lua_pop(L, 1);
				Lua.lua_newtable(L);
				Lua.lua_pushvalue(L, -1);
				Lua.lua_setupvalue(L, -i - 2, i);
			}
		}
		for (int j = 0; j < index; j++)
		{
			Lua.lua_remove(L, -2);
		}
	}

	public static void RegisterEnumType(IntPtr L, Type type)
	{
		ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
		string[] names = Enum.GetNames(type);
		foreach (string text in names)
		{
			RegisterObject(L, translator, -4, text, Enum.Parse(type, text));
		}
	}

	public static void MakePrivateAccessible(IntPtr L, Type type)
	{
		Lua.lua_checkstack(L, 20);
		int newTop = Lua.lua_gettop(L);
		Lua.luaL_getmetatable(L, type.FullName);
		if (Lua.lua_isnil(L, -1))
		{
			Lua.lua_settop(L, newTop);
			throw new Exception("can not find the metatable for " + type);
		}
		int obj_meta = Lua.lua_gettop(L);
		LoadCSTable(L, type);
		if (Lua.lua_isnil(L, -1))
		{
			Lua.lua_settop(L, newTop);
			throw new Exception("can not find the class for " + type);
		}
		int cls_field = Lua.lua_gettop(L);
		loadUpvalue(L, type, "LuaIndexs", 2);
		int obj_getter = Lua.lua_gettop(L);
		loadUpvalue(L, type, "LuaIndexs", 1);
		int obj_field = Lua.lua_gettop(L);
		loadUpvalue(L, type, "LuaNewIndexs", 1);
		int obj_setter = Lua.lua_gettop(L);
		loadUpvalue(L, type, "LuaClassIndexs", 1);
		int cls_getter = Lua.lua_gettop(L);
		loadUpvalue(L, type, "LuaClassNewIndexs", 1);
		int cls_setter = Lua.lua_gettop(L);
		makeReflectionWrap(L, type, cls_field, cls_getter, cls_setter, obj_field, obj_getter, obj_setter, obj_meta, out var _, out var _, BindingFlags.NonPublic);
		Lua.lua_settop(L, newTop);
		Type[] nestedTypes = type.GetNestedTypes(BindingFlags.NonPublic);
		foreach (Type type2 in nestedTypes)
		{
			if ((type2.IsAbstract() || !typeof(Delegate).IsAssignableFrom(type2)) && !type2.IsGenericTypeDefinition())
			{
				ObjectTranslatorPool.Instance.Find(L).TryDelayWrapLoader(L, type2);
				MakePrivateAccessible(L, type2);
			}
		}
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	internal static int LazyReflectionCall(IntPtr L)
	{
		try
		{
			ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
			objectTranslator.Get(L, Lua.xlua_upvalueindex(1), out Type v);
			LazyMemberTypes lazyMemberTypes = (LazyMemberTypes)Lua.xlua_tointeger(L, Lua.xlua_upvalueindex(2));
			string memberName = Lua.lua_tostring(L, Lua.xlua_upvalueindex(3));
			bool flag = Lua.lua_toboolean(L, Lua.xlua_upvalueindex(4));
			lua_CSFunction lua_CSFunction = null;
			switch (lazyMemberTypes)
			{
			case LazyMemberTypes.Method:
			{
				MemberInfo[] member = v.GetMember(memberName);
				if (member == null || member.Length == 0)
				{
					return Lua.luaL_error(L, "can not find " + memberName + " for " + v);
				}
				IEnumerable<MemberInfo> enumerable = member;
				if (!flag)
				{
					IEnumerable<MethodInfo> extensionMethodsOf = GetExtensionMethodsOf(v);
					if (extensionMethodsOf != null)
					{
						enumerable = enumerable.Concat(extensionMethodsOf.Where((MethodInfo m) => m.Name == memberName).Cast<MemberInfo>());
					}
				}
				lua_CSFunction = objectTranslator.methodWrapsCache._GenMethodWrap(v, memberName, enumerable.ToArray()).Call;
				if (flag)
				{
					LoadCSTable(L, v);
				}
				else
				{
					loadUpvalue(L, v, "LuaIndexs", 1);
				}
				if (Lua.lua_isnil(L, -1))
				{
					return Lua.luaL_error(L, "can not find the meta info for " + v);
				}
				break;
			}
			case LazyMemberTypes.FieldGet:
			case LazyMemberTypes.FieldSet:
			{
				FieldInfo field = v.GetField(memberName);
				if (field == null)
				{
					return Lua.luaL_error(L, "can not find " + memberName + " for " + v);
				}
				if (flag)
				{
					if (lazyMemberTypes == LazyMemberTypes.FieldGet)
					{
						loadUpvalue(L, v, "LuaClassIndexs", 1);
					}
					else
					{
						loadUpvalue(L, v, "LuaClassNewIndexs", 1);
					}
				}
				else if (lazyMemberTypes == LazyMemberTypes.FieldGet)
				{
					loadUpvalue(L, v, "LuaIndexs", 2);
				}
				else
				{
					loadUpvalue(L, v, "LuaNewIndexs", 1);
				}
				lua_CSFunction = ((lazyMemberTypes == LazyMemberTypes.FieldGet) ? genFieldGetter(v, field) : genFieldSetter(v, field));
				break;
			}
			case LazyMemberTypes.PropertyGet:
			case LazyMemberTypes.PropertySet:
			{
				PropertyInfo property = v.GetProperty(memberName);
				if (property == null)
				{
					return Lua.luaL_error(L, "can not find " + memberName + " for " + v);
				}
				if (flag)
				{
					if (lazyMemberTypes == LazyMemberTypes.PropertyGet)
					{
						loadUpvalue(L, v, "LuaClassIndexs", 1);
					}
					else
					{
						loadUpvalue(L, v, "LuaClassNewIndexs", 1);
					}
				}
				else if (lazyMemberTypes == LazyMemberTypes.PropertyGet)
				{
					loadUpvalue(L, v, "LuaIndexs", 2);
				}
				else
				{
					loadUpvalue(L, v, "LuaNewIndexs", 1);
				}
				if (Lua.lua_isnil(L, -1))
				{
					return Lua.luaL_error(L, "can not find the meta info for " + v);
				}
				lua_CSFunction = objectTranslator.methodWrapsCache._GenMethodWrap(property.DeclaringType, property.Name, new MethodBase[1] { (lazyMemberTypes == LazyMemberTypes.PropertyGet) ? property.GetGetMethod() : property.GetSetMethod() }).Call;
				break;
			}
			case LazyMemberTypes.Event:
			{
				EventInfo @event = v.GetEvent(memberName);
				if (@event == null)
				{
					return Lua.luaL_error(L, "can not find " + memberName + " for " + v);
				}
				if (flag)
				{
					LoadCSTable(L, v);
				}
				else
				{
					loadUpvalue(L, v, "LuaIndexs", 1);
				}
				if (Lua.lua_isnil(L, -1))
				{
					return Lua.luaL_error(L, "can not find the meta info for " + v);
				}
				lua_CSFunction = objectTranslator.methodWrapsCache.GetEventWrap(v, @event.Name);
				break;
			}
			default:
				return Lua.luaL_error(L, "unsupport member type" + lazyMemberTypes);
			}
			Lua.xlua_pushasciistring(L, memberName);
			objectTranslator.PushFixCSFunction(L, lua_CSFunction);
			Lua.lua_rawset(L, -3);
			Lua.lua_pop(L, 1);
			return lua_CSFunction(L);
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, "c# exception in LazyReflectionCall:" + ex);
		}
	}

	public static void ReflectionWrap(IntPtr L, Type type, bool privateAccessible)
	{
		Lua.lua_checkstack(L, 20);
		Lua.lua_gettop(L);
		ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
		Lua.luaL_getmetatable(L, type.FullName);
		if (Lua.lua_isnil(L, -1))
		{
			Lua.lua_pop(L, 1);
			Lua.luaL_newmetatable(L, type.FullName);
		}
		Lua.lua_pushlightuserdata(L, Lua.xlua_tag());
		Lua.lua_pushnumber(L, 1.0);
		Lua.lua_rawset(L, -3);
		int num = Lua.lua_gettop(L);
		Lua.lua_newtable(L);
		int index = Lua.lua_gettop(L);
		Lua.lua_newtable(L);
		int num2 = Lua.lua_gettop(L);
		Lua.lua_newtable(L);
		int num3 = Lua.lua_gettop(L);
		Lua.lua_newtable(L);
		int num4 = Lua.lua_gettop(L);
		Lua.lua_newtable(L);
		int num5 = Lua.lua_gettop(L);
		SetCSTable(L, type, num5);
		Lua.lua_newtable(L);
		int num6 = Lua.lua_gettop(L);
		Lua.lua_newtable(L);
		int num7 = Lua.lua_gettop(L);
		makeReflectionWrap(L, type, num5, num6, num7, num2, num3, num4, num, out var item_getter, out var item_setter, privateAccessible ? (BindingFlags.Public | BindingFlags.NonPublic) : BindingFlags.Public);
		Lua.xlua_pushasciistring(L, "__gc");
		Lua.lua_pushstdcallcfunction(L, objectTranslator.metaFunctions.GcMeta);
		Lua.lua_rawset(L, num);
		Lua.xlua_pushasciistring(L, "__tostring");
		Lua.lua_pushstdcallcfunction(L, objectTranslator.metaFunctions.ToStringMeta);
		Lua.lua_rawset(L, num);
		Lua.xlua_pushasciistring(L, "__index");
		Lua.lua_pushvalue(L, num2);
		Lua.lua_pushvalue(L, num3);
		objectTranslator.PushFixCSFunction(L, item_getter);
		objectTranslator.PushAny(L, type.BaseType());
		Lua.xlua_pushasciistring(L, "LuaIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		Lua.lua_pushnil(L);
		Lua.gen_obj_indexer(L);
		Lua.xlua_pushasciistring(L, "LuaIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		objectTranslator.Push(L, type);
		Lua.lua_pushvalue(L, -3);
		Lua.lua_rawset(L, -3);
		Lua.lua_pop(L, 1);
		Lua.lua_rawset(L, num);
		Lua.xlua_pushasciistring(L, "__newindex");
		Lua.lua_pushvalue(L, num4);
		objectTranslator.PushFixCSFunction(L, item_setter);
		objectTranslator.Push(L, type.BaseType());
		Lua.xlua_pushasciistring(L, "LuaNewIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		Lua.lua_pushnil(L);
		Lua.gen_obj_newindexer(L);
		Lua.xlua_pushasciistring(L, "LuaNewIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		objectTranslator.Push(L, type);
		Lua.lua_pushvalue(L, -3);
		Lua.lua_rawset(L, -3);
		Lua.lua_pop(L, 1);
		Lua.lua_rawset(L, num);
		Lua.xlua_pushasciistring(L, "UnderlyingSystemType");
		objectTranslator.PushAny(L, type);
		Lua.lua_rawset(L, num5);
		if (type != null && type.IsEnum())
		{
			Lua.xlua_pushasciistring(L, "__CastFrom");
			objectTranslator.PushFixCSFunction(L, genEnumCastFrom(type));
			Lua.lua_rawset(L, num5);
		}
		Lua.xlua_pushasciistring(L, "__index");
		Lua.lua_pushvalue(L, num6);
		Lua.lua_pushvalue(L, num5);
		objectTranslator.Push(L, type.BaseType());
		Lua.xlua_pushasciistring(L, "LuaClassIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		Lua.gen_cls_indexer(L);
		Lua.xlua_pushasciistring(L, "LuaClassIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		objectTranslator.Push(L, type);
		Lua.lua_pushvalue(L, -3);
		Lua.lua_rawset(L, -3);
		Lua.lua_pop(L, 1);
		Lua.lua_rawset(L, index);
		Lua.xlua_pushasciistring(L, "__newindex");
		Lua.lua_pushvalue(L, num7);
		objectTranslator.Push(L, type.BaseType());
		Lua.xlua_pushasciistring(L, "LuaClassNewIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		Lua.gen_cls_newindexer(L);
		Lua.xlua_pushasciistring(L, "LuaClassNewIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		objectTranslator.Push(L, type);
		Lua.lua_pushvalue(L, -3);
		Lua.lua_rawset(L, -3);
		Lua.lua_pop(L, 1);
		Lua.lua_rawset(L, index);
		lua_CSFunction lua_CSFunction = (typeof(Delegate).IsAssignableFrom(type) ? objectTranslator.metaFunctions.DelegateCtor : objectTranslator.methodWrapsCache.GetConstructorWrap(type));
		if (lua_CSFunction == null)
		{
			lua_CSFunction = (IntPtr LL) => Lua.luaL_error(LL, "No constructor for " + type);
		}
		Lua.xlua_pushasciistring(L, "__call");
		objectTranslator.PushFixCSFunction(L, lua_CSFunction);
		Lua.lua_rawset(L, index);
		Lua.lua_pushvalue(L, index);
		Lua.lua_setmetatable(L, num5);
		Lua.lua_pop(L, 8);
	}

	public static void BeginObjectRegister(Type type, IntPtr L, ObjectTranslator translator, int meta_count, int method_count, int getter_count, int setter_count, int type_id = -1)
	{
		if (type == null)
		{
			if (type_id == -1)
			{
				throw new Exception("Fatal: must provide a type of type_id");
			}
			Lua.xlua_rawgeti(L, LuaIndexes.LUA_REGISTRYINDEX, type_id);
		}
		else
		{
			Lua.luaL_getmetatable(L, type.FullName);
			if (Lua.lua_isnil(L, -1))
			{
				Lua.lua_pop(L, 1);
				Lua.luaL_newmetatable(L, type.FullName);
			}
		}
		Lua.lua_pushlightuserdata(L, Lua.xlua_tag());
		Lua.lua_pushnumber(L, 1.0);
		Lua.lua_rawset(L, -3);
		if ((type == null || !translator.HasCustomOp(type)) && type != typeof(decimal))
		{
			Lua.xlua_pushasciistring(L, "__gc");
			Lua.lua_pushstdcallcfunction(L, translator.metaFunctions.GcMeta);
			Lua.lua_rawset(L, -3);
		}
		Lua.xlua_pushasciistring(L, "__tostring");
		Lua.lua_pushstdcallcfunction(L, translator.metaFunctions.ToStringMeta);
		Lua.lua_rawset(L, -3);
		if (method_count == 0)
		{
			Lua.lua_pushnil(L);
		}
		else
		{
			Lua.lua_createtable(L, 0, method_count);
		}
		if (getter_count == 0)
		{
			Lua.lua_pushnil(L);
		}
		else
		{
			Lua.lua_createtable(L, 0, getter_count);
		}
		if (setter_count == 0)
		{
			Lua.lua_pushnil(L);
		}
		else
		{
			Lua.lua_createtable(L, 0, setter_count);
		}
	}

	private static int abs_idx(int top, int idx)
	{
		if (idx <= 0)
		{
			return top + idx + 1;
		}
		return idx;
	}

	public static void EndObjectRegister(Type type, IntPtr L, ObjectTranslator translator, lua_CSFunction csIndexer, lua_CSFunction csNewIndexer, Type base_type, lua_CSFunction arrayIndexer, lua_CSFunction arrayNewIndexer)
	{
		int top = Lua.lua_gettop(L);
		int index = abs_idx(top, -4);
		int index2 = abs_idx(top, -3);
		int index3 = abs_idx(top, -2);
		int index4 = abs_idx(top, -1);
		Lua.xlua_pushasciistring(L, "__index");
		Lua.lua_pushvalue(L, index2);
		Lua.lua_pushvalue(L, index3);
		if (csIndexer == null)
		{
			Lua.lua_pushnil(L);
		}
		else
		{
			Lua.lua_pushstdcallcfunction(L, csIndexer);
		}
		translator.Push(L, (type == null) ? base_type : type.BaseType());
		Lua.xlua_pushasciistring(L, "LuaIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		if (arrayIndexer == null)
		{
			Lua.lua_pushnil(L);
		}
		else
		{
			Lua.lua_pushstdcallcfunction(L, arrayIndexer);
		}
		Lua.gen_obj_indexer(L);
		if (type != null)
		{
			Lua.xlua_pushasciistring(L, "LuaIndexs");
			Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
			translator.Push(L, type);
			Lua.lua_pushvalue(L, -3);
			Lua.lua_rawset(L, -3);
			Lua.lua_pop(L, 1);
		}
		Lua.lua_rawset(L, index);
		Lua.xlua_pushasciistring(L, "__newindex");
		Lua.lua_pushvalue(L, index4);
		if (csNewIndexer == null)
		{
			Lua.lua_pushnil(L);
		}
		else
		{
			Lua.lua_pushstdcallcfunction(L, csNewIndexer);
		}
		translator.Push(L, (type == null) ? base_type : type.BaseType());
		Lua.xlua_pushasciistring(L, "LuaNewIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		if (arrayNewIndexer == null)
		{
			Lua.lua_pushnil(L);
		}
		else
		{
			Lua.lua_pushstdcallcfunction(L, arrayNewIndexer);
		}
		Lua.gen_obj_newindexer(L);
		if (type != null)
		{
			Lua.xlua_pushasciistring(L, "LuaNewIndexs");
			Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
			translator.Push(L, type);
			Lua.lua_pushvalue(L, -3);
			Lua.lua_rawset(L, -3);
			Lua.lua_pop(L, 1);
		}
		Lua.lua_rawset(L, index);
		Lua.lua_pop(L, 4);
	}

	public static void RegisterFunc(IntPtr L, int idx, string name, lua_CSFunction func)
	{
		idx = abs_idx(Lua.lua_gettop(L), idx);
		Lua.xlua_pushasciistring(L, name);
		Lua.lua_pushstdcallcfunction(L, func);
		Lua.lua_rawset(L, idx);
	}

	public static void RegisterLazyFunc(IntPtr L, int idx, string name, Type type, LazyMemberTypes memberType, bool isStatic)
	{
		idx = abs_idx(Lua.lua_gettop(L), idx);
		Lua.xlua_pushasciistring(L, name);
		ObjectTranslatorPool.Instance.Find(L).PushAny(L, type);
		Lua.xlua_pushinteger(L, (int)memberType);
		Lua.lua_pushstring(L, name);
		Lua.lua_pushboolean(L, isStatic);
		Lua.lua_pushstdcallcfunction(L, InternalGlobals.LazyReflectionWrap, 4);
		Lua.lua_rawset(L, idx);
	}

	public static void RegisterObject(IntPtr L, ObjectTranslator translator, int idx, string name, object obj)
	{
		idx = abs_idx(Lua.lua_gettop(L), idx);
		Lua.xlua_pushasciistring(L, name);
		translator.PushAny(L, obj);
		Lua.lua_rawset(L, idx);
	}

	public static void BeginClassRegister(Type type, IntPtr L, lua_CSFunction creator, int class_field_count, int static_getter_count, int static_setter_count)
	{
		ObjectTranslator objectTranslator = ObjectTranslatorPool.Instance.Find(L);
		Lua.lua_createtable(L, 0, class_field_count);
		Lua.xlua_pushasciistring(L, "UnderlyingSystemType");
		objectTranslator.PushAny(L, type);
		Lua.lua_rawset(L, -3);
		int num = Lua.lua_gettop(L);
		SetCSTable(L, type, num);
		Lua.lua_createtable(L, 0, 3);
		int index = Lua.lua_gettop(L);
		if (creator != null)
		{
			Lua.xlua_pushasciistring(L, "__call");
			Lua.lua_pushstdcallcfunction(L, creator);
			Lua.lua_rawset(L, -3);
		}
		if (static_getter_count == 0)
		{
			Lua.lua_pushnil(L);
		}
		else
		{
			Lua.lua_createtable(L, 0, static_getter_count);
		}
		if (static_setter_count == 0)
		{
			Lua.lua_pushnil(L);
		}
		else
		{
			Lua.lua_createtable(L, 0, static_setter_count);
		}
		Lua.lua_pushvalue(L, index);
		Lua.lua_setmetatable(L, num);
	}

	public static void EndClassRegister(Type type, IntPtr L, ObjectTranslator translator)
	{
		int top = Lua.lua_gettop(L);
		int index = abs_idx(top, -4);
		int index2 = abs_idx(top, -2);
		int index3 = abs_idx(top, -1);
		int index4 = abs_idx(top, -3);
		Lua.xlua_pushasciistring(L, "__index");
		Lua.lua_pushvalue(L, index2);
		Lua.lua_pushvalue(L, index);
		translator.Push(L, type.BaseType());
		Lua.xlua_pushasciistring(L, "LuaClassIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		Lua.gen_cls_indexer(L);
		Lua.xlua_pushasciistring(L, "LuaClassIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		translator.Push(L, type);
		Lua.lua_pushvalue(L, -3);
		Lua.lua_rawset(L, -3);
		Lua.lua_pop(L, 1);
		Lua.lua_rawset(L, index4);
		Lua.xlua_pushasciistring(L, "__newindex");
		Lua.lua_pushvalue(L, index3);
		translator.Push(L, type.BaseType());
		Lua.xlua_pushasciistring(L, "LuaClassNewIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		Lua.gen_cls_newindexer(L);
		Lua.xlua_pushasciistring(L, "LuaClassNewIndexs");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		translator.Push(L, type);
		Lua.lua_pushvalue(L, -3);
		Lua.lua_rawset(L, -3);
		Lua.lua_pop(L, 1);
		Lua.lua_rawset(L, index4);
		Lua.lua_pop(L, 4);
	}

	private static List<string> getPathOfType(Type type)
	{
		List<string> list = new List<string>();
		if (type.Namespace != null)
		{
			list.AddRange(type.Namespace.Split(new char[1] { '.' }));
		}
		string text = type.ToString().Substring((type.Namespace != null) ? (type.Namespace.Length + 1) : 0);
		if (type.IsNested)
		{
			list.AddRange(text.Split(new char[1] { '+' }));
		}
		else
		{
			list.Add(text);
		}
		return list;
	}

	public static void LoadCSTable(IntPtr L, Type type)
	{
		int newTop = Lua.lua_gettop(L);
		Lua.xlua_pushasciistring(L, "xlua_csharp_namespace");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		List<string> pathOfType = getPathOfType(type);
		for (int i = 0; i < pathOfType.Count; i++)
		{
			Lua.xlua_pushasciistring(L, pathOfType[i]);
			if (Lua.xlua_pgettable(L, -2) != 0)
			{
				Lua.lua_settop(L, newTop);
				Lua.lua_pushnil(L);
				break;
			}
			if (!Lua.lua_istable(L, -1) && i < pathOfType.Count - 1)
			{
				Lua.lua_settop(L, newTop);
				Lua.lua_pushnil(L);
				break;
			}
			Lua.lua_remove(L, -2);
		}
	}

	public static void SetCSTable(IntPtr L, Type type, int cls_table)
	{
		int num = Lua.lua_gettop(L);
		cls_table = abs_idx(num, cls_table);
		Lua.xlua_pushasciistring(L, "xlua_csharp_namespace");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		List<string> pathOfType = getPathOfType(type);
		for (int i = 0; i < pathOfType.Count - 1; i++)
		{
			Lua.xlua_pushasciistring(L, pathOfType[i]);
			if (Lua.xlua_pgettable(L, -2) != 0)
			{
				string text = Lua.lua_tostring(L, -1);
				Lua.lua_settop(L, num);
				throw new Exception("SetCSTable for [" + type?.ToString() + "] error: " + text);
			}
			if (Lua.lua_isnil(L, -1))
			{
				Lua.lua_pop(L, 1);
				Lua.lua_createtable(L, 0, 0);
				Lua.xlua_pushasciistring(L, pathOfType[i]);
				Lua.lua_pushvalue(L, -2);
				Lua.lua_rawset(L, -4);
			}
			else if (!Lua.lua_istable(L, -1))
			{
				Lua.lua_settop(L, num);
				throw new Exception("SetCSTable for [" + type?.ToString() + "] error: ancestors is not a table!");
			}
			Lua.lua_remove(L, -2);
		}
		Lua.xlua_pushasciistring(L, pathOfType[pathOfType.Count - 1]);
		Lua.lua_pushvalue(L, cls_table);
		Lua.lua_rawset(L, -3);
		Lua.lua_pop(L, 1);
		Lua.xlua_pushasciistring(L, "xlua_csharp_namespace");
		Lua.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
		ObjectTranslatorPool.Instance.Find(L).PushAny(L, type);
		Lua.lua_pushvalue(L, cls_table);
		Lua.lua_rawset(L, -3);
		Lua.lua_pop(L, 1);
	}

	public static bool IsParamsMatch(MethodInfo delegateMethod, MethodInfo bridgeMethod)
	{
		if (delegateMethod == null || bridgeMethod == null)
		{
			return false;
		}
		if (delegateMethod.ReturnType != bridgeMethod.ReturnType)
		{
			return false;
		}
		ParameterInfo[] parameters = delegateMethod.GetParameters();
		ParameterInfo[] parameters2 = bridgeMethod.GetParameters();
		if (parameters.Length != parameters2.Length)
		{
			return false;
		}
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].ParameterType != parameters2[i].ParameterType || parameters[i].IsOut != parameters2[i].IsOut)
			{
				return false;
			}
		}
		int num = parameters.Length - 1;
		if (num >= 0)
		{
			return parameters[num].IsDefined(typeof(ParamArrayAttribute), inherit: false) == parameters2[num].IsDefined(typeof(ParamArrayAttribute), inherit: false);
		}
		return true;
	}

	public static bool IsSupportedMethod(MethodInfo method)
	{
		if (!method.ContainsGenericParameters)
		{
			return true;
		}
		ParameterInfo[] parameters = method.GetParameters();
		Type returnType = method.ReturnType;
		bool flag = false;
		bool flag2 = !returnType.IsGenericParameter;
		for (int i = 0; i < parameters.Length; i++)
		{
			Type parameterType = parameters[i].ParameterType;
			if (!parameterType.IsGenericParameter)
			{
				continue;
			}
			Type[] genericParameterConstraints = parameterType.GetGenericParameterConstraints();
			if (genericParameterConstraints.Length == 0)
			{
				return false;
			}
			Type[] array = genericParameterConstraints;
			foreach (Type type in array)
			{
				if (!type.IsClass() || type == typeof(ValueType))
				{
					return false;
				}
			}
			flag = true;
			if (!flag2 && parameterType == returnType)
			{
				flag2 = true;
			}
		}
		return flag && flag2;
	}

	public static MethodInfo MakeGenericMethodWithConstraints(MethodInfo method)
	{
		try
		{
			Type[] genericArguments = method.GetGenericArguments();
			Type[] array = new Type[genericArguments.Length];
			for (int i = 0; i < genericArguments.Length; i++)
			{
				Type[] genericParameterConstraints = genericArguments[i].GetGenericParameterConstraints();
				array[i] = genericParameterConstraints[0];
			}
			return method.MakeGenericMethod(array);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static Type getExtendedType(MethodInfo method)
	{
		Type parameterType = method.GetParameters()[0].ParameterType;
		if (!parameterType.IsGenericParameter)
		{
			return parameterType;
		}
		Type[] genericParameterConstraints = parameterType.GetGenericParameterConstraints();
		if (genericParameterConstraints.Length == 0)
		{
			throw new InvalidOperationException();
		}
		Type obj = genericParameterConstraints[0];
		if (!obj.IsClass())
		{
			throw new InvalidOperationException();
		}
		return obj;
	}

	public static bool IsStaticPInvokeCSFunction(lua_CSFunction csFunction)
	{
		if (csFunction.Method.IsStatic)
		{
			return Attribute.IsDefined(csFunction.Method, typeof(MonoPInvokeCallbackAttribute));
		}
		return false;
	}

	public static bool IsPublic(Type type)
	{
		if (type.IsNested)
		{
			if (!type.IsNestedPublic())
			{
				return false;
			}
			return IsPublic(type.DeclaringType);
		}
		if (type.IsGenericType())
		{
			Type[] genericArguments = type.GetGenericArguments();
			for (int i = 0; i < genericArguments.Length; i++)
			{
				if (!IsPublic(genericArguments[i]))
				{
					return false;
				}
			}
		}
		return type.IsPublic();
	}
}
