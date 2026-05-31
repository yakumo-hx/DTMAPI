using UnityEngine;

namespace DolocTown.UI;

public class FactionMissionWidget : AutoSizeUIPanel
{
	[SerializeField]
	private Transform stickRoot;

	[SerializeField]
	private Transform emptyHint;

	[HideInInspector]
	public FactionSubSeriesSlot[] stickSlots;

	[SerializeField]
	public FactionMissionViewer viewer;

	private int maxStickCount;

	public IScrollContentRect contentRect => viewer;

	public int currentStickIndex { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		stickSlots = stickRoot.GetComponentsInChildren<FactionSubSeriesSlot>(includeInactive: true);
		int num = 0;
		FactionSubSeriesSlot[] array = stickSlots;
		foreach (FactionSubSeriesSlot obj in array)
		{
			obj.Init();
			obj.index = num++;
			obj.visible = false;
			obj.onSelect.AddListener(delegate(int index)
			{
				currentStickIndex = index;
			});
		}
		viewer.Init();
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
	}

	public void RenderSticks(string[] titles)
	{
		if (titles == null)
		{
			SetEmpty(value: true);
			return;
		}
		SetEmpty(value: false);
		if (titles.Length > stickSlots.Length)
		{
			Debug.LogWarning("传入数据超出ui界面最大展示数");
		}
		int num = (maxStickCount = Mathf.Min(titles.Length, stickSlots.Length));
		float num2 = 0f;
		for (int i = 0; i < num; i++)
		{
			stickSlots[i].visible = true;
			stickSlots[i].title = titles[i];
			num2 = Mathf.Max(num2, stickSlots[i].perfectLabelWidth);
		}
		for (int j = 0; j < num; j++)
		{
			stickSlots[j].labelWidth = num2;
		}
		for (int k = num; k < stickSlots.Length; k++)
		{
			stickSlots[k].visible = false;
		}
	}

	public void NextSticks()
	{
		int num = (currentStickIndex + 1) % maxStickCount;
		stickSlots[num].FireClick();
	}

	public void LastSticks()
	{
		int num = (currentStickIndex + maxStickCount - 1) % maxStickCount;
		stickSlots[num].FireClick();
	}

	public void SelectStick(int index)
	{
		if (index > maxStickCount)
		{
			Debug.LogWarning("超出展示范围");
		}
		int num = currentStickIndex;
		currentStickIndex = index % maxStickCount;
		stickSlots[currentStickIndex].highLighted = true;
		if (num != currentStickIndex)
		{
			stickSlots[num].highLighted = false;
		}
	}

	public void SetEmpty(bool value)
	{
		emptyHint.gameObject.SetActive(value);
		viewer.canvasGroup.alpha = ((!value) ? 1 : 0);
		stickRoot.gameObject.SetActive(!value);
	}
}
