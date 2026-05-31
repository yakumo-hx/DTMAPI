using UnityEngine;

namespace DolocTown.NodeCanvas;

public abstract class CustomMissionProgressFunc
{
	public abstract string GetProgressInfo();

	protected string SetColor(bool isFinished, string content)
	{
		Color color = (isFinished ? DolocUiColor.SLIENTCOLOR_GREEN : DolocUiColor.EYECATCHCOLOR_CYAN);
		return content.Colored(color);
	}
}
