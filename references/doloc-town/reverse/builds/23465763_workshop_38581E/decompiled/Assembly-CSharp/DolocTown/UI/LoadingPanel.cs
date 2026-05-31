using TMPro;
using UnityEngine;

namespace DolocTown.UI;

public class LoadingPanel : DolocUIPanel
{
	[SerializeField]
	private TMP_Text textCmp;

	private string originText;

	private string[] loadingStates = new string[4] { "...", "", ".", ".." };

	private int currentIndex;

	public float interval = 1f;

	private float timer;

	protected override void __Init()
	{
		base.__Init();
		base.displayAnimType = UiPanelDisplayAnimType.FadeInOut;
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		DolocAPI.SetResidentUiVisible(showBasicTip: false, showQuickInventory: false);
		originText = base.staticTexts.UiTipLoading;
		UpdateText();
		currentIndex = 0;
	}

	private void Update()
	{
		timer += Time.deltaTime;
		if (timer >= interval)
		{
			currentIndex = (currentIndex + 1) % loadingStates.Length;
			UpdateText();
			timer = 0f;
		}
		if (DolocAPI.IsDataLoaded)
		{
			Hide();
		}
	}

	private void UpdateText()
	{
		textCmp.text = originText + loadingStates[currentIndex];
	}
}
