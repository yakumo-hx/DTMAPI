namespace DolocTown;

public abstract class AnimalTask_HandleInteractable : AnimalTask
{
	private readonly IAnimalInteractable _interactable;

	protected AnimalTask_HandleInteractable(IAnimalInteractable interactable)
	{
		_interactable = interactable;
	}

	public override void OnBegin()
	{
		base.OnBegin();
		_interactable.AnimalCounter++;
	}

	public override void OnBreak()
	{
		base.OnBreak();
		_interactable.AnimalCounter--;
	}

	public override void OnFailure()
	{
		base.OnFailure();
		_interactable.AnimalCounter--;
	}

	public override void OnSuccess()
	{
		base.OnSuccess();
		_interactable.AnimalCounter--;
	}
}
