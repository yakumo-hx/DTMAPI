using System;
using DG.Tweening;
using RedSaw;
using UnityEngine;

namespace DolocTown.UI;

public class MessageBoxInSceneWithIconManager
{
	private readonly NashObjectPool<MessageBoxInSceneWithIcon> pool;

	public MessageBoxInSceneWithIconManager(Transform container)
	{
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_MSGBOX_INSCENE_WITHICON);
		pool = new NashObjectPool<MessageBoxInSceneWithIcon>(asset, container, 3);
		pool.OnCreate = delegate(MessageBoxInSceneWithIcon msgbox)
		{
			msgbox.Recycle = pool.Recycle;
		};
	}

	public void ShowAsWaiter(string content, Sprite icon, Vector2 positionWS, float durShow, float durWait, Ease ease)
	{
		pool.Next.ShowAsWaiter(content, icon, positionWS, durShow, durWait, ease);
	}

	public Action ShowAsManual(string content, Sprite icon, Vector2 positionWS, float durShow, Ease ease)
	{
		return pool.Next.ShowAsManual(content, icon, positionWS, durShow, ease);
	}
}
