using System;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

public class StackMethod : StackCallable
{
	public readonly object instance;

	public readonly MethodInfo methodInfo;

	public readonly ParameterInfo[] parameters;

	private readonly Delegate methodDelegate;

	public override string Name => methodInfo.Name;

	public override int ParameterCount => parameters.Length;

	public override Type ReturnType => methodInfo.ReturnType;

	public override object Instance => methodInfo;

	public string ParameterInfo
	{
		get
		{
			if (parameters.Length == 0)
			{
				return "()";
			}
			string text = string.Empty;
			ParameterInfo[] array = parameters;
			foreach (ParameterInfo parameterInfo in array)
			{
				text = text + parameterInfo.ParameterType.Name + " " + parameterInfo.Name + ", ";
			}
			return "(" + text[..^2] + ")";
		}
	}

	public override Delegate GetDelegate()
	{
		return methodDelegate;
	}

	public StackMethod(object instance, MethodInfo methodInfo)
	{
		this.instance = instance;
		this.methodInfo = methodInfo;
		parameters = methodInfo.GetParameters();
		methodDelegate = methodInfo.CreateCommandDelegate(instance);
	}

	public StackMethod(MethodInfo methodInfo)
	{
		this.methodInfo = methodInfo;
		if (!methodInfo.IsStatic)
		{
			throw new CommandExecuteException($"require static method but get instance method \"{methodInfo.Name}\" in \"{methodInfo.DeclaringType}\"");
		}
		parameters = methodInfo.GetParameters();
		methodDelegate = methodInfo.CreateCommandDelegate();
	}

	public override ParameterInfo GetParameter(int idx)
	{
		return parameters[idx];
	}

	public override bool TryGetParameterDefaultValue(int index, out object defaultValue)
	{
		if (index < 0 || index >= parameters.Length || !parameters[index].HasDefaultValue)
		{
			defaultValue = null;
			return false;
		}
		defaultValue = parameters[index].DefaultValue;
		return true;
	}

	public override object Invoke(object[] args)
	{
		return methodInfo.Invoke(instance, args);
	}
}
