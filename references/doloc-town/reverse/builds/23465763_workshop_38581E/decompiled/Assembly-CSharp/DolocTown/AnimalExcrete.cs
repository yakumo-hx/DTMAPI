using DolocTown.GameData;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public class AnimalExcrete : AnimalTask_HandleInteractable
{
	private readonly IAnimalToilet _toilet;

	private bool _raiseEmotion;

	public AnimalExcrete(IAnimalToilet toilet)
		: base(toilet)
	{
		_toilet = toilet;
	}

	public override TaskStatus OnExecute(float dt)
	{
		if (_toilet.IsToiletFull || !animal.Excrete())
		{
			if (animal.IsRender)
			{
				DolocAPI.RaiseEmotion(animal.Renderer.transform, EmotionName.ANGRY);
			}
			return TaskStatus.Failure;
		}
		_toilet.Excrete();
		RaiseEmotion();
		if (animal.IsRender)
		{
			DolocAPI.RaiseInstantAnimEffects(animal.Renderer.position2d, InstAnimEffectType.PLAYER_LAND_SMOKE);
		}
		return TaskStatus.Success;
	}

	private void RaiseEmotion()
	{
		if (animal.IsRender && !_raiseEmotion && Random.value < 0.3f)
		{
			_raiseEmotion = true;
			DolocAPI.RaiseEmotionLimited(animal.Renderer.transform, EmotionName.LAUGH);
		}
	}
}
