using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DolocTown.Config.Animal;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class AnimalManager
{
	[JsonProperty]
	private readonly IndexList<Animal> animals = new IndexList<Animal>();

	public IEnumerable<Animal> AllAnimals => animals;

	public AnimalManager()
	{
	}

	[JsonConstructor]
	protected AnimalManager(IndexList<Animal> animals)
	{
		this.animals = animals ?? new IndexList<Animal>();
	}

	public void AddAnimal(Animal animal)
	{
		if (animal != null && !animals.Contains(animal))
		{
			animals.Add(animal);
		}
	}

	public bool RemoveAnimal([NotNull] Animal animal)
	{
		return animals.Remove(animal);
	}

	public bool ContainsAnimal(Animal animal)
	{
		return animals.Contains(animal);
	}

	public Animal CreateAnimal(AnimalInfo proto, DateInfo now)
	{
		if (proto == null)
		{
			return null;
		}
		Animal animal = new Animal(proto, now);
		animals.Add(animal);
		return animal;
	}

	public int GetAnimalCount(string animaName)
	{
		if (!animaName.IsNullOrEmpty())
		{
			return animals.Count((Animal x) => x.protoName == animaName);
		}
		return 0;
	}
}
