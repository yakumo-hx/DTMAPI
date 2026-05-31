using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

internal static class CSharpUtils
{
	public static bool IsNullable(this Type type)
	{
		return !type.IsValueType;
	}

	public static MemberInfo GetDefaultMember(this Type type, string memberName)
	{
		return type.GetMember(memberName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
	}

	public static MethodInfo GetGetterMethod<TKey>(this Type type)
	{
		return type.GetGetterMethod(typeof(TKey));
	}

	public static MethodInfo GetGetterMethod(this Type type, Type keyType)
	{
		MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in methods)
		{
			if (!(methodInfo.Name != "get_Item"))
			{
				ParameterInfo[] parameters = methodInfo.GetParameters();
				if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(keyType))
				{
					return methodInfo;
				}
			}
		}
		return null;
	}

	public static MethodInfo GetSetterMethod<TKey>(this Type type)
	{
		return type.GetSetterMethod(typeof(TKey));
	}

	public static MethodInfo GetSetterMethod(this Type type, Type keyType)
	{
		MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in methods)
		{
			if (!(methodInfo.Name != "set_Item"))
			{
				ParameterInfo[] parameters = methodInfo.GetParameters();
				if (parameters.Length == 2 && parameters[0].ParameterType.IsAssignableFrom(keyType))
				{
					return methodInfo;
				}
			}
		}
		return null;
	}

	public static string GetMemberTypeName(this MemberInfo memberInfo)
	{
		return memberInfo.MemberType switch
		{
			MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType.Name, 
			MemberTypes.Field => ((FieldInfo)memberInfo).FieldType.Name, 
			MemberTypes.Method => ((MethodInfo)memberInfo).GetMethodInfo(), 
			_ => string.Empty, 
		};
	}

	public static Delegate CreateCommandDelegate(this MethodInfo methodInfo)
	{
		ParameterInfo[] parameters = methodInfo.GetParameters();
		if (methodInfo.ReturnType == typeof(void))
		{
			if (parameters.Length == 0)
			{
				return Delegate.CreateDelegate(typeof(Action), methodInfo);
			}
			return Delegate.CreateDelegate(Expression.GetActionType(parameters.Select((ParameterInfo p) => p.ParameterType).ToArray()), methodInfo);
		}
		return Delegate.CreateDelegate(Expression.GetDelegateType(parameters.Select((ParameterInfo p) => p.ParameterType).Append(methodInfo.ReturnType).ToArray()), methodInfo);
	}

	public static Delegate CreateCommandDelegate(this MethodInfo methodInfo, object instance)
	{
		ParameterInfo[] parameters = methodInfo.GetParameters();
		if (methodInfo.ReturnType == typeof(void))
		{
			if (parameters.Length == 0)
			{
				return Delegate.CreateDelegate(typeof(Action), instance, methodInfo);
			}
			return Delegate.CreateDelegate(Expression.GetActionType(parameters.Select((ParameterInfo p) => p.ParameterType).ToArray()), instance, methodInfo);
		}
		return Delegate.CreateDelegate(Expression.GetDelegateType(parameters.Select((ParameterInfo p) => p.ParameterType).Append(methodInfo.ReturnType).ToArray()), instance, methodInfo);
	}

	public static string GetMethodInfo(this MethodInfo methodInfo)
	{
		ParameterInfo[] parameters = methodInfo.GetParameters();
		if (parameters.Length == 0)
		{
			return "()";
		}
		return "(" + string.Join(", ", parameters.Select((ParameterInfo p) => p.ParameterType.Name)) + ") -> " + methodInfo.ReturnType.Name;
	}

	public static bool IsStatic(this PropertyInfo propertyInfo)
	{
		return propertyInfo.GetMethod?.IsStatic ?? propertyInfo.SetMethod?.IsStatic ?? false;
	}

	public static StackProperty CreateStackProperty(this PropertyInfo propertyInfo, string name)
	{
		if (!propertyInfo.IsStatic())
		{
			return null;
		}
		if (propertyInfo.GetIndexParameters().Length != 0)
		{
			return null;
		}
		if (propertyInfo.SetMethod == null)
		{
			if (propertyInfo.GetMethod == null)
			{
				return null;
			}
			return new StackPropertyPropertyOnlyGetter(name, null, propertyInfo);
		}
		if (propertyInfo.GetMethod == null)
		{
			return new StackPropertyPropertyOnlySetter(name, null, propertyInfo);
		}
		return new StackPropertyProperty(name, null, propertyInfo);
	}

	public static StackProperty CreateStackProperty(this PropertyInfo propertyInfo, object instance, string name)
	{
		if (instance == null)
		{
			return propertyInfo.CreateStackProperty(name);
		}
		if (propertyInfo.GetIndexParameters().Length != 0)
		{
			return null;
		}
		if (propertyInfo.SetMethod == null)
		{
			if (propertyInfo.GetMethod == null)
			{
				return null;
			}
			return new StackPropertyPropertyOnlyGetter(name, instance, propertyInfo);
		}
		if (propertyInfo.GetMethod == null)
		{
			return new StackPropertyPropertyOnlySetter(name, instance, propertyInfo);
		}
		return new StackPropertyProperty(name, instance, propertyInfo);
	}

	public static StackProperty CreateStackProperty(this PropertyInfo propertyInfo, string name, string description, string tag)
	{
		if (!propertyInfo.IsStatic())
		{
			return null;
		}
		if (propertyInfo.GetIndexParameters().Length != 0)
		{
			return null;
		}
		if (propertyInfo.SetMethod == null)
		{
			if (propertyInfo.GetMethod == null)
			{
				return null;
			}
			return new StackPropertyPropertyOnlyGetter(name, description, tag, null, propertyInfo);
		}
		if (propertyInfo.GetMethod == null)
		{
			return new StackPropertyPropertyOnlySetter(name, description, tag, null, propertyInfo);
		}
		return new StackPropertyProperty(name, description, tag, null, propertyInfo);
	}

	public static StackProperty CreateStackProperty(this PropertyInfo propertyInfo, object instance, string name, string description, string tag)
	{
		if (instance == null)
		{
			return propertyInfo.CreateStackProperty(name, description, tag);
		}
		if (propertyInfo.GetIndexParameters().Length != 0)
		{
			return null;
		}
		if (propertyInfo.SetMethod == null)
		{
			if (propertyInfo.GetMethod == null)
			{
				return null;
			}
			return new StackPropertyPropertyOnlyGetter(name, description, tag, instance, propertyInfo);
		}
		if (propertyInfo.GetMethod == null)
		{
			return new StackPropertyPropertyOnlySetter(name, description, tag, instance, propertyInfo);
		}
		return new StackPropertyProperty(name, description, tag, instance, propertyInfo);
	}

	public static StackProperty CreateStackProperty(this FieldInfo fieldInfo, string name)
	{
		if (fieldInfo.IsStatic)
		{
			return new StackPropertyField(name, null, fieldInfo);
		}
		return null;
	}

	public static StackProperty CreateStackProperty(this FieldInfo fieldInfo, object instance, string name)
	{
		if (instance == null)
		{
			return fieldInfo.CreateStackProperty(name);
		}
		return new StackPropertyField(name, instance, fieldInfo);
	}

	public static StackProperty CreateStackProperty(this FieldInfo fieldInfo, string name, string description, string tag)
	{
		if (fieldInfo.IsStatic)
		{
			return new StackPropertyField(name, description, tag, null, fieldInfo);
		}
		return null;
	}

	public static StackProperty CreateStackProperty(this FieldInfo fieldInfo, object instance, string name, string description, string tag)
	{
		if (instance == null)
		{
			return fieldInfo.CreateStackProperty(name, description, tag);
		}
		return new StackPropertyField(name, description, tag, instance, fieldInfo);
	}

	public static StackCallable CreateStackCallable(this MethodInfo methodInfo)
	{
		if (methodInfo.IsStatic)
		{
			return new StackMethod(null, methodInfo);
		}
		return null;
	}

	public static StackCallable CreateStackCallable(this MethodInfo methodInfo, object instance)
	{
		if (instance == null)
		{
			return methodInfo.CreateStackCallable();
		}
		return new StackMethod(instance, methodInfo);
	}
}
