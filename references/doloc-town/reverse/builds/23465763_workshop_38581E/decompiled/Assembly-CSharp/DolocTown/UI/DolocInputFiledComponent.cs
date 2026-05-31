using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DolocInputFiledComponent : InputField
{
	public UnityEvent onSelect { get; } = new UnityEvent();


	public UnityEvent onDeselect { get; } = new UnityEvent();


	public UnityEvent<MoveDirection> onMove { get; } = new UnityEvent<MoveDirection>();


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
