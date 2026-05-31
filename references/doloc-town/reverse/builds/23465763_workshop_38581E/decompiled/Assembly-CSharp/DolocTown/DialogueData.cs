using System.Linq;
using Newtonsoft.Json;
using Yarn.Compiler.Upgrader;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DialogueData
{
	[JsonProperty]
	public readonly string npcName;

	[JsonProperty]
	private readonly OrderedSet<string> candidateNodes;

	[JsonProperty]
	private string entrance { get; set; }

	public string overrideEntrance { get; set; }

	public bool useCandidateNodes => overrideEntrance.IsNullOrEmpty();

	private DialogueManager dialogueManager => DolocAPI.archiveHandle.cityData.dialogueManager;

	public int candidateCount => allCandidateNodes.Length;

	public string[] allCandidateNodes => candidateNodes.ToArray();

	public bool HasDialogueNode
	{
		get
		{
			if (string.IsNullOrEmpty(GetEntrance()))
			{
				return candidateCount > 0;
			}
			return true;
		}
	}

	[JsonConstructor]
	public DialogueData(string npcName, string entrance = null, OrderedSet<string> candidateNodes = null)
	{
		this.npcName = npcName;
		this.entrance = entrance;
		this.candidateNodes = candidateNodes ?? new OrderedSet<string>();
	}

	public string GetEntrance()
	{
		if (!overrideEntrance.IsNullOrEmpty())
		{
			return overrideEntrance;
		}
		return entrance;
	}

	public void SetEntrance(string nodeName)
	{
		entrance = nodeName;
	}

	public void SetOverrideEntrance(string nodeName)
	{
		overrideEntrance = nodeName;
	}

	public bool AddDialogueNode(string nodeName)
	{
		return candidateNodes.Add(nodeName);
	}

	public bool RemoveDialogueNode(string nodeName)
	{
		return candidateNodes.Remove(nodeName);
	}

	public bool Contains(string nodeName)
	{
		return candidateNodes.Contains(nodeName);
	}
}
