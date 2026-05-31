namespace DolocTown.UI;

public struct DocumentData : IUIData
{
	public string title;

	public string author;

	public string content;

	public bool notEmpty { get; }

	public DocumentData(string title, string author, string content)
	{
		notEmpty = !string.IsNullOrEmpty(title);
		this.title = title;
		this.author = author;
		this.content = content;
	}
}
