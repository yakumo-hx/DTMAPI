using NodeCanvas.Framework;

namespace DolocTown.NodeCanvas;

public abstract class DialogueTask : ActionTask
{
	public abstract string taskTitle { get; }

	protected sealed override string info => taskTitle;

	public virtual void DoAction(Graph graph)
	{
	}
}
