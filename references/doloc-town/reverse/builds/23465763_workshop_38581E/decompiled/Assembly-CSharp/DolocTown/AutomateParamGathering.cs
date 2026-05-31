using System.Collections.Generic;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

public class AutomateParamGathering : AutomateParam
{
	[JsonProperty]
	[DebugInfo("是否指定放置容器的色彩")]
	private bool filterContainerSkin;

	[JsonProperty]
	[DebugInfo("容器色彩列表")]
	private HashSet<int> containerSkinSet;

	[JsonProperty]
	[DebugInfo("是否采集农作物", AllowEdit = true)]
	private bool gatherCrop;

	[JsonProperty]
	[DebugInfo("是否收集掉落物", AllowEdit = true)]
	private bool gatherDropItems;

	[JsonProperty]
	[DebugInfo("是否收集设备中的物品", AllowEdit = true)]
	private bool gatherEquipmentItems;

	public bool GatherCrop
	{
		get
		{
			return gatherCrop;
		}
		set
		{
			gatherCrop = value;
		}
	}

	public bool GatherDropItems
	{
		get
		{
			return gatherDropItems;
		}
		set
		{
			gatherDropItems = value;
		}
	}

	public bool GatherEquipmentItems
	{
		get
		{
			return gatherEquipmentItems;
		}
		set
		{
			gatherEquipmentItems = value;
		}
	}

	public AutomateParamGathering(AutomateBot bot)
		: base(bot)
	{
	}

	[JsonConstructor]
	protected AutomateParamGathering(bool filterContainerSkin, HashSet<int> containerSkinSet, bool gatherCrop, bool gatherDropItems, bool gatherEquipmentItems)
	{
		this.filterContainerSkin = filterContainerSkin;
		this.containerSkinSet = containerSkinSet ?? new HashSet<int>();
		this.gatherCrop = gatherCrop;
		this.gatherDropItems = gatherDropItems;
		this.gatherEquipmentItems = gatherEquipmentItems;
	}

	public bool FilterContainer(Case container)
	{
		if (filterContainerSkin)
		{
			return containerSkinSet.Contains(container.skinIndex);
		}
		return true;
	}

	public override void LoadDefault()
	{
		filterContainerSkin = false;
		containerSkinSet = new HashSet<int>();
		gatherCrop = true;
		gatherDropItems = true;
		gatherEquipmentItems = true;
	}
}
