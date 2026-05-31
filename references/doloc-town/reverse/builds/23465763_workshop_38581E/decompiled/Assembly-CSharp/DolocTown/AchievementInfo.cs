namespace DolocTown;

public struct AchievementInfo
{
	public readonly string id;

	public bool isUnlocked;

	public AchievementInfo(string id, bool isUnlocked)
	{
		this.id = id;
		this.isUnlocked = isUnlocked;
	}
}
