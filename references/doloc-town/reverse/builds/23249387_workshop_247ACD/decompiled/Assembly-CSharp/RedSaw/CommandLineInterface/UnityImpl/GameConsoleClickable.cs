using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RedSaw.CommandLineInterface.UnityImpl;

[RequireComponent(typeof(Image))]
public abstract class GameConsoleClickable : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
	protected Image image;

	protected Color normalColor;

	[SerializeField]
	private Color hoverColor = Color.white;

	[SerializeField]
	private Color downColor = Color.white;

	protected bool isHover;

	protected bool isDown;

	protected Vector2 MousePosition => Mouse.current.position.ReadValue();

	public void Init()
	{
		image = GetComponent<Image>();
		if (image == null)
		{
			throw new Exception("GameConsoleClickable must attach to a GameObject with Image component");
		}
		normalColor = image.color;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		isDown = true;
		image.color = downColor;
		OnMouseButtonDown(MousePosition);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		isDown = false;
		image.color = (isHover ? hoverColor : normalColor);
		OnMouseButtonUp();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		isHover = true;
		image.color = (isDown ? downColor : hoverColor);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		isHover = false;
		if (!isDown)
		{
			image.color = (isHover ? hoverColor : normalColor);
		}
	}

	protected virtual void OnMouseButtonDown(Vector2 pos)
	{
	}

	protected virtual void OnMouseButtonUp()
	{
	}
}
