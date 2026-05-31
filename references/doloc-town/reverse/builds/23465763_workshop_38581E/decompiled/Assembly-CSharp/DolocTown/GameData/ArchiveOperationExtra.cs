using System;

namespace DolocTown.GameData;

public static class ArchiveOperationExtra
{
	public static void RecordDLC(this ArchiveDataHandle handle, uint dlcId, Version version, string content)
	{
		SteamDLCRecord value = new SteamDLCRecord(dlcId, version, content);
		handle.extraData.steamDLCRecords[dlcId] = value;
	}
}
