namespace DolocTown.UI;

public interface IClickable
{
	void FireClick(bool fireSelect = true, bool ignoreActiveState = false);
}
