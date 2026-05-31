using RedSaw;

namespace DolocTown.UI;

public abstract class DolocUiRecyclableObject : DolocUiObject, IRecyclable
{
	public virtual void OnCreated()
	{
		if (!base.isInitialized)
		{
			__Init();
		}
		SetVisible(value: true);
	}

	public virtual void OnRecycle()
	{
		SetVisible(value: false);
	}

	public virtual void OnReuse()
	{
		SetVisible(value: true);
	}

	public new void Init()
	{
		OnCreated();
	}
}
