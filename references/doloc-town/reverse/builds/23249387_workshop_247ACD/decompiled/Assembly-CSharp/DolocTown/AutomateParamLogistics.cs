using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;

namespace DolocTown;

public class AutomateParamLogistics : AutomateParam
{
	[JsonProperty]
	[DebugInfo("是否指定输出容器色彩")]
	private bool filterOutputSkinIdx;

	[JsonProperty]
	[DebugInfo("输出容器色彩ID")]
	private HashSet<int> outputSkinIdxSet;

	[JsonProperty]
	[DebugInfo("是否指定输出容器标签")]
	private bool filterOutputLabel;

	[JsonProperty]
	[DebugInfo("输出容器标签")]
	private HashSet<string> outputLabelSet;

	[JsonProperty]
	[DebugInfo("是否指定输出容器色彩")]
	private bool filterInputSkinIdx;

	[JsonProperty]
	[DebugInfo("输出容器色彩ID")]
	private HashSet<int> inputSkinIdxSet;

	[JsonProperty]
	[DebugInfo("是否指定输出容器标签")]
	private bool filterInputLabel;

	[JsonProperty]
	[DebugInfo("输出容器标签")]
	private HashSet<string> inputLabelSet;

	public AutomateParamLogistics(AutomateBot bot)
		: base(bot)
	{
	}

	[JsonConstructor]
	public AutomateParamLogistics(bool filterOutputSkinIdx, HashSet<int> outputSkinIdxSet, bool filterOutputLabel, HashSet<string> outputLabelSet, bool filterInputSkinIdx, HashSet<int> inputSkinIdxSet, bool filterInputLabel, HashSet<string> inputLabelSet)
	{
		this.filterOutputSkinIdx = filterOutputSkinIdx;
		this.outputSkinIdxSet = outputSkinIdxSet ?? new HashSet<int>();
		this.filterOutputLabel = filterOutputLabel;
		this.outputLabelSet = outputLabelSet ?? new HashSet<string>();
		this.filterInputSkinIdx = filterInputSkinIdx;
		this.inputSkinIdxSet = inputSkinIdxSet ?? new HashSet<int>();
		this.filterInputLabel = filterInputLabel;
		this.inputLabelSet = inputLabelSet ?? new HashSet<string>();
	}

	public override void LoadDefault()
	{
		filterInputLabel = false;
		inputLabelSet = new HashSet<string>();
		filterInputSkinIdx = false;
		inputSkinIdxSet = new HashSet<int>();
		filterOutputLabel = false;
		outputLabelSet = new HashSet<string>();
		filterOutputSkinIdx = false;
		outputSkinIdxSet = new HashSet<int>();
	}

	public bool MatchInput(Case container)
	{
		if (container.isOutput || !container.isInput)
		{
			return false;
		}
		if (filterInputLabel && !inputLabelSet.HasIntersectionWith(container.labels))
		{
			return false;
		}
		if (filterInputSkinIdx && !inputSkinIdxSet.Contains(container.skinIndex))
		{
			return false;
		}
		return !container.inventory.isRealFull;
	}

	public bool MatchOutput(Case container)
	{
		if (container.isInput || !container.isOutput)
		{
			return false;
		}
		if (filterOutputLabel && !outputLabelSet.HasIntersectionWith(container.labels))
		{
			return false;
		}
		if (filterOutputSkinIdx && !outputSkinIdxSet.Contains(container.skinIndex))
		{
			return false;
		}
		if (container.inventory.isEmpty)
		{
			return false;
		}
		return container.inventory.ReadAll().Any((Item x) => x.disposable);
	}

	public (Case[], Case[]) FilterCases(IEnumerable<Case> cases)
	{
		List<Case> list = new List<Case>();
		List<Case> list2 = new List<Case>();
		foreach (Case @case in cases)
		{
			if (MatchInput(@case))
			{
				list2.Add(@case);
			}
			else if (MatchOutput(@case))
			{
				list.Add(@case);
			}
		}
		return (list.ToArray(), list2.ToArray());
	}
}
