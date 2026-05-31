using DolocTown.Config;
using DolocTown.Config.Dialogue;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DialogueLineHistoryData
{
	[JsonProperty]
	private readonly string dialogueEntityId;

	[JsonProperty]
	public readonly string speakerTitle;

	[JsonProperty]
	public readonly DialogueHistoryType historyType;

	[JsonProperty]
	public readonly string content;

	public readonly DialogueEntityInfo dialogueEntityProto;

	public DialogueLineHistoryData(string dialogueEntityId, string speakerTitle, string content, DialogueHistoryType historyType)
	{
		this.dialogueEntityId = dialogueEntityId;
		this.speakerTitle = speakerTitle;
		this.content = content.ClearMarkUp().ClearRichTextLabel().Trim();
		this.historyType = historyType;
		dialogueEntityProto = DolocConfig.Tables.TbDialogueEntity.GetOrDefault(dialogueEntityId);
	}
}
