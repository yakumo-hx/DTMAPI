using System.Linq;
using DolocTown.Config;
using DolocTown.Config.UI;
using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace DolocTown;

public class MainMenuUiState : DolocUiState<MainMenuPanel>
{
	private string[] titles;

	private Sprite[] icons;

	private MenuUI menu => base.panel.menu;

	public override bool PermanentState => true;

	protected override bool hideOnPause => true;

	private int currentIndex => menu.selectedIndex;

	private TbMainMenu menuConfig => DolocConfig.Tables.TbMainMenu;

	protected override UnityEvent OnCloseButtonClick => menu.OnCloseButtonClick;

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		titles = menuConfig.DataList.Select((MainMenuInfo x) => x.Title).ToArray();
		icons = menuConfig.DataList.Select((MainMenuInfo x) => x.IconAsset.Asset).ToArray();
		menu.Render(titles, icons);
		if (!DolocAPI.archiveHandle.IsCalendarUnlocked())
		{
			menu.GetSlot(3).SetVisible(value: false);
		}
		DolocAPI.Broadcast(OperationEventType.OPEN_MAIN_MENU_PANEL);
	}

	protected override void Register()
	{
		menu.SetSelectCallbacks(OnMenuIconSelected);
		menu.SetClickCallbacks(OnMenuIconClicked);
		menu.SetPointerEnterCallbacks(OnMenuIconHover);
	}

	protected override void Unregister()
	{
		menu.RemoveCallbacks();
	}

	private void OnMenuIconHover(int index)
	{
		menu.Select(index);
	}

	private void OnMenuIconSelected(int index)
	{
		DolocAPI.UIRaiseRoll();
	}

	private void OnMenuIconClicked(int index)
	{
		menu.LoseFocus();
		switch (index)
		{
		case 0:
			DolocAPI.DelayFrame(delegate
			{
				DolocAPI.EnterUI<TechTreeUiState>();
			});
			break;
		case 1:
			DolocAPI.DelayFrame(delegate
			{
				DolocAPI.EnterUI<MissionPanelUiState>();
			});
			break;
		case 2:
			DolocAPI.EnterUI<CollectionBookUiState>();
			break;
		case 3:
			DolocAPI.EnterUI<CalendarUiState>();
			break;
		case 5:
			DolocAPI.EnterUI<InstructionUiState>();
			break;
		case 4:
			DolocAPI.EnterUI<SettingPanelUiState>();
			break;
		case 6:
			DolocAPI.DelayFrame(PhotoState.Enter);
			break;
		case 7:
			DolocAPI.EnterUI<SystemMenuUiState>();
			break;
		}
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed || (userInput.GlobalToggleMenu && userInput.BaseNotFixedOperation))
		{
			gameController.PopState();
		}
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		base.panel.BuildNavigation();
		EventSystem.current?.SetSelectedGameObject(null);
		menu.GetFocus();
	}

	protected override void Hide()
	{
		base.panel.Hide();
		base.Hide();
	}

	public override void OnPause()
	{
		DolocAPI.UIRaisePopUp();
		base.OnPause();
	}

	public override void OnResume()
	{
		base.OnResume();
		DolocAPI.UIRaisePopDown();
	}
}
