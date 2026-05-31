using DolocTown.Config;
using DolocTown.Config.Localization;
using NodeCanvas.Framework;

namespace DolocTown.NodeCanvas;

public abstract class DialogueTextTask : Task
{
	public abstract string taskTitle { get; }

	protected sealed override string info => taskTitle;

	protected TbStaticText staticTexts => DolocConfig.StaticTexts;

	public abstract string[] GetTexts();
}
