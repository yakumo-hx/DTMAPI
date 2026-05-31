namespace DolocTown.UI;

public abstract class DolocVerticalUI<T> : DolocGridUI<T> where T : DolocNavigationButton
{
	protected sealed override int lineCapacity
	{
		get
		{
			return 1;
		}
		set
		{
			base.lineCapacity = value;
		}
	}

	protected sealed override LayoutType layoutType => LayoutType.Vertical;

	public sealed override void SetCapacity(int totalCapacity, int lineCapacity)
	{
		SetCapacity(totalCapacity);
	}

	public virtual void SetCapacity(int totalCapacity)
	{
		base.SetCapacity(totalCapacity, 1);
	}
}
