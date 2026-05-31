using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DolocTown.UI;

public class DialogueOptionPanel : TextMenu, IDialogueOptionView, IView
{
	private Dictionary<int, int> dataIndexToLayoutIndex = new Dictionary<int, int>();

	private OptionGroupData currentGroupData;

	private int exitDataIndex;

	protected override GameObject slotPrefab => LocPfbs.UI_PFB_TEXT_OPTION;

	public bool InRender => base.isRender;

	public bool InAnimation => base.inAnimation;

	public override void Render(string[] options)
	{
		base.Render(options);
		exitDataIndex = -1;
		dataIndexToLayoutIndex.Clear();
		for (int i = 0; i < base.slots.Count; i++)
		{
			dataIndexToLayoutIndex[i] = i;
		}
	}

	public void RenderWithExitOption(string[] options)
	{
		Render(options);
		exitDataIndex = options.Length - 1;
	}

	public void Render(OptionGroupData groupData)
	{
		exitDataIndex = -1;
		currentGroupData = groupData;
		Vector2 cellSize = new Vector2(minCellWidth, slotLayoutGroup.cellSize.y);
		SetCapacity(groupData.count);
		dataIndexToLayoutIndex.Clear();
		for (int i = 0; i < base.slots.Count; i++)
		{
			OptionData optionData = groupData.options[i];
			TextButton textButton = base.slots[i];
			textButton.alignment = TextAlignmentOptions.Left;
			textButton.index = optionData.index;
			textButton.text = optionData.text;
			textButton.grayed = optionData.isVisited;
			textButton.visible = true;
			textButton.transform.SetSiblingIndex(i);
			cellSize.x = Mathf.Max(cellSize.x, textButton.preferredWidth);
			dataIndexToLayoutIndex[optionData.index] = i;
			if (optionData.isExitOption)
			{
				exitDataIndex = optionData.index;
			}
		}
		slotLayoutGroup.cellSize = cellSize;
		SetAnchoredShowPosition(groupData.count switch
		{
			1 => 60, 
			2 => 40, 
			_ => 30, 
		}, UIAlignmentType.BottomMiddle);
		Show();
	}

	void IDialogueOptionView.SelectByDataIndex(int dataIndex)
	{
		dataIndexToLayoutIndex.TryGetValue(dataIndex, out var value);
		Select(value);
	}

	public bool SelectThenFireClickExitOption()
	{
		if (!dataIndexToLayoutIndex.TryGetValue(exitDataIndex, out var value))
		{
			return false;
		}
		if (base.selectedIndex == exitDataIndex)
		{
			FireClick(value);
		}
		else
		{
			Select(value);
		}
		return true;
	}

	public void Pause()
	{
		Hide();
	}

	public void Resume()
	{
		Show(useTween: false);
		((IDialogueOptionView)this).SelectByDataIndex(base.selectedIndex);
	}
}
