using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.NPC;
using Newtonsoft.Json;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
public class CityArchiveData : IDataPersistence
{
	[JsonProperty]
	public readonly Dictionary<string, CityRoom> cityRooms = new Dictionary<string, CityRoom>();

	[JsonProperty]
	public readonly NpcManager npcManager;

	[JsonProperty]
	public readonly DialogueManager dialogueManager;

	[JsonProperty]
	public readonly StoreManager storeManager;

	[JsonProperty]
	public readonly DocumentManager documentManager;

	[JsonProperty]
	public readonly GlobalInteractableObjectManager globalInteractableObjectManager;

	[JsonProperty]
	public readonly BoardMissionManager boardMissionManager;

	[JsonProperty]
	public readonly TreatyPortFactionManager treatyPortFactionManager;

	[JsonProperty]
	public readonly FactionMissionManager factionMissionManager;

	[JsonProperty]
	public readonly GateManager gateManager;

	[JsonProperty]
	public readonly LikingManager likingManager;

	[JsonProperty]
	public readonly CalendarManager calendarManager;

	public CityArchiveData()
	{
		foreach (RoomProto allRoom in DolocAPI.assets.cityRooms.AllRooms)
		{
			cityRooms.Add(allRoom.name, new CityRoom(allRoom));
		}
		npcManager = new NpcManager();
		foreach (NpcInfo data in DolocConfig.Tables.TbNpc.DataList)
		{
			if (data.ShouldPreload)
			{
				npcManager.AddNpc(new Npc(data));
			}
		}
		dialogueManager = new DialogueManager();
		storeManager = new StoreManager();
		documentManager = new DocumentManager();
		treatyPortFactionManager = new TreatyPortFactionManager();
		factionMissionManager = new FactionMissionManager();
		globalInteractableObjectManager = new GlobalInteractableObjectManager();
		boardMissionManager = new BoardMissionManager();
		gateManager = new GateManager();
		likingManager = new LikingManager();
		calendarManager = new CalendarManager();
	}

	[JsonConstructor]
	public CityArchiveData(Dictionary<string, CityRoom> cityRooms, NpcManager npcManager, DialogueManager dialogueManager, StoreManager storeManager, DocumentManager documentManager, GlobalInteractableObjectManager globalInteractableObjectManager, TreatyPortFactionManager treatyPortFactionManager, FactionMissionManager factionMissionManager, GateManager gateManager, BoardMissionManager boardMissionManager, LikingManager likingManager, CalendarManager calendarManager)
	{
		this.cityRooms = cityRooms;
		VerifyCityRooms();
		this.npcManager = npcManager;
		this.dialogueManager = dialogueManager;
		this.likingManager = likingManager;
		this.storeManager = storeManager;
		this.documentManager = documentManager;
		this.treatyPortFactionManager = treatyPortFactionManager ?? new TreatyPortFactionManager();
		this.factionMissionManager = factionMissionManager;
		this.globalInteractableObjectManager = globalInteractableObjectManager;
		this.gateManager = gateManager ?? new GateManager();
		this.boardMissionManager = boardMissionManager;
		this.calendarManager = calendarManager ?? new CalendarManager();
	}

	private void VerifyCityRooms()
	{
		foreach (RoomProto allRoom in DolocAPI.assets.cityRooms.AllRooms)
		{
			if (!cityRooms.ContainsKey(allRoom.name))
			{
				cityRooms.Add(allRoom.name, new CityRoom(allRoom));
			}
		}
	}

	private void ClearInvalidRooms()
	{
		Dictionary<string, CityRoom> dictionary = new Dictionary<string, CityRoom>();
		foreach (CityRoom value in cityRooms.Values)
		{
			if (value.proto != null)
			{
				dictionary.Add(value.proto.name, value);
			}
		}
		cityRooms.Clear();
		foreach (KeyValuePair<string, CityRoom> item in dictionary)
		{
			cityRooms.Add(item.Key, item.Value);
		}
	}

	public void AfterNewGame(ref ArchiveDataHandle data)
	{
		foreach (CityRoom value in cityRooms.Values)
		{
			value.__AfterNewGame();
		}
	}

	public void AfterLoadData(ref ArchiveDataHandle data)
	{
		ClearInvalidRooms();
		foreach (CityRoom value in cityRooms.Values)
		{
			value.__AfterLoadData();
		}
		storeManager.AfterLoadData();
	}
}
