using System.Collections.Generic;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

public class AutomateParamFarming : AutomateParam
{
	[JsonProperty]
	private bool autoPlant;

	[JsonProperty]
	private bool autoFertilizer;

	[JsonProperty]
	private bool autoProtect;

	[JsonProperty]
	private bool autoWatering;

	[JsonProperty]
	private float wateringThreshold;

	[JsonProperty]
	private bool filterContainerSkin;

	[JsonProperty]
	private HashSet<int> containerSkinSet = new HashSet<int>();

	[DebugInfo("自动种植", Color = "#ff7f4f", AllowEdit = true)]
	public bool AutoPlant
	{
		get
		{
			return autoPlant;
		}
		set
		{
			autoPlant = value;
		}
	}

	[DebugInfo("自动施肥", Color = "#ff7f4f", AllowEdit = true)]
	public bool AutoFertilizer
	{
		get
		{
			return autoFertilizer;
		}
		set
		{
			autoFertilizer = value;
		}
	}

	[DebugInfo("自动使用塑料薄膜", Color = "#ff7f4f", AllowEdit = true)]
	public bool AutoProtect
	{
		get
		{
			return autoProtect;
		}
		set
		{
			autoProtect = value;
		}
	}

	[DebugInfo("自动浇水", Color = "#ff7f4f", AllowEdit = true)]
	public bool AutoWatering
	{
		get
		{
			return autoWatering;
		}
		set
		{
			autoWatering = value;
		}
	}

	[DebugInfo("自动浇水阈值", AllowEdit = true)]
	public float WateringThreshold
	{
		get
		{
			return wateringThreshold;
		}
		set
		{
			wateringThreshold = value;
		}
	}

	[DebugInfo("是否过滤容器皮肤", Color = "#ff7f4f", AllowEdit = true)]
	public bool FilterContainerSkin
	{
		get
		{
			return filterContainerSkin;
		}
		set
		{
			filterContainerSkin = value;
		}
	}

	[DebugInfo("容器皮肤ID列表", Color = "#ff7f4f")]
	public HashSet<int> ContainerSkinSet
	{
		get
		{
			return containerSkinSet;
		}
		set
		{
			containerSkinSet = value;
		}
	}

	public override void LoadDefault()
	{
		autoPlant = true;
		autoFertilizer = true;
		autoProtect = true;
		autoWatering = true;
		wateringThreshold = 0.7f;
		filterContainerSkin = false;
		containerSkinSet = new HashSet<int>();
	}

	public AutomateParamFarming(AutomateBot bot)
		: base(bot)
	{
	}

	[JsonConstructor]
	private AutomateParamFarming(bool autoPlant, bool autoFertilizer, bool autoProtect, bool autoWatering, float wateringThreshold, bool filterContainerSkin, HashSet<int> containerSkinSet)
	{
		this.autoPlant = autoPlant;
		this.autoFertilizer = autoFertilizer;
		this.autoProtect = autoProtect;
		this.autoWatering = autoWatering;
		this.wateringThreshold = wateringThreshold;
		this.filterContainerSkin = filterContainerSkin;
		this.containerSkinSet = containerSkinSet ?? new HashSet<int>();
	}
}
