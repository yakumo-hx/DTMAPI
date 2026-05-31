using NodeCanvas.Framework;

namespace DolocTown.NodeCanvas;

public abstract class DialogueConditionTask : ConditionTask
{
	public abstract string taskTitle { get; }

	protected sealed override string info => taskTitle;

	public virtual bool IsConditionMet
	{
		get
		{
			if (base.invert)
			{
				return !CheckCondition();
			}
			return CheckCondition();
		}
	}

	protected abstract bool CheckCondition();
}
