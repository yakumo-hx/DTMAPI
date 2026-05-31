using UnityEngine;

namespace DolocTown.UI;

public class DialogueHistoryPanel : DolocUIPanel
{
	[SerializeField]
	private DialogueHistoryList dialogueHistoryList;

	public IScrollContentRect scrollContentRect => dialogueHistoryList;

	protected override void __Init()
	{
		base.__Init();
		dialogueHistoryList.Init();
	}

	public void Render(DialogueBlockHistoryData[] data)
	{
		dialogueHistoryList.RenderSlots(data);
	}
}
