using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class QuestionBox : DolocUIPanel
{
	[SerializeField]
	private Text text;

	[SerializeField]
	public DolocButtonComponent buttonConfirm;

	[SerializeField]
	public DolocButtonComponent buttonCancel;

	public string title
	{
		get
		{
			return text.text;
		}
		set
		{
			text.text = value ?? "";
		}
	}

	protected override void __Init()
	{
		base.__Init();
		buttonConfirm.onClick.AddListener(DolocAPI.UIRaiseConfirm);
		buttonCancel.onClick.AddListener(DolocAPI.UIRaiseCancel);
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
	}
}
