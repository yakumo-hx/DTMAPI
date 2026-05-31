using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class MissionChainInfo
{
	[JsonProperty]
	private readonly HashSet<string> missionIds;

	public MissionChainInfo(IEnumerable<string> missionIds)
	{
		this.missionIds = new HashSet<string>(missionIds ?? Array.Empty<string>());
	}

	[JsonConstructor]
	public MissionChainInfo(HashSet<string> missionIds)
	{
		this.missionIds = missionIds;
	}

	public bool ContainsMission(string id)
	{
		if (id.IsNullOrEmpty())
		{
			return false;
		}
		return missionIds.Contains(id);
	}
}
