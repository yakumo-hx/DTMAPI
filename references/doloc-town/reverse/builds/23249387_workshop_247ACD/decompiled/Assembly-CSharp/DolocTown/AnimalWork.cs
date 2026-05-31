using System;
using RedSaw.AI.LinearTask;

namespace DolocTown;

[Serializable]
public abstract class AnimalWork
{
	public abstract string Title { get; }

	public virtual bool isGroup => false;

	public abstract bool GenTask(Animal animal, out LinearTask task);
}
