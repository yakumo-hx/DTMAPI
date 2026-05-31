using System;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class SmallTextMenuUiState : DolocUiState<SmallTextMenu>
{
	private string[] titles;

	private Action<string> onConfirm;

	private Vector2 position;

	public bool HandleStartUpArgs(string[] titles, Vector2 screenPosition, Action<string> onConfirm)
	{
		this.titles = titles;
		position = screenPosition;
		this.onConfirm = onConfirm;
		return true;
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
		}
	}

	protected override void Show()
	{
		base.Show();
		base.panel.SetShowPosition(position);
		base.panel.Show();
		base.panel.Select(0);
	}

	protected override void Hide()
	{
		base.panel.Hide();
		base.Hide();
	}

	protected override void Register()
	{
		base.panel.Render(titles);
		base.panel.SetClickCallbacks(delegate(int index)
		{
			gameController.PopState();
			onConfirm?.Invoke(titles[index]);
		});
	}

	protected override void Unregister()
	{
		base.panel.RemoveCallbacks();
	}
}
