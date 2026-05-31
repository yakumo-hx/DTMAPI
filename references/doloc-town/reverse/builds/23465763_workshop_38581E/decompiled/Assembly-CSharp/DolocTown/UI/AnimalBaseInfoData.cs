using DolocTown.Config;

namespace DolocTown.UI;

public class AnimalBaseInfoData : IUIData
{
	public string title;

	public string ageInfo;

	public float moodProgress;

	public float energyProgress;

	public string stateBaseInfo;

	public bool notEmpty { get; protected set; }

	public AnimalBaseInfoData(Animal animal)
	{
		if (animal != null)
		{
			notEmpty = true;
			title = animal.Title;
			ageInfo = animal.AgeInfo;
			energyProgress = animal.EnergyProcess;
			moodProgress = animal.MoodProcess;
			stateBaseInfo = DolocConfig.Tables.TbAnimalState.GetOrDefault(animal.CurrentState)?.Title ?? "";
		}
	}
}
