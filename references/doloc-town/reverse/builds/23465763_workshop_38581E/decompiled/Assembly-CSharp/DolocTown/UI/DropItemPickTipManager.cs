using System.Linq;
using UnityEngine;

namespace DolocTown.UI;

public class DropItemPickTipManager : DolocUiEntity
{
	private DropItemPickTip[] pool;

	private LRUCache<string, DropItemPickTip> lruCache;

	private Vector2 itemSize;

	protected override void __Init()
	{
		base.__Init();
		pool = GetComponentsInChildren<DropItemPickTip>();
		lruCache = new LRUCache<string, DropItemPickTip>(pool.Length);
		for (int i = 0; i < pool.Length; i++)
		{
			DropItemPickTip obj = pool[i];
			obj.transform.SetSiblingIndex(i);
			obj.Init();
			obj.onComplete.AddListener(RemoveCache);
		}
		base.gameObject.SetActive(value: true);
	}

	public bool RaiseTip(string name, Sprite icon, string title, int count = 1)
	{
		string key = name + "." + title;
		if (lruCache.ContainsKey(key))
		{
			lruCache[key].ShowContinues(count);
			return false;
		}
		DropItemPickTip nextTip = GetNextTip();
		nextTip.id = key;
		nextTip.Show(name, icon, title, count);
		lruCache.Add(key, nextTip);
		return true;
	}

	private DropItemPickTip GetNextTip()
	{
		DropItemPickTip[] array = pool;
		foreach (DropItemPickTip dropItemPickTip in array)
		{
			if (dropItemPickTip.idle)
			{
				return dropItemPickTip;
			}
		}
		return lruCache.Last().Value;
	}

	private void RemoveCache(DropItemPickTip tip)
	{
		lruCache.Remove(tip.id);
	}
}
