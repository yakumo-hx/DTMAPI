using DolocTown.GameData;
using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AnimalEmotion : AnimalTask
{
	private readonly EmotionName emotionName;

	public AnimalEmotion(EmotionName name)
	{
		emotionName = name;
	}

	public override TaskStatus OnExecute(float dt)
	{
		if (!animal.IsRender)
		{
			return TaskStatus.Success;
		}
		DolocAPI.RaiseEmotion(animal.Renderer.transform, emotionName);
		return TaskStatus.Success;
	}
}
