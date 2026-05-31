using System;
using System.Collections.Generic;
using System.Reflection;
using XLua.LuaDLL;

namespace XLua;

public class OverloadMethodWrap
{
	private ObjectTranslator translator;

	private Type targetType;

	private MethodBase method;

	private ObjectCheck[] checkArray;

	private ObjectCast[] castArray;

	private int[] inPosArray;

	private int[] outPosArray;

	private bool[] isOptionalArray;

	private object[] defaultValueArray;

	private bool isVoid = true;

	private int luaStackPosStart = 1;

	private bool targetNeeded;

	private object[] args;

	private int[] refPos;

	private Type paramsType;

	public bool HasDefalutValue { get; private set; }

	public OverloadMethodWrap(ObjectTranslator translator, Type targetType, MethodBase method)
	{
		this.translator = translator;
		this.targetType = targetType;
		this.method = method;
		HasDefalutValue = false;
	}

	public void Init(ObjectCheckers objCheckers, ObjectCasters objCasters)
	{
		if ((typeof(Delegate) != targetType && typeof(Delegate).IsAssignableFrom(targetType)) || !method.IsStatic || method.IsConstructor)
		{
			luaStackPosStart = 2;
			if (!method.IsConstructor)
			{
				targetNeeded = true;
			}
		}
		ParameterInfo[] parameters = method.GetParameters();
		refPos = new int[parameters.Length];
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		List<ObjectCheck> list3 = new List<ObjectCheck>();
		List<ObjectCast> list4 = new List<ObjectCast>();
		List<bool> list5 = new List<bool>();
		List<object> list6 = new List<object>();
		for (int i = 0; i < parameters.Length; i++)
		{
			refPos[i] = -1;
			if (!parameters[i].IsIn && parameters[i].IsOut)
			{
				list2.Add(i);
				continue;
			}
			if (parameters[i].ParameterType.IsByRef)
			{
				Type elementType = parameters[i].ParameterType.GetElementType();
				if (CopyByValue.IsStruct(elementType) && elementType != typeof(decimal))
				{
					refPos[i] = list.Count;
				}
				list2.Add(i);
			}
			list.Add(i);
			Type type = ((parameters[i].IsDefined(typeof(ParamArrayAttribute), inherit: false) || (!parameters[i].ParameterType.IsArray && parameters[i].ParameterType.IsByRef)) ? parameters[i].ParameterType.GetElementType() : parameters[i].ParameterType);
			list3.Add(objCheckers.GetChecker(type));
			list4.Add(objCasters.GetCaster(type));
			list5.Add(parameters[i].IsOptional);
			object obj = parameters[i].DefaultValue;
			if (parameters[i].IsOptional)
			{
				if (obj != null && obj.GetType() != parameters[i].ParameterType)
				{
					obj = ((!(obj.GetType() == typeof(Missing))) ? Convert.ChangeType(obj, parameters[i].ParameterType) : (parameters[i].ParameterType.IsValueType() ? Activator.CreateInstance(parameters[i].ParameterType) : Missing.Value));
				}
				HasDefalutValue = true;
			}
			list6.Add(parameters[i].IsOptional ? obj : null);
		}
		checkArray = list3.ToArray();
		castArray = list4.ToArray();
		inPosArray = list.ToArray();
		outPosArray = list2.ToArray();
		isOptionalArray = list5.ToArray();
		defaultValueArray = list6.ToArray();
		if (parameters.Length != 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute), inherit: false))
		{
			paramsType = parameters[^1].ParameterType.GetElementType();
		}
		args = new object[parameters.Length];
		if (method is MethodInfo)
		{
			isVoid = (method as MethodInfo).ReturnType == typeof(void);
		}
		else if (method is ConstructorInfo)
		{
			isVoid = false;
		}
	}

	public bool Check(IntPtr L)
	{
		int num = Lua.lua_gettop(L);
		int num2 = luaStackPosStart;
		for (int i = 0; i < checkArray.Length && (i != checkArray.Length - 1 || !(paramsType != null)); i++)
		{
			if (num2 > num && !isOptionalArray[i])
			{
				return false;
			}
			if (num2 <= num && !checkArray[i](L, num2))
			{
				return false;
			}
			if (num2 <= num || !isOptionalArray[i])
			{
				num2++;
			}
		}
		if (!(paramsType != null))
		{
			return num2 == num + 1;
		}
		if (num2 >= num + 1)
		{
			return true;
		}
		return checkArray[checkArray.Length - 1](L, num2);
	}

	public int Call(IntPtr L)
	{
		try
		{
			object obj = null;
			MethodBase methodBase = method;
			if (luaStackPosStart > 1)
			{
				obj = translator.FastGetCSObj(L, 1);
				if (obj is Delegate)
				{
					methodBase = ((Delegate)obj).Method;
				}
			}
			int num = Lua.lua_gettop(L);
			int num2 = luaStackPosStart;
			for (int i = 0; i < castArray.Length; i++)
			{
				if (num2 > num)
				{
					if (paramsType != null && i == castArray.Length - 1)
					{
						args[inPosArray[i]] = Array.CreateInstance(paramsType, 0);
					}
					else
					{
						args[inPosArray[i]] = defaultValueArray[i];
					}
					continue;
				}
				if (paramsType != null && i == castArray.Length - 1)
				{
					args[inPosArray[i]] = translator.GetParams(L, num2, paramsType);
				}
				else
				{
					args[inPosArray[i]] = castArray[i](L, num2, null);
				}
				num2++;
			}
			object obj2 = null;
			obj2 = (methodBase.IsConstructor ? ((ConstructorInfo)method).Invoke(args) : method.Invoke(targetNeeded ? obj : null, args));
			if (targetNeeded && targetType.IsValueType())
			{
				translator.Update(L, 1, obj);
			}
			int num3 = 0;
			if (!isVoid)
			{
				translator.PushAny(L, obj2);
				num3++;
			}
			for (int j = 0; j < outPosArray.Length; j++)
			{
				if (refPos[outPosArray[j]] != -1)
				{
					translator.Update(L, luaStackPosStart + refPos[outPosArray[j]], args[outPosArray[j]]);
				}
				translator.PushAny(L, args[outPosArray[j]]);
				num3++;
			}
			return num3;
		}
		finally
		{
			for (int k = 0; k < args.Length; k++)
			{
				args[k] = null;
			}
		}
	}
}
