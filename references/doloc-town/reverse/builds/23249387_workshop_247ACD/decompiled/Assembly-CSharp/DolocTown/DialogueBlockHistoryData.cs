using System.Collections.Generic;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DialogueBlockHistoryData
{
	[JsonProperty]
	public readonly string l10nId;

	[JsonProperty]
	public List<DialogueLineHistoryData> dataList;

	public int dataCount => dataList.Count;

	[JsonConstructor]
	public DialogueBlockHistoryData(string l10nId, List<DialogueLineHistoryData> dataList)
	{
		this.dataList = dataList;
		this.l10nId = l10nId;
	}
}
