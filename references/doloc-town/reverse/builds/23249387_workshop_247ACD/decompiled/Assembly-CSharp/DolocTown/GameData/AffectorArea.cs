namespace DolocTown.GameData;

public class AffectorArea
{
	public int horizontalRange;

	public int verticalRangeTop;

	public int verticalRangeBottom;

	public AffectorArea(int horizontalRange, int verticalRangeTop, int verticalRangeBottom)
	{
		this.horizontalRange = horizontalRange;
		this.verticalRangeTop = verticalRangeTop;
		this.verticalRangeBottom = verticalRangeBottom;
	}
}
