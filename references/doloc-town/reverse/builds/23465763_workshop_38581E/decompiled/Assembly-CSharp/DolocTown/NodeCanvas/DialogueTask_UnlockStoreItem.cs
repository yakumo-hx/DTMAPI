using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/成就")]
[Name("解锁商品", 0)]
[Description("解锁商店中的道具")]
public class DialogueTask_UnlockStoreItem : DialogueTask
{
	[SerializeField]
	[RequiredField]
	private string storeName;

	[SerializeField]
	[RequiredField]
	private string itemName;

	public override string taskTitle => "解锁商品" + storeName + "@" + itemName;

	public override void DoAction(Graph graph)
	{
		DolocAPI.UnlockStoreItem(storeName, itemName);
	}
}
