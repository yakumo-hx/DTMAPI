using System;

namespace RedSaw.CommandLineInterface;

public abstract class StackProperty : StackObject, IValueGetter, IValueSetter
{
	public readonly string name;

	public readonly string description;

	public readonly string tag;

	public virtual string Name => name;

	public abstract Type ValueType { get; }

	public StackProperty(string name, string description, string tag)
		: base(StackObjectType.ValueProperty)
	{
		this.name = name;
		this.description = description;
		this.tag = tag;
	}

	public StackProperty(string name)
		: base(StackObjectType.ValueProperty)
	{
		this.name = name;
		description = string.Empty;
		tag = null;
	}

	public abstract object GetValue();

	public abstract void SetValue(object value);

	public bool CompareTag(string tag)
	{
		if (this.tag == null)
		{
			return true;
		}
		return this.tag == tag;
	}
}
