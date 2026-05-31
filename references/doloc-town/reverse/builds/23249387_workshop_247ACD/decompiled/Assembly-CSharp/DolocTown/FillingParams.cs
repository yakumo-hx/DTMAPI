using System.Collections.Generic;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public class FillingParams
{
	[JsonProperty]
	[DebugInfo("填充阈值", AllowEdit = true)]
	public float threshold;

	[JsonProperty]
	[DebugInfo("填充物品类型过滤", AllowEdit = true)]
	public string itemSubType;

	[JsonProperty]
	[DebugInfo("是否过滤容器色彩", AllowEdit = true)]
	public bool filterContainerSkin;

	[JsonProperty]
	[DebugInfo("容器色彩列表")]
	public HashSet<int> containerSkinSet;

	public FillingParams(string subType, float threshold = 0.9f)
	{
		itemSubType = subType;
		this.threshold = threshold;
		filterContainerSkin = false;
		containerSkinSet = new HashSet<int>();
	}

	[JsonConstructor]
	public FillingParams(float threshold, string itemSubType, bool filterContainerSkin, HashSet<int> containerSkinSet)
	{
		this.threshold = threshold;
		this.itemSubType = itemSubType;
		this.filterContainerSkin = filterContainerSkin;
		this.containerSkinSet = containerSkinSet ?? new HashSet<int>();
	}
}
