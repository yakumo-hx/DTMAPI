using System.Reflection;

namespace RedSaw.CommandLineInterface;

public class DebugButton
{
	public readonly object instance;

	public readonly MethodInfo methodInfo;

	public readonly string title;

	public readonly string groupId;

	public readonly DebugButtonType buttonType;

	public bool couldInvokeWithNoParameters;

	public DebugButton(object instance, MethodInfo methodInfo, string title, string groupId, DebugButtonType buttonType = DebugButtonType.Normal)
	{
		this.instance = instance;
		this.methodInfo = methodInfo;
		this.title = title;
		this.groupId = groupId;
		this.buttonType = buttonType;
		couldInvokeWithNoParameters = CanInvokeWithNoParameters(methodInfo);
	}

	public bool CanInvokeWithNoParameters(MethodInfo methodInfo)
	{
		if (methodInfo == null)
		{
			return false;
		}
		ParameterInfo[] parameters = methodInfo.GetParameters();
		if (parameters.Length == 0)
		{
			return true;
		}
		ParameterInfo[] array = parameters;
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].HasDefaultValue)
			{
				return false;
			}
		}
		return true;
	}

	public void Invoke()
	{
		if (!(methodInfo == null) && couldInvokeWithNoParameters)
		{
			methodInfo.Invoke(methodInfo.IsStatic ? null : instance, null);
		}
	}
}
