using DolocTown.Config.Fishing;

namespace DolocTown.UI;

public struct CreatureDetailData : IUIData
{
	public bool notEmpty { get; }

	public bool isVisited { get; }

	public bool display { get; }

	public bool isAnimal { get; }

	public AnimalDetailData animalData { get; }

	public FishDetailData fishData { get; }

	public string documentContent { get; }

	public CreatureDetailData(string creatureId)
	{
		this = default(CreatureDetailData);
		FishDocumentInfo document2;
		if (DolocAPI.QueryAnimalDocument(creatureId, out var _))
		{
			notEmpty = true;
			isAnimal = true;
			animalData = new AnimalDetailData(creatureId);
			isVisited = animalData.isVisited;
			display = animalData.display;
			documentContent = animalData.documentContent;
		}
		else if (DolocAPI.QueryFishDocument(creatureId, out document2))
		{
			notEmpty = true;
			isAnimal = false;
			fishData = new FishDetailData(creatureId);
			isVisited = fishData.isVisited;
			display = fishData.display;
			documentContent = fishData.documentContent;
		}
	}
}
