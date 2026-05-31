using System;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

public class StackPropertyPropertyOnlyGetter : StackPropertyProperty
{
	public override Type ValueType => propertyInfo.PropertyType;

	public StackPropertyPropertyOnlyGetter(string name, object instance, PropertyInfo propertyInfo)
		: base(name, instance, propertyInfo)
	{
	}

	public StackPropertyPropertyOnlyGetter(string name, string description, string tag, object instance, PropertyInfo propertyInfo)
		: base(name, description, tag, instance, propertyInfo)
	{
	}

	public override object GetValue()
	{
		return propertyInfo.GetValue(instance);
	}

	public override void SetValue(object value)
	{
		throw new CommandExecuteException($"property \"{propertyInfo.Name} ({propertyInfo.PropertyType})\" has no setter");
	}
}
