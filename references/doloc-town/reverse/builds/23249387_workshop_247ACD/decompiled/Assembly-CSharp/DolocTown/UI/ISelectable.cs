using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace DolocTown.UI;

public interface ISelectable
{
	UnityEvent onSelect { get; }

	UnityEvent onDeselect { get; }

	UnityEvent<MoveDirection> onMove { get; }

	RectTransform rectTransform { get; }
}
