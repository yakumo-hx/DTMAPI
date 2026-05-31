using DolocTown.Config;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BoardMissionPanel : DolocHorizontalUI<BoardMissionSlot>
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Text lvInfo;

	[SerializeField]
	private Text hint;

	public UnityAction<int> SlotClick;

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_BOARD_MISSION_SLOT);

	protected override void __Init()
	{
		base.__Init();
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
	}

	public void Render(int index, BoardMissionData data)
	{
		Refresh(index, data);
		base.slots[index].confirmBtn.onClick.RemoveAllListeners();
		base.slots[index].confirmBtn.onClick.AddListener(delegate
		{
			SlotClick(index);
		});
	}

	public void Refresh(int index, BoardMissionData data)
	{
		if (index >= 0 && index <= base.slots.Count - 1)
		{
			base.slots[index].Render(data);
		}
	}

	public void SetLvInfo(string battleLv)
	{
		lvInfo.gameObject.SetActive(!battleLv.IsNullOrEmpty());
		lvInfo.text = string.Format(base.staticTexts.BoardMissionLv, battleLv);
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		title.text = DolocConfig.StaticTexts.BoardMissionPanelTitle;
		hint.text = DolocConfig.StaticTexts.BoardMissionExpTip;
	}
}
