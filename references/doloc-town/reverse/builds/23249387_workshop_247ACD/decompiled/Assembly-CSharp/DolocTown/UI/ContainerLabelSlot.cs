using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ContainerLabelSlot : DolocNavigationButton
{
	[SerializeField]
	private Text label;

	public void SetLabel(string label)
	{
		this.label.text = label;
	}
}
