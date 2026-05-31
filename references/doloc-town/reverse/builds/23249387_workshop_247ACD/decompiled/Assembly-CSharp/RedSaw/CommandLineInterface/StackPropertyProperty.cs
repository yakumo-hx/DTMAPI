using System;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

public class StackPropertyProperty : StackProperty
{
	public readonly PropertyInfo propertyInfo;

	public readonly object instance;

	public override Type ValueType => propertyInfo.PropertyType;

	public StackPropertyProperty(string name, string description, string tag, object instance, PropertyInfo propertyInfo)
		: base(name, description, tag)
	{
		this.instance = instance;
		this.propertyInfo = propertyInfo;
	}

	public StackPropertyProperty(string name, object instance, PropertyInfo propertyInfo)
		: base(name)
	{
		this.instance = instance;
		this.propertyInfo = propertyInfo;
	}

	public override object GetValue()
	{
		return propertyInfo.GetValue(instance);
	}

	public override void SetValue(object value)
	{
		propertyInfo.SetValue(instance, value);
	}
}
