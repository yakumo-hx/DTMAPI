using System;
using System.Linq;

namespace XLua;

internal static class TypeExtensions
{
	public static bool IsValueType(this Type type)
	{
		return type.IsValueType;
	}

	public static bool IsEnum(this Type type)
	{
		return type.IsEnum;
	}

	public static bool IsPrimitive(this Type type)
	{
		return type.IsPrimitive;
	}

	public static bool IsAbstract(this Type type)
	{
		return type.IsAbstract;
	}

	public static bool IsSealed(this Type type)
	{
		return type.IsSealed;
	}

	public static bool IsInterface(this Type type)
	{
		return type.IsInterface;
	}

	public static bool IsClass(this Type type)
	{
		return type.IsClass;
	}

	public static Type BaseType(this Type type)
	{
		return type.BaseType;
	}

	public static bool IsGenericType(this Type type)
	{
		return type.IsGenericType;
	}

	public static bool IsGenericTypeDefinition(this Type type)
	{
		return type.IsGenericTypeDefinition;
	}

	public static bool IsNestedPublic(this Type type)
	{
		return type.IsNestedPublic;
	}

	public static bool IsPublic(this Type type)
	{
		return type.IsPublic;
	}

	public static string GetFriendlyName(this Type type)
	{
		if (type == typeof(int))
		{
			return "int";
		}
		if (type == typeof(short))
		{
			return "short";
		}
		if (type == typeof(byte))
		{
			return "byte";
		}
		if (type == typeof(bool))
		{
			return "bool";
		}
		if (type == typeof(long))
		{
			return "long";
		}
		if (type == typeof(float))
		{
			return "float";
		}
		if (type == typeof(double))
		{
			return "double";
		}
		if (type == typeof(decimal))
		{
			return "decimal";
		}
		if (type == typeof(string))
		{
			return "string";
		}
		if (type.IsGenericType())
		{
			return type.FullName.Split('`')[0] + "<" + string.Join(", ", (from x in type.GetGenericArguments()
				select x.GetFriendlyName()).ToArray()) + ">";
		}
		return type.FullName;
	}
}
