using System.Collections.Generic;
using DolocTown.Config;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class CharacterDocumentManager
{
	[JsonProperty]
	public List<string> unlockedDocuments { get; private set; }

	public CharacterDocumentManager()
	{
		unlockedDocuments = new List<string>();
	}

	[JsonConstructor]
	private CharacterDocumentManager(List<string> unlockedDocuments)
	{
		this.unlockedDocuments = unlockedDocuments;
	}

	public bool UnLockNpcDocument(string docId)
	{
		if (!DolocConfig.Tables.TbCharacterDocument.DataMap.TryGetValue(docId, out var _))
		{
			return false;
		}
		if (unlockedDocuments.Contains(docId))
		{
			return false;
		}
		unlockedDocuments.Add(docId);
		return true;
	}

	public bool GetDocumentUnlockState(string docId)
	{
		return unlockedDocuments.Contains(docId);
	}
}
