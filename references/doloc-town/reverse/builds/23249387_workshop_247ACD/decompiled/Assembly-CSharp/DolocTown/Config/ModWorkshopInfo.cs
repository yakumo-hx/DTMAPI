using Newtonsoft.Json;

namespace DolocTown.Config;

[JsonObject(MemberSerialization.OptIn)]
public class ModWorkshopInfo
{
	[JsonProperty("workshop_id")]
	public ulong workshopId;

	public ModWorkshopInfo(ulong workshopId)
	{
		this.workshopId = workshopId;
	}
}
