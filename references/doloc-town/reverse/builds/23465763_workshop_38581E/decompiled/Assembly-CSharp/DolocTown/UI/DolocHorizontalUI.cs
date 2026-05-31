namespace DolocTown.UI;

public abstract class DolocHorizontalUI<T> : DolocGridUI<T> where T : DolocNavigationButton
{
	protected sealed override int lineCapacity
	{
		get
		{
			return base.totalCapacity;
		}
		set
		{
			base.lineCapacity = value;
		}
	}

	protected sealed override LayoutType layoutType => LayoutType.Horizontal;

	public sealed override void SetCapacity(int totalCapacity, int lineCapacity)
	{
		SetCapacity(totalCapacity);
	}

	public virtual void SetCapacity(int totalCapacity)
	{
		base.SetCapacity(totalCapacity, totalCapacity);
	}
}
