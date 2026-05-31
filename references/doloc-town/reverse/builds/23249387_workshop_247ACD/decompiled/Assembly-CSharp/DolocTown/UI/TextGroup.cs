namespace DolocTown.UI;

public struct TextGroup : IUIData
{
	public string title;

	public string info;

	public string content;

	public bool notEmpty
	{
		get
		{
			if (title.IsNullOrEmpty() && info.IsNullOrEmpty())
			{
				return !content.IsNullOrEmpty();
			}
			return true;
		}
	}

	public TextGroup(string title, string info, string content)
	{
		this.title = title;
		this.info = info;
		this.content = content;
	}
}
