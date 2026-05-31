using System;

namespace DolocTown;

public abstract class InstantGoEffects : GameEntity
{
	public Action<InstantGoEffects> Recycle { get; set; }

	public abstract void Raise();

	public void RecycleSelf()
	{
		Recycle?.Invoke(this);
	}
}
