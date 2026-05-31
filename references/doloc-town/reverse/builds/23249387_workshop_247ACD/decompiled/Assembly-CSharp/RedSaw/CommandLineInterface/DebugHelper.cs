using System;
using System.Collections.Generic;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

public static class DebugHelper
{
	private static Type[] GetParentTypes(Type type)
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

	public static (string, string)[] GetDebugInfos(this object instance, int depth = 0, int depthLimit = 5, string constraintNamespace = null, string objTitle = null)
	{
		if (depth >= depthLimit || instance == null)
		{
			return Array.Empty<(string, string)>();
		}
		HashSet<string> usedKeys = new HashSet<string>();
		Type type = instance.GetType();
		Type[] parentTypes = GetParentTypes(type);
		List<(string, string)> list = new List<(string, string)>();
		Type[] array = parentTypes;
		foreach (Type type2 in array)
		{
			if (constraintNamespace == null || type2.Namespace.StartsWith(constraintNamespace))
			{
				list.AddRange(GetDebugInfos(type2, instance, usedKeys, depth, constraintNamespace, objTitle));
			}
		}
		list.AddRange(GetDebugInfos(type, instance, usedKeys, depth, constraintNamespace, objTitle));
		return list.ToArray();
	}

	private static List<(string, string)> GetDebugInfos(Type type, object instance, HashSet<string> usedKeys, int depth, string constraintNamespace = null, string objTitle = null)
	{
		if (type == null)
		{
			return new List<(string, string)>();
		}
		List<(string, string)> list = new List<(string, string)>();
		string text = objTitle ?? type.Name;
		string text2 = new string(' ', (depth + 1) * 4);
		bool flag = false;
		FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			DebugInfoAttribute customAttribute = fieldInfo.GetCustomAttribute<DebugInfoAttribute>();
			if (customAttribute == null)
			{
				continue;
			}
			flag = true;
			string text3 = ((customAttribute.Key == string.Empty) ? fieldInfo.Name : customAttribute.Key);
			if (!usedKeys.Add(text3))
			{
				continue;
			}
			DebugObjectAttribute customAttribute2 = fieldInfo.FieldType.GetCustomAttribute<DebugObjectAttribute>();
			object value = fieldInfo.GetValue(instance);
			if (customAttribute2 != null)
			{
				if (value != null)
				{
					list.AddRange(value.GetDebugInfos(depth + 1, 5, constraintNamespace, customAttribute.Key));
					continue;
				}
				list.Add(("> " + text2 + text + "." + text3 + ": null", customAttribute.Color));
			}
			else
			{
				string text4 = value?.ToString() ?? "null";
				list.Add(("> " + text2 + text + "." + text3 + ": " + text4, customAttribute.Color));
			}
		}
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			DebugInfoAttribute customAttribute3 = propertyInfo.GetCustomAttribute<DebugInfoAttribute>();
			if (customAttribute3 == null)
			{
				continue;
			}
			flag = true;
			string text5 = ((customAttribute3.Key == string.Empty) ? propertyInfo.Name : customAttribute3.Key);
			if (!usedKeys.Add(text5))
			{
				continue;
			}
			DebugObjectAttribute customAttribute4 = propertyInfo.PropertyType.GetCustomAttribute<DebugObjectAttribute>();
			object value2 = propertyInfo.GetValue(instance);
			if (customAttribute4 != null)
			{
				if (value2 != null)
				{
					list.AddRange(value2.GetDebugInfos(depth + 1, 5, constraintNamespace, customAttribute3.Key));
					continue;
				}
				list.Add(("> " + text2 + text + "." + text5 + ": null", customAttribute3.Color));
			}
			else
			{
				string text6 = value2?.ToString() ?? "null";
				list.Add(("> " + text2 + text + "." + text5 + ": " + text6, customAttribute3.Color));
			}
		}
		if (flag)
		{
			string text7 = new string('-', depth * 4);
			(string, string) item = ("> " + text7 + "[" + text + "]", string.Empty);
			list.Insert(0, item);
		}
		return list;
	}

	public static Dictionary<string, MethodInfo> GetDebugButtons(Type instanceType, string constraintNamespace = null)
	{
		Dictionary<string, MethodInfo> debugButtonsOfType = GetDebugButtonsOfType(instanceType);
		Type[] parentTypes = GetParentTypes(instanceType);
		foreach (Type type in parentTypes)
		{
			if ((object)type == null || (constraintNamespace != null && type.Namespace != null && !type.Namespace.StartsWith(constraintNamespace)))
			{
				continue;
			}
			foreach (KeyValuePair<string, MethodInfo> item in GetDebugButtonsOfType(type))
			{
				debugButtonsOfType.TryAdd(item.Key, item.Value);
			}
		}
		return debugButtonsOfType;
	}

	public static Dictionary<string, MethodInfo> GetDebugButtons<T>(string constraintNamespace = null)
	{
		return GetDebugButtons(typeof(T), constraintNamespace);
	}

	private static Dictionary<string, MethodInfo> GetDebugButtonsOfType(Type type)
	{
		Dictionary<string, MethodInfo> dictionary = new Dictionary<string, MethodInfo>();
		MethodInfo[] methods = type.GetMethods();
		foreach (MethodInfo methodInfo in methods)
		{
			if (methodInfo.GetParameters().Length == 0)
			{
				DebugButtonAttribute customAttribute = methodInfo.GetCustomAttribute<DebugButtonAttribute>();
				if (customAttribute != null)
				{
					string key = customAttribute.title ?? methodInfo.Name;
					dictionary.TryAdd(key, methodInfo);
				}
			}
		}
		return dictionary;
	}
}
