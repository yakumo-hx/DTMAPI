using UnityEngine.EventSystems;

namespace RedSaw.UI;

public abstract class RedSawButtonPrototype : RedSawUiObject, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
	protected PointerEventData data;

	protected bool isDown;

	protected bool isHover;

	protected virtual void onClick()
	{
	}

	protected virtual void onHover()
	{
	}

	protected virtual void onQuit()
	{
	}

	protected virtual void onDown()
	{
	}

	protected virtual void onUp()
	{
	}

	public void OnPointerClick(PointerEventData data)
	{
		this.data = data;
		onClick();
	}

	public void OnPointerDown(PointerEventData data)
	{
		this.data = data;
		isDown = true;
		onDown();
	}

	public void OnPointerUp(PointerEventData data)
	{
		this.data = data;
		isDown = false;
		onUp();
	}

	public void OnPointerEnter(PointerEventData data)
	{
		this.data = data;
		isHover = true;
		onHover();
	}

	public void OnPointerExit(PointerEventData data)
	{
		this.data = data;
		isHover = false;
		onQuit();
	}
}
