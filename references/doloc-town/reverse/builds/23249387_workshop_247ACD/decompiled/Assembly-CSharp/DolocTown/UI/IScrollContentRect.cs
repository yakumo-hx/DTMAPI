using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public interface IScrollContentRect
{
	ScrollRect scrollRect { get; }

	float moveDelta { get; }

	float currentValue => scrollRect.verticalScrollbar.value;

	void SetScrollValue(float value)
	{
		scrollRect.verticalScrollbar.value = Mathf.Clamp01(value);
	}

	void SetScrollMoveCallback(float delta)
	{
		Scrollbar verticalScrollbar = scrollRect.verticalScrollbar;
		delta = Mathf.Clamp(delta, -1f, 1f);
		verticalScrollbar.value = Mathf.Clamp01(verticalScrollbar.value + delta * moveDelta);
		OnMove();
	}

	void OnMove();
}
