using System;
using System.Collections;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

public static class VirtualMachineUtils
{
	public static object GetElement(object arrlike, int index)
	{
		Type type = arrlike.GetType();
		if (type.IsSZArray)
		{
			if (!(arrlike is Array array))
			{
				throw new CommandExecuteException("cannot index null object");
			}
			if (index < 0 || index >= array.Length)
			{
				throw new CommandExecuteException($"index out of range: {index}");
			}
			return array.GetValue(index);
		}
		if (arrlike is IList list)
		{
			if (index < 0 || index >= list.Count)
			{
				throw new CommandExecuteException($"index out of range: {index}");
			}
			return list[index];
		}
		MethodInfo getterMethod = type.GetGetterMethod<int>();
		if (getterMethod != null)
		{
			return getterMethod.Invoke(arrlike, new object[1] { index });
		}
		throw new CommandExecuteException($"cannot index object of type {type}");
	}

	public static object GetElement(object dictlike, object key)
	{
		if (key == null)
		{
			throw new CommandExecuteException("cannot index object by null");
		}
		Type type = dictlike.GetType();
		if (dictlike is IDictionary dictionary && type.IsGenericType)
		{
			if (type.GenericTypeArguments[0].IsAssignableFrom(key.GetType()))
			{
				return dictionary[key];
			}
			throw new CommandExecuteException($"cannot index object of type {type} by {key.GetType()}");
		}
		MethodInfo getterMethod = type.GetGetterMethod(key.GetType());
		if (getterMethod != null)
		{
			return getterMethod.Invoke(dictlike, new object[1] { key });
		}
		throw new CommandExecuteException($"cannot index object of type {type}");
	}

	public static void SetElement(object arrlike, int index, object value)
	{
		Type type = arrlike.GetType();
		if (type.IsSZArray)
		{
			if (!(arrlike is Array array))
			{
				throw new CommandExecuteException($"cannot set element of \"{type}\" object");
			}
			if (index < 0 || index >= array.Length)
			{
				throw new CommandExecuteException($"index out of range: {index}");
			}
			Type type2 = type.GetElementType() ?? throw new CommandExecuteException("invalid element type");
			if (value == null)
			{
				if (!type2.IsNullable())
				{
					throw new CommandExecuteException($"cannot set null to element of \"{type}\" object");
				}
				array.SetValue(null, index);
			}
			else
			{
				array.SetValue(value, index);
			}
			return;
		}
		if (arrlike is IList list)
		{
			if (index < 0 || index >= list.Count)
			{
				throw new CommandExecuteException($"index out of range: {index}");
			}
			if (type.IsGenericType)
			{
				Type type3 = type.GenericTypeArguments[0];
				if (value == null)
				{
					if (type3.IsNullable())
					{
						list[index] = null;
						return;
					}
					throw new CommandExecuteException($"cannot set null to element of \"{type}\" object");
				}
				Type type4 = value.GetType();
				if (type3.IsAssignableFrom(type4))
				{
					list[index] = value;
					return;
				}
				throw new CommandExecuteException($"cannot set \"{type4}\" to element of \"{type}\" object");
			}
			list[index] = value;
		}
		MethodInfo getterMethod = type.GetGetterMethod<int>();
		if (getterMethod != null)
		{
			ParameterInfo[] parameters = getterMethod.GetParameters();
			if (value == null)
			{
				if (parameters[1].ParameterType.IsNullable())
				{
					getterMethod.Invoke(arrlike, new object[2] { index, null });
					return;
				}
				throw new CommandExecuteException($"cannot set null to element of \"{type}\" object");
			}
			Type type5 = value.GetType();
			if (parameters[1].ParameterType.IsAssignableFrom(type5))
			{
				getterMethod.Invoke(arrlike, new object[2] { index, value });
				return;
			}
			throw new CommandExecuteException($"cannot set \"{type5}\" to element of \"{type}\" object");
		}
		throw new CommandExecuteException($"cannot set element of \"{type}\" object");
	}

	public static void SetElement(object dictlike, object key, object value)
	{
		Type type = dictlike.GetType();
		Type type2 = key.GetType();
		if (type is IDictionary dictionary && type.IsGenericType)
		{
			if (!type.GenericTypeArguments[0].IsAssignableFrom(type2))
			{
				throw new CommandExecuteException($"\"{type2}\" cannot be index of \"{type}\" object");
			}
			Type type3 = type.GenericTypeArguments[1];
			if (value == null)
			{
				if (!type3.IsNullable())
				{
					throw new CommandExecuteException($"cannot set null to element of \"{type}\" object");
				}
				dictionary[key] = null;
			}
			else
			{
				if (!type3.IsAssignableFrom(value.GetType()))
				{
					throw new CommandExecuteException($"cannot set \"{value.GetType()}\" to element of \"{type}\" object");
				}
				dictionary[key] = value;
			}
			return;
		}
		MethodInfo setterMethod = type.GetSetterMethod(type2);
		if (setterMethod != null)
		{
			ParameterInfo[] parameters = setterMethod.GetParameters();
			if (value == null)
			{
				if (!parameters[1].ParameterType.IsNullable())
				{
					throw new CommandExecuteException($"cannot set null to element of \"{type}\" object");
				}
				setterMethod.Invoke(dictlike, new object[2] { key, null });
			}
			else
			{
				if (!parameters[1].ParameterType.IsAssignableFrom(value.GetType()))
				{
					throw new CommandExecuteException($"cannot set \"{value.GetType()}\" to element of \"{type}\" object");
				}
				setterMethod.Invoke(dictlike, new object[2] { key, value });
			}
			return;
		}
		throw new CommandExecuteException($"cannot set element of \"{type}\" object");
	}

	public static Type GetElementTypeOfArray(object instance)
	{
		Type type = instance.GetType();
		if (type.IsSZArray)
		{
			return type.GetElementType();
		}
		if (instance is IList && type.IsGenericType)
		{
			return type.GenericTypeArguments[0];
		}
		MethodInfo setterMethod = type.GetSetterMethod<int>();
		if (setterMethod != null)
		{
			return setterMethod.GetParameters()[1].ParameterType;
		}
		return null;
	}

	public static Type GetElementTypeOfDict(object instance, Type indexType)
	{
		Type type = instance.GetType();
		if (instance is IDictionary && type.IsGenericType && type.GenericTypeArguments[0].IsAssignableFrom(indexType))
		{
			return type.GenericTypeArguments[1];
		}
		MethodInfo setterMethod = type.GetSetterMethod(indexType);
		if (setterMethod != null)
		{
			return setterMethod.GetParameters()[1].ParameterType;
		}
		return null;
	}
}
