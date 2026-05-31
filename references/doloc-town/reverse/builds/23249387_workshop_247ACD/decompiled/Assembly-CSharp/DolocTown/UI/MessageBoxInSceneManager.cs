using System;
using DG.Tweening;
using RedSaw;
using UnityEngine;

namespace DolocTown.UI;

public class MessageBoxInSceneManager
{
	private readonly NashObjectPool<MessageBoxInScene> pool;

	public MessageBoxInSceneManager(Transform container)
	{
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_MSGBOX_INSCENE);
		pool = new NashObjectPool<MessageBoxInScene>(asset, container, 3);
		pool.OnCreate = delegate(MessageBoxInScene msgbox)
		{
			msgbox.Recycle = pool.Recycle;
		};
	}

	public void ShowAsWaiter(string content, Vector2 positionWS, float durShow, float durWait, Ease ease)
	{
		pool.Next.ShowAsWaiter(content, positionWS, durShow, durWait, ease);
	}

	public Action ShowAsManual(string content, Vector2 positionWS, float durShow, Ease ease)
	{
		return pool.Next.ShowAsManual(content, positionWS, durShow, ease);
	}
}
