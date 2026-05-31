using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class AnimalViewer : DolocUiObject
{
	[SerializeField]
	public CanvasGroup contentCanvas;

	[SerializeField]
	public ConfirmCraftButton callButton;

	[SerializeField]
	public DolocButtonComponent renameButton;

	[SerializeField]
	protected Text emptyInfo;

	[SerializeField]
	protected Text title;

	[SerializeField]
	protected Text birthdayInfo;

	[SerializeField]
	protected Text ageInfo;

	[SerializeField]
	protected Text growthInfo;

	[SerializeField]
	protected Text stateInfo;

	[SerializeField]
	protected Text stateDescription;

	[SerializeField]
	protected Text positionInfo;

	[SerializeField]
	protected ProgressBar energyBar;

	[SerializeField]
	protected ProgressBar moodBar;

	protected override void __Init()
	{
		base.__Init();
		callButton.Init();
		energyBar.Init();
		moodBar.Init();
		SetEmpty(value: true);
	}

	public void Show(AnimalFullInfoData data)
	{
		if (!data.notEmpty)
		{
			SetEmpty(value: true);
			return;
		}
		if (!data.visible)
		{
			SetEmpty(value: true, base.staticTexts.UiAnimalNotVisible);
			return;
		}
		SetEmpty(value: false);
		OnShow(data);
	}

	protected virtual void OnShow(AnimalFullInfoData data)
	{
		SetText(title, data.title);
		SetText(birthdayInfo, data.birthdayInfo);
		SetText(growthInfo, data.growthInfo);
		SetText(stateInfo, data.stateInfo);
		SetText(stateDescription, data.stateDescription);
		SetText(positionInfo, data.positionInfo);
		SetText(ageInfo, data.ageInfo);
		energyBar.SetProgress(data.energyProgress, data.energyInfo);
		moodBar.SetProgress(data.moodProgress, data.moodInfo);
		callButton.gameObject.SetActive(!data.callButtonText.IsNullOrEmpty());
		callButton.buttonText = data.callButtonText;
	}

	public void Hide()
	{
	}

	public void SetEmpty(bool value, string text = "")
	{
		contentCanvas.alpha = ((!value) ? 1 : 0);
		emptyInfo.gameObject.SetActive(value);
		emptyInfo.text = text;
	}
}
