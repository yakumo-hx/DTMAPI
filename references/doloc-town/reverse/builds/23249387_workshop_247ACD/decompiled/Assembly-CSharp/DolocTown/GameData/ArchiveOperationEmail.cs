namespace DolocTown.GameData;

public static class ArchiveOperationEmail
{
	public static Email[] GetEmails(this ArchiveDataHandle handle)
	{
		return handle.farmData.emailManager.emails.ToArray();
	}

	public static bool ContainsEmailName(this ArchiveDataHandle handle, string name)
	{
		return handle.farmData.emailManager.ContainsEmailName(name);
	}

	public static bool HasNewEmail(this ArchiveDataHandle handle)
	{
		return handle.farmData.emailManager.HasNewEmail();
	}
}
