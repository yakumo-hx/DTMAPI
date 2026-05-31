using DolocTown.Config;

namespace DolocTown.GameData;

public interface IFileDataHandler
{
	bool LoadGame(int index, out ArchiveDataHandle data);

	bool SaveGame(ArchiveDataHandle data, int index);

	bool DuplicateGame(int index, out int targetIndex);

	bool DeleteGame(int index);

	BaseArchiveData GetArchiveInfo(int index);

	BaseArchiveData[] GetAllArchiveInfo();

	bool LoadUserSettings(out UserSettings settings);

	bool SaveUserSettings(UserSettings settings);

	bool HasDataStartWith(string prefix);

	bool LoadUserBindingOverrides(out string rebinds);

	bool IsFirstPlayGame(bool shouldMark);

	bool SaveUserBindingOverrides(string rebinds);

	string GetArchiveDataAsString(ArchiveDataHandle data);

	bool LoadModManager(out ModManager settings);

	bool SaveModManager(ModManager settings);
}
