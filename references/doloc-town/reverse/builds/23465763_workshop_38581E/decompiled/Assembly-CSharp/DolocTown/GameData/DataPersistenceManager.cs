using System;
using DolocTown.Config;
using UnityEngine;

namespace DolocTown.GameData;

public class DataPersistenceManager
{
	private IFileDataHandler fileDataHandler;

	public ArchiveDataHandle gameData;

	public UserSettings userSettings { get; private set; }

	public ModManager modManager { get; private set; }

	public bool IsDataLoaded { get; private set; }

	private IDataPersistence[] dataPersistences
	{
		get
		{
			if (gameData == null)
			{
				return Array.Empty<IDataPersistence>();
			}
			return new IDataPersistence[4] { gameData.cityData, gameData.dungeonData, gameData.farmData, gameData.extraData };
		}
	}

	public DataPersistenceManager()
	{
		fileDataHandler = new LocalSave();
		LoadUserSettings();
		LoadModManager();
	}

	public void NewGame(int index)
	{
		IsDataLoaded = false;
		IDataPersistence[] array = dataPersistences;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].BeforeNewGame();
		}
		gameData = new ArchiveDataHandle(index);
		array = dataPersistences;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].AfterNewGame(ref gameData);
		}
		IsDataLoaded = true;
	}

	public bool LoadGame(int index)
	{
		IsDataLoaded = false;
		if (!fileDataHandler.LoadGame(index, out var data))
		{
			return false;
		}
		if (data == null)
		{
			Debug.LogWarning("没有数据，新建存档");
			NewGame(index);
			return false;
		}
		IDataPersistence[] array = dataPersistences;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].BeforeLoadData(ref gameData);
		}
		gameData = data;
		array = dataPersistences;
		foreach (IDataPersistence dataPersistence in array)
		{
			try
			{
				dataPersistence.AfterLoadData(ref gameData);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				Debug.LogError("加载数据块\"" + dataPersistence.GetType().Name + "\"时遇到异常" + ex.Message);
			}
		}
		IsDataLoaded = true;
		return true;
	}

	public bool SaveGame(int index)
	{
		IDataPersistence[] array = dataPersistences;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].BeforeSaveData(ref gameData);
		}
		if (!fileDataHandler.SaveGame(gameData, index))
		{
			return false;
		}
		array = dataPersistences;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].AfterSaveData(ref gameData);
		}
		return true;
	}

	public void UnloadGame()
	{
		IsDataLoaded = false;
		gameData = null;
	}

	public bool DuplicateGame(int index, out int targetIndex)
	{
		return fileDataHandler.DuplicateGame(index, out targetIndex);
	}

	public void DeleteGame(int index)
	{
		fileDataHandler.DeleteGame(index);
	}

	public BaseArchiveData GetArchiveInfo(int index)
	{
		return fileDataHandler.GetArchiveInfo(index);
	}

	public BaseArchiveData[] GetAllArchiveInfo()
	{
		return fileDataHandler.GetAllArchiveInfo();
	}

	public bool HasDataStartWith(string prefix)
	{
		return fileDataHandler.HasDataStartWith(prefix);
	}

	public string GetCurrentArchiveDataAsString()
	{
		return fileDataHandler.GetArchiveDataAsString(DolocAPI.archiveHandle);
	}

	public bool LoadUserSettings()
	{
		if (fileDataHandler.LoadUserSettings(out var settings))
		{
			userSettings = settings;
		}
		else
		{
			userSettings = new UserSettings();
			SaveUserSettings(userSettings);
		}
		return true;
	}

	public bool SaveUserSettings(UserSettings userSettings)
	{
		return fileDataHandler.SaveUserSettings(userSettings);
	}

	public void ResetUserSettings()
	{
		userSettings.Reset();
	}

	public void RevertUserSettings(UserSettings settings)
	{
		userSettings.Revert(settings);
	}

	public void RevertUserSettingsToDefaultByGroup(string groupId)
	{
		userSettings.RevertToDefaultByGroup(groupId);
	}

	public bool LoadBindingOverrides(out string rebinds)
	{
		return fileDataHandler.LoadUserBindingOverrides(out rebinds);
	}

	public bool SaveBindingOverrides(string rebinds)
	{
		return fileDataHandler.SaveUserBindingOverrides(rebinds);
	}

	public bool IsFirstPlayGame(bool shouldMark)
	{
		return fileDataHandler.IsFirstPlayGame(shouldMark);
	}

	public bool LoadModManager()
	{
		if (fileDataHandler.LoadModManager(out var settings))
		{
			modManager = settings;
		}
		else
		{
			modManager = new ModManager();
			SaveModManager(modManager);
		}
		return true;
	}

	public bool SaveModManager(ModManager modManager)
	{
		return fileDataHandler.SaveModManager(modManager);
	}
}
