using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ModChangeListPanel : DolocUIPanel
{
	[SerializeField]
	public DolocButtonComponent buttonConfirm;

	[SerializeField]
	public DolocButtonComponent buttonCancel;

	[SerializeField]
	public Text textAddInfo;

	[SerializeField]
	public Text textAddList;

	[SerializeField]
	public Text textRemoveInfo;

	[SerializeField]
	public Text textRemoveList;

	protected override void __Init()
	{
		base.__Init();
		buttonConfirm.onClick.AddListener(DolocAPI.UIRaiseConfirm);
		buttonCancel.onClick.AddListener(DolocAPI.UIRaiseCancel);
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
	}

	public void Render(ModChangeData data)
	{
		if (data.notEmpty)
		{
			SetText(textAddList, data.addListText, new Component[1] { textAddInfo });
			SetText(textRemoveList, data.removeListText, new Component[1] { textRemoveInfo });
		}
	}
}
