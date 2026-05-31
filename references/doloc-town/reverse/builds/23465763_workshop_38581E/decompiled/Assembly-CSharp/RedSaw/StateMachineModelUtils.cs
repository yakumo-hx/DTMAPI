using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RedSaw;

public static class StateMachineModelUtils
{
	private static readonly Type[] allActionTaskTypes = (from t in Assembly.GetExecutingAssembly().GetTypes()
		where typeof(ActionTask).IsAssignableFrom(t) && !t.IsAbstract
		select t).ToArray();

	private static readonly Dictionary<string, Type[]> models = new Dictionary<string, Type[]>();

	public static StateMachine CreateStateMachine(IBlackboard blackboard, string modelName)
	{
		if (models.TryGetValue(modelName, out var value))
		{
			return new StateMachine(blackboard, value);
		}
		List<Type> list = new List<Type>();
		Type[] array = allActionTaskTypes;
		foreach (Type type in array)
		{
			StateMachineModelAttribute customAttribute = type.GetCustomAttribute<StateMachineModelAttribute>();
			if (customAttribute != null && customAttribute.modelName == modelName)
			{
				list.Add(type);
			}
		}
		Type[] array2 = list.ToArray();
		models.Add(modelName, array2);
		return new StateMachine(blackboard, array2);
	}
}
