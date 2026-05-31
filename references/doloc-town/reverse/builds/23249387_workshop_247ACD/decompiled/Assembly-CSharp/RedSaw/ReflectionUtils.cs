using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DolocTown;

namespace RedSaw;

public static class ReflectionUtils
{
	public static void SetEnumValueFromInt(this PropertyInfo propertyInfo, object instance, int value)
	{
		if (instance != null && propertyInfo.PropertyType.IsEnum && propertyInfo.CanWrite)
		{
			int num = Enum.GetValues(propertyInfo.PropertyType).Cast<int>().Max();
			if (value >= 0 && value <= num)
			{
				object value2 = Enum.ToObject(propertyInfo.PropertyType, value);
				propertyInfo.SetValue(instance, value2);
			}
		}
	}

	public static void SetEnumValueFromInt(this FieldInfo fieldInfo, object instance, int value)
	{
		if (instance != null && fieldInfo.FieldType.IsEnum)
		{
			int num = Enum.GetValues(fieldInfo.FieldType).Cast<int>().Max();
			if (value >= 0 && value <= num)
			{
				object value2 = Enum.ToObject(fieldInfo.FieldType, value);
				fieldInfo.SetValue(instance, value2);
			}
		}
	}

	public static bool IsFunctionOverride(this Type parent, Type childType, string name)
	{
		if (name.IsNullOrEmpty() || !parent.IsAssignableFrom(childType))
		{
			return false;
		}
		MethodInfo method = childType.GetMethod(name);
		if (method == null)
		{
			return false;
		}
		return method.DeclaringType != parent;
	}

	public static bool IsFunctionRefined<T1, T2>(string name)
	{
		return typeof(T1).IsFunctionOverride(typeof(T2), name);
	}

	public static bool WriteReadonlyField<T>(this T instance, string fieldName, object value)
	{
		FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (field == null)
		{
			return false;
		}
		field.SetValue(instance, value);
		return true;
	}

	public static MemberInfo[] GetMemberInfosWithAttr<T>(Type type) where T : Attribute
	{
		if (type == null)
		{
			return Array.Empty<MemberInfo>();
		}
		Type attribute = typeof(T);
		return (from member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			where Attribute.IsDefined(member, attribute)
			select member).ToArray();
	}

	public static FieldInfo[] GetFieldInfosWithAttr<T>(Type type) where T : Attribute
	{
		if (type == null)
		{
			return Array.Empty<FieldInfo>();
		}
		Type attribute = typeof(T);
		return (from field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			where Attribute.IsDefined(field, attribute)
			select field).ToArray();
	}

	public static PropertyInfo[] GetPropertyInfosWithAttr<T>(Type type) where T : Attribute
	{
		if (type == null)
		{
			return Array.Empty<PropertyInfo>();
		}
		Type attribute = typeof(T);
		return (from property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			where Attribute.IsDefined(property, attribute)
			select property).ToArray();
	}

	public static MethodInfo[] GetMethodInfosWithAttr<T>(Type type) where T : Attribute
	{
		if (type == null)
		{
			return Array.Empty<MethodInfo>();
		}
		Type attribute = typeof(T);
		return (from method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			where Attribute.IsDefined(method, attribute)
			select method).ToArray();
	}

	public static MethodInfo[] FindMethodsWithAttribute<T>(this Assembly asm, BindingFlags flags, string namespacePrefix = null) where T : Attribute
	{
		List<MethodInfo> list = new List<MethodInfo>();
		Type typeFromHandle = typeof(T);
		Type[] types = asm.GetTypes();
		if (namespacePrefix != null)
		{
			types = types.Where((Type x) => x.Namespace == null || x.Namespace.StartsWith(namespacePrefix)).ToArray();
		}
		Type[] types2 = asm.GetTypes();
		for (int i = 0; i < types2.Length; i++)
		{
			MethodInfo[] methods = types2[i].GetMethods(flags);
			foreach (MethodInfo methodInfo in methods)
			{
				if (Attribute.IsDefined(methodInfo, typeFromHandle))
				{
					list.Add(methodInfo);
				}
			}
		}
		return list.ToArray();
	}

	public static MethodInfo[] FindMethodsWithAttributeInExecutingAsm<T>(BindingFlags flags, string namespaceFilter = null) where T : Attribute
	{
		return Assembly.GetExecutingAssembly().FindMethodsWithAttribute<T>(flags, namespaceFilter);
	}

	public static Type[] FindTypesWithAttribute<T>(this Assembly asm, BindingFlags flags, string namespacePrefix = null) where T : Attribute
	{
		Type attribute = typeof(T);
		Type[] source = asm.GetTypes();
		if (namespacePrefix != null)
		{
			source = source.Where((Type x) => x.Namespace == null || x.Namespace.StartsWith(namespacePrefix)).ToArray();
		}
		return source.Where((Type classType) => Attribute.IsDefined(classType, attribute)).ToArray();
	}

	public static Type[] FindTypesWithAttributeInExecutingAsm<T>(BindingFlags flags, string namespacePrefix = null) where T : Attribute
	{
		return Assembly.GetExecutingAssembly().FindTypesWithAttribute<T>(flags, namespacePrefix);
	}

	public static Type[] GetSubTypes(this Type type)
	{
		return (from x in type.Assembly.GetTypes()
			where !x.IsAbstract
			where !x.IsGenericTypeDefinition
			select x).Where(type.IsAssignableFrom).ToArray();
	}

	public static Type[] GetSubTypes(this Type type, string namespacePrefix)
	{
		return (from x in (from x in type.Assembly.GetTypes()
				where !x.IsAbstract
				where !x.IsGenericTypeDefinition
				select x).Where(type.IsAssignableFrom)
			where x.Namespace == null || x.Namespace.StartsWith(namespacePrefix)
			select x).ToArray();
	}

	public static Type[] GetSubTypes<T>()
	{
		return typeof(T).GetSubTypes();
	}

	public static Dictionary<string, Type> LoadIntoDict(Type[] types)
	{
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		foreach (Type type in types)
		{
			if (!dictionary.ContainsKey(type.Name))
			{
				dictionary.Add(type.Name, type);
			}
		}
		return dictionary;
	}

	public static Type[] GetParentTypes(this Type type)
	{
		Stack<Type> stack = new Stack<Type>();
		Type baseType = type.BaseType;
		while (baseType != null)
		{
			stack.Push(baseType);
			baseType = baseType.BaseType;
		}
		return stack.ToArray();
	}
}
