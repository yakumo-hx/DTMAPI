using System;

namespace DolocTown;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class WorkerRendererAttribute : Attribute
{
	public readonly string workerRendererName;

	public WorkerRendererAttribute(string workerRendererName)
	{
		this.workerRendererName = workerRendererName;
	}
}
