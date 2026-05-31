using RedSaw;

namespace DolocTown;

public class DolocRecyclableObject : DolocObject, IRecyclable
{
	public virtual void OnCreated()
	{
		SetVisible(value: true);
		Init();
	}

	public virtual void OnRecycle()
	{
		SetVisible(value: false);
	}

	public virtual void OnReuse()
	{
		SetVisible(value: true);
	}
}
