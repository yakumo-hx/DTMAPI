using System;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

public class StackPropertyPropertyOnlySetter : StackPropertyProperty
{
	public override Type ValueType => propertyInfo.PropertyType;

	public StackPropertyPropertyOnlySetter(string name, object instance, PropertyInfo propertyInfo)
		: base(name, instance, propertyInfo)
	{
	}

	public StackPropertyPropertyOnlySetter(string name, string description, string tag, object instance, PropertyInfo propertyInfo)
		: base(name, description, tag, instance, propertyInfo)
	{
	}

	public override object GetValue()
	{
		throw new CommandExecuteException($"property \"{propertyInfo.Name} ({propertyInfo.PropertyType})\" has no getter");
	}

	public override void SetValue(object value)
	{
		propertyInfo.SetValue(instance, value);
	}
}
