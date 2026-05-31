using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DocumentManager
{
	public Item[] latestSubmitItems;

	public Item[] latestOverflowItems;

	[JsonProperty]
	public ChipDocumentManager chipDocMgr { get; private set; }

	[JsonProperty]
	public PlantDocumentManager plantDocMgr { get; private set; }

	[JsonProperty]
	public CharacterDocumentManager characterDocMgr { get; private set; }

	[JsonProperty]
	public FishDocumentManager fishDocMgr { get; private set; }

	[JsonProperty]
	public bool isSynchronizePlantDoc { get; private set; }

	[JsonProperty]
	public bool isSynchronizeChipDoc { get; private set; }

	[JsonConstructor]
	public DocumentManager(ChipDocumentManager chipDocMgr = null, PlantDocumentManager plantDocMgr = null, CharacterDocumentManager characterDocMgr = null, FishDocumentManager fishDocMgr = null, bool isSynchronizePlantDoc = false, bool isSynchronizeChipDoc = false)
	{
		this.chipDocMgr = chipDocMgr ?? new ChipDocumentManager();
		this.plantDocMgr = plantDocMgr ?? new PlantDocumentManager();
		this.characterDocMgr = characterDocMgr ?? new CharacterDocumentManager();
		this.fishDocMgr = fishDocMgr ?? new FishDocumentManager();
		this.isSynchronizePlantDoc = isSynchronizePlantDoc;
		this.isSynchronizeChipDoc = isSynchronizeChipDoc;
	}

	public void SynchronizePlantDoc()
	{
		isSynchronizePlantDoc = true;
	}

	public void SynchronizeChipDoc()
	{
		isSynchronizeChipDoc = true;
	}
}
