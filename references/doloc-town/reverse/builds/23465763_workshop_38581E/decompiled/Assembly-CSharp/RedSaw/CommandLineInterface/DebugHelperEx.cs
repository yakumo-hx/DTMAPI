using System;
using System.Collections.Generic;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

public static class DebugHelperEx
{
	public static Dictionary<string, DebugInfo[]> ClassifyToGroups(DebugInfo[] infos)
	{
		string empty = string.Empty;
		Dictionary<string, List<DebugInfo>> dictionary = new Dictionary<string, List<DebugInfo>>();
		foreach (DebugInfo debugInfo in infos)
		{
			string key = (string.IsNullOrEmpty(debugInfo.groupId) ? empty : debugInfo.groupId);
			if (!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, new List<DebugInfo>());
			}
			dictionary[key].Add(debugInfo);
		}
		Dictionary<string, DebugInfo[]> dictionary2 = new Dictionary<string, DebugInfo[]>();
		foreach (KeyValuePair<string, List<DebugInfo>> item in dictionary)
		{
			dictionary2[item.Key] = item.Value.ToArray();
		}
		return dictionary2;
	}

	public static Dictionary<string, DebugButton[]> ClassifyToGroups(DebugButton[] buttons)
	{
		string empty = string.Empty;
		Dictionary<string, List<DebugButton>> dictionary = new Dictionary<string, List<DebugButton>>();
		foreach (DebugButton debugButton in buttons)
		{
			string key = (string.IsNullOrEmpty(debugButton.groupId) ? empty : debugButton.groupId);
			if (!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, new List<DebugButton>());
			}
			dictionary[key].Add(debugButton);
		}
		Dictionary<string, DebugButton[]> dictionary2 = new Dictionary<string, DebugButton[]>();
		foreach (KeyValuePair<string, List<DebugButton>> item in dictionary)
		{
			dictionary2[item.Key] = item.Value.ToArray();
		}
		return dictionary2;
	}

	public static DebugButton[] GetDebugButtons(object instance)
	{
		if (instance == null)
		{
			return Array.Empty<DebugButton>();
		}
		MethodInfo[] methodInfosWithAttr = ReflectionUtils.GetMethodInfosWithAttr<DebugButtonAttribute>(instance.GetType());
		if (methodInfosWithAttr.Length == 0)
		{
			return Array.Empty<DebugButton>();
		}
		List<DebugButton> list = new List<DebugButton>();
		MethodInfo[] array = methodInfosWithAttr;
		foreach (MethodInfo methodInfo in array)
		{
			DebugButton item = CreateButtonFromMethodInfo(methodInfo.GetCustomAttribute<DebugButtonAttribute>(), instance, methodInfo);
			list.Add(item);
		}
		return list.ToArray();
	}

	private static DebugButton CreateButtonFromMethodInfo(DebugButtonAttribute attr, object instance, MethodInfo methodInfo)
	{
		string title = (string.IsNullOrEmpty(attr.title) ? methodInfo.Name : attr.title);
		DebugGroupAttribute customAttribute = methodInfo.GetCustomAttribute<DebugGroupAttribute>();
		string groupId = ((customAttribute == null) ? string.Empty : (string.IsNullOrEmpty(customAttribute.groupId) ? string.Empty : customAttribute.groupId));
		return new DebugButton(instance, methodInfo, title, groupId, attr.ButtonType);
	}

	public static DebugInfo[] GetDebugInfo(object instance, int depth = 0, int depthLimit = 4, string constraintNamespace = null)
	{
		if (instance == null || depth > depthLimit)
		{
			return Array.Empty<DebugInfo>();
		}
		Type type = instance.GetType();
		List<DebugInfo> list = new List<DebugInfo>();
		list.AddRange(GetDebugInfos(type, instance, depth, depthLimit, constraintNamespace));
		return list.ToArray();
	}

	private static DebugInfo[] GetDebugInfos(Type type, object instance, int depth, int depthLimit, string namespaceConstraint = null)
	{
		if (type == null)
		{
			return Array.Empty<DebugInfo>();
		}
		MemberInfo[] memberInfosWithAttr = ReflectionUtils.GetMemberInfosWithAttr<DebugInfoAttribute>(type);
		if (memberInfosWithAttr.Length == 0)
		{
			return Array.Empty<DebugInfo>();
		}
		List<DebugInfo> list = new List<DebugInfo>();
		MemberInfo[] array = memberInfosWithAttr;
		foreach (MemberInfo obj in array)
		{
			DebugInfoAttribute customAttribute = obj.GetCustomAttribute<DebugInfoAttribute>();
			if (obj is FieldInfo fieldInfo)
			{
				list.Add(GetDebugInfoFromField(customAttribute, fieldInfo, instance, depth, depthLimit, namespaceConstraint));
			}
			if (obj is PropertyInfo propertyInfo)
			{
				list.Add(GetDebugInfoFromProperty(customAttribute, propertyInfo, instance, depth, depthLimit, namespaceConstraint));
			}
		}
		return list.ToArray();
	}

	private static DebugInfo GetDebugInfoFromField(DebugInfoAttribute debugInfoAttribute, FieldInfo fieldInfo, object instance, int depth, int depthLimit, string namespaceConstraint)
	{
		DebugField field;
		if (fieldInfo.FieldType.GetCustomAttribute<DebugObjectAttribute>() != null)
		{
			object value = fieldInfo.GetValue(instance);
			field = new DebugFieldOfFieldInfo(instance, fieldInfo);
			if (value == null)
			{
				return CreateDebugInfo(field, debugInfoAttribute);
			}
			DebugInfo[] debugInfo = GetDebugInfo(value, depth + 1, depthLimit, namespaceConstraint);
			DebugButton[] debugButtons = GetDebugButtons(value);
			return CreateDebugObject(field, debugInfo, debugButtons, debugInfoAttribute);
		}
		field = new DebugFieldOfFieldInfo(instance, fieldInfo);
		return CreateDebugInfo(field, debugInfoAttribute);
	}

	private static DebugInfo GetDebugInfoFromProperty(DebugInfoAttribute debugInfoAttribute, PropertyInfo propertyInfo, object instance, int depth, int depthLimit, string namespaceConstraint)
	{
		DebugField field;
		if (propertyInfo.PropertyType.GetCustomAttribute<DebugObjectAttribute>() != null)
		{
			object value = propertyInfo.GetValue(instance);
			field = new DebugFieldOfPropertyInfo(instance, propertyInfo);
			if (value == null)
			{
				return CreateDebugInfo(field, debugInfoAttribute);
			}
			DebugInfo[] debugInfo = GetDebugInfo(value, depth + 1, depthLimit, namespaceConstraint);
			DebugButton[] debugButtons = GetDebugButtons(value);
			return CreateDebugObject(field, debugInfo, debugButtons, debugInfoAttribute);
		}
		field = new DebugFieldOfPropertyInfo(instance, propertyInfo);
		return CreateDebugInfo(field, debugInfoAttribute);
	}

	private static DebugInfo CreateDebugInfo(DebugField field, DebugInfoAttribute debugAttr)
	{
		string title = (string.IsNullOrEmpty(debugAttr.Key) ? field.Name : debugAttr.Key);
		string color = debugAttr.Color;
		string groupId = GetGroupId(field.memberInfo);
		return new DebugInfo(title, groupId, color, debugAttr.AllowEdit, field);
	}

	private static DebugObject CreateDebugObject(DebugField field, DebugInfo[] debugInfos, DebugButton[] debugButtons, DebugInfoAttribute debugAttr)
	{
		string title = (string.IsNullOrEmpty(debugAttr.Key) ? field.Name : debugAttr.Key);
		string color = debugAttr.Color;
		string groupId = GetGroupId(field.memberInfo);
		return new DebugObject(title, groupId, color, field, debugInfos);
	}

	private static string GetGroupId(MemberInfo memberInfo)
	{
		return memberInfo.GetCustomAttribute<DebugGroupAttribute>()?.groupId;
	}
}
