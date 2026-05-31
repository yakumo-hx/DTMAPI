using RedSaw;
using UnityEngine;

namespace DolocTown.UI;

public class EffectGroupViewer : DolocUiObject
{
	[SerializeField]
	private Transform slotRoot;

	private ObjectPool<EffectSlot> slots;

	protected override void __Init()
	{
		base.__Init();
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_EFFECT_SLOT);
		slots = new ObjectPool<EffectSlot>(asset, slotRoot);
		EffectSlot[] componentsInChildren = GetComponentsInChildren<EffectSlot>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].SetVisible(value: false);
		}
		for (int j = 0; j < slots.ActiveCount; j++)
		{
			slots[j].Alpha = 0f;
		}
	}

	public void Render(EffectGroupData data)
	{
		if (!data.notEmpty)
		{
			SetVisible(value: false);
			return;
		}
		SetVisible(value: true);
		Render(data.icons, data.descriptions);
	}

	private void Render(Sprite[] icons, string[] descriptions)
	{
		int num = icons.Length;
		CheckCount(num);
		if (descriptions.Length != num)
		{
			Debug.LogError("EffectGroupViewer: 参数长度不一致");
			return;
		}
		slots.CheckCount(num);
		for (int i = 0; i < num; i++)
		{
			slots[i].Render(icons[i], descriptions[i]);
		}
	}

	private void CheckCount(int count)
	{
		slots.CheckCount(count);
		slots.Sort();
	}
}
