using System;
using UnityEngine;

namespace DolocTown.UI;

public class RenamingBox : InputNameBox
{
	[SerializeField]
	public DolocButtonComponent cancelButton;

	[SerializeField]
	public DolocButtonComponent revertButton;

	[HideInInspector]
	public bool isCancelSelected;

	private Action onCancel;

	private string defaultText;

	protected override void __Init()
	{
		base.__Init();
		cancelButton.onClick.AddListener(delegate
		{
			onCancel();
		});
		cancelButton.onSelect.AddListener(delegate
		{
			isCancelSelected = true;
		});
		cancelButton.onDeselect.AddListener(delegate
		{
			isCancelSelected = false;
		});
		revertButton.onClick.AddListener(delegate
		{
			inputField.text = defaultText;
			inputField.Select();
		});
	}

	public void Render(string title, string holdPlace, string defaultText, Action<string> onConfirm, int maxLength, Action onCancel)
	{
		Render(title, holdPlace, onConfirm, maxLength);
		isCancelSelected = false;
		this.onCancel = onCancel;
		this.defaultText = defaultText;
	}

	protected override void OnConfirm()
	{
		string obj = inputField.text.Trim();
		if (CheckText())
		{
			onConfirm?.Invoke(obj);
		}
	}
}
