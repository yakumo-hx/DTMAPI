using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DolocSelectableComponent : Selectable, ISelectable
{
	private RectTransform _rectTransform;

	[HideInInspector]
	public UnityEvent onSelect { get; } = new UnityEvent();


	[HideInInspector]
	public UnityEvent onDeselect { get; } = new UnityEvent();


	[HideInInspector]
	public UnityEvent<MoveDirection> onMove { get; } = new UnityEvent<MoveDirection>();


	public RectTransform rectTransform
	{
		get
		{
			if (_rectTransform == null && (object)_rectTransform == null)
			{
				_rectTransform = GetComponent<RectTransform>();
			}
			return _rectTransform;
		}
	}

	public override void OnSelect(BaseEventData eventData)
	{
		base.OnSelect(eventData);
		onSelect.Invoke();
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		base.OnDeselect(eventData);
		onDeselect.Invoke();
	}

	public override void OnMove(AxisEventData eventData)
	{
		base.OnMove(eventData);
		onMove.Invoke(eventData.moveDir);
	}
}
