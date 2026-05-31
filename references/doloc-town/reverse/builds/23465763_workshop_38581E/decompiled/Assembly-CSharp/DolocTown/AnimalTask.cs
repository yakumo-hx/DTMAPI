using RedSaw.AI.LinearTask;

namespace DolocTown;

public abstract class AnimalTask : LinearTask
{
	protected Animal animal;

	public void SetAnimal(Animal animal)
	{
		this.animal = animal;
		OnSetAnimal(animal);
	}

	protected virtual void OnSetAnimal(Animal animal)
	{
	}
}
