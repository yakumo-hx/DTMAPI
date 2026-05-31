using System.Collections.Generic;

namespace DolocTown;

public class CollectionRecord
{
	public bool isRead;

	public bool isUnlock;

	public HashSet<string> documents;

	public CollectionRecord()
	{
		isRead = false;
		isUnlock = false;
		documents = new HashSet<string>();
	}

	public void RefreshRecord(string documentId)
	{
		documents.Add(documentId);
		isRead = false;
	}
}
