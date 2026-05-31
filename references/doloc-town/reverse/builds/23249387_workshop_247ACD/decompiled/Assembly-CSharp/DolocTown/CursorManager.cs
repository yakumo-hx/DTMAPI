using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DolocTown;

public class CursorManager : DolocObject
{
	[SerializeField]
	public Texture2D defaultCursorTexture;

	private bool shouldShowCursor = true;

	private bool hiddenCursorByGamepad;

	private readonly Vector2 hotspot = new Vector2(0f, 0f);

	private Coroutine countdownCoroutine;

	private DolocInputDeviceType currentDeviceType;

	private DolocUserInput userInput => DolocAPI.UserInput;

	public bool HiddenCursorByGamepad => hiddenCursorByGamepad;

	public Vector2 CursorPosition
	{
		get
		{
			return userInput.MousePosition;
		}
		set
		{
			userInput.MousePosition = value;
		}
	}

	public Vector2 VirtualCursorPosition { get; set; }

	protected override void __Init()
	{
		base.__Init();
		Cursor.SetCursor(defaultCursorTexture, hotspot, CursorMode.Auto);
		currentDeviceType = userInput.DeviceType;
		if (currentDeviceType != 0)
		{
			hiddenCursorByGamepad = true;
		}
		UpdateCursorState();
		userInput.BindDeviceChangedCallback(OnDeviceChanged);
	}

	private void OnDeviceChanged(DolocInputDeviceType type)
	{
		currentDeviceType = type;
		if (type == DolocInputDeviceType.KeyboardMouse)
		{
			hiddenCursorByGamepad = false;
			if (countdownCoroutine != null)
			{
				StopCoroutine(countdownCoroutine);
				countdownCoroutine = null;
			}
		}
		else if (countdownCoroutine == null)
		{
			countdownCoroutine = StartCoroutine(WaitToHideCursor());
		}
		UpdateCursorState();
	}

	private IEnumerator WaitToHideCursor()
	{
		float startTime = Time.time;
		while (Time.time - startTime < DolocAPI.GlobalParameter.HideCursorDuration)
		{
			yield return null;
		}
		hiddenCursorByGamepad = true;
		UpdateCursorState();
		countdownCoroutine = null;
	}

	public void SetCursorVisible(bool value)
	{
		shouldShowCursor = value;
		UpdateCursorState();
	}

	private void UpdateCursorState()
	{
		Cursor.lockState = ((!(Cursor.visible = shouldShowCursor && !hiddenCursorByGamepad)) ? CursorLockMode.Locked : CursorLockMode.None);
	}

	public GameObject SimulatePointerHover(Vector2 position, Vector2 delta, GameObject lastHoveredObject)
	{
		List<RaycastResult> list = RaycastUI(position);
		GameObject gameObject = (list.IsNullOrEmpty() ? null : list[0].gameObject);
		if (lastHoveredObject == gameObject)
		{
			return gameObject;
		}
		if (gameObject != null)
		{
			ExecuteEvents.Execute(gameObject, new PointerEventData(EventSystem.current)
			{
				position = position,
				delta = delta
			}, ExecuteEvents.pointerEnterHandler);
		}
		if (lastHoveredObject != null)
		{
			ExecuteEvents.Execute(lastHoveredObject, new PointerEventData(EventSystem.current)
			{
				position = position,
				delta = delta
			}, ExecuteEvents.pointerExitHandler);
		}
		return gameObject;
	}

	private List<RaycastResult> RaycastUI(Vector2 position)
	{
		PointerEventData eventData = new PointerEventData(EventSystem.current)
		{
			position = position
		};
		List<RaycastResult> list = new List<RaycastResult>();
		EventSystem.current?.RaycastAll(eventData, list);
		return list;
	}
}
