using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class QuestionBoxLarge : DolocUIPanel
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Text description;

	[SerializeField]
	public DolocButtonComponent buttonConfirm;

	[SerializeField]
	public DolocButtonComponent buttonCancel;

	public string Title
	{
		set
		{
			title.text = value ?? "";
		}
	}

	public string Description
	{
		set
		{
			description.text = value ?? "";
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
