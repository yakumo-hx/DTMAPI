using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class RewardViewer : DolocUiObject
{
	private DolocIconWithTextEx[] slots;

	[SerializeField]
	private Text txtRewardsInfo;

	[SerializeField]
	private int fixedSlotCount;

	public DolocIconWithTextEx[] Slots => slots;

	protected override void __Init()
	{
		base.__Init();
		slots = GetComponentsInChildren<DolocIconWithTextEx>(includeInactive: true);
	}

	public void Render(RewardData data, bool showEmptyInfo = false)
	{
		if (!data.notEmpty)
		{
			if (showEmptyInfo)
			{
				SetText(txtRewardsInfo, data.rewardsInfo);
			}
			base.gameObject.SetActive(value: false);
			return;
		}
		for (int i = 0; i < slots.Length; i++)
		{
			DolocIconWithTextEx dolocIconWithTextEx = slots[i];
			if (i < data.rewardsCount)
			{
				if (data.rewardIcons[i] == null)
				{
					dolocIconWithTextEx.gameObject.SetActive(value: false);
					continue;
				}
				dolocIconWithTextEx.gameObject.SetActive(value: true);
				SetSprite(dolocIconWithTextEx.iconImg, data.rewardIcons[i]);
				dolocIconWithTextEx.number = data.rewardCounts[i];
			}
			if (i < data.rewardsCount)
			{
				dolocIconWithTextEx.SetVisible(value: true);
				dolocIconWithTextEx.alpha = 1f;
				dolocIconWithTextEx.interactable = true;
			}
			else if (i < fixedSlotCount)
			{
				dolocIconWithTextEx.SetVisible(value: true);
				dolocIconWithTextEx.alpha = 0f;
				dolocIconWithTextEx.interactable = false;
			}
			else
			{
				dolocIconWithTextEx.SetVisible(value: false);
			}
		}
		SetText(txtRewardsInfo, data.rewardsInfo);
		base.gameObject.SetActive(value: true);
	}
}
