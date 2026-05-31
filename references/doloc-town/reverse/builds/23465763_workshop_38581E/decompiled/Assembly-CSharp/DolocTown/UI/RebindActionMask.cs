using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DolocTown.UI;

public class RebindActionMask : DolocUiObject
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Text conflictHint;

	[SerializeField]
	private DolocButtonComponent btnConfirm;

	[SerializeField]
	private DolocButtonComponent btnCancel;

	[SerializeField]
	public InputActionKeyIcon keyIcon;

	[SerializeField]
	public GameObject buttonGroup;

	private Coroutine coroutine;

	private Action onConfirm;

	private Action onCancel;

	private bool isSelectConfirm;

	protected override void __Init()
	{
		base.__Init();
		keyIcon.Init();
		btnConfirm.onSelect.AddListener(delegate
		{
			isSelectConfirm = true;
		});
		btnConfirm.onDeselect.AddListener(delegate
		{
			isSelectConfirm = false;
		});
		btnConfirm.onClick.AddListener(delegate
		{
			Hide();
			onConfirm?.Invoke();
		});
		btnCancel.onSelect.AddListener(delegate
		{
			isSelectConfirm = false;
		});
		btnCancel.onClick.AddListener(delegate
		{
			Hide();
			onCancel?.Invoke();
		});
	}

	public void Show(Action onConfirm, Action onCancel, string title)
	{
		this.title.text = title ?? "";
		base.gameObject.SetActive(value: true);
		this.onConfirm = onConfirm;
		this.onCancel = onCancel;
		SetText(conflictHint, "");
		keyIcon.SetText(string.Empty);
		keyIcon.Clear();
		if (coroutine == null)
		{
			coroutine = StartCoroutine(DotLoop());
		}
		buttonGroup.SetActive(value: false);
		isSelectConfirm = false;
	}

	public bool RenderKey(InputAction action, int bindingIndex, DolocInputDeviceType deviceType)
	{
		if (!keyIcon.Render(action, bindingIndex, deviceType))
		{
			return false;
		}
		StopCoroutine(coroutine);
		coroutine = null;
		buttonGroup.SetActive(value: true);
		btnConfirm.Select();
		isSelectConfirm = true;
		return true;
	}

	public void SetConflictHint(string hint)
	{
		SetText(conflictHint, hint);
	}

	public void Hide()
	{
		if (coroutine != null)
		{
			StopCoroutine(coroutine);
		}
		coroutine = null;
		base.gameObject.SetActive(value: false);
	}

	private IEnumerator DotLoop()
	{
		string[] dots = new string[3] { ".", "..", "..." };
		int index = 0;
		while (true)
		{
			keyIcon.SetText(dots[index]);
			index = (index + 1) % dots.Length;
			yield return new WaitForSeconds(0.5f);
		}
	}

	public void TryCancel()
	{
		if (isSelectConfirm)
		{
			btnCancel.Select();
		}
		else
		{
			btnCancel.FireClick();
		}
	}
}
