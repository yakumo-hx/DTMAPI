using UnityEngine;

namespace DolocTown.UI;

public class CollectionBookSubMenu : HorizontalTextMenu
{
	protected override GameObject pfb => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_BOOK_MENU_SLOT);

	private int labelCount => slotPool.ActiveCount;

	public void Render(string[] title, bool[] validList)
	{
		Render(title);
		for (int i = 0; i < validList.Length; i++)
		{
			slotPool[i].SetVisible(validList[i]);
		}
	}

	public void NextLabel()
	{
		int num = base.selectIndex;
		for (int i = 0; i < labelCount; i++)
		{
			num = (num + 1) % labelCount;
			if (slotPool[num].isVisible)
			{
				Select(num);
				break;
			}
		}
	}

	public void PrevLabel()
	{
		int num = base.selectIndex;
		for (int i = 0; i < labelCount; i++)
		{
			num = (num + labelCount - 1) % labelCount;
			if (slotPool[num].isVisible)
			{
				Select(num);
				break;
			}
		}
	}

	public void LastLabel()
	{
		for (int num = labelCount - 1; num >= 0; num--)
		{
			if (slotPool[num].isVisible)
			{
				Select(num);
				break;
			}
		}
	}
}
