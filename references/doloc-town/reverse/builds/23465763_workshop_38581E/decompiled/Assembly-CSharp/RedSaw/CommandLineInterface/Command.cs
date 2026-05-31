using System.Reflection;

namespace RedSaw.CommandLineInterface;

public class Command : StackMethod
{
	public readonly string name;

	public readonly string description;

	public readonly string tag;

	public override string Name => name;

	public Command(string name, string description, string tag, MethodInfo method)
		: base(null, method)
	{
		this.name = name;
		this.description = description;
		this.tag = tag;
	}

	public Command(string name, string description, string tag, object instance, MethodInfo method)
		: base(instance, method)
	{
		this.name = name;
		this.description = description;
		this.tag = tag;
	}

	public bool CompareTag(string tag)
	{
		if (tag == null || tag.Length == 0)
		{
			return true;
		}
		return this.tag == tag;
	}

	public override string ToString()
	{
		return name + " : " + description;
	}
}
