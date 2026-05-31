using System;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

public abstract class StackCallable : StackObject
{
	public abstract string Name { get; }

	public abstract Type ReturnType { get; }

	public abstract int ParameterCount { get; }

	public bool HasReturnValue => ReturnType != typeof(void);

	public abstract object Instance { get; }

	public virtual Delegate GetDelegate()
	{
		return null;
	}

	public StackCallable()
		: base(StackObjectType.Callable)
	{
	}

	public abstract ParameterInfo GetParameter(int index);

	public abstract bool TryGetParameterDefaultValue(int index, out object defaultValue);

	public abstract object Invoke(object[] args);
}
