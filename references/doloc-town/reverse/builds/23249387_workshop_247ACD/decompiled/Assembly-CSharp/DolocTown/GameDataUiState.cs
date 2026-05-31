using System.Collections.Generic;
using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class GameDataUiState : DolocUiState<GameDataPanel>
{
	private Dictionary<int, BaseArchiveData> baseArchiveDatas = new Dictionary<int, BaseArchiveData>();

	private int currentIndex;

	private int slotCount => DolocAPI.gameManager.archiveFileCount;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	private GameDataSlot currentSlot => base.panel.GetSlot(currentIndex);

	private string GetDataIndexText(int index)
	{
		return $"#{index + 1}";
	}

	private void OnDataSlotSelect(int index)
	{
		currentIndex = index;
		base.panel.HighLightButton(base.panel.btnConfirm);
		base.panel.SetConfirmText(currentSlot.grayed ? base.staticTexts.GameDataStart : base.staticTexts.GameDataLoad);
	}

	private void OnDataSlotClick(int index)
	{
		if (DolocButtonComponent.latestClickType != ClickType.Mouse)
		{
			base.panel.btnConfirm.Select();
		}
	}

	private void OnDelete()
	{
		if (!currentSlot.grayed)
		{
			DolocAPI.UIRaiseCancel();
			DolocAPI.ShowQuestionBox(DolocUtils.Format(base.staticTexts.GameDataDelConfirm, GetDataIndexText(currentIndex).Colored(DolocUiColor.EYECATCHCOLOR_CYAN)), delegate
			{
				DolocAPI.DeleteGame(currentIndex);
				baseArchiveDatas[currentIndex] = null;
				base.panel.Render(currentIndex, null);
				base.panel.Select(currentIndex);
				DolocAPI.ShowMessageBoxSmall(DolocUtils.Format(base.staticTexts.GameDataDelSuccess, GetDataIndexText(currentIndex)));
			}, null, firstSelectConfirm: false);
		}
		else
		{
			DolocAPI.ShowMessageBoxSmall(base.staticTexts.GameDataErrEmpty);
			base.panel.Select(currentIndex);
		}
	}

	private void OnDuplicate()
	{
		int targetIndex;
		if (currentSlot.grayed)
		{
			DolocAPI.ShowMessageBoxSmall(base.staticTexts.GameDataErrEmpty);
			base.panel.Select(currentIndex);
		}
		else if (DolocAPI.DuplicateGame(currentIndex, out targetIndex))
		{
			DolocAPI.UIRaiseConfirm();
			baseArchiveDatas[targetIndex] = DolocAPI.GetArchiveInfo(targetIndex);
			base.panel.Render(targetIndex, baseArchiveDatas[targetIndex]);
			base.panel.Select(targetIndex);
			DolocAPI.ShowMessageBoxSmall(DolocUtils.Format(base.staticTexts.GameDataDuplicateSucess, GetDataIndexText(targetIndex)));
		}
		else
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.GameDataFull);
			base.panel.Select(currentIndex);
		}
	}

	private void OnConfirm()
	{
		DolocAPI.UIRaiseConfirm();
		if (baseArchiveDatas[currentIndex] == null)
		{
			Confirm();
			return;
		}
		ModChangeData modChangeData = new ModChangeData(baseArchiveDatas[currentIndex].enabledModInfos, DolocAPI.modManager.EnabledMods);
		if (modChangeData.hasChanged)
		{
			DolocAPI.EnterUI((ModChangeListUiState state) => state.HandleStartUpArgs(modChangeData, Confirm, null, !modChangeData.hasRemoved));
		}
		else
		{
			Confirm();
		}
		void Confirm()
		{
			bool grayed = currentSlot.grayed;
			DolocAPI.gameUiStates.GetState<HomePageUiState>().Kill();
			gameController.PopState();
			if (gameController.CheckState<HomePageUiState>())
			{
				gameController.PopState();
			}
			DolocAPI.uiSystem.GetEntity<LoadingPanel>().Show(useTween: true, delegate
			{
				DolocAPI.DelayFrame(delegate
				{
					Load(grayed, currentIndex);
				});
			});
		}
	}

	private void Load(bool isNew, int index)
	{
		if (isNew)
		{
			DolocAPI.NewGame(index);
		}
		else if (!DolocAPI.LoadGame(index))
		{
			DolocAPI.ShowMessageBoxSmall(base.staticTexts.GameDataErrRead);
			DolocAPI.HidePanel<LoadingPanel>();
			DolocAPI.EnterUI<HomePageUiState>();
			return;
		}
		DolocAPI.HidePanel<LoadingPanel>();
	}

	protected override void Register()
	{
		base.panel.SetSelectCallbacks(OnDataSlotSelect);
		base.panel.SetClickCallbacks(OnDataSlotClick);
		base.panel.btnConfirm.onClick.AddListener(OnConfirm);
		base.panel.btnCopy.onClick.AddListener(OnDuplicate);
		base.panel.btnDelete.onClick.AddListener(OnDelete);
	}

	protected override void Unregister()
	{
		base.panel.RemoveCallbacks();
		base.panel.btnConfirm.onClick.RemoveListener(OnConfirm);
		base.panel.btnCopy.onClick.RemoveListener(OnDuplicate);
		base.panel.btnDelete.onClick.RemoveListener(OnDelete);
	}

	protected override void Show()
	{
		base.Show();
		BaseArchiveData[] allArchiveInfos = DolocAPI.GetAllArchiveInfos();
		for (int i = 0; i < allArchiveInfos.Length; i++)
		{
			baseArchiveDatas[i] = allArchiveInfos[i];
		}
		base.panel.Render(allArchiveInfos, DolocAPI.Has087DemoData());
		base.panel.Show();
		base.panel.GetSlot(0).Select();
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			if (base.panel.focusedOnFunctionButtons)
			{
				base.panel.GetSlot(currentIndex).Select();
			}
			else
			{
				gameController.PopState();
			}
		}
	}
}
