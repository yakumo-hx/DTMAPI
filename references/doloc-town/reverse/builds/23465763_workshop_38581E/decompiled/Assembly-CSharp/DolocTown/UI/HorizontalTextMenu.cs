using RedSaw;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(HorizontalLayoutGroup))]
public abstract class HorizontalTextMenu : DolocUIPanel
{
	[SerializeField]
	private Color normalColor;

	[SerializeField]
	private Color highLightColor;

	[SerializeField]
	private Color normalTextColor;

	[SerializeField]
	private Color highLightTextColor;

	protected ObjectPool<TextButton> slotPool;

	public int selectIndex { get; private set; }

	protected abstract GameObject pfb { get; }

	protected override void __Init()
	{
		base.__Init();
		slotPool = new ObjectPool<TextButton>(pfb, base.transform, usePreset: true);
	}

	public void Render(string[] title)
	{
		slotPool.CheckCount(title.Length);
		slotPool.Sort();
		for (int i = 0; i < title.Length; i++)
		{
			slotPool[i].text = title[i];
		}
	}

	public void SetClickCallbacks(UnityAction<int> callback)
	{
		for (int i = 0; i < slotPool.ActiveCount; i++)
		{
			TextButton textButton = slotPool[i];
			textButton.index = i;
			textButton.onClick.RemoveAllListeners();
			textButton.onClick.AddListener(callback);
			textButton.onClick.AddListener(OnSlotSelect);
		}
	}

	public void Select(int index)
	{
		if (index >= 0 && index < slotPool.ActiveCount)
		{
			slotPool[index].FireClick();
		}
	}

	private void OnSlotSelect(int index)
	{
		slotPool[selectIndex].backgroundColor = normalColor;
		slotPool[selectIndex].textColor = normalTextColor;
		selectIndex = index;
		slotPool[index].backgroundColor = highLightColor;
		slotPool[index].textColor = highLightTextColor;
	}
}
