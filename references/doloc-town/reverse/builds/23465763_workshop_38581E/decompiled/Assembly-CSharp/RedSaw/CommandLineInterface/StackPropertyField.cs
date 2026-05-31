using System;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

public class StackPropertyField : StackProperty
{
	public readonly FieldInfo fieldInfo;

	public readonly object instance;

	public override Type ValueType => fieldInfo.FieldType;

	public StackPropertyField(string name, string description, string tag, object instance, FieldInfo fieldInfo)
		: base(name, description, tag)
	{
		this.instance = instance;
		this.fieldInfo = fieldInfo;
	}

	public StackPropertyField(string name, object instance, FieldInfo fieldInfo)
		: base(name)
	{
		this.instance = instance;
		this.fieldInfo = fieldInfo;
	}

	public override object GetValue()
	{
		return fieldInfo.GetValue(instance);
	}

	public override void SetValue(object value)
	{
		fieldInfo.SetValue(instance, value);
	}
}
