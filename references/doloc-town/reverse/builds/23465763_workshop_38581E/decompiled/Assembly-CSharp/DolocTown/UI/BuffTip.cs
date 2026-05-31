using System.Collections.Generic;
using DolocTown.Config.Buff;
using RedSaw;
using UnityEngine;

namespace DolocTown.UI;

public class BuffTip : DolocBasicTip
{
	private NashObjectPool<BuffIconRenderer> icons;

	private readonly Dictionary<string, BuffIconRenderer> usedIcons = new Dictionary<string, BuffIconRenderer>();

	protected override void __Init()
	{
		base.__Init();
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_BUFF_ICON);
		icons = new NashObjectPool<BuffIconRenderer>(asset, base.transform, 5, 3, usePreset: true);
	}

	public void Add(BuffInfo buffProto, int currentDuration)
	{
		if (!usedIcons.TryGetValue(buffProto.Id, out var value))
		{
			value = icons.Next;
			usedIcons.Add(buffProto.Id, value);
		}
		value.Render(buffProto, currentDuration);
	}

	public void Remove(string Id)
	{
		if (usedIcons.TryGetValue(Id, out var value))
		{
			usedIcons.Remove(Id);
			icons.Recycle(value);
		}
	}

	public void UpdateProgress(string Id, int duration)
	{
		if (usedIcons.TryGetValue(Id, out var value))
		{
			value.Duration = duration;
			value.TryRefreshHoverBox();
		}
	}

	public void Clear()
	{
		icons.RecycleAll();
		usedIcons.Clear();
	}
}
