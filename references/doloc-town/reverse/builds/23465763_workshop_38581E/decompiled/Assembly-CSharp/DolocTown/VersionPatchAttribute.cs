using System;

namespace DolocTown;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class VersionPatchAttribute : Attribute
{
	public readonly string version;

	public string Description { get; set; }

	public VersionPatchAttribute(string version)
	{
		this.version = version;
	}
}
