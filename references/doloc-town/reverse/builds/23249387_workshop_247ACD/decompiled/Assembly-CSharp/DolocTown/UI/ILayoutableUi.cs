using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public interface ILayoutableUi
{
	RectTransform layoutTrans { get; }

	void IgnoreLayout(bool value)
	{
		if (!layoutTrans.TryGetComponent<LayoutElement>(out var component))
		{
			component = layoutTrans.gameObject.AddComponent<LayoutElement>();
		}
		component.ignoreLayout = value;
	}
}
