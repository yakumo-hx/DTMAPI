namespace DolocTown;

public abstract class FishingGame
{
	protected readonly DolocUserInput userInput;

	public abstract FishingGameController.GameStatus CurrentGameStatus { get; }

	protected FishingGame(DolocUserInput userInput)
	{
		this.userInput = userInput;
	}

	public virtual void OnStart()
	{
	}

	public virtual void OnUpdate(float dt)
	{
	}

	public virtual void OnFixedUpdate(float dt)
	{
	}

	public virtual void OnEnd()
	{
	}
}
