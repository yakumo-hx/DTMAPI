using UnityEngine.UI;

namespace DolocTown.UI;

public interface INavPanel
{
	Selectable[] allSelectablesArray { get; }

	int allSelectableCount { get; }
}
