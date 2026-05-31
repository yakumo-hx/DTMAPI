using UnityEngine;

namespace DolocTown;

public class AgentStateFishingBattle : AgentStateFishing
{
	private readonly FishingGameScrollBar gameHandle;

	private bool isReelInNow;

	public AgentStateFishingBattle(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
		gameHandle = new FishingGameScrollBar();
	}

	protected override AgentStateBase NextState()
	{
		if (!gameHandle.IsValid)
		{
			return GetState(delegate(AgentStateFishingPull s)
			{
				s.IsFailed = true;
			});
		}
		gameHandle.UpdateGame(Time.deltaTime);
		FishingGameController.GameStatus status = gameHandle.CurrentGameStatus;
		if (status == FishingGameController.GameStatus.Running)
		{
			return this;
		}
		return GetState(delegate(AgentStateFishingPull s)
		{
			s.IsFailed = status == FishingGameController.GameStatus.Failed;
		});
	}

	public override void OnPlay()
	{
		gameHandle.FixedUpdateGame(Time.fixedDeltaTime);
		HandleBattleBehaviours();
	}

	private void HandleBattleBehaviours()
	{
		if (gameHandle.ReelIn != isReelInNow)
		{
			isReelInNow = gameHandle.ReelIn;
			if (isReelInNow)
			{
				body.PlayAnimation("fishing_battle");
				body.fishRodRenderer.Play(base._fishRod.name, "fishing_battle");
			}
			else
			{
				body.PlayAnimation("fishing_wait");
				body.fishRodRenderer.Play(base._fishRod.proto.Id, "fishing_wait");
			}
		}
	}

	public override void OnEnter()
	{
		gameHandle.StartGame(body.FishingCache.FishingRod, body.FishingCache.FishProto);
	}

	public override void OnExit()
	{
		body.fishRodRenderer.HideFishShadow();
		gameHandle.StopGame();
	}
}
