using Newtonsoft.Json;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
public class DungeonArchiveData : IDataPersistence
{
	public Dungeon currentDungeon;

	[JsonProperty]
	public readonly DungeonManager dungeonManager;

	[JsonProperty]
	public bool trainFlag;

	[JsonProperty]
	public readonly ResourceManager resourceManager;

	[JsonProperty]
	public readonly GuaranteedManager guaranteedManager;

	public DungeonArchiveData()
	{
		dungeonManager = new DungeonManager();
		foreach (DungeonProto totalDungeon in DolocAPI.assets.dungeons.totalDungeons)
		{
			dungeonManager.CreateDungeon(totalDungeon);
		}
		resourceManager = new ResourceManager();
		guaranteedManager = new GuaranteedManager();
		trainFlag = true;
	}

	public void __ReGenDungeonDatas()
	{
		DolocAPI.outputSuccess("正在刷新地牢的所有数据..");
		int num = 0;
		foreach (Dungeon totalDungeon in dungeonManager.totalDungeons)
		{
			num += totalDungeon.__ReGenDungeonDatas();
		}
		DolocAPI.outputSuccess($"刷新完成，共刷新{num}个房间的数据");
	}

	[JsonConstructor]
	public DungeonArchiveData(DungeonManager dungeonManager, ResourceManager resourceManager, GuaranteedManager guaranteedManager, bool trainFlag = true)
	{
		this.dungeonManager = dungeonManager;
		this.trainFlag = trainFlag;
		this.resourceManager = resourceManager ?? new ResourceManager();
		this.guaranteedManager = guaranteedManager ?? new GuaranteedManager();
	}

	public void AfterNewGame(ref ArchiveDataHandle data)
	{
		dungeonManager.AfterNewGame();
	}

	public void AfterLoadData(ref ArchiveDataHandle data)
	{
		dungeonManager.AfterLoadData();
	}
}
