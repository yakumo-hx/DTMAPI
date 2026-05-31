using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DecalInfo
{
	[JsonProperty]
	public string DecalHostType;

	[JsonProperty]
	public int DecalHostIndex;

	[JsonProperty]
	public int DecalSlotIndex;

	[JsonConstructor]
	public DecalInfo(string DecalHostType, int DecalHostIndex, int DecalSlotIndex)
	{
		this.DecalHostType = DecalHostType;
		this.DecalHostIndex = DecalHostIndex;
		this.DecalSlotIndex = DecalSlotIndex;
	}
}
