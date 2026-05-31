using UnityEngine.Events;

namespace DolocTown.UI;

public abstract class PageUiStateBase<TPanel, TData> : DolocUiState<TPanel> where TPanel : DolocUIPanel, IPageUI<TData> where TData : IUIData
{
	protected abstract int totalCapacity { get; }

	protected virtual int firstSelectedIndex { get; set; }

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	protected override void Register()
	{
		base.panel.SetTotalCapacity(totalCapacity);
		base.panel.DataGetter = DataGetter;
	}

	protected override void Unregister()
	{
		base.panel.DataGetter = null;
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (!ContinuouslyPressLast(deltaTime, base.panel.PrevPage) && !ContinuouslyPressNext(deltaTime, base.panel.NextPage) && userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
		}
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		if (totalCapacity > 0)
		{
			base.panel.Select(firstSelectedIndex);
		}
	}

	protected override void Hide()
	{
		base.panel.Hide();
		base.Hide();
		firstSelectedIndex = 0;
	}

	protected abstract TData[] DataGetter(int start, int end);

	public override void OnResume()
	{
		base.OnResume();
		base.panel.SetTotalCapacity(totalCapacity);
		base.panel.RefreshView();
	}
}
