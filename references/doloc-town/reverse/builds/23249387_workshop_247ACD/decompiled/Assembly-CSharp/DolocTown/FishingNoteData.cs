namespace DolocTown;

public class FishingNoteData
{
	public readonly int index;

	public readonly FishingNoteType noteType;

	public readonly float startTime;

	public readonly float endTime;

	public readonly float scrollSpeed;

	public readonly float widthMultiplier;

	public FishingNoteData(int index, FishingNoteType noteType, float startTime, float endTime, float speedMultiplier)
	{
		this.index = index;
		this.noteType = noteType;
		this.startTime = startTime;
		this.endTime = endTime;
		scrollSpeed = (float)((noteType != FishingNoteType.Bonus) ? DolocAPI.GlobalParameter.FishingNoteScrollSpeedBase : DolocAPI.GlobalParameter.FishingNoteScrollSpeedBonus) * speedMultiplier * 4f;
		widthMultiplier = (float)DolocAPI.GlobalParameter.FishingNoteScrollSpeedBase * 4f * speedMultiplier;
	}
}
