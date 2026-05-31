using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown.UI;

public class SwitchBar : DolocUiObject
{
	[SerializeField]
	private TextMeshProUGUI mainTitle;

	[SerializeField]
	private TextMeshProUGUI subTitle;

	[SerializeField]
	private DolocNavigationButton switchButton;

	public UnityEvent OnSwitch = new UnityEvent();

	protected override void __Init()
	{
		base.__Init();
		switchButton.Init();
		switchButton.onClick.AddListener(delegate
		{
			OnSwitch.Invoke();
		});
	}

	public void SetTitle(string main, string sub)
	{
		if (main.IsNullOrEmpty() || sub.IsNullOrEmpty())
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		SetText(mainTitle, main);
		subTitle.text = sub;
		base.gameObject.SetActive(value: true);
	}
}
