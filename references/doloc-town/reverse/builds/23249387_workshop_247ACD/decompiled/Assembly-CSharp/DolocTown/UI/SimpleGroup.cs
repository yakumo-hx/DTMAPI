using System.Collections.Generic;
using System.Linq;

namespace DolocTown.UI;

public class SimpleGroup : DolocUiObject
{
	public void UpdateVisibleState()
	{
		List<DolocUiObject> list = GetComponentsInChildren<DolocUiObject>(includeInactive: false).ToList();
		list.Remove(this);
		SetVisible(!list.IsNullOrEmpty());
	}
}
