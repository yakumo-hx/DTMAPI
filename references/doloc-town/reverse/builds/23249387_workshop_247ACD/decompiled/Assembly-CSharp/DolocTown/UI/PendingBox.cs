using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class PendingBox : DolocUIPanel
{
	[SerializeField]
	private Text textCmp;

	private string originText;

	private string[] loadingStates = new string[3] { "...", ".<color=#00000000>..</color>", "..<color=#00000000>.</color>" };

	private int currentIndex;

	public float interval = 0.5f;

	private float timer;

	protected override void __Init()
	{
		base.__Init();
		base.displayAnimType = UiPanelDisplayAnimType.FadeInOut;
	}

	public void Render(string text)
	{
		originText = text;
		UpdateText();
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
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
