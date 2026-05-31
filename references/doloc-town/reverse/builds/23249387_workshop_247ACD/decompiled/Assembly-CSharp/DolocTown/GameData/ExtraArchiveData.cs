using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
public class ExtraArchiveData : IDataPersistence
{
	[JsonProperty]
	public AchievementSystem achievementSystem;

	[JsonProperty]
	public readonly Dictionary<uint, SteamDLCRecord> steamDLCRecords;

	[JsonConstructor]
	public ExtraArchiveData(AchievementSystem achievementSystem = null, Dictionary<uint, SteamDLCRecord> steamDLCRecords = null)
	{
		this.achievementSystem = achievementSystem ?? new AchievementSystem();
		this.steamDLCRecords = steamDLCRecords ?? new Dictionary<uint, SteamDLCRecord>();
	}

	public void AfterNewGame(ref ArchiveDataHandle data)
	{
		DolocAPI.archiveHandle.farmData.missionManager.AddMissionManagerComponent(achievementSystem);
	}

	public void AfterLoadData(ref ArchiveDataHandle data)
	{
		DolocAPI.archiveHandle.farmData.missionManager.AddMissionManagerComponent(achievementSystem);
		achievementSystem?.AfterLoadData();
		DolocAPI.archiveHandle.farmData.missionManager._ClearAllCompletedMissions();
		DolocAPI.archiveHandle.farmData.missionManager._ClearCompletedMissions(achievementSystem?.LockedAchievements.Select((IMission x) => x.Id));
	}
}
