namespace DolocTown.UI;

public struct OptionData
{
	public int index;

	public string type;

	public string text;

	public bool isVisited;

	public float typeOrder;

	public float optionOrder;

	public bool isExitOption => type == DolocConst.Dialogue_OptionExitType;
}
