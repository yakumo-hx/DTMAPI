namespace DolocTown;

public struct RichTextMarkup
{
	public readonly string name;

	public readonly string openLabel;

	public readonly string closeLabel;

	public RichTextMarkup(string name, string openLabel, string closeLabel)
	{
		this.name = name;
		this.openLabel = openLabel;
		this.closeLabel = closeLabel;
	}
}
