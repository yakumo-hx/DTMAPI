using DolocTown.Config.Fishing;

namespace DolocTown;

public abstract class FishingGameController
{
	public enum GameStatus
	{
		Running,
		Success,
		Failed
	}

	public abstract GameStatus CurrentGameStatus { get; }

	public abstract bool IsValid { get; protected set; }

	public abstract void StartGame(ItemFishingRod fishingRod, FishInfo fishProto);

	public abstract void UpdateGame(float dt);

	public abstract void FixedUpdateGame(float dt);

	public abstract void StopGame();
}
