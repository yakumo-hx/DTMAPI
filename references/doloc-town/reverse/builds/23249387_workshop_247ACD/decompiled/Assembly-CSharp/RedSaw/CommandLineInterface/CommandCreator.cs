using System;
using System.Collections.Generic;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

internal static class CommandCreator
{
	public static IEnumerable<Command> CollectCommands<T>() where T : CommandAttribute
	{
		Type[] types = Assembly.GetExecutingAssembly().GetTypes();
		foreach (Type type in types)
		{
			MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				T customAttribute = methodInfo.GetCustomAttribute<T>();
				if (customAttribute != null)
				{
					yield return new Command(customAttribute.Name ?? methodInfo.Name, customAttribute.Desc, customAttribute.Tag, methodInfo);
				}
			}
		}
	}

	public static IEnumerable<StackProperty> CollectProperties<T>() where T : CommandPropertyAttribute
	{
		Type[] types = Assembly.GetExecutingAssembly().GetTypes();
		foreach (Type type in types)
		{
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (PropertyInfo propertyInfo in properties)
			{
				T customAttribute = propertyInfo.GetCustomAttribute<T>();
				if (customAttribute != null)
				{
					StackProperty stackProperty = propertyInfo.CreateStackProperty(customAttribute.Name ?? propertyInfo.Name, customAttribute.Desc, customAttribute.Tag);
					if (stackProperty != null)
					{
						yield return stackProperty;
					}
				}
			}
			FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in fields)
			{
				T customAttribute2 = fieldInfo.GetCustomAttribute<T>();
				if (customAttribute2 != null)
				{
					StackProperty stackProperty2 = fieldInfo.CreateStackProperty(customAttribute2.Name ?? fieldInfo.Name, customAttribute2.Desc, customAttribute2.Tag);
					if (stackProperty2 != null)
					{
						yield return stackProperty2;
					}
				}
			}
		}
	}

	public static IEnumerable<(ValueParser, Type, string)> CollectValueParsers<T>() where T : CommandValueParserAttribute
	{
		Type[] types = Assembly.GetExecutingAssembly().GetTypes();
		foreach (Type type in types)
		{
			MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				T customAttribute = methodInfo.GetCustomAttribute<T>();
				if (customAttribute != null && TryConvertDelegate<ValueParser>(methodInfo, out var result))
				{
					yield return (result, customAttribute.type, customAttribute.Alias);
				}
			}
		}
	}

	private static bool TryConvertDelegate<T>(MethodInfo methodInfo, out T result) where T : Delegate
	{
		result = null;
		MethodInfo method = typeof(T).GetMethod("JumpTo");
		if (methodInfo.ReturnType != method.ReturnType)
		{
			return false;
		}
		ParameterInfo[] parameters = methodInfo.GetParameters();
		ParameterInfo[] parameters2 = method.GetParameters();
		if (parameters.Length != parameters2.Length)
		{
			return false;
		}
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].ParameterType != parameters2[i].ParameterType)
			{
				return false;
			}
		}
		result = Delegate.CreateDelegate(typeof(T), null, methodInfo) as T;
		return true;
	}
}
