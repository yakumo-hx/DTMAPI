using RedSaw;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DialogueHistoryTextSlot : DolocUiRecyclableObject, IHasIndex
{
	[SerializeField]
	private Text speaker;

	[SerializeField]
	private Text content;

	public int index { get; set; }

	public bool isDeserializationValid => true;

	public void Render(DialogueLineHistoryData data)
	{
		speaker.text = data.speakerTitle;
		speaker.gameObject.SetActive(!data.speakerTitle.IsNullOrEmpty());
		content.text = data.content;
		speaker.color = data.dialogueEntityProto.SpeakerColor;
		content.color = data.dialogueEntityProto.ContentColor;
	}
}
