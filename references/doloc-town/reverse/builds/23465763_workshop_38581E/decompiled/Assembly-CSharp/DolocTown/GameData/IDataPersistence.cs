namespace DolocTown.GameData;

public interface IDataPersistence
{
	void BeforeNewGame()
	{
	}

	void AfterNewGame(ref ArchiveDataHandle data)
	{
	}

	void BeforeLoadData(ref ArchiveDataHandle data)
	{
	}

	void AfterLoadData(ref ArchiveDataHandle data)
	{
	}

	void BeforeSaveData(ref ArchiveDataHandle data)
	{
	}

	void AfterSaveData(ref ArchiveDataHandle data)
	{
	}
}
