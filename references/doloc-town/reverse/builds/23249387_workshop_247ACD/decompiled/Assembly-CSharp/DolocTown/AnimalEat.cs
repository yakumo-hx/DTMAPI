using DolocTown.GameData;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public class AnimalEat : AnimalTask_HandleInteractable
{
	private readonly IFeeder feeder;

	private bool raiseEmotion;

	private bool isEatingNow;

	private bool playAnimation;

	public AnimalEat(IFeeder feeder, bool playAnimation)
		: base(feeder)
	{
		this.feeder = feeder;
		this.playAnimation = playAnimation;
	}

	public override TaskStatus OnExecute(float dt)
	{
		if (isEatingNow)
		{
			isEatingNow = false;
			return TaskStatus.Executing;
		}
		string name;
		int num = feeder.TakeFeeds(animal.proto.EatCount, out name);
		if (num == 0)
		{
			if (animal.IsRender)
			{
				DolocAPI.RaiseEmotion(animal.Renderer.transform, EmotionName.CONFUSE);
			}
			return TaskStatus.Failure;
		}
		animal.Face(feeder.AnimalInteractablePositionWS);
		animal.Eat(num, name, playAnimation);
		isEatingNow = true;
		RaiseEmotion();
		if (!animal.IsHungry)
		{
			return TaskStatus.Success;
		}
		return TaskStatus.Executing;
	}

	private void RaiseEmotion()
	{
		if (animal.IsRender && !raiseEmotion && Random.value < 0.2f)
		{
			raiseEmotion = true;
			DolocAPI.RaiseEmotionLimited(animal.Renderer.transform, EmotionName.LOVE);
		}
	}
}
