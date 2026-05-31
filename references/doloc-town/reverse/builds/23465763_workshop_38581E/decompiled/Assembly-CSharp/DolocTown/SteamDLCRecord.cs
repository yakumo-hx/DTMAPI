using System;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public struct SteamDLCRecord
{
	public readonly uint appId;

	public readonly Version gameVersion;

	public readonly string script;

	public readonly DateTime timeStamp;

	public SteamDLCRecord(uint appId, Version version, string script)
	{
		this.appId = appId;
		gameVersion = version;
		this.script = script;
		timeStamp = DateTime.Now;
	}

	public bool CompareVersion(Version currentVersion)
	{
		return currentVersion.CompareTo(gameVersion) > 0;
	}
}
