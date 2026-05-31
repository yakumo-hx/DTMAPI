using UnityEngine;

namespace DolocTown.UI;

public struct EnvOptimizerBranchData : IUIData
{
	public bool notEmpty { get; }

	public string title { get; }

	public int currentScore { get; }

	public int totalScore { get; }

	public float progress { get; }

	public string progressText { get; }

	public string description { get; }

	public string comment { get; }

	public EnvOptimizerBranchData(EnvOptimizerBranch data)
	{
		this = default(EnvOptimizerBranchData);
		if (data != null)
		{
			notEmpty = true;
			title = data.branchProto.Title;
			currentScore = data.Count;
			totalScore = data.Limitation;
			progress = data.Process;
			progressText = currentScore.ToString();
			if (Mathf.Approximately(progress, 1f))
			{
				progressText = progressText.Colored(DolocUiColor.EYECATCHCOLOR_CYAN);
			}
			description = data.branchProto.Description;
			comment = data.branchProto.Comment;
		}
	}
}
