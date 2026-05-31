using System.Collections.Generic;
using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class InstructionUiState : DolocUiState<TutorialPanel>
{
	private int currentIndex;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	private List<string> unlockedTutorials => DolocAPI.archiveHandle.farmData.unlockedTutorials;

	private int tutorialCount => unlockedTutorials.Count;

	protected override void Register()
	{
		base.panel.LeftBtn.onClick.AddListener(PrevImage);
		base.panel.RightBtn.onClick.AddListener(NextImage);
	}

	protected override void Unregister()
	{
		base.panel.LeftBtn.onClick.RemoveListener(PrevImage);
		base.panel.RightBtn.onClick.RemoveListener(NextImage);
	}

	private void PrevImage()
	{
		currentIndex = (currentIndex + tutorialCount - 1) % tutorialCount;
		base.panel.LoadPage(unlockedTutorials[currentIndex]);
		base.panel.SetPage(currentIndex, tutorialCount);
	}

	private void NextImage()
	{
		currentIndex = (currentIndex + 1) % tutorialCount;
		base.panel.LoadPage(unlockedTutorials[currentIndex]);
		base.panel.SetPage(currentIndex, tutorialCount);
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (!ContinuouslyPressLast(deltaTime, PrevImage) && !ContinuouslyPressNext(deltaTime, NextImage) && userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
		}
	}

	protected override void Show()
	{
		base.Show();
		base.panel.SetMode(TutorialPanel.TutorialPanelMode.Multiple);
		if (tutorialCount > 0)
		{
			currentIndex = tutorialCount - 1;
			base.panel.LoadPage(unlockedTutorials[currentIndex]);
			base.panel.SetPage(currentIndex, tutorialCount);
		}
		base.panel.Show();
	}

	protected override void Hide()
	{
		base.panel.Hide();
		base.Hide();
	}
}
